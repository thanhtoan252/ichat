# Frontend architecture

Feature-based: find anything by the feature name first, technical role second.

```
src/app/
  core/                     app-wide singletons — HTTP config, error formatting, SSE
                            transport, RetrievalApi (shared by 3 features — see below),
                            auth (session store, JWT interceptor, route guards),
                            icon registry, theme service
  shared/
    ui/                     dumb, reusable presentational components/directives
                            (includes the vendored spartan/ui component library)
    util/                   pure functions, type guards — no Angular DI
  features/
    auth/                   the sign-in screen
    chat/
    documents/
    retrieval/
    settings/
  layout/                   the app shell (sidebar, nav) — not a feature: it isn't
                            independently routed or deletable, and it isn't a
                            singleton service either
  app.routes.ts
  app.config.ts
```

## Authentication

`core/auth/` owns the session; `features/auth/` owns only the one screen that asks for
it. There is no sign-up and no account-management screen: accounts come from the API's
seeder alone (`admin`/`admin` and `user`/`user`), which is what makes this a showcase
rather than a multi-tenant product. That split is what the boundary rules require: `app.routes.ts` may import `core`
but never a feature's internals, and the guards are referenced from the route table.

- `auth.store.ts` is the single owner of the session. The access token lives in a plain
  private field — never a signal, never `localStorage` — so it cannot be rendered and
  does not survive a tab close. What survives is the httpOnly refresh cookie, which the
  store trades for a new access token on startup (`provideAppInitializer`) and again
  whenever the current one is within 30s of expiring. Concurrent callers share one
  in-flight refresh: the API rotates the refresh token on every use, so three parallel
  refreshes would leave two of them holding a revoked token.
- `auth.interceptor.ts` attaches the bearer token to everything except `/auth/*`, and
  gives a 401 exactly one forced refresh before it gives up.
- `auth.guard.ts` exports `authGuard` and `adminGuard`. Both wait for the bootstrap
  refresh before deciding. An anonymous reader goes to `/login?returnUrl=…` and lands
  back on the page they were aiming at once they sign in — `safeReturnUrl()` in
  `login-page.ts` accepts only an in-app path, so a crafted link cannot turn the login
  screen into an open redirector. `adminGuard` sends a signed-in non-administrator to
  `/chat` rather than to `/login`, because asking them to sign in again would read as
  though their password had stopped working.
- `app.routes.ts` — **the order of the two empty-path routes is load-bearing.** The shell
  comes first and the auth screens second; the router takes the first route whose
  children can absorb the rest of the URL, so with auth first `/` matched it, found no
  empty-path child, and rendered a blank page that never reached a guard.
  `app.routes.spec.ts` pins this down against the real route table.

**The chat stream is the exception.** `core/api/sse.ts` talks to `fetch` directly (the
endpoint is a POST, which `EventSource` cannot do), so no `HttpClient` interceptor ever
sees it. `conversations.api.ts#streamAnswer()` therefore attaches the `Authorization`
header by hand. Any future transport that bypasses `HttpClient` has to do the same.

Roles are `User` and `Admin`. Admin-only surfaces — the Retrieval lab, the provider
catalog on Settings, and every write on the knowledge base — are hidden in the UI *and*
rejected by the API. The hiding is a courtesy; the API is the
control.

Conversations are the one thing the role does **not** unlock: they belong to their owner
and to no one else, an administrator included. Nothing in the UI offers to browse another
account's threads, because the API would answer `404` if it tried.

## Inside a feature

```
features/<name>/
  data/           API service, DTOs (wire shape), mappers (DTO → view model)
  state/          signal store for this feature — exposes readonly signals
  components/     presentational — inputs/outputs only, no injected services
  pages/          routed smart components
  <name>.routes.ts
```

Every feature is lazy-loaded: `app.routes.ts` points `loadChildren` straight at
`<name>.routes.ts`. There is no per-feature barrel except `chat`'s (see below) — a
feature's routes file is its only required public surface.

`documents` is a representative example:

```
features/documents/
  data/
    documents.api.ts        HTTP calls, returns *Dto types — components never call
                             HttpClient directly, only this file does
    documents.dto.ts        wire shape: DocumentSummaryDto, DocumentChunkDto...
    documents.mapper.ts      toDocumentSummary(dto), toReindexResult(dto)...
    document.model.ts        view models: DocumentSummary, StatusFilter, UploadProgress
  state/
    documents.store.ts       calls the API, maps through the mapper, exposes signals
  components/                 documents-table, documents-toolbar, status-filter...
  pages/
    documents-page.ts/html
  documents.routes.ts
```

`chat`, `retrieval`, and `settings` follow the same shape.

## Import boundaries

- A feature may import from `core/*` and `shared/*` freely.
- A feature may **not** import another feature's internals.
- `layout/` is not a feature but the rule still applies to it, with one necessary
  exception: `layout/app-shell.ts` needs `ChatStore`, `groupConversations`, and
  `ConversationList` to render the sidebar's conversation history (visible on every
  route, not just `/chat`). `chat` is the only feature with an `index.ts` — kept
  solely for this, re-exporting just those three. Every other feature has none;
  `app.routes.ts` loads all four via their `<name>.routes.ts` directly.
- No barrel re-exports everything globally — `chat/index.ts` is scoped to what
  `layout/` needs, not a dumping ground for the whole feature's surface.

`eslint.config.js` declares `eslint-plugin-boundaries` element types matching this
layout (`feature-pages`, `feature-components`, `feature-data`, `feature-state`,
`feature-index`, `shared-ui`, `shared-util`, `core`, `layout`, `app-root`) and a
`boundaries/dependencies` policy set encoding the two rules above. **It does not yet
enforce them** — see "Known gap" below.

## DTOs and view models

Where a resource is actually fetched, stored, and rendered (`Conversation`,
`ChatMessage`, `DocumentSummary`, `ProviderCatalog`, `SearchResult`...), `data/` has
three pieces:

1. **`*.dto.ts`** — the exact wire shape ASP.NET Core serialises (camelCase fields,
   `PascalCase` enum values). Only the API service and the mapper ever see these.
2. **`*.mapper.ts`** — pure `toX(dto) => X` functions. This is the one place a wire
   shape becomes a view-model shape.
3. **The store** calls the API, maps immediately, and exposes only view-model-typed
   signals. Nothing past the store — no component, no template — ever sees a `*Dto`
   type.

The one deliberate exception is `chat`'s SSE stream frames (`StatusPayload`,
`DeltaPayload`, `DonePayload`, ...): they're transient, consumed inline in
`ChatStore`'s `for await` loop, and never become a persisted resource on their own. A
DTO/mapper pair for a type that's read exactly once and discarded is ceremony, not
safety, so they live in `data/chat-stream.model.ts` as a single type used directly.

`RetrievalApi` (`core/api/retrieval.api.ts`) is the one API service shared by three
features — `/search` (retrieval), `/providers` (settings), `/admin/reindex`
(documents). Its DTOs live in `core/api/retrieval.dto.ts` next to it; each consuming
feature owns its own mapper and view model (e.g. `ReindexResult` is modelled in
`features/documents/data/`, even though the endpoint it maps from sits on the shared
client in `core/`).

## Modernization notes

- Every component is standalone, `OnPush`, uses `input()`/`output()`/`viewChild()`
  and `inject()`. No `NgModule` exists anywhere in `src/app`.
- Templates use `@if`/`@for`/`@switch` exclusively — no `CommonModule` structural
  directives.
- The app runs zoneless (`provideZonelessChangeDetection()` in `app.config.ts`).
- `retrieval`'s form component uses a small typed `FormGroup` (Reactive Forms) — the
  only form in the app; everything else is signal-driven `input()`/`output()`.
- State is signals throughout (`signal`/`computed`/`effect`); no `BehaviorSubject`
  anywhere in feature code.

## Known gap: boundaries/dependencies is not actually enforced yet

`eslint.config.js`'s `boundaries/dependencies` rule is fully written and its element
classification is correct — confirmed with `ESLINT_PLUGIN_BOUNDARIES_DEBUG=1`, every
file resolves to the right `feature-*`/`shared-*`/`core`/`layout` type. What doesn't
work is resolving the **target** of a local import: `eslint-plugin-boundaries` v7
resolves dependencies through `eslint-module-utils`, a resolution path from before
ESLint's flat config existed. In this project (ESLint 10 flat config +
typescript-eslint), that path returns "unresolved" for every relative import and every
`@app/*` alias — and an unresolved local target is silently *not checked*
(`checkUnknownLocals` defaults to `false`), rather than flagged. So today, nothing
stops a feature from importing another feature's internals; the guardrail described
above is not live.

Tried, in order, none of which closed the gap:
- No resolver configured (the plugin's own bundled default, `eslint-import-resolver-node`)
- `eslint-import-resolver-typescript@4` via `settings['import/resolver']`
- `eslint-import-resolver-typescript@3` (the version built for the legacy resolver
  interface, not v4's newer `import-x` target)
- The bundled `eslint-import-resolver-node` with `.ts`/`.js` extensions configured

The rule is left in place rather than deleted — it's correct and will start working
the moment this is fixed (likely either a boundaries release that moves off
`eslint-module-utils`, or a resolver shim that backfills what flat config's rule
context doesn't provide, e.g. `context.parserPath`). Until then, review PRs for
cross-feature imports by eye; `git grep "from '.*features/"` in a changed file outside
its own `features/<name>/` is the quick manual check.

## What's deliberately not "clean"

- `shared/ui`'s ~40 component folders (`accordion/`, `button/`, `dialog/`, ...) are a
  vendored spartan/ui (shadcn-for-Angular) library, scaffolded and updated via
  `npm run ui:add` (`components.json` points spartan's CLI at this folder). They're
  not restructured to match the rest of the app — that's vendor code, not feature
  code. `eslint.config.js` excludes them from linting entirely.
- `documents/state/documents.store.ts`'s `uploadFile()` still calls `.subscribe()`
  directly instead of going through `toSignal`/`takeUntilDestroyed`: it needs
  per-event upload-progress percentages, which a `Promise`-based or `toSignal`-based
  API doesn't expose. It's scoped to one HTTP call's lifetime (not a long-lived
  subscription), and the store is provided at the `documents-page` route level, so
  it's bounded by that route's lifetime in practice.

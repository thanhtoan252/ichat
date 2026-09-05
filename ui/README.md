# iChat UI

Angular front end for the IChat RAG API. Ask questions against the indexed knowledge base,
read answers with citations you can open, and inspect the retrieval pipeline that produced
them.

## Stack

| Concern | Choice |
| --- | --- |
| Framework | Angular 22, standalone, **zoneless**, signal inputs/outputs, `OnPush` everywhere |
| Components | [spartan/ui](https://spartan.ng) — `@spartan-ng/brain` primitives plus Helm components vendored into `src/app/shared/ui` |
| Styling | Tailwind CSS v4 (`@tailwindcss/postcss`), spartan theme tokens, dark mode via `.dark` on `<html>` |
| Icons | `@ng-icons/lucide`, registered once in `src/app/core/icons.ts` |
| State | Signals only — one store per feature, no external state library |
| Data | `HttpClient` with `withFetch()`; the chat stream uses `fetch` + a hand-rolled SSE reader |
| Markdown | `marked`, sanitised against an allowlist before it reaches the DOM |
| Tests | Vitest via `@angular/build:unit-test` |

Helm components are **source in this repo**, not a dependency. Edit them freely; add more with
`npm run ui:add -- --name=<primitive>`.

## Running it

The API is expected on `http://localhost:8080` (its `http` launch profile). `ng serve` proxies
`/api` and `/health` there — see `proxy.conf.json` — so there is no CORS setup on either side.

```bash
# terminal 1 — API
cd ../api && dotnet run --project src/IChat.Api

# terminal 2 — UI
npm install
npm start          # http://localhost:4200
```

Point at a different API host by editing `proxy.conf.json` (dev) or
`src/environments/environment.production.ts` (`apiBaseUrl`, empty means same origin).

```bash
npm run build          # production bundle into dist/ui
npm test               # unit tests
npm run format         # prettier
```

### With Docker

The compose file in the repository root runs Postgres, the API and this UI together:

```bash
cd ..
docker compose --env-file .env.openai.example up   # UI on :4200, API on :8080
```

The image is a two-stage build — `npm ci && npm run build`, then nginx serving
`dist/ui/browser`. In the container nginx does the job `proxy.conf.json` does in dev:
`/api` and `/health` are proxied to the `api` service, so the browser sees a single origin
and the API still needs no CORS configuration.

`proxy_buffering off` in [`nginx-proxy.inc`](nginx-proxy.inc) is load-bearing. With buffering
on, nginx holds the SSE frames until the response finishes, and the answer lands in one lump
instead of streaming.

## Component structure

Every component is either a **container** or **presentational**, and the directory says which.

| | Container | Presentational |
| --- | --- | --- |
| Lives in | `<feature>/<feature>-page.ts`, `layout/app-shell.ts` | any `components/` folder |
| May `inject()` | stores, `Router`, `DestroyRef` | nothing but `ElementRef` |
| Knows about | HTTP, routes, toasts, app state | its inputs, and nothing else |
| Talks by | reading a store, calling its methods | `input()` in, `output()` out |
| Template holds | composition and layout only | all the real markup |

Templates live in a sibling `.html` file next to the component (`upload-progress.ts` →
`upload-progress.html`) rather than inline, so the markup gets real HTML tooling — Prettier's
Angular parser, editor folding, and diffs that are not wrapped in a template literal. The one
exception is `AnswerContent`, whose template is empty by design: it swaps in a sanitised
DocumentFragment imperatively. The vendored spartan components under `shared/ui` keep their
inline templates so they stay diffable against what `ng g @spartan-ng/cli:ui` regenerates.

Containers never call an API client directly — they go through a store, so where state lives
is never a question. Stores are `@Injectable()` and provided by their container
(`providers: [DocumentsStore]`), which scopes their lifetime to the route; `ChatStore` is the
exception at `providedIn: 'root'`, because the sidebar and the chat route share one
conversation list.

Two conventions that keep the presentational side honest:

- **Route links are data.** `ConversationList` renders real anchors, but each item arrives
  carrying its own `link: ['/chat', id]` from the container. Middle-click and "open in new tab"
  keep working, and the component still knows no routes.
- **Effects are requests.** `MessageTurn` does not touch the clipboard; it emits
  `copyRequest` and the container performs it, along with the toast.

Verify the boundary at any time:

```bash
grep -rE "inject(<[^>]*>)?\(" src/app --include="*.ts" \
  | grep "/components/" | grep -v ElementRef      # must print nothing
```

## Layout

```
src/app/
  core/
    api/          typed clients + the models mirroring IChat.Api v1 contracts
      sse.ts      SSE reader over fetch (the chat endpoint is a POST, so EventSource is out)
    theme/        the .dark class on <html>, plus the shared theme options
    icons.ts
  features/
    chat/
      chat-page.ts          container
      chat.store.ts         streaming, history, conversations (root-provided)
      conversation-groups.ts   pure recency bucketing for the sidebar
      components/           header, thread, turns, composer, sources, conversation list
    documents/
      documents-page.ts     container
      documents.store.ts    listing, upload progress, ingestion polling
      components/           toolbar, stats, filter, table, upload bar, empty state
    retrieval/
      retrieval-page.ts     container
      retrieval.store.ts    criteria, the search call, stage ordering
      components/           toolbar, form, stage list, empty state
    settings/
      settings-page.ts      container
      settings.store.ts     provider catalog
      components/           provider card, theme picker, toolbar
  layout/
    app-shell.ts            container: sidebar state, nav, theme, routed outlet
    components/             workspace nav, theme toggle
  shared/
    markdown/     markdown rendering, sanitising, and [n] citation chips
    scroll/       stick-to-bottom directive for the transcript
    ui/           spartan Helm components (generated, yours to edit)
```

## Things worth knowing

**The chat stream is a POST.** `EventSource` only issues GET, so `core/api/sse.ts` parses the
wire format itself. It handles frames split across network chunks, CRLF endings, multi-line
`data:`, and comment keep-alives.

**Business errors arrive as `error` frames under HTTP 200.** The API decided that a failed turn
is part of the stream, not a status code, so `ChatStore` treats the stream — not the response —
as the source of truth for whether an answer succeeded.

**Citation markers are re-derived, not trusted.** `shared/markdown/markdown.ts` mirrors the
server's `CitationExtractor`: `[n]` becomes a clickable chip only when it is inside the verified
range and not inside code. Markers the server would have rejected stay as literal text.

**Streamed tokens are batched to an animation frame.** The answer re-parses as markdown on every
change, and a fast provider emits tokens far more often than the screen refreshes.

**Model output is never trusted as HTML.** `marked` passes raw HTML straight through, so the
rendered fragment is walked against a tag/attribute allowlist before it is inserted.

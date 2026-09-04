# IChat RAG Backend

Backend API for a RAG-based AI chatbox, .NET 10 + PostgreSQL/pgvector, with LLM providers swappable by configuration alone.

## Quick start

`docker-compose.yml` and the `.env.*.example` files live in the **repository root**, one level
up, because the stack spans both `api/` and `ui/`:

```bash
cd ..
docker compose --env-file .env.openai.example up      # OpenAI
docker compose --env-file .env.anthropic.example up   # Claude (chat) + OpenAI (embedding)
docker compose --env-file .env.gemini.example up      # Gemini (chat) + OpenAI (embedding)
```

Brings up Postgres, the API and the Angular UI together:

| | |
| --- | --- |
| UI | <http://localhost:4200> |
| API | <http://localhost:8080> |
| API reference (Development only) | <http://localhost:8080/docs> |

Check it: `curl localhost:8080/health/ready` and `curl localhost:8080/api/v1/providers`.

The UI container is nginx serving the production bundle, and it proxies `/api` and `/health`
to the `api` service, so the browser only ever talks to one origin and the API needs no CORS
configuration. Run just the backend with `docker compose up postgres api`.

## Architecture

```
Api  ──►  Infrastructure  ──►  Core
                                 ▲
                    only Microsoft.Extensions.AI.Abstractions
                    + Microsoft.EntityFrameworkCore
```

```
POST /documents ─► Channel<Guid> ─► DocumentIngestionWorker
                                       │
              parse (docx│pdf│md│txt) ─┤─► HeadingAwareChunker ─► batched embedding ─► pgvector
                                       │
Question ─► rewrite ─► ┌─ vector ─┐
                       ├─ full-text ┼─► RRF ─► MMR ─► rerank ─► neighbors ─► context ─► LLM ─► SSE
                       └─ trigram ─┘
```

### Which files a provider change touches

Exactly two: `appsettings.json` (or environment variables) and — only when adding a brand new
vendor — one new file under `Infrastructure/Ai/Providers/` implementing
`IChatProviderClientFactory`. Core and Api do not contain a single line that knows about
OpenAI/Anthropic/Google. Middleware (cache, telemetry, logging) is attached once in
`AiServiceCollectionExtensions`, so every provider behaves identically.

### API keys

Keys bind straight into `AiOptions` through `IConfiguration` — there is no bespoke resolver.
Set `Ai:Chat:ApiKey` and `Ai:Embedding:ApiKey` by whichever configuration source suits the
environment:

```bash
# local development
dotnet user-secrets set "Ai:Chat:ApiKey" "sk-..." --project src/IChat.Api
dotnet user-secrets set "Ai:Embedding:ApiKey" "sk-..." --project src/IChat.Api

# containers and CI (double underscore = section separator)
Ai__Chat__ApiKey=sk-...
Ai__Embedding__ApiKey=sk-...
```

Chat and embedding take **separate** keys, because they are often different vendors. A missing
key fails fast at startup via `AiOptionsValidator`, not at the user's first question. The
`/api/v1/providers` endpoint reports only whether a key is present — it never returns one,
not even masked.

The chat provider and the embedding provider are **decoupled**, because Anthropic has no
embedding API:

```
Ai:Chat:Provider      = Anthropic   (claude-sonnet-5)
Ai:Embedding:Provider = OpenAI      (text-embedding-3-small)
```

### Choosing the Anthropic package

Use the NuGet package named **`Anthropic` v12.43.0** — that is the official SDK (from v10 up).
Do not use `tryAGI.Anthropic` (which is `Anthropic` ≤ 3.x under its old name) and do not use
tghamm's `Anthropic.SDK`; both are community packages.

One practical difference from the original documentation: this SDK does **not** implement
`IChatClient` itself. It provides the extension
`AnthropicClientExtensions.AsIChatClient(model, defaultMaxOutputTokens)`. That second parameter
is where the `MaxOutputTokens` value Anthropic requires goes.

## Traps that have already been handled

### `plainto_tsquery` kills the full-text branch

`plainto_tsquery` and `websearch_to_tsquery` join every lexeme with `&`. A 15-word question then
demands a chunk containing all 15 lexemes, so the full-text branch comes back empty almost every
time — the system still runs, still answers, and the operator believes hybrid search is working
while only the vector branch actually is.

Measured on real PostgreSQL against a chunk that clearly matches:

| tsquery construction | result |
|---|---|
| `plainto_tsquery` (AND over 10 lexemes) | **0 rows** |
| `TsQueryBuilder` (OR + stopword filtering) | 1 row, `ts_rank_cd` = 1.0 |

`TsQueryBuilder` deliberately keeps out of its stopword list any word that, once unaccented,
collides with a common content word: `moi` ← "môi trường", `tai` ← "tài liệu", `nen` ← "nền tảng",
`ai` ← "AI", `dau` ← "đầu vào". Keeping a weak lexeme beats dropping a content word.

### `unaccent()` is not IMMUTABLE

The `content_tsv` generated column **cannot be created** as originally documented: a generated
column requires an IMMUTABLE expression, and `unaccent()` is only STABLE. The migration creates
a wrapper up front:

```sql
CREATE OR REPLACE FUNCTION immutable_unaccent(text)
RETURNS text LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT
AS $$ SELECT public.unaccent('public.unaccent', $1) $$;
```

### `InvariantGlobalization` breaks Vietnamese diacritic stripping

Enabling `InvariantGlobalization` turns `String.Normalize` into a **silent** no-op, so
`TsQueryBuilder` cannot strip diacritics and the full-text branch matches nothing in the
unaccented column. The flag is set to `false` on purpose.

### RRF is fusion, not reranking

RRF only blends several ranked lists by rank position. Reranking needs a cross-encoder or an LLM
to score again — that is `IReranker` with mode `Llm`.

### Citations must be verified

The `[n]` markers a model produces are **not trusted**. Once streaming finishes,
`CitationExtractor` parses the markers, drops out-of-range ones (with a warning log), drops
markers inside code blocks, and writes to `message_citations` only the chunks that were genuinely
referenced.

`sources` (SSE) = everything that entered the context. `citations` (the `done` event) = what the
answer actually cited.

### DOCX

| trap | handling |
|---|---|
| Tracked changes | Drop `w:del`, keep `w:ins`. Citing a struck-out clause is the worst kind of silent bug. |
| Custom style names | Fall back to `w:outlineLvl` when the style does not match `^Heading[1-9]$` |
| Text boxes | Traverse `w:txbxContent` separately, without duplicating into the main paragraph flow |
| Tables | One atomic `Block(Table)`, merged cells padded to the right column count |
| Header/footer, TOC | Dropped entirely |
| Fragmented runs | Join every `w:t` within a paragraph |
| `.doc` files | 415 with instructions to re-save as `.docx`, never a 500 |
| Page numbers | `metadata.page` exists for PDF only; docx locates content by `startBlockIndex` |

## Changing the embedding model

This is the nastiest bug to get wrong: old and new vectors live in different spaces, search still
returns results but they are completely wrong, and **nothing reports an error**.

1. `EmbeddingModelGuard` (an IHostedService) compares the configuration against the data in the
   database at startup. A **model** mismatch logs a warning. A **dimension** mismatch fails fast
   and refuses to start the app.
2. If the new dimension count is not 1536, add an EF migration that changes the column type:
   ```bash
   dotnet ef migrations add ChangeEmbeddingDimensions -p src/IChat.Infrastructure
   # edit the migration: ALTER TABLE document_chunks ALTER COLUMN embedding TYPE vector(N)
   # and update EmbeddingDimensions.Default + ColumnType
   ```
3. `POST /api/v1/admin/reindex` resets every document to `Pending` and pushes it back onto the queue.

The HNSW index is created in the first migration (against an empty database). For a large initial
backfill, prefer `DROP INDEX ix_chunks_embedding_hnsw`, insert, then recreate it.

## Tests

```bash
dotnet test                                   # unit + integration
dotnet test --filter "Category!=RequiresApiKey"   # skip contract tests that need a real key
```

Integration tests use Testcontainers with the `pgvector/pgvector:pg17` image and a deterministic
fake AI client, so they need no API key and make no network calls.

There is also a Postman/Newman suite that runs against a live API — 35 requests covering
ingestion, search, SSE and the error cases:

```bash
npx newman run postman/IChat.postman_collection.json \
    -e postman/IChat.local.postman_environment.json --working-dir postman
```

Details in [postman/README.md](postman/README.md).

## Measuring retrieval quality

```bash
(cd .. && docker compose up -d postgres api)
dotnet run --project tools/IChat.RagEval -- seed
dotnet run --project tools/IChat.RagEval -- eval
dotnet run --project tools/IChat.RagEval -- compare   # rewriting on/off
```

Golden set: 29 items in `tools/IChat.RagEval/golden-set.json`, split into three groups —
`fact` (18), `multi-hop` (6), `followup` (5 — follow-up questions containing pronouns).

### Recorded measurements

Measured by `GoldenSetEvalTests`, running through the real pipeline over a fixed corpus:

| metric | value |
|---|---|
| full-text branch empty rate | **0 / 29 = 0.0 %** |
| recall@8 using the full-text branch only | 23 / 29 = 79.3 % |
| ├ group `fact` | 16 / 18 |
| ├ group `multi-hop` | 4 / 6 |
| └ group `followup` (rewriting OFF) | 3 / 5 |

**How to read these two numbers.** They are measured with a deterministic fake embedding, so the
recall here is *not* a measure of the semantic quality of the vectors. They are still genuinely
useful:

- The full-text empty rate **does not depend on embeddings at all** — the full-text branch never
  touches a vector. The 0 % figure is direct evidence that the OR-style tsquery works. With
  `plainto_tsquery` this number would be close to 100 %.
- The `followup` group runs with rewriting off and reaches 3/5. That is the baseline `compare`
  uses to prove query rewriting has an effect.

Full recall@8 and MRR with real embeddings need a real API key; run `eval` above to get them.

## Conventions

No MediatR, no CQRS, no AutoMapper, no generic repository. Each business area is one service
(`DocumentService`, `ConversationService`, `ChatService`, `SearchService`, `AdminService`) behind
an interface in `IChat.Core/Abstractions`; endpoints inject the interface and call the method
directly. DTOs live in `IChat.Core/Contracts`, validators in `IChat.Core/Validation`. Business
errors use `Result<T>` rather than throwing. Errors are returned per RFC 7807 with a `traceId`;
the SDK's original message is never exposed to the client.

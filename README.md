# iChat

A retrieval-augmented chat application: ask questions in natural language and get
answers composed **only** from your own indexed documents, with every claim carrying
a citation you can open and verify.

.NET 10 API + PostgreSQL/pgvector, Angular UI, LLM providers swappable by configuration alone.

![Chat with grounded answers and citations](docs/screenshots/chat.png)

## How it works

### 1. Ask a question, get a cited answer

Answers are composed from retrieved context, never from the model's own memory. Inline
markers link each statement back to the chunk it came from, and the **Cited sources**
panel shows the source heading and relevance score for each one. When the knowledge base
has nothing relevant, iChat says so instead of inventing an answer.

Behind a single question:

```
Question ─► rewrite ─► ┌─ vector ───┐
                       ├─ full-text ┼─► RRF ─► MMR ─► neighbors ─► rerank ─► context ─► LLM ─► SSE
                       └─ trigram ──┘
```

### 2. Build the knowledge base

![Knowledge base with indexed documents](docs/screenshots/knowledge-base.png)

Upload `pdf`, `docx`, `md` or `txt`. A background worker parses each file, splits it with a
heading-aware chunker, embeds the chunks in batches and writes them to pgvector. The table
tracks per-document status (indexed / processing / pending / failed) and chunk counts, so a
document that failed to parse is visible rather than silently missing from answers.

### 3. Inspect the retrieval pipeline

![Retrieval lab tracing a question through each stage](docs/screenshots/retrieval-lab.png)

The **Retrieval lab** runs a question through the pipeline and reports what every stage
produced and how long it took — so you can see exactly where a chunk stopped surviving.
Switch between hybrid, vector, full-text and trigram retrieval, toggle query rewriting, MMR,
neighbor expansion and reranking, and change top-K, then compare the resulting candidate
sets and scores.

In the screenshot above, the vector branch returned 11 candidates in 9 ms and full-text
returned 2 in 24 ms, while trigram returned none — the kind of imbalance that is invisible
from the chat window alone.

### 4. Swap providers without touching code

![Provider and appearance settings](docs/screenshots/settings.png)

Chat and embedding providers are configured **separately**, because some vendors (Anthropic)
have no embedding API — so a chat vendor can be paired with an embedding vendor that does.
A third "utility chat" slot runs the cheaper side tasks like query rewriting. Settings reports
which providers are available and which models are selectable; keys live on the server and are
never returned to the browser, not even masked.

## Quick start

```bash
docker compose --env-file .env.openai.example up      # OpenAI
docker compose --env-file .env.anthropic.example up   # Claude (chat) + OpenAI (embedding)
docker compose --env-file .env.gemini.example up      # Gemini (chat) + OpenAI (embedding)
```

Copy the relevant `.env.*.example` to a local file and fill in your API keys first — real
`.env` files are gitignored.

| | |
| --- | --- |
| UI | <http://localhost:4200> |
| API | <http://localhost:8080> |
| API reference (Development only) | <http://localhost:8080/docs> |

Check it with `curl localhost:8080/health/ready` and `curl localhost:8080/api/v1/providers`.
Run just the backend with `docker compose up postgres api`.

## Repository layout

| Path | What's in it |
| --- | --- |
| [`api/`](api/README.md) | .NET solution — `IChat.Core` (domain), `IChat.Infrastructure` (persistence + providers), `IChat.Api` (HTTP host), plus test suites |
| [`ui/`](ui/README.md) | Angular workspace — chat, knowledge base, retrieval lab and settings |
| `docker-compose.yml` | Postgres (pgvector) + API + UI |
| `IChat.md` | Design document |

Dependencies point inward only:

```
Api  ──►  Infrastructure  ──►  Core
```

Core knows nothing about OpenAI, Anthropic or Google — adding a vendor touches configuration
and one factory under `Infrastructure/Ai/Providers/`.

See [`api/README.md`](api/README.md) for the backend in depth, including the retrieval traps
already handled (full-text `tsquery` construction, `unaccent()` immutability) and how API keys
bind through configuration.

# Postman test suite

End-to-end tests for the IChat API: ingestion → hybrid search → RAG question answering over SSE →
error cases → reindex → cleanup. **37 requests, 202 assertions.**

| file | role |
|---|---|
| `IChat.postman_collection.json` | the main collection |
| `IChat.local.postman_environment.json` | `baseUrl=http://localhost:5140` (`dotnet run`) |
| `IChat.docker.postman_environment.json` | `baseUrl=http://localhost:8080` (`docker compose`) |
| `fixtures/ichat-sample.md` | the document to upload; its content matches the queries in the suite |
| `fixtures/unsupported.png` | the file used for the 415 case |

## Running with Newman

```bash
cd api
npx newman run postman/IChat.postman_collection.json \
    -e postman/IChat.local.postman_environment.json \
    --working-dir postman
```

`--working-dir` is required: the two upload requests point at files in `fixtures/` by relative
path.

Export a JUnit report for CI:

```bash
npx newman run postman/IChat.postman_collection.json \
    -e postman/IChat.local.postman_environment.json \
    --working-dir postman \
    -r cli,junit --reporter-junit-export postman/newman-report.xml
```

## Running in the Postman app

1. Import both the collection and the environment.
2. Select the environment in the top right corner.
3. Open the upload requests and re-attach the `file` field (Postman does not keep file paths on import).
4. Run it with the **Collection Runner**, not request by request.

## The run order is mandatory

The suite runs top to bottom; later folders use the `documentId` and `conversationId` produced by
earlier ones through collection variables. Running a single request from the middle will fail.

| folder | content |
|---|---|
| 1. Health | `/health/live`, `/health/ready`. If ready fails the suite stops immediately instead of pouring out identical errors |
| 2. Providers | provider configuration, and the assertion that no API key leaks |
| 3. Documents | upload → wait for ingestion → offset/limit paging, including the clamped cap and an offset that skips → filter by status → inspect chunks |
| 4. Search | hybrid, full-text, `topK`, and an all-stopword query |
| 5. Conversations & Chat | create a conversation, ask over SSE, read the history back with citations |
| 6. Error cases | 415, 404, 400 and three error cases travelling over the SSE channel |
| 7. Admin | reindex, then wait for indexing to finish |
| 8. Cleanup | delete the document, assert that chunks cascade with it |

## Waiting for ingestion

Ingestion runs in the background, so the two "wait for …" requests poll themselves. Tune them with
collection variables:

- `ingestMaxAttempts` — maximum number of polls (default 60)
- `ingestPollMs` — interval between two polls, in ms (default 1000)

## What the suite needs to go green

- Postgres with pgvector is running and the schema is migrated.
- Both the chat provider and the embedding provider are fully configured (`/health/ready` returns `Healthy`).
- `Ai:AllowedChatModels` does not contain `model-khong-ton-tai` (the allowlist test case).
- If the embedding provider is weak or the corpus is too short, the assertion
  `The vector branch is not empty` can go red because `Rag:Retrieval:VectorMinSimilarity`
  (default 0.20) filters out every candidate.

The suite cleans up the documents it creates in folder 8, so it can be run repeatedly.
Conversations are not deleted — the API has no delete-conversation endpoint yet.

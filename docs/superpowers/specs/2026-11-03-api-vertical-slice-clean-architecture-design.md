# API Vertical Slice Architecture with Clean Architecture

## Intent

Restructure the iChat API so each HTTP use case keeps its endpoint, transport request/response, mapping, handler, and validation close together under `IChat.Api/Features`, while preserving Clean Architecture dependency boundaries. The goal is to make changes local to a use case without weakening domain rules or coupling business behavior to PostgreSQL or provider SDKs.

## Current state

- `IChat.Api` has endpoint groups by area under `Endpoints/<Area>/V1`, with DTOs, validators, and mappers in separate subfolders.
- `IChat.Core` contains shared contracts, abstractions, domain models, validation, and area services (`AuthService`, `DocumentService`, `ConversationService`, `ChatService`, `SearchService`, `AdminService`).
- `IChat.Infrastructure` implements persistence, AI, ingestion, storage, and security adapters and references Core.
- API references Infrastructure; Infrastructure references Core. Core has no reference to API or Infrastructure.
- API behavior includes owner-only conversation access, admin-only writes/search/provider catalog, RFC 7807 errors, `Result<T>`, and SSE streaming. Those contracts and behaviors must remain stable.

## Goals

- Organize application behavior around vertical use cases rather than area-wide service classes.
- Keep domain entities and domain policies independent from transport and infrastructure.
- Keep Infrastructure as adapter implementations behind application-owned ports.
- Avoid introducing MediatR, AutoMapper, generic repositories, or a second application framework.
- Preserve HTTP routes, request/response contracts, authorization, error semantics, SSE event behavior, persistence schema, and background ingestion behavior.
- Make dependencies and feature boundaries mechanically reviewable.

## Non-goals

- Redesigning endpoints or changing API versioning/contracts.
- Rewriting the RAG algorithm, persistence model, authentication model, or worker lifecycle.
- Splitting the solution into one project per feature.
- Moving ASP.NET-specific concerns into Core or Infrastructure.

## Approaches considered

1. **Full vertical-slice conversion (selected):** Move all application use cases into feature folders in one coherent migration. Keep the existing project dependency direction and domain/infrastructure boundaries. This gives one consistent destination and avoids maintaining two application patterns for long; it requires a larger, carefully sequenced change.
2. **Incremental conversion:** Convert slices only when touched. Lower immediate risk, but leaves old area services and new slices in parallel for an extended period, with no clear completion point.
3. **Endpoint-only regrouping:** Co-locate HTTP DTOs, validators, and mappings but keep area services as the application layer. Smaller change, but execution and business decisions remain spread across broad services.

## Proposed architecture

Use `IChat.Api/Features` as the vertical-slice/application entry layer, organized by feature and use case. `IChat.Core` remains the domain and application-abstraction layer, and `IChat.Infrastructure` remains the adapter layer. A representative target shape is:

```text
IChat.Api/
  Features/
    Documents/
      UploadDocument/
        Endpoint.cs
        Request.cs
        Response.cs
        Mapper.cs
        Handler.cs
        Validator.cs
      GetDocument/
      GetDocuments/
      GetDocumentChunks/
      DeleteDocument/
      ReindexDocuments/
    Chat/
      AskQuestion/
    Conversations/
      CreateConversation/
      ListConversations/
      GetConversation/
      GetMessages/
    Auth/
      Login/
      Register/
      Refresh/
      Logout/
      GetCurrentUser/
    Search/
      SearchChunks/
    KnowledgeBases/              # only if this is a distinct existing API capability
  Common/
    Middleware/
    Extensions/
    Mapping/
IChat.Core/
  Domain/                         # current Domain models and policies
  Abstractions/                   # application-owned ports
  Services/                       # focused shared capabilities, not endpoint dispatch
  Common/                          # Result<T>, errors, pagination
IChat.Infrastructure/
  Persistence/
  Storage/
  Search/
  Ai/
  Ingestion/
  Security/
```

Each slice in `IChat.Api/Features` owns its endpoint, HTTP request/response DTOs, mapper, handler, and validator. Handlers orchestrate the use case through interfaces in `IChat.Core/Abstractions`; they must not reference Infrastructure implementation types. Endpoint code owns HTTP binding, status/header/cookie handling, Problem Details translation, and SSE framing. Common middleware, extensions, and genuinely shared mapping helpers stay in `IChat.Api/Common`; feature-specific mappings stay inside their slice.

Handlers use constructor-injected Core ports and focused collaborators. Core owns ports needed by use cases; Infrastructure implements them. Existing entities, domain policies, `Result<T>`, RAG pipeline components, parser contracts, and infrastructure adapters remain in their appropriate layers. Existing Core services may remain when they represent genuinely shared domain/application capabilities; endpoint use cases move from broad area services into slice handlers. Avoid adding a generic repository abstraction: the current Core database port and focused search/storage ports remain unless a concrete use case demonstrates a need for a narrower port.

No mediator is introduced: endpoint delegates resolve/invoke concrete handlers through DI (or use a small explicit dispatcher only if the final code demonstrates meaningful duplication). Feature handler code depends on Core abstractions, never Infrastructure. Infrastructure depends on Core; API is the composition root and may reference both API framework facilities and Infrastructure for registration. Project references remain API → Infrastructure → Core, with API also referencing Core.

## Migration outline

1. Establish `IChat.Api/Features` and `IChat.Api/Common`, plus dependency rules; classify existing services, contracts, validators, and endpoint mappings by use case.
2. Convert bounded, non-streaming slices first (provider catalog, search, auth, document reads/writes), moving transport and handler code into each API slice while preserving authorization and response contracts.
3. Convert conversation and chat slices, keeping SSE transport details in the endpoint and orchestration in the slice handler/Core collaborators; preserve cancellation and interrupted-turn persistence behavior.
4. Remove superseded area services and old registrations after all callers and tests move; retain focused shared collaborators and worker entry points where reuse/lifecycle requires them.
5. Add/adjust architecture checks and tests so forbidden project and feature dependencies fail in CI; update API architecture documentation and conventions.

The exact order should be refined against existing test coverage and coupling during the implementation plan. The migration should keep each step buildable and preserve the current public API.

## Constraints and acceptance criteria

- Project references remain API → Infrastructure → Core, with API also acting as composition root; Core must not reference API, Infrastructure, or web framework packages.
- Each endpoint has a clearly identifiable use-case handler beside its transport DTOs and validator; no handler becomes a catch-all replacement for an area service.
- Infrastructure access occurs through Core-owned ports, except framework composition/configuration performed at the API boundary.
- Existing endpoint paths, authorization decisions, RFC 7807 mapping, SSE frames, database schema, and ingestion queue semantics remain unchanged.
- Existing unit/integration/contract coverage continues to validate the behavior; add architecture checks for project and feature boundaries.
- Documentation describes the resulting layout and where new endpoint behavior belongs.

## Decisions left to the implementation plan

- Choose file granularity per slice (single-file handler for small operations; split validator/handler/model when the code warrants it).
- Determine whether current `Core/Contracts` wire-like types are removed, retained as domain/application types, or moved to their owning slices after classifying each usage.
- Pick the most maintainable architecture test mechanism already compatible with the .NET solution and CI.
- Finalize conversion order from actual coupling and test coverage.

namespace IChat.Api.Endpoints.Search.V1.DTOs;

/// <summary>An API-layer copy, so the v1 shape does not drift with the pipeline enum.</summary>
public enum SearchModeDto
{
    Hybrid,
    Vector,
    FullText,
    Trigram
}

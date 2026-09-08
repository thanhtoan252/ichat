namespace IChat.Api.Endpoints.Search.V1.DTOs;

/// <summary>Bản sao ở tầng API để shape của v1 không trôi theo enum của pipeline.</summary>
public enum SearchModeDto
{
    Hybrid,
    Vector,
    FullText,
    Trigram
}

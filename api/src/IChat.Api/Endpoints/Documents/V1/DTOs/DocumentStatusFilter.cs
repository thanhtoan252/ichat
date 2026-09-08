namespace IChat.Api.Endpoints.Documents.V1.DTOs;

/// <summary>Bản sao ở tầng API để shape của v1 không trôi theo enum domain.</summary>
public enum DocumentStatusFilter
{
    Pending = 0,
    Processing = 1,
    Indexed = 2,
    Failed = 3
}

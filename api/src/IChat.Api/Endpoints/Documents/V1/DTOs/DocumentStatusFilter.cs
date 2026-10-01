namespace IChat.Api.Endpoints.Documents.V1.DTOs;

/// <summary>An API-layer copy, so the v1 shape does not drift with the domain enum.</summary>
public enum DocumentStatusFilter
{
    Pending = 0,
    Processing = 1,
    Indexed = 2,
    Failed = 3
}

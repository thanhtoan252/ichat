namespace IChat.Core.Abstractions;

/// <summary>
/// Kiến thức về một định dạng nằm ngay tại parser của định dạng đó, nên thêm format mới
/// chỉ phải sửa một chỗ: viết parser và khai báo Format của nó.
/// </summary>
public sealed record DocumentFormat
{
    public required string ContentType { get; init; }

    public required IReadOnlyList<string> Extensions { get; init; }

    public IReadOnlyList<byte[]> MagicBytes { get; init; } = [];

    /// <summary>Đuôi file khớp nhưng magic bytes không khớp => từ chối, đừng đoán bừa (ca .docx giả).</summary>
    public bool RejectOnMagicMismatch { get; init; }
}

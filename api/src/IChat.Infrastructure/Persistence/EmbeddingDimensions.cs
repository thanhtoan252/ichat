namespace IChat.Infrastructure.Persistence;

/// <summary>
/// Số chiều vector bị cố định ở cấp schema (cột vector(N)). Đổi giá trị này là
/// breaking change ở tầng dữ liệu và bắt buộc phải kèm một EF migration đổi kiểu cột
/// cùng một lần reindex toàn bộ. Xem README mục "Đổi model embedding".
/// </summary>
public static class EmbeddingDimensions
{
    public const int Default = 1536;

    /// <summary>Phải khớp với <see cref="Default"/>.</summary>
    public const string ColumnType = "vector(1536)";
}

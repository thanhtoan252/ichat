namespace IChat.Api.Endpoints.Common;

/// <summary>
/// Chuẩn hoá offset/limit của query string. Không có gì ở đây là lỗi 400: bỏ trống hoặc
/// phi lý (offset &lt; 0, limit &lt;= 0) rơi về mặc định, limit quá lớn bị cắt về trần.
/// Client không cần gửi offset=0 cho trang đầu và không bị chặn vì xin quá nhiều — trần
/// là chuyện của server, phản hồi luôn nói lại limit thật sự đã dùng.
/// </summary>
public static class PagingQuery
{
    public static int ResolveOffset(int? offset)
    {
        return offset is null or < 0 ? 0 : offset.Value;
    }

    public static int ResolveLimit(int? limit, int defaultLimit, int maxLimit)
    {
        return Math.Clamp(limit is null or <= 0 ? defaultLimit : limit.Value, 1, maxLimit);
    }
}

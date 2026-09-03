namespace IChat.Api.Endpoints.Common;

/// <summary>
/// Quy tắc áp mặc định cho tham số phân trang. Bỏ trống hoặc phi lý (&lt;= 0) đều rơi về
/// mặc định thay vì thành lỗi 400 — client không cần gửi page=1 cho trang đầu tiên.
/// </summary>
public static class PageQuery
{
    public const int FirstPage = 1;

    public static int ResolvePage(int? page)
    {
        return page is null or <= 0 ? FirstPage : page.Value;
    }

    public static int ResolvePageSize(int? pageSize, int defaultPageSize)
    {
        return pageSize is null or <= 0 ? defaultPageSize : pageSize.Value;
    }
}

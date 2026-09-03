namespace IChat.Api.Endpoints.Common;

/// <summary>
/// Query string phân trang, sau khi đã áp giá trị mặc định. Tồn tại để một validator
/// duy nhất kiểm được mọi endpoint có phân trang.
/// </summary>
public interface IPagedQuery
{
    int EffectivePage { get; }

    int EffectivePageSize { get; }
}

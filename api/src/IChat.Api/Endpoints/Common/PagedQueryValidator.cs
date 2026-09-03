namespace IChat.Api.Endpoints.Common;

using FluentValidation;

/// <summary>
/// Kiểm tham số phân trang cho mọi endpoint có phân trang. Chỉ trần pageSize là khác nhau
/// giữa các endpoint, nên nó là tham số của constructor chứ không phải lý do để copy
/// nguyên một validator.
/// </summary>
public abstract class PagedQueryValidator<TQuery> : AbstractValidator<TQuery>
    where TQuery : IPagedQuery
{
    protected PagedQueryValidator(int maxPageSize)
    {
        RuleFor(query => query.EffectivePage)
            .GreaterThan(0)
            .WithName("page").OverridePropertyName("page");

        RuleFor(query => query.EffectivePageSize)
            .InclusiveBetween(1, maxPageSize)
            .WithName("pageSize").OverridePropertyName("pageSize");
    }
}

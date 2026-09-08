namespace IChat.Core.Abstractions;

using IChat.Core.Common;
using IChat.Core.Contracts.Admin;

public interface IAdminService
{
    Task<Result<ReindexResult>> ReindexAsync(CancellationToken cancellationToken);
}

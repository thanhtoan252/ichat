namespace IChat.Core.Abstractions;

using IChat.Core.Common;
using IChat.Core.Contracts.Search;

public interface ISearchService
{
    Task<Result<SearchChunksResult>> SearchAsync(SearchChunksRequest request, CancellationToken cancellationToken);
}

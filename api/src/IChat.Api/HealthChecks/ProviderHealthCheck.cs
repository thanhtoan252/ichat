namespace IChat.Api.HealthChecks;

using IChat.Core.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Probe phải rẻ: chỉ kiểm tra cấu hình và sự hiện diện của credential, cache 30 giây.
/// Không bao giờ gọi hẳn một lượt chat completion mỗi lần probe.
/// </summary>
public sealed class ProviderHealthCheck(IModelCatalog modelCatalog, TimeProvider timeProvider) : IHealthCheck
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
    private readonly Lock _gate = new();

    private HealthCheckResult? _cached;
    private DateTimeOffset _cachedAt = DateTimeOffset.MinValue;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        lock (_gate)
        {
            if (_cached is { } cached && now - _cachedAt < CacheDuration)
            {
                return Task.FromResult(cached);
            }
        }

        var snapshot = modelCatalog.GetSnapshot();
        var unavailable = new[] { snapshot.Chat, snapshot.UtilityChat, snapshot.Embedding }
            .Where(descriptor => !descriptor.Available)
            .ToList();

        var result = unavailable.Count == 0
            ? HealthCheckResult.Healthy("Chat and embedding providers are fully configured.")
            : HealthCheckResult.Unhealthy(string.Join(" | ", unavailable.Select(d => $"{d.Kind}/{d.Provider}: {d.Reason}")));

        lock (_gate)
        {
            _cached = result;
            _cachedAt = now;
        }

        return Task.FromResult(result);
    }
}

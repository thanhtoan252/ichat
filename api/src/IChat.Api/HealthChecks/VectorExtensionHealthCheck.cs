namespace IChat.Api.HealthChecks;

using IChat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class VectorExtensionHealthCheck(IChatDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = dbContext.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT count(*) FROM pg_extension WHERE extname = 'vector';";

            var found = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));

            return found > 0
                ? HealthCheckResult.Healthy("The vector extension is installed.")
                : HealthCheckResult.Unhealthy("The vector extension is not installed in the database.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Could not check the vector extension.", exception);
        }
    }
}

namespace IChat.Infrastructure.Persistence;

using IChat.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddIChatPersistence(this IServiceCollection services, string connectionString)
    {

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        var dataSource = dataSourceBuilder.Build();

services.AddSingleton(dataSource);

services.AddDbContext<IChatDbContext>((serviceProvider, options) =>
        {
            options
                .UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>(), npgsql => npgsql.UseVector())
                .UseSnakeCaseNamingConvention();
        });

services.AddDbContextFactory<IChatDbContext>((serviceProvider, options) =>
        {
            options
                .UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>(), npgsql => npgsql.UseVector())
                .UseSnakeCaseNamingConvention();
        }, lifetime: ServiceLifetime.Scoped);

services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<IChatDbContext>());

return services;
    }
}

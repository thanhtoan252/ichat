namespace IChat.Infrastructure.Persistence;

using IChat.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddIChatPersistence(this IServiceCollection services, string connectionString)
    {
        // UseVector() on the data source is mandatory; without it the vector type fails to map at runtime.
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

        // The retrieval branches run in parallel, so each one needs its own DbContext —
        // a DbContext is not thread-safe.
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

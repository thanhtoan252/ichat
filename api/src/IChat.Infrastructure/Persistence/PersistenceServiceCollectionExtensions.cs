namespace IChat.Infrastructure.Persistence;

using IChat.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddIChatPersistence(this IServiceCollection services, string connectionString)
    {
        // UseVector() trên data source là bắt buộc, thiếu nó sẽ lỗi map kiểu vector lúc runtime.
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

        // Các nhánh retrieval chạy song song nên mỗi nhánh cần DbContext riêng —
        // DbContext không thread-safe.
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

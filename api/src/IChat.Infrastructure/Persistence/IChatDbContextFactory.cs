namespace IChat.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>Chỉ dùng cho `dotnet ef` lúc design-time, không tham gia DI lúc chạy.</summary>
public sealed class IChatDbContextFactory : IDesignTimeDbContextFactory<IChatDbContext>
{
    public IChatDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=ichat;Username=ichat;Password=ichat";

        var options = new DbContextOptionsBuilder<IChatDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseVector())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new IChatDbContext(options);
    }
}

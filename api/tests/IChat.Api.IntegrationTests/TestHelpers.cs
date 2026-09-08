namespace IChat.Api.IntegrationTests;

using System.Net.Http.Headers;
using System.Text.Json;
using IChat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class TestHelpers
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string FixturePath(string name) => Path.Combine(AppContext.BaseDirectory, "fixtures", name);

    public static MultipartFormDataContent FileContent(string fixtureName, string contentType, string? title = null)
    {
        var bytes = File.ReadAllBytes(FixturePath(fixtureName));
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var form = new MultipartFormDataContent { { content, "file", fixtureName } };

        if (title is not null)
        {
            form.Add(new StringContent(title), "title");
        }

        return form;
    }

    public static MultipartFormDataContent InlineContent(string fileName, string contentType, string body)
    {
        var content = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(body));
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        return new MultipartFormDataContent { { content, "file", fileName } };
    }

    /// <summary>Chờ worker xử lý xong; worker chạy nền nên test phải poll trạng thái.</summary>
    public static async Task<string> WaitForStatusAsync(IChatApiFactory factory, Guid documentId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IChatDbContext>();

            var status = await dbContext.Documents
                .AsNoTracking()
                .Where(document => document.Id == documentId)
                .Select(document => document.Status)
                .SingleOrDefaultAsync();

            if (status is Core.Domain.Documents.DocumentStatus.Indexed or Core.Domain.Documents.DocumentStatus.Failed)
            {
                return status.ToString();
            }

            await Task.Delay(200);
        }

        return "Timeout";
    }
}

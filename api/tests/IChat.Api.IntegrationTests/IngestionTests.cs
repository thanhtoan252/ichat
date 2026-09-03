namespace IChat.Api.IntegrationTests;

using System.Net;
using System.Text.Json;
using IChat.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Collection(nameof(IChatApiCollection))]
public class IngestionTests(IChatApiFactory factory)
{
    private static readonly TimeSpan IngestTimeout = TimeSpan.FromSeconds(30);

    private async Task<Guid> UploadAsync(HttpClient client, MultipartFormDataContent content)
    {
        var response = await client.PostAsync("/api/v1/documents", content);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return payload.RootElement.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Upload_LegacyDocBinary_Returns415WithGuidance_NotServerError()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/api/v1/documents",
            TestHelpers.FileContent("sample-legacy.doc", "application/msword"));

        response.StatusCode.Should().Be(
            HttpStatusCode.UnsupportedMediaType,
            "this is the most common user mistake, and it must not blow up into a 500");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(".docx", "the message must tell the user to save the file as .docx");
    }

    [Fact]
    public async Task Upload_UnsupportedExtension_Returns415()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/api/v1/documents",
            TestHelpers.InlineContent("anh.png", "image/png", "khong phai tai lieu"));

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task Upload_Docx_ProducesChunksWithHeadingPathAndEmbeddedText()
    {
        var client = factory.CreateClient();
        var documentId = await UploadAsync(client, TestHelpers.FileContent(
            "sample-traps.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "So tay ky thuat"));

        var status = await TestHelpers.WaitForStatusAsync(factory, documentId, IngestTimeout);
        status.Should().Be("Indexed");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IChatDbContext>();
        var chunks = await dbContext.DocumentChunks.AsNoTracking()
            .Where(chunk => chunk.DocumentId == documentId)
            .OrderBy(chunk => chunk.ChunkIndex)
            .ToListAsync();

        chunks.Should().NotBeEmpty();
        chunks.Should().Contain(chunk => chunk.HeadingPath != null);
        chunks.Should().OnlyContain(chunk => chunk.EmbeddedText.StartsWith("So tay ky thuat"));
        chunks.Should().OnlyContain(chunk => chunk.EmbeddingDimensions == 1536);
        chunks.Should().OnlyContain(chunk => chunk.Embedding.Length == 1536);

        var allContent = string.Join("\n", chunks.Select(chunk => chunk.Content));
        allContent.Should().NotContain("KHONG_DUOC_XUAT_HIEN_TRONG_INDEX", "deleted tracked changes must never reach the index");
        allContent.Should().NotContain("HEADER_CONG_TY_KHONG_DUOC_VAO_INDEX");
    }

    [Fact]
    public async Task Upload_Markdown_ProducesHeadingPath()
    {
        var client = factory.CreateClient();
        var documentId = await UploadAsync(client, TestHelpers.FileContent("sample.md", "text/markdown", "Tai lieu MD"));

        (await TestHelpers.WaitForStatusAsync(factory, documentId, IngestTimeout)).Should().Be("Indexed");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IChatDbContext>();
        var chunks = await dbContext.DocumentChunks.AsNoTracking().Where(chunk => chunk.DocumentId == documentId).ToListAsync();

        chunks.Should().NotBeEmpty();
        chunks.Should().Contain(chunk => chunk.HeadingPath != null && chunk.HeadingPath.Contains("Cai dat"));
    }

    [Fact]
    public async Task Upload_PlainText_ProducesChunksWithoutHeadingPath()
    {
        var client = factory.CreateClient();
        var documentId = await UploadAsync(client, TestHelpers.FileContent("sample.txt", "text/plain", "Tai lieu TXT"));

        (await TestHelpers.WaitForStatusAsync(factory, documentId, IngestTimeout)).Should().Be("Indexed");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IChatDbContext>();
        var chunks = await dbContext.DocumentChunks.AsNoTracking().Where(chunk => chunk.DocumentId == documentId).ToListAsync();

        chunks.Should().NotBeEmpty();
        chunks.Should().OnlyContain(chunk => chunk.HeadingPath == null);
    }

    [Fact]
    public async Task Upload_Pdf_ProducesChunksWithHeadingPathAndPageMetadata()
    {
        var client = factory.CreateClient();
        var documentId = await UploadAsync(client, TestHelpers.FileContent("sample.pdf", "application/pdf", "Tai lieu PDF"));

        (await TestHelpers.WaitForStatusAsync(factory, documentId, IngestTimeout)).Should().Be("Indexed");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IChatDbContext>();
        var chunks = await dbContext.DocumentChunks.AsNoTracking().Where(chunk => chunk.DocumentId == documentId).ToListAsync();

        chunks.Should().NotBeEmpty();
        chunks.Should().Contain(chunk => chunk.HeadingPath != null && chunk.HeadingPath.Contains("Cai dat he thong"));
        chunks.Should().OnlyContain(chunk => chunk.EmbeddedText.StartsWith("Tai lieu PDF"));

        // page chỉ được điền khi nguồn là PDF.
        chunks.Should().Contain(chunk => chunk.Metadata.Contains("\"page\": 1"));
    }

    [Fact]
    public async Task GetChunks_ExposesHeadingPathAndEmbeddedTextForDebugging()
    {
        var client = factory.CreateClient();
        var documentId = await UploadAsync(client, TestHelpers.FileContent("sample.md", "text/markdown", "Tai lieu"));

        (await TestHelpers.WaitForStatusAsync(factory, documentId, IngestTimeout)).Should().Be("Indexed");

        var response = await client.GetAsync($"/api/v1/documents/{documentId}/chunks");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var first = payload.RootElement.GetProperty("items")[0];

        first.TryGetProperty("headingPath", out _).Should().BeTrue();
        first.GetProperty("embeddedText").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Delete_CascadesChunks_AndReturns204()
    {
        var client = factory.CreateClient();
        var documentId = await UploadAsync(client, TestHelpers.FileContent("sample.md", "text/markdown"));

        (await TestHelpers.WaitForStatusAsync(factory, documentId, IngestTimeout)).Should().Be("Indexed");

        var response = await client.DeleteAsync($"/api/v1/documents/{documentId}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IChatDbContext>();

        (await dbContext.DocumentChunks.CountAsync(chunk => chunk.DocumentId == documentId)).Should().Be(0);
    }

    [Fact]
    public async Task GetDocumentById_UnknownId_Returns404()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/documents/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

namespace IChat.Infrastructure.Storage;

using IChat.Core.Abstractions;
using Microsoft.Extensions.Options;

public sealed class LocalFileStorage(IOptions<StorageOptions> options) : IFileStorage
{
    private readonly string _rootPath = options.Value.RootPath;

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_rootPath);

        var safeName = Path.GetFileName(fileName);
        var storedName = $"{Guid.CreateVersion7():N}_{safeName}";
        var fullPath = Path.Combine(_rootPath, storedName);

        await using (var target = File.Create(fullPath))
        {
            await content.CopyToAsync(target, cancellationToken);
        }

        return storedName;
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
    {
        var fullPath = Resolve(storagePath);

        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
    {
        var fullPath = Resolve(storagePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    // Chỉ nhận tên file trong thư mục gốc: chặn path traversal từ giá trị lưu trong DB.
    private string Resolve(string storagePath)
    {
        var name = Path.GetFileName(storagePath);

        return Path.Combine(_rootPath, name);
    }
}

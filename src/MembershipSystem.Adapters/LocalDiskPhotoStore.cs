using MembershipSystem.Domain;
using MembershipSystem.UseCases.Ports;

namespace MembershipSystem.Adapters;

public sealed class LocalDiskPhotoStore(string rootDirectory) : IPhotoStore
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly Dictionary<string, string> ExtensionsByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
    };

    public async Task<PhotoSaveResult> Save(MemberId memberId, Stream content, string contentType)
    {
        if (!ExtensionsByContentType.TryGetValue(contentType, out var extension))
        {
            return PhotoSaveResult.Failure(
                $"Unsupported photo content type '{contentType}'. Only JPEG and PNG are allowed.");
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer);

        if (buffer.Length > MaxBytes)
        {
            return PhotoSaveResult.Failure("Photo exceeds the maximum allowed size of 5 MB.");
        }

        Directory.CreateDirectory(rootDirectory);
        var fileName = $"{memberId.Value}{extension}";
        var fullPath = Path.Combine(rootDirectory, fileName);

        buffer.Position = 0;
        await using var fileStream = File.Create(fullPath);
        await buffer.CopyToAsync(fileStream);

        return PhotoSaveResult.Success(Path.Combine("data", "photos", fileName));
    }

    public Task<Stream?> Get(string photoPath)
    {
        var fullPath = ResolveFullPath(photoPath);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult<Stream?>(stream);
    }

    public Task Delete(string photoPath)
    {
        var fullPath = ResolveFullPath(photoPath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string ResolveFullPath(string photoPath) =>
        Path.Combine(rootDirectory, Path.GetFileName(photoPath));
}

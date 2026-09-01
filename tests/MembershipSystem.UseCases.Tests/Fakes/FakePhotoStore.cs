using MembershipSystem.Domain;
using MembershipSystem.UseCases.Ports;

namespace MembershipSystem.UseCases.Tests.Fakes;

public sealed class FakePhotoStore : IPhotoStore
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
    };

    private readonly Dictionary<string, byte[]> _stored = [];
    private int _sequence;

    public Task<PhotoSaveResult> Save(MemberId memberId, Stream content, string contentType)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            return Task.FromResult(PhotoSaveResult.Failure(
                $"Unsupported photo content type '{contentType}'. Only JPEG and PNG are allowed."));
        }

        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        var bytes = buffer.ToArray();

        if (bytes.LongLength > MaxBytes)
        {
            return Task.FromResult(PhotoSaveResult.Failure(
                "Photo exceeds the maximum allowed size of 5 MB."));
        }

        var path = $"data/photos/{memberId.Value}-{++_sequence}.bin";
        _stored[path] = bytes;
        return Task.FromResult(PhotoSaveResult.Success(path));
    }

    public Task<Stream?> Get(string photoPath)
    {
        if (!_stored.TryGetValue(photoPath, out var bytes))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(new MemoryStream(bytes));
    }

    public Task Delete(string photoPath)
    {
        _stored.Remove(photoPath);
        return Task.CompletedTask;
    }
}

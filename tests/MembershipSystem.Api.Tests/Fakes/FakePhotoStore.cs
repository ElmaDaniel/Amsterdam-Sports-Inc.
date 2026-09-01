using MembershipSystem.Domain;
using MembershipSystem.UseCases.Ports;

namespace MembershipSystem.Api.Tests.Fakes;

public sealed class FakePhotoStore : IPhotoStore
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
    };

    public Task<PhotoSaveResult> Save(MemberId memberId, Stream content, string contentType)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            return Task.FromResult(PhotoSaveResult.Failure(
                $"Unsupported photo content type '{contentType}'. Only JPEG and PNG are allowed."));
        }

        using var buffer = new MemoryStream();
        content.CopyTo(buffer);

        if (buffer.Length > MaxBytes)
        {
            return Task.FromResult(PhotoSaveResult.Failure("Photo exceeds the maximum allowed size of 5 MB."));
        }

        return Task.FromResult(PhotoSaveResult.Success($"data/photos/{memberId.Value}.bin"));
    }

    public Task<Stream?> Get(string photoPath) => Task.FromResult<Stream?>(null);

    public Task Delete(string photoPath) => Task.CompletedTask;
}

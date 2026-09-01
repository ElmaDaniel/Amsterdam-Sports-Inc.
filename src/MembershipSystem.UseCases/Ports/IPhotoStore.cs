using MembershipSystem.Domain;

namespace MembershipSystem.UseCases.Ports;

public interface IPhotoStore
{
    /// <summary>
    /// Saves photo content for a member. Implementations must reject
    /// content type outside image/jpeg and image/png, or content exceeding
    /// 5 MB, by returning a failed <see cref="PhotoSaveResult"/> rather
    /// than throwing — this is part of the port's contract (AC 12), not a
    /// choice left to any one adapter.
    /// </summary>
    Task<PhotoSaveResult> Save(MemberId memberId, Stream content, string contentType);

    Task<Stream?> Get(string photoPath);
    Task Delete(string photoPath);
}

public sealed record PhotoSaveResult
{
    public bool IsSuccess { get; }
    public string? PhotoPath { get; }
    public string? Error { get; }

    private PhotoSaveResult(bool isSuccess, string? photoPath, string? error)
    {
        IsSuccess = isSuccess;
        PhotoPath = photoPath;
        Error = error;
    }

    public static PhotoSaveResult Success(string photoPath) => new(true, photoPath, null);
    public static PhotoSaveResult Failure(string error) => new(false, null, error);
}

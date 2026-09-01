using MembershipSystem.Domain;

namespace MembershipSystem.Adapters.Tests;

public class LocalDiskPhotoStoreTests : IDisposable
{
    private readonly string _rootDirectory;

    public LocalDiskPhotoStoreTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "membership-photo-tests-" + Guid.NewGuid());
    }

    [Fact]
    public async Task Save_Writes_The_File_And_Returns_A_Path()
    {
        var store = new LocalDiskPhotoStore(_rootDirectory);
        using var content = new MemoryStream([1, 2, 3, 4]);

        var result = await store.Save(MemberId.New(), content, "image/jpeg");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.PhotoPath);
        Assert.True(File.Exists(Path.Combine(_rootDirectory, Path.GetFileName(result.PhotoPath))));
    }

    [Fact]
    public async Task Save_Then_Get_Returns_The_Same_Bytes()
    {
        var store = new LocalDiskPhotoStore(_rootDirectory);
        byte[] original = [10, 20, 30];
        using var content = new MemoryStream(original);

        var saveResult = await store.Save(MemberId.New(), content, "image/png");
        await using var retrieved = await store.Get(saveResult.PhotoPath!);

        Assert.NotNull(retrieved);
        using var buffer = new MemoryStream();
        await retrieved.CopyToAsync(buffer);
        Assert.Equal(original, buffer.ToArray());
    }

    [Fact]
    public async Task AC12_Rejects_Unsupported_Content_Type()
    {
        var store = new LocalDiskPhotoStore(_rootDirectory);
        using var content = new MemoryStream([1, 2, 3]);

        var result = await store.Save(MemberId.New(), content, "image/gif");

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task AC12_Rejects_Content_Over_5MB()
    {
        var store = new LocalDiskPhotoStore(_rootDirectory);
        using var content = new MemoryStream(new byte[5 * 1024 * 1024 + 1]);

        var result = await store.Save(MemberId.New(), content, "image/jpeg");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Get_Returns_Null_For_An_Unknown_Path()
    {
        var store = new LocalDiskPhotoStore(_rootDirectory);

        var result = await store.Get("data/photos/does-not-exist.bin");

        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_Removes_A_Previously_Saved_File()
    {
        var store = new LocalDiskPhotoStore(_rootDirectory);
        using var content = new MemoryStream([1, 2, 3]);
        var saveResult = await store.Save(MemberId.New(), content, "image/jpeg");

        await store.Delete(saveResult.PhotoPath!);

        Assert.Null(await store.Get(saveResult.PhotoPath!));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}

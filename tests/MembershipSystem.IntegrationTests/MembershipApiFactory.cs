using MembershipSystem.Adapters;
using MembershipSystem.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MembershipSystem.IntegrationTests;

/// <summary>
/// Boots the real ASP.NET Core pipeline (real DI container, real
/// routing/model binding, real controllers) against a real SQLite file
/// database and a real temp photo directory — the actual adapters wired
/// exactly as Program.cs wires them, just pointed at isolated test
/// locations instead of the app's normal data/ folder.
/// </summary>
public sealed class MembershipApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"membership-it-{Guid.NewGuid()}.db");
    public string PhotosDirectory { get; } = Path.Combine(Path.GetTempPath(), $"membership-it-photos-{Guid.NewGuid()}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MembershipDbContext>>();
            services.AddDbContext<MembershipDbContext>(options => options.UseSqlite($"Data Source={_dbPath}"));

            services.RemoveAll<MembershipSystem.UseCases.Ports.IPhotoStore>();
            services.AddSingleton<MembershipSystem.UseCases.Ports.IPhotoStore>(
                new LocalDiskPhotoStore(PhotosDirectory));
        });
    }

    public async Task<BranchId> SeedBranch(string name = "North Amsterdam")
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MembershipDbContext>();
        var branch = new Branch(BranchId.New(), name);
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch.Id;
    }

    /// <summary>
    /// Seeds a sport directly (no HTTP surface exists for this since
    /// the create-sport endpoint was removed — sports are seed/admin
    /// data now, same as branches originally were).
    /// </summary>
    public async Task<SportId> SeedSport(BranchId branchId, string name)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MembershipDbContext>();
        var sport = new Sport(SportId.New(), branchId, name);
        context.Sports.Add(sport);
        await context.SaveChangesAsync();
        return sport.Id;
    }

    public Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MembershipDbContext>();
        context.Database.Migrate();
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();

        // EF/SQLite pools connections even after the DbContext is
        // disposed, keeping the file handle open — clear the pool first
        // or file deletion races the pool's own cleanup.
        SqliteConnection.ClearAllPools();

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        if (Directory.Exists(PhotosDirectory))
        {
            Directory.Delete(PhotosDirectory, recursive: true);
        }
    }
}

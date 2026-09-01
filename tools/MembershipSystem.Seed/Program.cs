using MembershipSystem.Adapters;
using MembershipSystem.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var apiProjectDirectory = Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "MembershipSystem.Api");

var dbPath = args.Length > 0
    ? args[0]
    : ResolveDbPathFromApiConfig(apiProjectDirectory);

var fullDbPath = Path.GetFullPath(dbPath);
Console.WriteLine($"Seeding database: {fullDbPath}");

if (!File.Exists(fullDbPath))
{
    Console.Error.WriteLine(
        "Database file does not exist yet. Run the API once first (dotnet run --project src/MembershipSystem.Api) " +
        "so migrations create it, then re-run this seed tool.");
    Environment.Exit(1);
}

var options = new DbContextOptionsBuilder<MembershipDbContext>()
    .UseSqlite($"Data Source={fullDbPath}")
    .Options;

await using var context = new MembershipDbContext(options);

if (await context.Branches.AnyAsync())
{
    Console.WriteLine("Branches already exist — skipping seed (idempotent, not re-seeding on top of existing data).");
    return;
}

context.Branches.AddRange(BranchSeedData.All);
context.Sports.AddRange(SportSeedData.All);
context.Members.AddRange(MemberSeedData.BuildAll());

await context.SaveChangesAsync();

Console.WriteLine($"Seeded {BranchSeedData.All.Count} branches, {SportSeedData.All.Count} sports, {MemberSeedData.BuildAll().Count} members.");
foreach (var branch in BranchSeedData.All)
{
    Console.WriteLine($"  Branch '{branch.Name}': {branch.Id.Value}");
}

// Reads the same Database:Path setting the API itself uses
// (src/MembershipSystem.Api/appsettings*.json), so the db path is
// defined once and both projects agree on where it lives.
static string ResolveDbPathFromApiConfig(string apiProjectDirectory)
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Path.GetFullPath(apiProjectDirectory))
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .Build();

    var relativePath = configuration["Database:Path"] ?? "data/app.db";
    return Path.Combine(apiProjectDirectory, relativePath);
}

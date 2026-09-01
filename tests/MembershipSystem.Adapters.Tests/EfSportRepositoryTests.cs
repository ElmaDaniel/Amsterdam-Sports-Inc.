using MembershipSystem.Domain;

namespace MembershipSystem.Adapters.Tests;

public class EfSportRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _db = new();
    private readonly BranchId _branchId = BranchId.New();

    public EfSportRepositoryTests()
    {
        using var context = _db.CreateContext();
        context.Branches.Add(new Branch(_branchId, "North Amsterdam"));
        context.SaveChanges();
    }

    [Fact]
    public async Task ListByBranch_Returns_A_Previously_Saved_Sport()
    {
        await using (var context = _db.CreateContext())
        {
            context.Sports.Add(new Sport(SportId.New(), _branchId, "Tennis"));
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext();
        var readRepository = new EfSportRepository(readContext);
        var found = await readRepository.ListByBranch(_branchId);

        Assert.Single(found);
        Assert.Equal("Tennis", found[0].Name);
    }

    [Fact]
    public async Task ListByBranch_Does_Not_Return_Sports_From_Another_Branch()
    {
        var otherBranchId = BranchId.New();
        await using (var context = _db.CreateContext())
        {
            context.Branches.Add(new Branch(otherBranchId, "South Amsterdam"));
            context.Sports.Add(new Sport(SportId.New(), otherBranchId, "Football"));
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext();
        var readRepository = new EfSportRepository(readContext);
        var found = await readRepository.ListByBranch(_branchId);

        Assert.Empty(found);
    }

    [Fact]
    public async Task GetById_Returns_Null_When_Sport_Belongs_To_A_Different_Branch()
    {
        var sportId = SportId.New();
        await using (var context = _db.CreateContext())
        {
            context.Sports.Add(new Sport(sportId, _branchId, "Tennis"));
            await context.SaveChangesAsync();
        }

        var otherBranchId = BranchId.New();
        await using var readContext = _db.CreateContext();
        readContext.Branches.Add(new Branch(otherBranchId, "South Amsterdam"));
        await readContext.SaveChangesAsync();
        var readRepository = new EfSportRepository(readContext);

        var found = await readRepository.GetById(otherBranchId, sportId);

        Assert.Null(found);
    }

    public void Dispose() => _db.Dispose();
}

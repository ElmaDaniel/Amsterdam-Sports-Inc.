using MembershipSystem.Domain;

namespace MembershipSystem.Adapters.Tests;

public class EfBranchRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _db = new();

    [Fact]
    public async Task GetById_Returns_A_Previously_Saved_Branch()
    {
        var branchId = BranchId.New();
        await using (var context = _db.CreateContext())
        {
            context.Branches.Add(new Branch(branchId, "North Amsterdam"));
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext();
        var repository = new EfBranchRepository(readContext);

        var found = await repository.GetById(branchId);

        Assert.NotNull(found);
        Assert.Equal("North Amsterdam", found.Name);
    }

    [Fact]
    public async Task GetById_Returns_Null_For_Unknown_Branch()
    {
        await using var context = _db.CreateContext();
        var repository = new EfBranchRepository(context);

        var found = await repository.GetById(BranchId.New());

        Assert.Null(found);
    }

    [Fact]
    public async Task ListAll_Returns_Every_Saved_Branch()
    {
        await using (var context = _db.CreateContext())
        {
            context.Branches.Add(new Branch(BranchId.New(), "North Amsterdam"));
            context.Branches.Add(new Branch(BranchId.New(), "South Amsterdam"));
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext();
        var repository = new EfBranchRepository(readContext);

        var found = await repository.ListAll();

        Assert.Equal(2, found.Count);
        Assert.Contains(found, b => b.Name == "North Amsterdam");
        Assert.Contains(found, b => b.Name == "South Amsterdam");
    }

    [Fact]
    public async Task ListAll_Returns_Empty_When_No_Branches_Exist()
    {
        await using var context = _db.CreateContext();
        var repository = new EfBranchRepository(context);

        var found = await repository.ListAll();

        Assert.Empty(found);
    }

    [Fact]
    public async Task Add_Then_GetById_Returns_The_Saved_Branch()
    {
        var branchId = BranchId.New();

        await using (var context = _db.CreateContext())
        {
            var repository = new EfBranchRepository(context);
            await repository.Add(new Branch(branchId, "North Amsterdam"));
        }

        await using var readContext = _db.CreateContext();
        var readRepository = new EfBranchRepository(readContext);
        var found = await readRepository.GetById(branchId);

        Assert.NotNull(found);
        Assert.Equal("North Amsterdam", found.Name);
    }

    public void Dispose() => _db.Dispose();
}

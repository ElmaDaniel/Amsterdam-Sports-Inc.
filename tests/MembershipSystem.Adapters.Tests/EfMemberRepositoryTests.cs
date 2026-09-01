using MembershipSystem.Domain;

namespace MembershipSystem.Adapters.Tests;

public class EfMemberRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _db = new();
    private readonly BranchId _branchId = BranchId.New();

    public EfMemberRepositoryTests()
    {
        using var context = _db.CreateContext();
        context.Branches.Add(new Branch(_branchId, "North Amsterdam"));
        context.SaveChanges();
    }

    [Fact]
    public async Task Add_Then_GetById_Returns_The_Member_With_Sports()
    {
        var tennis = new Sport(SportId.New(), _branchId, "Tennis");
        var member = new Member(MemberId.New(), _branchId, "Ada", "Lovelace");
        member.AssignSport(tennis);

        await using (var context = _db.CreateContext())
        {
            context.Sports.Add(tennis);
            await context.SaveChangesAsync();
            var repository = new EfMemberRepository(context);
            await repository.Add(member);
        }

        await using var readContext = _db.CreateContext();
        var readRepository = new EfMemberRepository(readContext);
        var found = await readRepository.GetById(_branchId, member.Id);

        Assert.NotNull(found);
        Assert.Equal("Ada", found.FirstName);
        Assert.Equal("Lovelace", found.LastName);
        Assert.Contains(tennis.Id, found.SportIds);
    }

    [Fact]
    public async Task ListByBranch_AC2_Returns_Empty_For_A_Branch_With_No_Members()
    {
        await using var context = _db.CreateContext();
        var repository = new EfMemberRepository(context);

        var found = await repository.ListByBranch(_branchId);

        Assert.Empty(found);
    }

    [Fact]
    public async Task GetById_AC4_Returns_Null_When_Member_Belongs_To_A_Different_Branch()
    {
        var member = new Member(MemberId.New(), _branchId, "Ada", "Lovelace");
        await using (var context = _db.CreateContext())
        {
            var repository = new EfMemberRepository(context);
            await repository.Add(member);
        }

        var otherBranchId = BranchId.New();
        await using var readContext = _db.CreateContext();
        readContext.Branches.Add(new Branch(otherBranchId, "South Amsterdam"));
        await readContext.SaveChangesAsync();
        var readRepository = new EfMemberRepository(readContext);

        var found = await readRepository.GetById(otherBranchId, member.Id);

        Assert.Null(found);
    }

    [Fact]
    public async Task Update_Persists_Renamed_Fields_And_Changed_Sport_Set()
    {
        var tennis = new Sport(SportId.New(), _branchId, "Tennis");
        var squash = new Sport(SportId.New(), _branchId, "Squash");
        var member = new Member(MemberId.New(), _branchId, "Ada", "Lovelace");
        member.AssignSport(tennis);

        await using (var context = _db.CreateContext())
        {
            context.Sports.Add(tennis);
            context.Sports.Add(squash);
            await context.SaveChangesAsync();
            var repository = new EfMemberRepository(context);
            await repository.Add(member);
        }

        await using (var context = _db.CreateContext())
        {
            var repository = new EfMemberRepository(context);
            var loaded = await repository.GetById(_branchId, member.Id);
            loaded!.Rename("Augusta", "King");
            loaded.RemoveSport(tennis.Id);
            var squashReloaded = await context.Sports.FindAsync(squash.Id);
            loaded.AssignSport(squashReloaded!);
            await repository.Update(loaded);
        }

        await using var readContext = _db.CreateContext();
        var readRepository = new EfMemberRepository(readContext);
        var found = await readRepository.GetById(_branchId, member.Id);

        Assert.Equal("Augusta", found!.FirstName);
        Assert.Equal("King", found.LastName);
        Assert.DoesNotContain(tennis.Id, found.SportIds);
        Assert.Contains(squash.Id, found.SportIds);
    }

    [Fact]
    public async Task Remove_AC8_Deletes_The_Member()
    {
        var member = new Member(MemberId.New(), _branchId, "Ada", "Lovelace");
        await using (var context = _db.CreateContext())
        {
            var repository = new EfMemberRepository(context);
            await repository.Add(member);
        }

        await using (var context = _db.CreateContext())
        {
            var repository = new EfMemberRepository(context);
            await repository.Remove(_branchId, member.Id);
        }

        await using var readContext = _db.CreateContext();
        var readRepository = new EfMemberRepository(readContext);
        var found = await readRepository.GetById(_branchId, member.Id);

        Assert.Null(found);
    }

    [Fact]
    public async Task Remove_Is_A_NoOp_When_Member_Belongs_To_A_Different_Branch()
    {
        var member = new Member(MemberId.New(), _branchId, "Ada", "Lovelace");
        await using (var context = _db.CreateContext())
        {
            var repository = new EfMemberRepository(context);
            await repository.Add(member);
        }

        var otherBranchId = BranchId.New();
        await using (var context = _db.CreateContext())
        {
            context.Branches.Add(new Branch(otherBranchId, "South Amsterdam"));
            var repository = new EfMemberRepository(context);
            await repository.Remove(otherBranchId, member.Id);
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext();
        var readRepository = new EfMemberRepository(readContext);
        var found = await readRepository.GetById(_branchId, member.Id);

        Assert.NotNull(found);
    }

    public void Dispose() => _db.Dispose();
}

using MembershipSystem.Domain;

namespace MembershipSystem.Domain.Tests;

public class SportTests
{
    [Fact]
    public void Constructor_Sets_Id_BranchId_And_Name()
    {
        var id = SportId.New();
        var branchId = BranchId.New();

        var sport = new Sport(id, branchId, "Tennis");

        Assert.Equal(id, sport.Id);
        Assert.Equal(branchId, sport.BranchId);
        Assert.Equal("Tennis", sport.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_Throws_When_Name_Is_Missing(string? name)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new Sport(SportId.New(), BranchId.New(), name!));

        Assert.Contains("Name", ex.Message);
    }

    [Fact]
    public void Two_Sports_In_Different_Branches_Can_Share_The_Same_Name()
    {
        // AC 13/Decision 3: sports are per-branch, so the same name is a
        // distinct, valid row in each branch — not a domain-level conflict.
        // (Cross-branch uniqueness is not a domain invariant; the
        // within-branch uniqueness check lives at the repository/use-case
        // boundary, since it requires querying existing rows.)
        var branchA = BranchId.New();
        var branchB = BranchId.New();

        var sportA = new Sport(SportId.New(), branchA, "Tennis");
        var sportB = new Sport(SportId.New(), branchB, "Tennis");

        Assert.Equal(sportA.Name, sportB.Name);
        Assert.NotEqual(sportA.BranchId, sportB.BranchId);
    }
}

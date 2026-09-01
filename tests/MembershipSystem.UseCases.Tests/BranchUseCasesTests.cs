using MembershipSystem.Domain;
using MembershipSystem.UseCases.Tests.Fakes;

namespace MembershipSystem.UseCases.Tests;

public class BranchUseCasesTests
{
    [Fact]
    public async Task ListBranches_Returns_All_Seeded_Branches()
    {
        var branches = new FakeBranchRepository();
        branches.Seed(new Branch(BranchId.New(), "North Amsterdam"));
        branches.Seed(new Branch(BranchId.New(), "South Amsterdam"));
        var useCases = new BranchUseCases(branches);

        var result = await useCases.ListBranches();

        Assert.Equal(UseCaseOutcome.Success, result.Outcome);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(result.Value, b => b.Name == "North Amsterdam");
        Assert.Contains(result.Value, b => b.Name == "South Amsterdam");
    }

    [Fact]
    public async Task ListBranches_Returns_Empty_Collection_When_No_Branches_Exist()
    {
        var branches = new FakeBranchRepository();
        var useCases = new BranchUseCases(branches);

        var result = await useCases.ListBranches();

        Assert.Equal(UseCaseOutcome.Success, result.Outcome);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task CreateBranch_Creates_A_Branch_Immediately_Listable()
    {
        var branches = new FakeBranchRepository();
        var useCases = new BranchUseCases(branches);

        var result = await useCases.CreateBranch("North Amsterdam");

        Assert.Equal(UseCaseOutcome.Success, result.Outcome);
        Assert.Equal("North Amsterdam", result.Value!.Name);
        var listed = await branches.ListAll();
        Assert.Contains(listed, b => b.Name == "North Amsterdam");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateBranch_Rejects_Missing_Name(string name)
    {
        var branches = new FakeBranchRepository();
        var useCases = new BranchUseCases(branches);

        var result = await useCases.CreateBranch(name);

        Assert.Equal(UseCaseOutcome.ValidationFailed, result.Outcome);
        Assert.NotEmpty(result.Errors);
    }
}

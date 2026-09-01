using MembershipSystem.Domain;
using MembershipSystem.UseCases.Tests.Fakes;

namespace MembershipSystem.UseCases.Tests;

public class SportUseCasesTests
{
    private static (SportUseCases useCases, FakeSportRepository sports, FakeBranchRepository branches, BranchId branchId)
        CreateSut()
    {
        var branches = new FakeBranchRepository();
        var branchId = BranchId.New();
        branches.Seed(new Branch(branchId, "North Amsterdam"));
        var sports = new FakeSportRepository();
        var useCases = new SportUseCases(sports, branches);
        return (useCases, sports, branches, branchId);
    }

    [Fact]
    public async Task ListSports_AC10_Returns_All_Sports_For_The_Branch()
    {
        var (useCases, sports, _, branchId) = CreateSut();
        await sports.Add(new Sport(SportId.New(), branchId, "Tennis"));
        await sports.Add(new Sport(SportId.New(), branchId, "Squash"));

        var result = await useCases.ListSports(branchId);

        Assert.Equal(UseCaseOutcome.Success, result.Outcome);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(result.Value, s => s.Name == "Tennis");
        Assert.Contains(result.Value, s => s.Name == "Squash");
    }

    [Fact]
    public async Task ListSports_AC10_Returns_Empty_When_Branch_Has_No_Sports_Yet()
    {
        var (useCases, _, _, branchId) = CreateSut();

        var result = await useCases.ListSports(branchId);

        Assert.Equal(UseCaseOutcome.Success, result.Outcome);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task ListSports_AC11_Does_Not_Return_Sports_From_Another_Branch()
    {
        var (useCases, sports, branches, branchId) = CreateSut();
        var otherBranchId = BranchId.New();
        branches.Seed(new Branch(otherBranchId, "South Amsterdam"));
        await sports.Add(new Sport(SportId.New(), otherBranchId, "Football"));

        var result = await useCases.ListSports(branchId);

        Assert.Equal(UseCaseOutcome.Success, result.Outcome);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task ListSports_Returns_NotFound_When_Branch_Does_Not_Exist()
    {
        var (useCases, _, _, _) = CreateSut();

        var result = await useCases.ListSports(BranchId.New());

        Assert.Equal(UseCaseOutcome.NotFound, result.Outcome);
    }
}

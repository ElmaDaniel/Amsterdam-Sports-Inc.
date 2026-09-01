using MembershipSystem.Api.Contracts;
using MembershipSystem.Api.Controllers;
using MembershipSystem.Api.Tests.Fakes;
using MembershipSystem.Domain;
using MembershipSystem.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace MembershipSystem.Api.Tests;

public class SportsControllerTests
{
    private sealed record Sut(SportsController Controller, FakeSportRepository Sports, BranchId BranchId);

    private static Sut CreateSut()
    {
        var sports = new FakeSportRepository();
        var branches = new FakeBranchRepository();
        var branchId = BranchId.New();
        branches.Seed(new Branch(branchId, "North Amsterdam"));
        var useCases = new SportUseCases(sports, branches);
        return new Sut(new SportsController(useCases), sports, branchId);
    }

    [Fact]
    public async Task List_AC10_Returns_200_With_Sports_When_Populated()
    {
        var sut = CreateSut();
        await sut.Sports.Add(new Sport(SportId.New(), sut.BranchId, "Tennis"));

        var result = await sut.Controller.List(sut.BranchId.Value);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsAssignableFrom<IReadOnlyList<SportResponse>>(ok.Value);
        Assert.Contains(body, s => s.Name == "Tennis");
    }

    [Fact]
    public async Task List_Returns_200_With_Empty_Collection_When_Branch_Has_No_Sports()
    {
        var sut = CreateSut();

        var result = await sut.Controller.List(sut.BranchId.Value);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsAssignableFrom<IReadOnlyList<SportResponse>>(ok.Value);
        Assert.Empty(body);
    }

    [Fact]
    public async Task List_Returns_404_When_Branch_Does_Not_Exist()
    {
        var sut = CreateSut();

        var result = await sut.Controller.List(BranchId.New().Value);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}

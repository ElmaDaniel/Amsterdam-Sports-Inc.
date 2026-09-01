using MembershipSystem.Api.Contracts;
using MembershipSystem.Api.Controllers;
using MembershipSystem.Api.Tests.Fakes;
using MembershipSystem.Domain;
using MembershipSystem.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace MembershipSystem.Api.Tests;

public class BranchesControllerTests
{
    [Fact]
    public async Task List_Returns_200_With_All_Branches()
    {
        var branches = new FakeBranchRepository();
        branches.Seed(new Branch(BranchId.New(), "North Amsterdam"));
        branches.Seed(new Branch(BranchId.New(), "South Amsterdam"));
        var controller = new BranchesController(new BranchUseCases(branches));

        var result = await controller.List();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsAssignableFrom<IReadOnlyList<BranchResponse>>(ok.Value);
        Assert.Equal(2, body.Count);
        Assert.Contains(body, b => b.Name == "North Amsterdam");
    }

    [Fact]
    public async Task List_Returns_200_With_Empty_Collection_When_No_Branches_Exist()
    {
        var branches = new FakeBranchRepository();
        var controller = new BranchesController(new BranchUseCases(branches));

        var result = await controller.List();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsAssignableFrom<IReadOnlyList<BranchResponse>>(ok.Value);
        Assert.Empty(body);
    }

    [Fact]
    public async Task Create_Returns_201_With_Created_Branch()
    {
        var branches = new FakeBranchRepository();
        var controller = new BranchesController(new BranchUseCases(branches));

        var result = await controller.Create(new CreateBranchRequest("North Amsterdam"));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var body = Assert.IsType<BranchResponse>(created.Value);
        Assert.Equal("North Amsterdam", body.Name);
    }

    [Fact]
    public async Task Create_Returns_400_For_Missing_Name()
    {
        var branches = new FakeBranchRepository();
        var controller = new BranchesController(new BranchUseCases(branches));

        var result = await controller.Create(new CreateBranchRequest("   "));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<ValidationProblemDetails>(badRequest.Value);
    }
}

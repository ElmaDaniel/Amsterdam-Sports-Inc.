using MembershipSystem.Api.Contracts;
using MembershipSystem.Api.Controllers;
using MembershipSystem.Api.Tests.Fakes;
using MembershipSystem.Domain;
using MembershipSystem.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace MembershipSystem.Api.Tests;

public class MembersControllerTests
{
    private sealed record Sut(
        MembersController Controller,
        FakeMemberRepository Members,
        FakeSportRepository Sports,
        FakeBranchRepository Branches,
        BranchId BranchId);

    private static Sut CreateSut()
    {
        var members = new FakeMemberRepository();
        var sports = new FakeSportRepository();
        var branches = new FakeBranchRepository();
        var photos = new FakePhotoStore();
        var branchId = BranchId.New();
        branches.Seed(new Branch(branchId, "North Amsterdam"));
        var useCases = new MemberUseCases(members, sports, branches, photos);
        return new Sut(new MembersController(useCases), members, sports, branches, branchId);
    }

    [Fact]
    public async Task List_AC1_Returns_200_With_Members_And_Their_Sports()
    {
        var sut = CreateSut();
        var tennis = new Sport(SportId.New(), sut.BranchId, "Tennis");
        await sut.Sports.Add(tennis);
        var member = new Member(MemberId.New(), sut.BranchId, "Ada", "Lovelace");
        member.AssignSport(tennis);
        await sut.Members.Add(member);

        var result = await sut.Controller.List(sut.BranchId.Value);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsAssignableFrom<IReadOnlyList<MemberListItemResponse>>(ok.Value);
        var item = Assert.Single(body);
        Assert.Equal("Ada", item.FirstName);
        Assert.Contains("Tennis", item.Sports);
    }

    [Fact]
    public async Task List_AC2_Returns_200_With_Empty_Collection_When_Branch_Has_No_Members()
    {
        var sut = CreateSut();

        var result = await sut.Controller.List(sut.BranchId.Value);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsAssignableFrom<IReadOnlyList<MemberListItemResponse>>(ok.Value);
        Assert.Empty(body);
    }

    [Fact]
    public async Task List_Returns_404_When_Branch_Does_Not_Exist()
    {
        var sut = CreateSut();

        var result = await sut.Controller.List(BranchId.New().Value);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Get_AC3_Returns_200_With_Member_Detail()
    {
        var sut = CreateSut();
        var member = new Member(MemberId.New(), sut.BranchId, "Ada", "Lovelace");
        await sut.Members.Add(member);

        var result = await sut.Controller.Get(sut.BranchId.Value, member.Id.Value);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<MemberDetailResponse>(ok.Value);
        Assert.Equal("Ada", body.FirstName);
        Assert.Null(body.PhotoPath);
    }

    [Fact]
    public async Task Get_AC4_Returns_404_For_Unknown_Member()
    {
        var sut = CreateSut();

        var result = await sut.Controller.Get(sut.BranchId.Value, Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_AC5_Returns_201_With_Created_Member()
    {
        var sut = CreateSut();
        var tennis = new Sport(SportId.New(), sut.BranchId, "Tennis");
        await sut.Sports.Add(tennis);

        var result = await sut.Controller.Create(
            sut.BranchId.Value, new CreateMemberRequest("Ada", "Lovelace", [tennis.Id.Value]));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var body = Assert.IsType<MemberDetailResponse>(created.Value);
        Assert.Equal("Ada", body.FirstName);
        Assert.Contains(body.Sports, s => s.Name == "Tennis");
    }

    [Fact]
    public async Task Create_AC5_Allows_Null_SportIds_And_No_Photo()
    {
        var sut = CreateSut();

        var result = await sut.Controller.Create(
            sut.BranchId.Value, new CreateMemberRequest("Ada", "Lovelace", null));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var body = Assert.IsType<MemberDetailResponse>(created.Value);
        Assert.Empty(body.Sports);
    }

    [Fact]
    public async Task Create_AC6_Returns_400_For_Missing_Required_Fields()
    {
        var sut = CreateSut();

        var result = await sut.Controller.Create(
            sut.BranchId.Value, new CreateMemberRequest("", "Lovelace", null));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<ValidationProblemDetails>(badRequest.Value);
    }

    [Fact]
    public async Task Create_Returns_404_When_Branch_Does_Not_Exist()
    {
        var sut = CreateSut();

        var result = await sut.Controller.Create(
            BranchId.New().Value, new CreateMemberRequest("Ada", "Lovelace", null));

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_AC7_Returns_200_With_Updated_Member()
    {
        var sut = CreateSut();
        var member = new Member(MemberId.New(), sut.BranchId, "Ada", "Lovelace");
        await sut.Members.Add(member);

        var result = await sut.Controller.Update(
            sut.BranchId.Value, member.Id.Value, new UpdateMemberRequest("Augusta", "King", null));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<MemberDetailResponse>(ok.Value);
        Assert.Equal("Augusta", body.FirstName);
    }

    [Fact]
    public async Task Update_Returns_404_For_Unknown_Member()
    {
        var sut = CreateSut();

        var result = await sut.Controller.Update(
            sut.BranchId.Value, Guid.NewGuid(), new UpdateMemberRequest("Ada", "Lovelace", null));

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_Returns_400_For_Missing_Required_Fields()
    {
        var sut = CreateSut();
        var member = new Member(MemberId.New(), sut.BranchId, "Ada", "Lovelace");
        await sut.Members.Add(member);

        var result = await sut.Controller.Update(
            sut.BranchId.Value, member.Id.Value, new UpdateMemberRequest("", "Lovelace", null));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Delete_AC8_Returns_204_For_An_Existing_Member()
    {
        var sut = CreateSut();
        var member = new Member(MemberId.New(), sut.BranchId, "Ada", "Lovelace");
        await sut.Members.Add(member);

        var result = await sut.Controller.Delete(sut.BranchId.Value, member.Id.Value);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_AC9_Returns_404_For_Unknown_Member()
    {
        var sut = CreateSut();

        var result = await sut.Controller.Delete(sut.BranchId.Value, Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SetPhoto_Returns_200_On_Success()
    {
        var sut = CreateSut();
        var member = new Member(MemberId.New(), sut.BranchId, "Ada", "Lovelace");
        await sut.Members.Add(member);
        using var content = new MemoryStream([1, 2, 3]);

        var result = await sut.Controller.SetPhoto(sut.BranchId.Value, member.Id.Value, content, "image/jpeg");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<MemberDetailResponse>(ok.Value);
        Assert.NotNull(body.PhotoPath);
    }

    [Fact]
    public async Task SetPhoto_AC12_Returns_400_For_Unsupported_Content_Type()
    {
        var sut = CreateSut();
        var member = new Member(MemberId.New(), sut.BranchId, "Ada", "Lovelace");
        await sut.Members.Add(member);
        using var content = new MemoryStream([1, 2, 3]);

        var result = await sut.Controller.SetPhoto(sut.BranchId.Value, member.Id.Value, content, "image/gif");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task SetPhoto_Returns_404_For_Unknown_Member()
    {
        var sut = CreateSut();
        using var content = new MemoryStream([1, 2, 3]);

        var result = await sut.Controller.SetPhoto(sut.BranchId.Value, Guid.NewGuid(), content, "image/jpeg");

        Assert.IsType<NotFoundResult>(result.Result);
    }
}

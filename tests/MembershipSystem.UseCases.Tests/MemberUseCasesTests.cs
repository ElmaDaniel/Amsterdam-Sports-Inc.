using MembershipSystem.Domain;
using MembershipSystem.UseCases.Tests.Fakes;

namespace MembershipSystem.UseCases.Tests;

public class MemberUseCasesTests
{
    private sealed record Sut(
        MemberUseCases UseCases,
        FakeMemberRepository Members,
        FakeSportRepository Sports,
        FakeBranchRepository Branches,
        FakePhotoStore Photos,
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
        return new Sut(useCases, members, sports, branches, photos, branchId);
    }

    [Fact]
    public async Task ListMembers_AC1_Returns_Members_With_Their_Sports()
    {
        var sut = CreateSut();
        var tennis = new Sport(SportId.New(), sut.BranchId, "Tennis");
        await sut.Sports.Add(tennis);
        var member = new Member(MemberId.New(), sut.BranchId, "Ada", "Lovelace");
        member.AssignSport(tennis);
        await sut.Members.Add(member);

        var result = await sut.UseCases.ListMembers(sut.BranchId);

        Assert.Equal(UseCaseOutcome.Success, result.Outcome);
        var summary = Assert.Single(result.Value!);
        Assert.Equal("Ada", summary.FirstName);
        Assert.Equal("Lovelace", summary.LastName);
        Assert.Contains("Tennis", summary.SportNames);
    }

    [Fact]
    public async Task ListMembers_AC2_Returns_Empty_When_Branch_Has_No_Members()
    {
        var sut = CreateSut();

        var result = await sut.UseCases.ListMembers(sut.BranchId);

        Assert.Equal(UseCaseOutcome.Success, result.Outcome);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task ListMembers_Returns_NotFound_When_Branch_Does_Not_Exist()
    {
        var sut = CreateSut();

        var result = await sut.UseCases.ListMembers(BranchId.New());

        Assert.Equal(UseCaseOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task GetMember_AC3_Returns_Member_Detail_With_Sports()
    {
        var sut = CreateSut();
        var squash = new Sport(SportId.New(), sut.BranchId, "Squash");
        await sut.Sports.Add(squash);
        var member = new Member(MemberId.New(), sut.BranchId, "Grace", "Hopper");
        member.AssignSport(squash);
        await sut.Members.Add(member);

        var result = await sut.UseCases.GetMember(sut.BranchId, member.Id);

        Assert.Equal(UseCaseOutcome.Success, result.Outcome);
        Assert.Equal("Grace", result.Value!.FirstName);
        Assert.Contains(result.Value.Sports, s => s.Name == "Squash");
    }

    [Fact]
    public async Task GetMember_AC4_Returns_NotFound_For_Unknown_Member()
    {
        var sut = CreateSut();

        var result = await sut.UseCases.GetMember(sut.BranchId, MemberId.New());

        Assert.Equal(UseCaseOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task GetMember_AC4_Returns_NotFound_When_Member_Belongs_To_Another_Branch()
    {
        var sut = CreateSut();
        var otherBranchId = BranchId.New();
        sut.Branches.Seed(new Branch(otherBranchId, "South Amsterdam"));
        var member = new Member(MemberId.New(), otherBranchId, "Ada", "Lovelace");
        await sut.Members.Add(member);

        var result = await sut.UseCases.GetMember(sut.BranchId, member.Id);

        Assert.Equal(UseCaseOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task CreateMember_AC5_Creates_A_Member_With_Sports()
    {
        var sut = CreateSut();
        var tennis = new Sport(SportId.New(), sut.BranchId, "Tennis");
        await sut.Sports.Add(tennis);

        var result = await sut.UseCases.CreateMember(sut.BranchId, "Ada", "Lovelace", [tennis.Id.Value]);

        Assert.Equal(UseCaseOutcome.Success, result.Outcome);
        Assert.Equal("Ada", result.Value!.FirstName);
        Assert.Contains(result.Value.Sports, s => s.Name == "Tennis");
    }

    [Fact]
    public async Task CreateMember_AC5_Allows_Zero_Sports_And_No_Photo()
    {
        var sut = CreateSut();

        var result = await sut.UseCases.CreateMember(sut.BranchId, "Ada", "Lovelace", []);

        Assert.Equal(UseCaseOutcome.Success, result.Outcome);
        Assert.Empty(result.Value!.Sports);
        Assert.Null(result.Value.PhotoPath);
    }

    [Theory]
    [InlineData("", "Lovelace")]
    [InlineData("Ada", "")]
    public async Task CreateMember_AC6_Rejects_Missing_Required_Fields(string firstName, string lastName)
    {
        var sut = CreateSut();

        var result = await sut.UseCases.CreateMember(sut.BranchId, firstName, lastName, []);

        Assert.Equal(UseCaseOutcome.ValidationFailed, result.Outcome);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task CreateMember_Rejects_A_Sport_That_Does_Not_Belong_To_The_Branch()
    {
        var sut = CreateSut();
        var otherBranchId = BranchId.New();
        sut.Branches.Seed(new Branch(otherBranchId, "South Amsterdam"));
        var otherBranchSport = new Sport(SportId.New(), otherBranchId, "Tennis");
        await sut.Sports.Add(otherBranchSport);

        var result = await sut.UseCases.CreateMember(
            sut.BranchId, "Ada", "Lovelace", [otherBranchSport.Id.Value]);

        Assert.Equal(UseCaseOutcome.ValidationFailed, result.Outcome);
    }

    [Fact]
    public async Task CreateMember_Returns_NotFound_When_Branch_Does_Not_Exist()
    {
        var sut = CreateSut();

        var result = await sut.UseCases.CreateMember(BranchId.New(), "Ada", "Lovelace", []);

        Assert.Equal(UseCaseOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task UpdateMember_AC7_Updates_Names_And_Sport_Associations()
    {
        var sut = CreateSut();
        var tennis = new Sport(SportId.New(), sut.BranchId, "Tennis");
        var squash = new Sport(SportId.New(), sut.BranchId, "Squash");
        await sut.Sports.Add(tennis);
        await sut.Sports.Add(squash);
        var member = new Member(MemberId.New(), sut.BranchId, "Ada", "Lovelace");
        member.AssignSport(tennis);
        await sut.Members.Add(member);

        var result = await sut.UseCases.UpdateMember(
            sut.BranchId, member.Id, "Augusta", "King", [squash.Id.Value]);

        Assert.Equal(UseCaseOutcome.Success, result.Outcome);
        Assert.Equal("Augusta", result.Value!.FirstName);
        Assert.Equal("King", result.Value.LastName);
        Assert.DoesNotContain(result.Value.Sports, s => s.Name == "Tennis");
        Assert.Contains(result.Value.Sports, s => s.Name == "Squash");
    }

    [Fact]
    public async Task UpdateMember_Returns_NotFound_For_Unknown_Member()
    {
        var sut = CreateSut();

        var result = await sut.UseCases.UpdateMember(
            sut.BranchId, MemberId.New(), "Ada", "Lovelace", []);

        Assert.Equal(UseCaseOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task UpdateMember_Rejects_Missing_Required_Fields()
    {
        var sut = CreateSut();
        var member = new Member(MemberId.New(), sut.BranchId, "Ada", "Lovelace");
        await sut.Members.Add(member);

        var result = await sut.UseCases.UpdateMember(sut.BranchId, member.Id, "", "Lovelace", []);

        Assert.Equal(UseCaseOutcome.ValidationFailed, result.Outcome);
    }

    [Fact]
    public async Task RemoveMember_AC8_Removes_An_Existing_Member()
    {
        var sut = CreateSut();
        var member = new Member(MemberId.New(), sut.BranchId, "Ada", "Lovelace");
        await sut.Members.Add(member);

        var result = await sut.UseCases.RemoveMember(sut.BranchId, member.Id);

        Assert.Equal(UseCaseOutcome.Success, result.Outcome);
        Assert.Null(await sut.Members.GetById(sut.BranchId, member.Id));
    }

    [Fact]
    public async Task RemoveMember_AC9_Returns_NotFound_For_Unknown_Member()
    {
        var sut = CreateSut();

        var result = await sut.UseCases.RemoveMember(sut.BranchId, MemberId.New());

        Assert.Equal(UseCaseOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task SetMemberPhoto_Sets_The_Photo_Path_On_Success()
    {
        var sut = CreateSut();
        var member = new Member(MemberId.New(), sut.BranchId, "Ada", "Lovelace");
        await sut.Members.Add(member);
        using var content = new MemoryStream([1, 2, 3]);

        var result = await sut.UseCases.SetMemberPhoto(sut.BranchId, member.Id, content, "image/jpeg");

        Assert.Equal(UseCaseOutcome.Success, result.Outcome);
        Assert.NotNull(result.Value!.PhotoPath);
    }

    [Fact]
    public async Task SetMemberPhoto_AC12_Rejects_Unsupported_Content_Type()
    {
        var sut = CreateSut();
        var member = new Member(MemberId.New(), sut.BranchId, "Ada", "Lovelace");
        await sut.Members.Add(member);
        using var content = new MemoryStream([1, 2, 3]);

        var result = await sut.UseCases.SetMemberPhoto(sut.BranchId, member.Id, content, "image/gif");

        Assert.Equal(UseCaseOutcome.ValidationFailed, result.Outcome);
    }

    [Fact]
    public async Task SetMemberPhoto_AC12_Rejects_Content_Over_Size_Cap()
    {
        var sut = CreateSut();
        var member = new Member(MemberId.New(), sut.BranchId, "Ada", "Lovelace");
        await sut.Members.Add(member);
        using var content = new MemoryStream(new byte[6 * 1024 * 1024]);

        var result = await sut.UseCases.SetMemberPhoto(sut.BranchId, member.Id, content, "image/png");

        Assert.Equal(UseCaseOutcome.ValidationFailed, result.Outcome);
    }

    [Fact]
    public async Task SetMemberPhoto_Returns_NotFound_For_Unknown_Member()
    {
        var sut = CreateSut();
        using var content = new MemoryStream([1, 2, 3]);

        var result = await sut.UseCases.SetMemberPhoto(sut.BranchId, MemberId.New(), content, "image/jpeg");

        Assert.Equal(UseCaseOutcome.NotFound, result.Outcome);
    }
}

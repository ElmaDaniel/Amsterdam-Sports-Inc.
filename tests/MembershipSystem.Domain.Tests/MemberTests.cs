using MembershipSystem.Domain;

namespace MembershipSystem.Domain.Tests;

public class MemberTests
{
    private static readonly BranchId SomeBranch = BranchId.New();

    private static Member CreateMember(string firstName = "Ada", string lastName = "Lovelace") =>
        new(MemberId.New(), SomeBranch, firstName, lastName);

    [Fact]
    public void Constructor_Sets_Id_BranchId_Names_And_Starts_With_No_Photo_Or_Sports()
    {
        var id = MemberId.New();

        var member = new Member(id, SomeBranch, "Ada", "Lovelace");

        Assert.Equal(id, member.Id);
        Assert.Equal(SomeBranch, member.BranchId);
        Assert.Equal("Ada", member.FirstName);
        Assert.Equal("Lovelace", member.LastName);
        Assert.Null(member.PhotoPath);
        Assert.Empty(member.SportIds);
    }

    [Theory]
    [InlineData("", "Lovelace")]
    [InlineData("   ", "Lovelace")]
    [InlineData(null, "Lovelace")]
    public void Constructor_Throws_When_FirstName_Is_Missing(string? firstName, string lastName)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new Member(MemberId.New(), SomeBranch, firstName!, lastName));

        Assert.Contains("FirstName", ex.Message);
    }

    [Theory]
    [InlineData("Ada", "")]
    [InlineData("Ada", "   ")]
    [InlineData("Ada", null)]
    public void Constructor_Throws_When_LastName_Is_Missing(string firstName, string? lastName)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new Member(MemberId.New(), SomeBranch, firstName, lastName!));

        Assert.Contains("LastName", ex.Message);
    }

    [Fact]
    public void AssignSport_Adds_A_Sport_From_The_Members_Own_Branch()
    {
        // AC 1/AC 7: a member can play more than one sport.
        var member = CreateMember();
        var tennis = new Sport(SportId.New(), SomeBranch, "Tennis");

        member.AssignSport(tennis);

        Assert.Contains(tennis.Id, member.SportIds);
    }

    [Fact]
    public void AssignSport_Allows_Multiple_Sports()
    {
        var member = CreateMember();
        var tennis = new Sport(SportId.New(), SomeBranch, "Tennis");
        var squash = new Sport(SportId.New(), SomeBranch, "Squash");

        member.AssignSport(tennis);
        member.AssignSport(squash);

        Assert.Equal(2, member.SportIds.Count);
        Assert.Contains(tennis.Id, member.SportIds);
        Assert.Contains(squash.Id, member.SportIds);
    }

    [Fact]
    public void AssignSport_Throws_When_Sport_Belongs_To_A_Different_Branch()
    {
        // Layer map invariant: sports collection "may not contain a
        // SportId that isn't a sport of the member's own branch."
        var member = CreateMember();
        var otherBranchSport = new Sport(SportId.New(), BranchId.New(), "Tennis");

        var ex = Assert.Throws<InvalidOperationException>(
            () => member.AssignSport(otherBranchSport));

        Assert.Contains("branch", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(member.SportIds);
    }

    [Fact]
    public void AssignSport_Is_Idempotent_For_The_Same_Sport()
    {
        var member = CreateMember();
        var tennis = new Sport(SportId.New(), SomeBranch, "Tennis");

        member.AssignSport(tennis);
        member.AssignSport(tennis);

        Assert.Single(member.SportIds);
    }

    [Fact]
    public void RemoveSport_Removes_A_Previously_Assigned_Sport()
    {
        // AC 7: edit a member's sport associations (add/remove).
        var member = CreateMember();
        var tennis = new Sport(SportId.New(), SomeBranch, "Tennis");
        member.AssignSport(tennis);

        member.RemoveSport(tennis.Id);

        Assert.Empty(member.SportIds);
    }

    [Fact]
    public void RemoveSport_Is_A_NoOp_When_Sport_Was_Not_Assigned()
    {
        var member = CreateMember();
        var neverAssigned = SportId.New();

        member.RemoveSport(neverAssigned);

        Assert.Empty(member.SportIds);
    }

    [Fact]
    public void SetPhotoPath_Sets_The_Path()
    {
        var member = CreateMember();

        member.SetPhotoPath("data/photos/some-file.jpg");

        Assert.Equal("data/photos/some-file.jpg", member.PhotoPath);
    }

    [Fact]
    public void Rename_Updates_First_And_Last_Name()
    {
        // AC 7: update first name, last name.
        var member = CreateMember();

        member.Rename("Grace", "Hopper");

        Assert.Equal("Grace", member.FirstName);
        Assert.Equal("Hopper", member.LastName);
    }

    [Theory]
    [InlineData("", "Hopper")]
    [InlineData("Grace", "")]
    public void Rename_Throws_When_Either_Name_Is_Missing(string firstName, string lastName)
    {
        var member = CreateMember();

        Assert.Throws<ArgumentException>(() => member.Rename(firstName, lastName));
    }
}

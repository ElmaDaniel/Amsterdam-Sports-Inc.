using MembershipSystem.Domain;

namespace MembershipSystem.Domain.Tests;

public class BranchTests
{
    [Fact]
    public void Constructor_Sets_Id_And_Name()
    {
        var id = BranchId.New();

        var branch = new Branch(id, "North Amsterdam");

        Assert.Equal(id, branch.Id);
        Assert.Equal("North Amsterdam", branch.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_Throws_When_Name_Is_Missing(string? name)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new Branch(BranchId.New(), name!));

        Assert.Contains("Name", ex.Message);
    }
}

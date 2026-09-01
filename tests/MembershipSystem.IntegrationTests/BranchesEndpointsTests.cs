using System.Net;
using System.Net.Http.Json;
using MembershipSystem.Api.Contracts;

namespace MembershipSystem.IntegrationTests;

public class BranchesEndpointsTests : IClassFixture<MembershipApiFactory>
{
    private readonly MembershipApiFactory _factory;
    private readonly HttpClient _client;

    public BranchesEndpointsTests(MembershipApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task List_Returns_Every_Seeded_Branch()
    {
        var branchA = await _factory.SeedBranch("North Amsterdam");
        var branchB = await _factory.SeedBranch("South Amsterdam");

        var response = await _client.GetAsync("/branches");

        response.EnsureSuccessStatusCode();
        var branches = await response.Content.ReadFromJsonAsync<List<BranchResponse>>();
        Assert.Contains(branches!, b => b.Id == branchA.Value);
        Assert.Contains(branches!, b => b.Id == branchB.Value);
    }

    [Fact]
    public async Task Create_Then_List_Returns_The_New_Branch()
    {
        var createResponse = await _client.PostAsJsonAsync("/branches", new CreateBranchRequest("East Amsterdam"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<BranchResponse>();

        var listResponse = await _client.GetAsync("/branches");
        var branches = await listResponse.Content.ReadFromJsonAsync<List<BranchResponse>>();

        Assert.Contains(branches!, b => b.Id == created!.Id && b.Name == "East Amsterdam");
    }

    [Fact]
    public async Task Create_Returns_400_For_Missing_Name()
    {
        var response = await _client.PostAsJsonAsync("/branches", new CreateBranchRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

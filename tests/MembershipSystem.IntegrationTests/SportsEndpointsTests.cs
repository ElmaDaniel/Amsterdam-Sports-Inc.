using System.Net;
using System.Net.Http.Json;
using MembershipSystem.Api.Contracts;

namespace MembershipSystem.IntegrationTests;

public class SportsEndpointsTests : IClassFixture<MembershipApiFactory>
{
    private readonly MembershipApiFactory _factory;
    private readonly HttpClient _client;

    public SportsEndpointsTests(MembershipApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AC10_List_Returns_Empty_For_A_Branch_With_No_Sports()
    {
        var branchId = await _factory.SeedBranch();

        var response = await _client.GetAsync($"/branches/{branchId.Value}/sports");

        response.EnsureSuccessStatusCode();
        var sports = await response.Content.ReadFromJsonAsync<List<SportResponse>>();
        Assert.Empty(sports!);
    }

    [Fact]
    public async Task AC10_List_Returns_Seeded_Sports_For_The_Branch()
    {
        var branchId = await _factory.SeedBranch();
        await _factory.SeedSport(branchId, "Padel");

        var response = await _client.GetAsync($"/branches/{branchId.Value}/sports");

        var sports = await response.Content.ReadFromJsonAsync<List<SportResponse>>();
        Assert.Contains(sports!, s => s.Name == "Padel");
    }

    [Fact]
    public async Task AC11_Sports_Seeded_In_One_Branch_Are_Invisible_From_Another_Branchs_List()
    {
        var branchA = await _factory.SeedBranch("North Amsterdam");
        var branchB = await _factory.SeedBranch("South Amsterdam");
        await _factory.SeedSport(branchA, "Football");

        var listForB = await _client.GetAsync($"/branches/{branchB.Value}/sports");
        var sportsForB = await listForB.Content.ReadFromJsonAsync<List<SportResponse>>();

        Assert.DoesNotContain(sportsForB!, s => s.Name == "Football");
    }

    [Fact]
    public async Task List_Returns_404_When_Branch_Does_Not_Exist()
    {
        var response = await _client.GetAsync($"/branches/{Guid.NewGuid()}/sports");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

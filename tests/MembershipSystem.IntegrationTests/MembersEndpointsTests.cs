using System.Net;
using System.Net.Http.Json;
using MembershipSystem.Api.Contracts;
using MembershipSystem.Domain;

namespace MembershipSystem.IntegrationTests;

public class MembersEndpointsTests : IClassFixture<MembershipApiFactory>
{
    private readonly MembershipApiFactory _factory;
    private readonly HttpClient _client;

    public MembersEndpointsTests(MembershipApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AC2_List_Returns_Empty_Collection_For_A_New_Branch()
    {
        var branchId = await _factory.SeedBranch();

        var response = await _client.GetAsync($"/branches/{branchId.Value}/members");

        response.EnsureSuccessStatusCode();
        var members = await response.Content.ReadFromJsonAsync<List<MemberListItemResponse>>();
        Assert.Empty(members!);
    }

    [Fact]
    public async Task AC1_AC5_Create_Then_List_Returns_The_Member_With_Its_Sport()
    {
        var branchId = await _factory.SeedBranch();
        var sportId = await _factory.SeedSport(branchId, "Tennis");

        var createResponse = await _client.PostAsJsonAsync(
            $"/branches/{branchId.Value}/members",
            new CreateMemberRequest("Ada", "Lovelace", [sportId.Value]));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var listResponse = await _client.GetAsync($"/branches/{branchId.Value}/members");
        var members = await listResponse.Content.ReadFromJsonAsync<List<MemberListItemResponse>>();

        var member = Assert.Single(members!);
        Assert.Equal("Ada", member.FirstName);
        Assert.Contains("Tennis", member.Sports);
    }

    [Fact]
    public async Task AC3_Get_Returns_Member_Detail_With_Sports_After_Create()
    {
        var branchId = await _factory.SeedBranch();
        var sportId = await _factory.SeedSport(branchId, "Squash");
        var created = await CreateMember(branchId.Value, "Grace", "Hopper", [sportId.Value]);

        var response = await _client.GetAsync($"/branches/{branchId.Value}/members/{created.Id}");

        response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<MemberDetailResponse>();
        Assert.Equal("Grace", detail!.FirstName);
        Assert.Contains(detail.Sports, s => s.Name == "Squash");
    }

    [Fact]
    public async Task AC4_Get_Returns_404_For_A_Member_In_A_Different_Branch()
    {
        var branchId = await _factory.SeedBranch("North Amsterdam");
        var otherBranchId = await _factory.SeedBranch("South Amsterdam");
        var member = await CreateMember(otherBranchId.Value, "Ada", "Lovelace", []);

        var response = await _client.GetAsync($"/branches/{branchId.Value}/members/{member.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AC6_Create_Returns_400_For_Missing_Required_Fields()
    {
        var branchId = await _factory.SeedBranch();

        var response = await _client.PostAsJsonAsync(
            $"/branches/{branchId.Value}/members",
            new CreateMemberRequest("", "Lovelace", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AC7_Update_Persists_Renamed_Fields_And_Changed_Sports_Across_Requests()
    {
        var branchId = await _factory.SeedBranch();
        var tennisId = await _factory.SeedSport(branchId, "Tennis");
        var squashId = await _factory.SeedSport(branchId, "Squash");
        var created = await CreateMember(branchId.Value, "Ada", "Lovelace", [tennisId.Value]);

        var updateResponse = await _client.PutAsJsonAsync(
            $"/branches/{branchId.Value}/members/{created.Id}",
            new UpdateMemberRequest("Augusta", "King", [squashId.Value]));
        updateResponse.EnsureSuccessStatusCode();

        var getResponse = await _client.GetAsync($"/branches/{branchId.Value}/members/{created.Id}");
        var detail = await getResponse.Content.ReadFromJsonAsync<MemberDetailResponse>();

        Assert.Equal("Augusta", detail!.FirstName);
        Assert.Equal("King", detail.LastName);
        Assert.DoesNotContain(detail.Sports, s => s.Name == "Tennis");
        Assert.Contains(detail.Sports, s => s.Name == "Squash");
    }

    [Fact]
    public async Task AC8_AC9_Delete_Removes_The_Member_And_A_Second_Delete_Returns_404()
    {
        var branchId = await _factory.SeedBranch();
        var created = await CreateMember(branchId.Value, "Ada", "Lovelace", []);

        var firstDelete = await _client.DeleteAsync($"/branches/{branchId.Value}/members/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, firstDelete.StatusCode);

        var getAfterDelete = await _client.GetAsync($"/branches/{branchId.Value}/members/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);

        var secondDelete = await _client.DeleteAsync($"/branches/{branchId.Value}/members/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, secondDelete.StatusCode);
    }

    [Fact]
    public async Task AC12_Photo_Upload_Persists_A_Real_File_On_Disk_And_Is_Retrievable_By_Path()
    {
        var branchId = await _factory.SeedBranch();
        var created = await CreateMember(branchId.Value, "Ada", "Lovelace", []);

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        form.Add(fileContent, "file", "photo.jpg");

        var response = await _client.PutAsync($"/branches/{branchId.Value}/members/{created.Id}/photo", form);

        response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<MemberDetailResponse>();
        Assert.NotNull(detail!.PhotoPath);

        var savedFileName = Path.GetFileName(detail.PhotoPath);
        var fullPath = Path.Combine(_factory.PhotosDirectory, savedFileName);
        Assert.True(File.Exists(fullPath), $"expected photo file at {fullPath}");
    }

    [Fact]
    public async Task AC12_Photo_Upload_Rejects_Unsupported_Content_Type_Over_Real_HTTP()
    {
        var branchId = await _factory.SeedBranch();
        var created = await CreateMember(branchId.Value, "Ada", "Lovelace", []);

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([1, 2, 3]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/gif");
        form.Add(fileContent, "file", "photo.gif");

        var response = await _client.PutAsync($"/branches/{branchId.Value}/members/{created.Id}/photo", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AC12_Photo_Upload_Rejects_Content_Over_5MB_Over_Real_HTTP()
    {
        var branchId = await _factory.SeedBranch();
        var created = await CreateMember(branchId.Value, "Ada", "Lovelace", []);

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[5 * 1024 * 1024 + 1]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "big.png");

        var response = await _client.PutAsync($"/branches/{branchId.Value}/members/{created.Id}/photo", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_Returns_404_When_Branch_Does_Not_Exist()
    {
        var response = await _client.GetAsync($"/branches/{Guid.NewGuid()}/members");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<MemberDetailResponse> CreateMember(
        Guid branchId, string firstName, string lastName, IReadOnlyList<Guid> sportIds)
    {
        var response = await _client.PostAsJsonAsync(
            $"/branches/{branchId}/members", new CreateMemberRequest(firstName, lastName, sportIds));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MemberDetailResponse>())!;
    }
}

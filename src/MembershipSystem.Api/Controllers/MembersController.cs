using MembershipSystem.Api.Contracts;
using MembershipSystem.Domain;
using MembershipSystem.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace MembershipSystem.Api.Controllers;

[ApiController]
[Route("branches/{branchId:guid}/members")]
public sealed class MembersController(MemberUseCases memberUseCases) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MemberListItemResponse>>> List(Guid branchId)
    {
        var result = await memberUseCases.ListMembers(new BranchId(branchId));

        return result.Outcome switch
        {
            UseCaseOutcome.NotFound => NotFound(),
            _ => Ok(result.Value!
                .Select(m => new MemberListItemResponse(m.Id, m.FirstName, m.LastName, m.SportNames))
                .ToList()),
        };
    }

    [HttpGet("{memberId:guid}")]
    public async Task<ActionResult<MemberDetailResponse>> Get(Guid branchId, Guid memberId)
    {
        var result = await memberUseCases.GetMember(new BranchId(branchId), new MemberId(memberId));

        return result.Outcome switch
        {
            UseCaseOutcome.NotFound => NotFound(),
            _ => Ok(ToResponse(result.Value!)),
        };
    }

    [HttpPost]
    public async Task<ActionResult<MemberDetailResponse>> Create(Guid branchId, [FromBody] CreateMemberRequest request)
    {
        var result = await memberUseCases.CreateMember(
            new BranchId(branchId), request.FirstName, request.LastName, request.SportIds ?? []);

        return result.Outcome switch
        {
            UseCaseOutcome.NotFound => NotFound(),
            UseCaseOutcome.ValidationFailed => BadRequest(ApiProblemDetails.ValidationFailed(result.Errors)),
            _ => CreatedAtAction(
                nameof(Get),
                new { branchId, memberId = result.Value!.Id },
                ToResponse(result.Value!)),
        };
    }

    [HttpPut("{memberId:guid}")]
    public async Task<ActionResult<MemberDetailResponse>> Update(
        Guid branchId, Guid memberId, [FromBody] UpdateMemberRequest request)
    {
        var result = await memberUseCases.UpdateMember(
            new BranchId(branchId), new MemberId(memberId), request.FirstName, request.LastName,
            request.SportIds ?? []);

        return result.Outcome switch
        {
            UseCaseOutcome.NotFound => NotFound(),
            UseCaseOutcome.ValidationFailed => BadRequest(ApiProblemDetails.ValidationFailed(result.Errors)),
            _ => Ok(ToResponse(result.Value!)),
        };
    }

    [HttpDelete("{memberId:guid}")]
    public async Task<IActionResult> Delete(Guid branchId, Guid memberId)
    {
        var result = await memberUseCases.RemoveMember(new BranchId(branchId), new MemberId(memberId));

        return result.Outcome switch
        {
            UseCaseOutcome.NotFound => NotFound(),
            _ => NoContent(),
        };
    }

    [HttpPut("{memberId:guid}/photo")]
    public async Task<ActionResult<MemberDetailResponse>> SetPhotoFromForm(
        Guid branchId, Guid memberId, [FromForm] IFormFile file)
    {
        await using var content = file.OpenReadStream();
        return await SetPhoto(branchId, memberId, content, file.ContentType);
    }

    public async Task<ActionResult<MemberDetailResponse>> SetPhoto(
        Guid branchId, Guid memberId, Stream content, string contentType)
    {
        var result = await memberUseCases.SetMemberPhoto(
            new BranchId(branchId), new MemberId(memberId), content, contentType);

        return result.Outcome switch
        {
            UseCaseOutcome.NotFound => NotFound(),
            UseCaseOutcome.ValidationFailed => BadRequest(ApiProblemDetails.ValidationFailed(result.Errors)),
            _ => Ok(ToResponse(result.Value!)),
        };
    }

    private static MemberDetailResponse ToResponse(MemberDetail detail) =>
        new(
            detail.Id,
            detail.FirstName,
            detail.LastName,
            detail.PhotoPath,
            detail.Sports.Select(s => new SportRefResponse(s.Id, s.Name)).ToList());
}

using MembershipSystem.Api.Contracts;
using MembershipSystem.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace MembershipSystem.Api.Controllers;

[ApiController]
[Route("branches")]
public sealed class BranchesController(BranchUseCases branchUseCases) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BranchResponse>>> List()
    {
        var result = await branchUseCases.ListBranches();

        return Ok(result.Value!.Select(b => new BranchResponse(b.Id, b.Name)).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<BranchResponse>> Create([FromBody] CreateBranchRequest request)
    {
        var result = await branchUseCases.CreateBranch(request.Name);

        return result.Outcome switch
        {
            UseCaseOutcome.ValidationFailed => BadRequest(ApiProblemDetails.ValidationFailed(result.Errors)),
            _ => CreatedAtAction(
                nameof(List),
                null,
                new BranchResponse(result.Value!.Id, result.Value.Name)),
        };
    }
}

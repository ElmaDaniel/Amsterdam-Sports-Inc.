using MembershipSystem.Api.Contracts;
using MembershipSystem.Domain;
using MembershipSystem.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace MembershipSystem.Api.Controllers;

[ApiController]
[Route("branches/{branchId:guid}/sports")]
public sealed class SportsController(SportUseCases sportUseCases) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SportResponse>>> List(Guid branchId)
    {
        var result = await sportUseCases.ListSports(new BranchId(branchId));

        return result.Outcome switch
        {
            UseCaseOutcome.NotFound => NotFound(),
            _ => Ok(result.Value!.Select(s => new SportResponse(s.Id, s.Name)).ToList()),
        };
    }
}

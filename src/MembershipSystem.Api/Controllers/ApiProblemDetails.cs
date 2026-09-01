using Microsoft.AspNetCore.Mvc;

namespace MembershipSystem.Api.Controllers;

internal static class ApiProblemDetails
{
    public static ValidationProblemDetails ValidationFailed(IReadOnlyList<string> errors) =>
        new(new Dictionary<string, string[]> { ["errors"] = errors.ToArray() });
}

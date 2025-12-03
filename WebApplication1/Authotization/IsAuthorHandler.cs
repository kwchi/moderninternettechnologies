using Microsoft.AspNetCore.Authorization;
using WebApplicationData.Data;
using System.Security.Claims;

public class IsAuthorHandler : AuthorizationHandler<IsAuthorRequirement, AppResource>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        IsAuthorRequirement requirement,
        AppResource resource)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId != null && resource.AuthorId == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
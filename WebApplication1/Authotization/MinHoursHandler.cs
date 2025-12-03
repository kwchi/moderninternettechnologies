using Microsoft.AspNetCore.Authorization;

namespace WebApplication1.Authorization
{
    public class MinHoursHandler : AuthorizationHandler<MinHoursRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinHoursRequirement requirement)
        {
            var hoursClaim = context.User.FindFirst("WorkingHours");

            if (hoursClaim != null && int.TryParse(hoursClaim.Value, out int hours))
            {
                if (hours >= requirement.MinHours)
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
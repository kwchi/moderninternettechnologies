using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace WebApplication1.Authorization
{
    public class ForumAccessHandler : AuthorizationHandler<ForumAccessRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ForumAccessRequirement requirement)
        {
            bool hasAccess = context.User.HasClaim(c => c.Type == "IsMentor") ||
                             context.User.HasClaim(c => c.Type == "IsVerifiedUser") ||
                             context.User.HasClaim(c => c.Type == "HasForumAccess");

            if (hasAccess)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
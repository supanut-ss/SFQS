using System.Security.Claims;
using Freito.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Freito.Api.Controllers;

public abstract class ProtectedControllerBase : ControllerBase
{
    protected IActionResult? RequireActor(out int actorId, params UserRole[] allowedRoles)
    {
        actorId = 0;
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "Sign in with an internal account before changing operational data.",
                Status = StatusCodes.Status401Unauthorized,
            });
        }

        if (allowedRoles.Length > 0 && !allowedRoles.Any(role => User.IsInRole(role.ToString())))
        {
            return Forbid();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userId, out actorId) || actorId <= 0)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid user identity",
                Detail = "The authenticated account does not include a valid user ID.",
                Status = StatusCodes.Status401Unauthorized,
            });
        }

        return null;
    }

    protected IActionResult? RequireRoles(params UserRole[] allowedRoles)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Status = StatusCodes.Status401Unauthorized,
            });
        }

        return allowedRoles.Any(role => User.IsInRole(role.ToString())) ? null : Forbid();
    }
}

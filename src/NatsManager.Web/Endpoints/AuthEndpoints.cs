using System.Security.Claims;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Auth.Commands;
using NatsManager.Web.Presenters;

namespace NatsManager.Web.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .RequireAuthorization();

        group.MapPost("/login", Login).AllowAnonymous().DisableAntiforgery();
        group.MapPost("/logout", Logout);
        group.MapGet("/me", GetCurrentUser);
    }

    private static async Task<IResult> Login(LoginCommand command, IUseCase<LoginCommand, LoginResult> useCase, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<LoginResult>();
        await useCase.ExecuteAsync(command, presenter, cancellationToken);
        if (presenter.IsSuccess)
        {
            httpContext.Session.SetString("UserId", presenter.Value!.Id.ToString());
            httpContext.Session.SetString("Username", presenter.Value.Username);
            return Results.Ok(presenter.Value);
        }
        return presenter.ToResult();
    }

    private static IResult Logout(HttpContext httpContext)
    {
        httpContext.Session.Clear();
        return Results.Ok();
    }

    private static IResult GetCurrentUser(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        var displayName = httpContext.User.FindFirst("DisplayName")?.Value;
        var roles = httpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        return Results.Ok(new { Id = userId, Username = username, DisplayName = displayName, Roles = roles });
    }
}

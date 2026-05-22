using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Auth.Commands;
using NatsManager.Web.Configuration;
using NatsManager.Web.Presenters;
using NatsManager.Web.Security;

namespace NatsManager.Web.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .RequireAuthorization();

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .DisableAntiforgery()
            .RequireRateLimiting(RateLimitPolicyNames.Login);
        group.MapGet("/config", GetAuthConfig)
            .AllowAnonymous();
        group.MapGet("/oidc/login", LoginWithOidc)
            .AllowAnonymous();
        group.MapPost("/oidc/logout", LogoutWithOidc);
        group.MapPost("/logout", async (HttpContext httpContext) =>
        {
            httpContext.Session.Clear();
            await httpContext.SignOutAsync(NatsManagerAuthenticationSchemes.OidcCookie);
            return Results.Ok();
        });
        group.MapGet("/me", GetCurrentUser)
            .AllowAnonymous();
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

    private static IResult GetAuthConfig(IOptions<OidcOptions> options)
    {
        return Results.Ok(new AuthConfigResponse(
            OidcEnabled: options.Value.Enabled,
            OidcLoginPath: "/api/auth/oidc/login"));
    }

    private static IResult LoginWithOidc([FromQuery] string? returnUrl, IOptions<OidcOptions> options)
    {
        if (!options.Value.Enabled)
        {
            return Results.NotFound();
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = GetSafeReturnUrl(returnUrl, options.Value)
        };

        return Results.Challenge(properties, [OpenIdConnectDefaults.AuthenticationScheme]);
    }

    private static IResult LogoutWithOidc([FromQuery] string? returnUrl, HttpContext httpContext, IOptions<OidcOptions> options)
    {
        httpContext.Session.Clear();
        var redirectUri = GetSafeReturnUrl(returnUrl, options.Value);
        if (!options.Value.Enabled)
        {
            return Results.Redirect(redirectUri);
        }

        return Results.SignOut(
            new AuthenticationProperties { RedirectUri = redirectUri },
            [NatsManagerAuthenticationSchemes.OidcCookie, OpenIdConnectDefaults.AuthenticationScheme]);
    }

    private static IResult GetCurrentUser(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return Results.Content("null", "application/json", Encoding.UTF8, StatusCodes.Status200OK);
        }

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        var displayName = httpContext.User.FindFirst("DisplayName")?.Value;
        var roles = httpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var authProvider = string.Equals(
            httpContext.User.FindFirst(OidcClaimsAugmentor.AuthProviderClaimType)?.Value,
            "oidc",
            StringComparison.OrdinalIgnoreCase)
            ? "oidc"
            : "local";

        return Results.Ok(new { Id = userId, Username = username, DisplayName = displayName, Roles = roles, AuthProvider = authProvider });
    }

    private static string GetSafeReturnUrl(string? returnUrl, OidcOptions options)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (Uri.TryCreate(returnUrl, UriKind.Relative, out _) && !returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return returnUrl;
        }

        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var absoluteReturnUri))
        {
            return "/";
        }

        foreach (var origin in options.AllowedRedirectOrigins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var allowedOrigin))
            {
                continue;
            }

            if (string.Equals(absoluteReturnUri.Scheme, allowedOrigin.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(absoluteReturnUri.Host, allowedOrigin.Host, StringComparison.OrdinalIgnoreCase)
                && absoluteReturnUri.Port == allowedOrigin.Port)
            {
                return returnUrl;
            }
        }

        return "/";
    }

    private sealed record AuthConfigResponse(bool OidcEnabled, string OidcLoginPath);
}

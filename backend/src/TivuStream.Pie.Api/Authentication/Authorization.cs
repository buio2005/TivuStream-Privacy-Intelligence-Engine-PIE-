using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using TivuStream.Pie.Api.Contracts;
using TivuStream.Pie.Storage;

namespace TivuStream.Pie.Api.Authentication;

/// <summary>
/// What each endpoint requires of whoever calls it.
/// </summary>
/// <remarks>
/// Authentication Specification, Authorization. Every endpoint declares the
/// role it requires. One that does not requires <c>Administrator</c>:
/// forgetting a declaration produces a refusal, never an opening.
/// </remarks>
internal static class AuthorizationPolicies
{
    /// <summary>
    /// Any signed in account: the aggregated data.
    /// </summary>
    internal const string Viewer = "Viewer";

    /// <summary>
    /// Administrators only.
    /// </summary>
    internal const string Administrator = "Administrator";

    internal static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(
            Viewer,
            policy => policy.RequireRole(nameof(AccountRole.Viewer), nameof(AccountRole.Administrator)));

        options.AddPolicy(
            Administrator,
            policy => policy.RequireRole(nameof(AccountRole.Administrator)));

        // Applies to every endpoint that declares nothing.
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireRole(nameof(AccountRole.Administrator))
            .Build();
    }
}

/// <summary>
/// Answers a refused request in the standard envelope.
/// </summary>
/// <remarks>
/// The default answer of the framework is a bare status, or a redirect to a
/// page that does not exist. A client of the API expects the same structure
/// as any other answer, and the browser must never be sent to a login dialog
/// of its own.
/// </remarks>
internal sealed class AuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            return Results.Json(
                ApiResponse.Failed<object>("AuthenticationRequired", "Authentication is required."),
                statusCode: StatusCodes.Status401Unauthorized).ExecuteAsync(context);
        }

        if (authorizeResult.Forbidden)
        {
            return Results.Json(
                ApiResponse.Failed<object>("Forbidden", "The role of this account does not allow this."),
                statusCode: StatusCodes.Status403Forbidden).ExecuteAsync(context);
        }

        return next(context);
    }
}

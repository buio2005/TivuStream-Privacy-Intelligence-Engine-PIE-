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
/// <para>
/// A password that must be changed blocks every policy but one. The
/// requirement is part of the fallback too, so that an endpoint added without
/// a declaration cannot become the way around it.
/// </para>
/// </remarks>
internal static class AuthorizationPolicies
{
    /// <summary>
    /// Any signed in account, even one whose password must be changed first:
    /// what is needed to change it, to see the session and to leave.
    /// </summary>
    internal const string SignedIn = "SignedIn";

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
            SignedIn,
            policy => policy.RequireRole(nameof(AccountRole.Viewer), nameof(AccountRole.Administrator)));

        options.AddPolicy(
            Viewer,
            policy => policy
                .RequireRole(nameof(AccountRole.Viewer), nameof(AccountRole.Administrator))
                .AddRequirements(PasswordSettledRequirement.Instance));

        options.AddPolicy(
            Administrator,
            policy => policy
                .RequireRole(nameof(AccountRole.Administrator))
                .AddRequirements(PasswordSettledRequirement.Instance));

        // Applies to every endpoint that declares nothing.
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireRole(nameof(AccountRole.Administrator))
            .AddRequirements(PasswordSettledRequirement.Instance)
            .Build();
    }
}

/// <summary>
/// Met when the account signed in has no password waiting to be changed.
/// </summary>
internal sealed class PasswordSettledRequirement : AuthorizationHandler<PasswordSettledRequirement>, IAuthorizationRequirement
{
    /// <summary>
    /// Claim present on the identity of an account that must change its
    /// password before anything else.
    /// </summary>
    internal const string PendingClaim = "pie:password_change_required";

    internal static readonly PasswordSettledRequirement Instance = new();

    private PasswordSettledRequirement()
    {
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PasswordSettledRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true && !context.User.HasClaim(claim => claim.Type == PendingClaim))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
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
            // On an installation with no account, "authenticate" would be an
            // instruction nobody can follow. What is needed is the first one.
            bool setupRequired = context.RequestServices.GetRequiredService<SetupService>().IsRequired;

            return Results.Json(
                setupRequired
                    ? ApiResponse.Failed<object>("SetupRequired", "The installation has no account yet.")
                    : ApiResponse.Failed<object>("AuthenticationRequired", "Authentication is required."),
                statusCode: StatusCodes.Status401Unauthorized).ExecuteAsync(context);
        }

        if (authorizeResult.Forbidden)
        {
            // The password comes before anything else, including what the
            // role would not allow: "forbidden" tells someone who can do
            // nothing yet nothing useful.
            bool passwordPending = authorizeResult.AuthorizationFailure?.FailedRequirements
                .OfType<PasswordSettledRequirement>()
                .Any() == true;

            return Results.Json(
                passwordPending
                    ? ApiResponse.Failed<object>("PasswordChangeRequired", "The password must be changed first.")
                    : ApiResponse.Failed<object>("Forbidden", "The role of this account does not allow this."),
                statusCode: StatusCodes.Status403Forbidden).ExecuteAsync(context);
        }

        return next(context);
    }
}

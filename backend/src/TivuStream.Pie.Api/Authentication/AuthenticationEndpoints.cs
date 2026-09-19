using TivuStream.Pie.Api.Contracts;
using TivuStream.Pie.Storage;

namespace TivuStream.Pie.Api.Authentication;

/// <summary>
/// Body of a request to sign in.
/// </summary>
/// <param name="Username">Name of the account, in any case.</param>
/// <param name="Password">Password as it was typed.</param>
internal sealed record LoginRequest(string? Username, string? Password);

/// <summary>
/// Who is signed in, as the interface needs to know it.
/// </summary>
internal sealed record AccountInfo
{
    public required string Username { get; init; }

    public required AccountRole Role { get; init; }

    public required bool PasswordChangeRequired { get; init; }

    /// <summary>
    /// The earlier of the two instants at which the session would end.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}

/// <summary>
/// The endpoints that open, close and describe a session.
/// </summary>
/// <remarks>
/// <c>login</c> is one of the two endpoints reachable without credentials,
/// because it is how they are obtained. It returns nothing about the network
/// or the system.
/// </remarks>
internal static class AuthenticationEndpoints
{
    // The same words for every reason a sign in can fail. What the reason was
    // is nobody's business but the account's owner.
    private const string RefusedMessage = "The credentials were not accepted.";

    internal static void MapAuthentication(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/api/v1/auth");

        group.MapPost("/login", SignIn).AllowAnonymous();

        group.MapPost("/logout", SignOut).RequireAuthorization(AuthorizationPolicies.Viewer);

        group.MapGet("/session", Describe).RequireAuthorization(AuthorizationPolicies.Viewer);
    }

    private static IResult SignIn(
        LoginRequest? request,
        HttpContext context,
        CredentialVerifier credentials,
        SessionService sessions)
    {
        StoredAccount? account = credentials.Verify(request?.Username, request?.Password);

        if (account is null)
        {
            return Results.Json(
                ApiResponse.Failed<AccountInfo>("AuthenticationFailed", RefusedMessage),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        // Signing in again where a session is already open replaces it. The
        // identifier changes at every sign in, and the old one stops working.
        string? previous = SessionCookie.Read(context);

        if (previous is not null)
        {
            sessions.EndByToken(previous);
        }

        (string token, DateTimeOffset expiresAt) = sessions.Start(account);

        SessionCookie.Write(context, token);

        return Results.Ok(ApiResponse.Ok(ToInfo(account, expiresAt)));
    }

    private static IResult SignOut(HttpContext context, SessionService sessions)
    {
        sessions.End(CurrentSession(context).SessionId);

        SessionCookie.Clear(context);

        return Results.Ok(ApiResponse.Ok<object?>(null));
    }

    private static IResult Describe(HttpContext context)
    {
        SessionContext session = CurrentSession(context);

        return Results.Ok(ApiResponse.Ok(ToInfo(session.Account, session.ExpiresAt)));
    }

    private static AccountInfo ToInfo(StoredAccount account, DateTimeOffset expiresAt)
    {
        return new AccountInfo
        {
            Username = account.Username,
            Role = account.Role,
            PasswordChangeRequired = account.PasswordChangeRequired,
            ExpiresAt = expiresAt,
        };
    }

    private static SessionContext CurrentSession(HttpContext context)
    {
        // Present whenever the authorization of the endpoint has passed: the
        // handler that recognised the person put it there.
        return (SessionContext)context.Items[SessionAuthenticationHandler.SessionItem]!;
    }
}

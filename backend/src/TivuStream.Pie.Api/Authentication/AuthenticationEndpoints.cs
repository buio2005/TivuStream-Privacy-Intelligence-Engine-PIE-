using System.Globalization;
using System.Net;
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
/// Body of a request to create the first administrator.
/// </summary>
/// <param name="SetupCode">Code shown on the standard output of the process.</param>
/// <param name="Username">Name the administrator will have.</param>
/// <param name="Password">Password the administrator will have.</param>
internal sealed record SetupRequest(string? SetupCode, string? Username, string? Password);

/// <summary>
/// Body of a request to change one's own password.
/// </summary>
/// <param name="CurrentPassword">The password in use.</param>
/// <param name="NewPassword">The password that replaces it.</param>
internal sealed record PasswordChangeRequest(string? CurrentPassword, string? NewPassword);

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
/// <c>login</c> and <c>setup</c> are the two endpoints reachable without
/// credentials, because they are how credentials are obtained. Neither returns
/// anything about the network or the system.
/// </remarks>
internal static class AuthenticationEndpoints
{
    // The same words for every reason a sign in can fail. What the reason was
    // is nobody's business but the account's owner.
    private const string RefusedMessage = "The credentials were not accepted.";

    internal static void MapAuthentication(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/v1/setup", Setup).AllowAnonymous();

        RouteGroupBuilder group = routes.MapGroup("/api/v1/auth");

        group.MapPost("/login", SignIn).AllowAnonymous();

        // Reachable while the password must be changed: they are how it gets
        // changed, how the person learns that it must be, and how they leave.
        group.MapPost("/logout", SignOut).RequireAuthorization(AuthorizationPolicies.SignedIn);

        group.MapGet("/session", Describe).RequireAuthorization(AuthorizationPolicies.SignedIn);

        group.MapPost("/password", ChangePassword).RequireAuthorization(AuthorizationPolicies.SignedIn);
    }

    private static IResult Setup(
        SetupRequest? request,
        HttpContext context,
        SetupService setup,
        SessionService sessions,
        AttemptLimiter attempts,
        ILoggerFactory loggers)
    {
        if (!CredentialTransport.IsSuitable(context))
        {
            return CredentialTransport.Refusal();
        }

        IPAddress? source = context.Connection.RemoteIpAddress;

        if (attempts.RetryAfter(source, account: null) is TimeSpan wait)
        {
            return TooManyAttempts(context, wait);
        }

        SetupResult result = setup.Complete(request?.SetupCode, request?.Username, request?.Password);

        switch (result.Outcome)
        {
            case SetupOutcome.CodeRejected:
                attempts.RecordFailure(source, account: null);

                return Results.Json(
                    ApiResponse.Failed<AccountInfo>("SetupCodeRejected", "The setup code was not accepted."),
                    statusCode: StatusCodes.Status401Unauthorized);

            case SetupOutcome.UsernameRejected:
                return Results.Json(
                    ApiResponse.Failed<AccountInfo>("UsernameRejected", "The name is not acceptable."),
                    statusCode: StatusCodes.Status422UnprocessableEntity);

            case SetupOutcome.PasswordRejected:
                return Results.Json(
                    ApiResponse.Failed<AccountInfo>(
                        "PasswordRejected",
                        "The password is not acceptable.",
                        result.PasswordProblem.ToString()),
                    statusCode: StatusCodes.Status422UnprocessableEntity);

            case SetupOutcome.AlreadyCompleted:
                return Results.Json(
                    ApiResponse.Failed<AccountInfo>("SetupAlreadyCompleted", "The installation already has an account."),
                    statusCode: StatusCodes.Status409Conflict);
        }

        ILogger log = AuthenticationLog.For(loggers);
        AuthenticationLog.SetupCompleted(log, result.Account!.Username);

        // The person who has just created the administrator is signed in.
        (string token, DateTimeOffset expiresAt) = sessions.Start(result.Account!);

        SessionCookie.Write(context, token);

        return Results.Json(
            ApiResponse.Ok(ToInfo(result.Account!, expiresAt)),
            statusCode: StatusCodes.Status201Created);
    }

    private static IResult SignIn(
        LoginRequest? request,
        HttpContext context,
        SetupService setup,
        CredentialVerifier credentials,
        SessionService sessions,
        AttemptLimiter attempts,
        ILoggerFactory loggers)
    {
        if (!CredentialTransport.IsSuitable(context))
        {
            return CredentialTransport.Refusal();
        }

        IPAddress? source = context.Connection.RemoteIpAddress;

        // Counted under the name tried, whether or not it exists: slowing down
        // only the names that exist would tell which ones do.
        string name = AccountPolicy.CanonicalUsername(request?.Username);

        // While the delay lasts the credentials are not checked, not even
        // right ones: otherwise the delay would slow nobody down.
        if (attempts.RetryAfter(source, name) is TimeSpan wait)
        {
            return TooManyAttempts(context, wait);
        }

        // Nobody can sign in to an installation that has no account, and
        // saying that the credentials were wrong would be untrue.
        if (setup.IsRequired)
        {
            return Results.Json(
                ApiResponse.Failed<AccountInfo>("SetupRequired", "The installation has no account yet."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        StoredAccount? account = credentials.Verify(request?.Username, request?.Password);

        if (account is null)
        {
            attempts.RecordFailure(source, name);

            // A count, not the name: a password typed into the name field must
            // not end up in the logs.
            ILogger refusalLog = AuthenticationLog.For(loggers);
            AuthenticationLog.SignInRefused(refusalLog, credentials.Refusals);

            return Results.Json(
                ApiResponse.Failed<AccountInfo>("AuthenticationFailed", RefusedMessage),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        attempts.RecordSuccess(name);

        ILogger log = AuthenticationLog.For(loggers);
        AuthenticationLog.SignedIn(log, account.Username);

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

    private static IResult ChangePassword(
        PasswordChangeRequest? request,
        HttpContext context,
        PasswordHasher hasher,
        AccountRepository accounts,
        SessionService sessions,
        AttemptLimiter attempts,
        ILoggerFactory loggers)
    {
        if (!CredentialTransport.IsSuitable(context))
        {
            return CredentialTransport.Refusal();
        }

        SessionContext session = CurrentSession(context);
        StoredAccount account = session.Account;
        IPAddress? source = context.Connection.RemoteIpAddress;

        // Whoever finds a session left open must not be able to try passwords
        // on it without limit.
        if (attempts.RetryAfter(source, account.Username) is TimeSpan wait)
        {
            return TooManyAttempts(context, wait);
        }

        string newPassword = request?.NewPassword ?? string.Empty;

        // An open session left on a browser must not be enough to take the
        // account. Not a 401: the session is valid, and the interface reads a
        // 401 as the session having ended.
        if (hasher.Verify(request?.CurrentPassword ?? string.Empty, account.PasswordHash) == PasswordVerification.Failed)
        {
            attempts.RecordFailure(source, account.Username);

            return Results.Json(
                ApiResponse.Failed<AccountInfo>("CurrentPasswordRejected", "The current password was not accepted."),
                statusCode: StatusCodes.Status403Forbidden);
        }

        // The secret was proven, as by a sign in.
        attempts.RecordSuccess(account.Username);

        // Putting back the same password would satisfy the obligation to
        // change it while leaving it known to whoever set it.
        PasswordProblem? problem = AccountPolicy.CheckPassword(newPassword, account.Username)
            ?? (hasher.Verify(newPassword, account.PasswordHash) == PasswordVerification.Failed
                ? null
                : PasswordProblem.Unchanged);

        if (problem is not null)
        {
            return Results.Json(
                ApiResponse.Failed<AccountInfo>("PasswordRejected", "The password is not acceptable.", problem.ToString()),
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        accounts.ReplacePasswordHash(account.Id, hasher.Hash(newPassword), passwordChangeRequired: false);

        // Every other place the account was signed in stops working. This one,
        // where the change was made, goes on.
        sessions.EndAllFor(account.Id, session.SessionId);

        ILogger log = AuthenticationLog.For(loggers);
        AuthenticationLog.PasswordChanged(log, account.Username);

        return Results.Ok(ApiResponse.Ok(ToInfo(account with { PasswordChangeRequired = false }, session.ExpiresAt)));
    }

    private static IResult Describe(HttpContext context)
    {
        SessionContext session = CurrentSession(context);

        return Results.Ok(ApiResponse.Ok(ToInfo(session.Account, session.ExpiresAt)));
    }

    private static IResult TooManyAttempts(HttpContext context, TimeSpan wait)
    {
        int seconds = Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds));

        context.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);

        return Results.Json(
            ApiResponse.Failed<AccountInfo>("TooManyAttempts", "Too many attempts. Try again later."),
            statusCode: StatusCodes.Status429TooManyRequests);
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

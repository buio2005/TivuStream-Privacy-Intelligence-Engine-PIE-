using TivuStream.Pie.Api.Contracts;
using TivuStream.Pie.Storage;

namespace TivuStream.Pie.Api.Authentication;

/// <summary>
/// An account, as an administrator sees it.
/// </summary>
/// <remarks>
/// The hash of the password never leaves the Backend.
/// </remarks>
internal sealed record AccountSummary
{
    public required string Username { get; init; }

    public required AccountRole Role { get; init; }

    public required bool Enabled { get; init; }

    public required bool PasswordChangeRequired { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    internal static AccountSummary Of(StoredAccount account)
    {
        return new AccountSummary
        {
            Username = account.Username,
            Role = account.Role,
            Enabled = account.Enabled,
            PasswordChangeRequired = account.PasswordChangeRequired,
            CreatedAt = account.CreatedAt,
        };
    }
}

/// <summary>
/// Body of a request to create an account.
/// </summary>
/// <param name="Username">Name the account will have.</param>
/// <param name="Role">Role by name: <c>Administrator</c> or <c>Viewer</c>.</param>
/// <param name="Password">Initial password, to be changed at the first sign in.</param>
internal sealed record AccountCreationRequest(string? Username, string? Role, string? Password);

/// <summary>
/// Body of a request to change an account. What is left out stays as it is.
/// </summary>
/// <param name="Role">New role by name.</param>
/// <param name="Enabled">Whether the account may sign in.</param>
/// <param name="Password">New password, set by the administrator: a reset.</param>
internal sealed record AccountChangeRequest(string? Role, bool? Enabled, string? Password);

/// <summary>
/// The endpoints with which an administrator manages the accounts.
/// </summary>
/// <remarks>
/// Authentication Specification, Account management. The constraint that an
/// administrator able to sign in always exists is kept by the repository,
/// in the same transaction as the change, and only reported here.
/// </remarks>
internal static class AccountEndpoints
{
    internal static void MapAccounts(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes
            .MapGroup("/api/v1/accounts")
            .RequireAuthorization(AuthorizationPolicies.Administrator);

        group.MapGet(string.Empty, List);
        group.MapPost(string.Empty, Create);
        group.MapPatch("/{username}", Change);
        group.MapDelete("/{username}", Delete);
    }

    private static IResult List(AccountRepository accounts)
    {
        return Results.Ok(ApiResponse.Ok(accounts.List().Select(AccountSummary.Of).ToList()));
    }

    private static IResult Create(
        AccountCreationRequest? request,
        HttpContext context,
        AccountRepository accounts,
        PasswordHasher hasher,
        TimeProvider time)
    {
        // The initial password is a password like any other (D9).
        if (!CredentialTransport.IsSuitable(context))
        {
            return CredentialTransport.Refusal();
        }

        string name = AccountPolicy.CanonicalUsername(request?.Username);

        if (!AccountPolicy.IsValidUsername(name))
        {
            return UsernameRejected();
        }

        if (!AccountPolicy.TryParseRole(request?.Role, out AccountRole role))
        {
            return RoleRejected();
        }

        string password = request?.Password ?? string.Empty;

        if (AccountPolicy.CheckPassword(password, name) is PasswordProblem problem)
        {
            return PasswordRejected(problem);
        }

        // Whoever chose the password is not the person who will use it, and
        // must stop knowing it as soon as that person signs in.
        StoredAccount? created = accounts.Create(name, role, hasher.Hash(password), passwordChangeRequired: true, time.GetUtcNow());

        if (created is null)
        {
            return Results.Json(
                ApiResponse.Failed<AccountSummary>("AccountExists", "An account with that name already exists."),
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Json(ApiResponse.Ok(AccountSummary.Of(created)), statusCode: StatusCodes.Status201Created);
    }

    private static IResult Change(
        string username,
        AccountChangeRequest? request,
        HttpContext context,
        AccountRepository accounts,
        PasswordHasher hasher,
        SessionService sessions)
    {
        if (request?.Password is not null && !CredentialTransport.IsSuitable(context))
        {
            return CredentialTransport.Refusal();
        }

        string name = AccountPolicy.CanonicalUsername(username);

        AccountRole? role = null;

        if (request?.Role is not null)
        {
            if (!AccountPolicy.TryParseRole(request.Role, out AccountRole parsed))
            {
                return RoleRejected();
            }

            role = parsed;
        }

        string? passwordHash = null;

        if (request?.Password is not null)
        {
            if (AccountPolicy.CheckPassword(request.Password, name) is PasswordProblem problem)
            {
                return PasswordRejected(problem);
            }

            passwordHash = hasher.Hash(request.Password);
        }

        // Everything the request carries is applied together or not at all.
        (AccountChangeOutcome outcome, StoredAccount? changed) = accounts.Change(
            name,
            new AccountChange
            {
                Role = role,
                Enabled = request?.Enabled,
                PasswordHash = passwordHash,

                // A password set by an administrator is known to the
                // administrator. It is a starting point, not a password.
                PasswordChangeRequired = passwordHash is null ? null : true,
            });

        if (outcome != AccountChangeOutcome.Changed)
        {
            return Refusal<AccountSummary>(outcome);
        }

        // Whoever had the old password, or had the account, may be the one
        // holding a session. Every session of the account ends, the caller's
        // own included when the account is theirs.
        if (passwordHash is not null || request?.Enabled == false)
        {
            sessions.EndAllFor(changed!.Id, exceptSessionId: null);
        }

        return Results.Ok(ApiResponse.Ok(AccountSummary.Of(changed!)));
    }

    private static IResult Delete(string username, AccountRepository accounts)
    {
        AccountChangeOutcome outcome = accounts.Delete(AccountPolicy.CanonicalUsername(username));

        return outcome == AccountChangeOutcome.Changed
            ? Results.Ok(ApiResponse.Ok<object?>(null))
            : Refusal<object>(outcome);
    }

    private static IResult Refusal<TData>(AccountChangeOutcome outcome)
    {
        return outcome == AccountChangeOutcome.NotFound
            ? Results.Json(
                ApiResponse.Failed<TData>("AccountNotFound", "No account has that name."),
                statusCode: StatusCodes.Status404NotFound)
            : Results.Json(
                ApiResponse.Failed<TData>(
                    "LastAdministrator",
                    "The installation would be left without an administrator able to sign in."),
                statusCode: StatusCodes.Status409Conflict);
    }

    private static IResult UsernameRejected()
    {
        return Results.Json(
            ApiResponse.Failed<AccountSummary>("UsernameRejected", "The name is not acceptable."),
            statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    private static IResult RoleRejected()
    {
        return Results.Json(
            ApiResponse.Failed<AccountSummary>("RoleRejected", "The role must be Administrator or Viewer."),
            statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    private static IResult PasswordRejected(PasswordProblem problem)
    {
        return Results.Json(
            ApiResponse.Failed<AccountSummary>("PasswordRejected", "The password is not acceptable.", problem.ToString()),
            statusCode: StatusCodes.Status422UnprocessableEntity);
    }
}

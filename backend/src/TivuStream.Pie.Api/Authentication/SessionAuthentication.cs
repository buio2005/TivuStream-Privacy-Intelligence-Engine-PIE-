using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TivuStream.Pie.Api.Authentication;

/// <summary>
/// The cookie that carries the session, and the way it is written.
/// </summary>
/// <remarks>
/// Authentication Specification, Sessions. The browser never handles the
/// identifier in code the page can run: the cookie is not readable by script.
/// It is sent only to this site, only to the API, and only over an encrypted
/// connection when there is one.
/// <para>
/// It carries no expiry of its own. It ends with the browser, and the
/// server-side limits are ceilings on top of that, not a promise to stay
/// signed in.
/// </para>
/// </remarks>
internal static class SessionCookie
{
    internal const string Name = "pie_session";

    internal static void Write(HttpContext context, string token)
    {
        context.Response.Cookies.Append(Name, token, Options(context));
    }

    internal static void Clear(HttpContext context)
    {
        context.Response.Cookies.Delete(Name, Options(context));
    }

    internal static string? Read(HttpContext context)
    {
        return context.Request.Cookies.TryGetValue(Name, out string? token) && !string.IsNullOrEmpty(token)
            ? token
            : null;
    }

    private static CookieOptions Options(HttpContext context)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api",
            Secure = context.Request.IsHttps,
            IsEssential = true,
        };
    }
}

/// <summary>
/// Recognises the person behind a request from the session cookie.
/// </summary>
/// <remarks>
/// A cookie that is absent, unknown, expired or of a disabled account yields
/// no identity at all. It is not a failure of a different kind: to whoever
/// asks it is the same as never having signed in.
/// </remarks>
internal sealed class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    internal const string SchemeName = "PieSession";

    internal const string SessionItem = "Pie.Session";

    private readonly SessionService _sessions;

    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        SessionService sessions)
        : base(options, logger, encoder)
    {
        _sessions = sessions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token = SessionCookie.Read(Context);

        SessionContext? session = token is null ? null : _sessions.Validate(token);

        if (session is null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Kept for the endpoints that act on the session itself.
        Context.Items[SessionItem] = session;

        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, session.Account.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, session.Account.Username),
            new Claim(ClaimTypes.Role, session.Account.Role.ToString()),
        ];

        if (session.Account.PasswordChangeRequired)
        {
            claims.Add(new Claim(PasswordSettledRequirement.PendingClaim, "true"));
        }

        ClaimsIdentity identity = new(claims, SchemeName);

        return Task.FromResult(
            AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

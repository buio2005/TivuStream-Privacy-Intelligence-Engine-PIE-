using System.Net;
using TivuStream.Pie.Api.Contracts;

namespace TivuStream.Pie.Api.Authentication;

/// <summary>
/// Decides whether a password may be received over the connection a request
/// came on.
/// </summary>
/// <remarks>
/// Authentication Specification, Transport. Only over an encrypted connection,
/// or from the loopback. The criterion is the remote address of the
/// connection, not the <c>Host</c> header, which whoever writes the request
/// controls. An address that is unknown is not the loopback.
/// </remarks>
internal static class CredentialTransport
{
    internal static bool IsSuitable(HttpContext context)
    {
        if (context.Request.IsHttps)
        {
            return true;
        }

        IPAddress? remote = context.Connection.RemoteIpAddress;

        if (remote is null)
        {
            return false;
        }

        // The framework recognises ::ffff:127.0.0.1 but not the rest of
        // 127.0.0.0/8 written as IPv6, which is the loopback just the same.
        return IPAddress.IsLoopback(remote.IsIPv4MappedToIPv6 ? remote.MapToIPv4() : remote);
    }

    /// <summary>
    /// The answer to a password sent where it should not have been.
    /// </summary>
    internal static IResult Refusal()
    {
        return Results.Json(
            ApiResponse.Failed<object>(
                "TransportNotSecure",
                "Credentials cannot be sent over an unencrypted connection."),
            statusCode: StatusCodes.Status403Forbidden);
    }
}

/// <summary>
/// The names under which the installation may be reached.
/// </summary>
/// <remarks>
/// Authentication Specification, Transport. A request addressed to any other
/// name is refused by the host filter of the framework. With <c>*</c>, a site
/// that points its own name at the local address (DNS rebinding) would reach
/// the API through the administrator's browser.
/// <para>
/// Without a setting only the loopback names are accepted: a line missing
/// from a configuration file must not open anything. <c>*</c> stops the start:
/// a setting that switches a protection off is the first one somebody leaves
/// on for convenience.
/// </para>
/// </remarks>
internal static class HostPolicy
{
    internal const string SettingName = "AllowedHosts";

    internal const string Default = "localhost;127.0.0.1;[::1]";

    /// <summary>
    /// The names to accept, as the configuration states them.
    /// </summary>
    /// <exception cref="InvalidOperationException">The configuration accepts any name.</exception>
    internal static string[] AllowedHosts(IConfiguration configuration)
    {
        string configured = configuration[SettingName] is { Length: > 0 } value ? value : Default;

        string[] hosts = configured.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (hosts.Length == 0 || hosts.Contains("*"))
        {
            throw new InvalidOperationException(
                $"'{SettingName}' must list the names this installation is reached by, separated by ';'. "
                + "'*' would let any site reach the API through a browser on this network.");
        }

        return hosts;
    }
}

/// <summary>
/// The headers and checks that protect every answer of the API from being
/// used by another site.
/// </summary>
/// <remarks>
/// Authentication Specification, Headers and Transport.
/// </remarks>
internal static class RequestProtection
{
    internal static void UseRequestProtection(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            // Nothing served here may be framed by another site.
            context.Response.Headers.ContentSecurityPolicy = "frame-ancestors 'none'";

            if (context.Request.IsHttps)
            {
                context.Response.Headers.StrictTransportSecurity = "max-age=31536000";
            }

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // Every answer of the API carries data about the network, and
                // none of it may stay in the cache of a browser or of anything
                // between.
                context.Response.Headers.CacheControl = "no-store";

                if (!OriginAllowed(context.Request))
                {
                    await Results.Json(
                        ApiResponse.Failed<object>(
                            "OriginNotAllowed",
                            "Requests that change data are accepted only from this service's own address."),
                        statusCode: StatusCodes.Status403Forbidden).ExecuteAsync(context);

                    return;
                }
            }

            await next(context);
        });
    }

    /// <summary>
    /// Indicates whether a request may change data as far as its origin goes.
    /// </summary>
    /// <remarks>
    /// A page of another site can make the browser send a request that
    /// changes data, with the cookie. <c>SameSite=Strict</c> keeps the cookie
    /// out; this keeps the request out even where a browser would not.
    /// <para>
    /// A request without <c>Origin</c> is accepted: browsers send it on every
    /// request that changes data, and a client that does not is not a browser
    /// carrying somebody's session.
    /// </para>
    /// </remarks>
    private static bool OriginAllowed(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method)
            || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method)
            || HttpMethods.IsTrace(request.Method))
        {
            return true;
        }

        string? origin = request.Headers.Origin;

        if (origin is null)
        {
            return true;
        }

        return string.Equals(origin, $"{request.Scheme}://{request.Host}", StringComparison.OrdinalIgnoreCase);
    }
}

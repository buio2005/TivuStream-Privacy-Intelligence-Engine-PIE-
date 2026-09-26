using Microsoft.Net.Http.Headers;
using TivuStream.Pie.Api.Contracts;

namespace TivuStream.Pie.Api;

/// <summary>
/// Serves the compiled interface on the address of the API.
/// </summary>
/// <remarks>
/// Transport Security Specification, The Interface Served By The Engine. One
/// address for both: one certificate, no rules between origins.
/// <para>
/// Decided after routing rather than with fallback endpoints. A fallback
/// endpoint under /api would take the place of the framework's answer to a
/// wrong method or content type, and one for the interface would miss the
/// addresses of domains, whose dots make them look like missing files.
/// </para>
/// </remarks>
internal static class InterfaceHosting
{
    private const string Page = "/index.html";

    internal static void UseInterface(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.GetEndpoint() is null)
            {
                // An API address that does not exist is an API error, never
                // the page: a client expecting data must not be handed HTML.
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    await Results.Json(
                        ApiResponse.Failed<object>("NotFound", "No such endpoint."),
                        statusCode: StatusCodes.Status404NotFound).ExecuteAsync(context);

                    return;
                }

                // The routes of the interface exist only in the browser. What
                // is not a file of the interface receives its page, which
                // decides what to show.
                if ((HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
                    && !app.Environment.WebRootFileProvider.GetFileInfo(context.Request.Path.Value ?? "/").Exists)
                {
                    context.Request.Path = Page;
                }
            }

            await next(context);
        });

        app.UseStaticFiles(new StaticFileOptions
        {
            // Never cached, so that an update reaches the browser at the next
            // visit. The other files carry a hash in their name.
            OnPrepareResponse = file =>
            {
                if (file.Context.Request.Path.Equals(Page, StringComparison.Ordinal))
                {
                    file.Context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
                }
            },
        });
    }
}

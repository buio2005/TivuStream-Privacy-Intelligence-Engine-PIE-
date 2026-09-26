using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TivuStream.Pie.Api.Authentication;
using TivuStream.Pie.Storage;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// What an endpoint answered.
/// </summary>
internal sealed record Answer(HttpStatusCode Status, JsonElement Body, string Raw, HttpResponseHeaders Headers)
{
    internal void Deconstruct(out HttpStatusCode status, out JsonElement body, out string raw)
    {
        status = Status;
        body = Body;
        raw = Raw;
    }
}

/// <summary>
/// A clock that stands still until it is told to move.
/// </summary>
internal sealed class TestClock : TimeProvider
{
    private DateTimeOffset _now;

    internal TestClock(DateTimeOffset start)
    {
        _now = start;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _now;
    }

    internal void Advance(TimeSpan by)
    {
        _now += by;
    }
}

/// <summary>
/// Collects what the host writes to its logs, so that a test can look for what
/// must never be there.
/// </summary>
internal sealed class CollectingLoggerProvider : ILoggerProvider
{
    private readonly List<string> _sink;

    internal CollectingLoggerProvider(List<string> sink)
    {
        _sink = sink;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new Collector(_sink);
    }

    public void Dispose()
    {
    }

    private sealed class Collector : ILogger
    {
        private readonly List<string> _sink;

        internal Collector(List<string> sink)
        {
            _sink = sink;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (_sink)
            {
                _sink.Add(formatter(state, exception));
            }
        }
    }
}

/// <summary>
/// The real host, in memory, on a database of its own.
/// </summary>
/// <remarks>
/// Two things are taken away on purpose. The background services would reach
/// the Data Source and download classification lists from the Internet, and a
/// test must do neither. And the developer's own <c>appsettings.Local.json</c>
/// must not be read: it holds a real token, and pointing the content root at
/// an empty folder is what keeps it out.
/// <para>
/// Passwords are hashed with far fewer iterations than the real ones. What a
/// test checks is what the endpoints do with a password, not how long the
/// computation takes, and two hundred milliseconds per sign in would make the
/// suite wait for nothing.
/// </para>
/// </remarks>
internal sealed class PieApplication : WebApplicationFactory<Program>
{
    /// <summary>
    /// Credential configured for the Data Source. It must never appear in an
    /// answer.
    /// </summary>
    internal const string SourceToken = "source-token-that-must-never-leak";

    /// <summary>
    /// Password every test account is given, unless a test says otherwise.
    /// </summary>
    internal const string Password = "a-password-for-the-tests";

    /// <summary>
    /// Header with which a test says where its request comes from. Without it
    /// a request comes from the loopback, as the real host sees a local browser.
    /// </summary>
    internal const string RemoteAddressHeader = "X-Test-Remote-Address";

    /// <summary>
    /// The page of the stand-in interface.
    /// </summary>
    internal const string InterfacePage = "<!doctype html><title>stand-in interface</title>";

    /// <summary>
    /// A file of the stand-in interface.
    /// </summary>
    internal const string InterfaceScript = "console.log('stand-in script')";

    /// <summary>
    /// The instant the host believes it is at the start: half past noon.
    /// </summary>
    internal static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 30, 0, TimeSpan.Zero);

    private const int TestIterations = 1_000;

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "pie-api-" + Guid.NewGuid().ToString("N"));

    private HttpClient? _administrator;

    /// <summary>
    /// The clock the host reads. Moving it moves time for every session.
    /// </summary>
    internal TestClock Clock { get; } = new(Now);

    /// <summary>
    /// What the host writes to its standard output: where the setup code goes.
    /// </summary>
    internal StringWriter Output { get; } = new();

    /// <summary>
    /// What the host writes to its logs.
    /// </summary>
    internal List<string> Logs { get; } = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_directory);

        // A stand-in for the compiled interface, where the host looks for it.
        string interfaceRoot = Directory.CreateDirectory(Path.Combine(_directory, "wwwroot", "assets")).Parent!.FullName;
        File.WriteAllText(Path.Combine(interfaceRoot, "index.html"), InterfacePage);
        File.WriteAllText(Path.Combine(interfaceRoot, "assets", "app.js"), InterfaceScript);

        builder.UseContentRoot(_directory);

        // As `dotnet run` starts. In Development the framework would also
        // serve the interface compiled into the project, and the tests would
        // depend on whether anyone had built it.
        builder.UseEnvironment(Environments.Production);

        // The encrypted channel closed: what the tests accept must not depend
        // on the names of the machine they run on. The channel has tests of
        // its own.
        builder.UseSetting("Transport:HttpsPort", "0");

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DatabasePath"] = Path.Combine(_directory, "pie.db"),
                ["Storage:ListDirectoryPath"] = Path.Combine(_directory, "lists"),
                ["Technitium:BaseAddress"] = "http://source.invalid",
                ["Technitium:ApiToken"] = SourceToken,
            }));

        builder.ConfigureLogging(logging => logging.AddProvider(new CollectingLoggerProvider(Logs)));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<SetupOutput>();
            services.AddSingleton(new SetupOutput(Output));

            services.RemoveAll<IHostedService>();
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

            services.RemoveAll<PasswordHasher>();
            services.AddSingleton(new PasswordHasher(TestIterations));

            services.AddSingleton<IStartupFilter, RemoteAddressFilter>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        _administrator?.Dispose();

        // Pooled connections hold the file open. Only this database's: other tests
        // run at the same time on databases of their own.
        SqliteConnectionFactory.ReleaseConnections(Path.Combine(_directory, "pie.db"));

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    /// <summary>
    /// The setup code the host showed when it started.
    /// </summary>
    internal string SetupCode()
    {
        // The host announces the code as it starts, which is when the first
        // service is asked for.
        _ = Services;

        Match match = Regex.Match(Output.ToString(), "[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}");

        return match.Success ? match.Value : throw new InvalidOperationException("The host showed no setup code.");
    }

    /// <summary>
    /// Creates an account, unless one with that name exists.
    /// </summary>
    internal StoredAccount AddAccount(string username, AccountRole role, string password = Password, bool passwordChangeRequired = false)
    {
        AccountRepository accounts = Services.GetRequiredService<AccountRepository>();

        return accounts.FindByUsername(username)
            ?? accounts.Create(
                username,
                role,
                Services.GetRequiredService<PasswordHasher>().Hash(password),
                passwordChangeRequired,
                Now)!;
    }

    /// <summary>
    /// The account with that name as it is kept now, when there is one.
    /// </summary>
    internal StoredAccount? Account(string username)
    {
        return Services.GetRequiredService<AccountRepository>().FindByUsername(username);
    }

    /// <summary>
    /// The administrator client the tests share, signed in on first use.
    /// </summary>
    internal async Task<HttpClient> AdministratorAsync()
    {
        return _administrator ??= await SignedInAsync();
    }

    /// <summary>
    /// A client that keeps its cookies, as a browser does, and has not signed in.
    /// </summary>
    internal HttpClient NewClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });
    }

    /// <summary>
    /// A client signed in through the real endpoint, with an account created for the purpose.
    /// </summary>
    internal async Task<HttpClient> SignedInAsync(AccountRole role = AccountRole.Administrator, string username = "tester")
    {
        AddAccount(username, role);

        HttpClient client = NewClient();

        Answer answer = await SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", new { username, password = Password });

        if (answer.Status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"The test account could not sign in: {answer.Raw}");
        }

        return client;
    }

    /// <summary>
    /// Calls an endpoint as an administrator, unless told otherwise.
    /// </summary>
    internal async Task<Answer> GetAsync(string path, HttpClient? client = null)
    {
        return await SendAsync(client ?? await AdministratorAsync(), HttpMethod.Get, path);
    }

    /// <summary>
    /// Calls an endpoint the way someone would who has not signed in.
    /// </summary>
    internal async Task<Answer> GetAnonymouslyAsync(string path)
    {
        using HttpClient client = NewClient();

        return await SendAsync(client, HttpMethod.Get, path);
    }

    /// <summary>
    /// Every endpoint the host exposes.
    /// </summary>
    internal List<RouteEndpoint> Endpoints()
    {
        return Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();
    }

    internal static bool IsAnonymous(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
    }

    internal static HttpMethod MethodOf(RouteEndpoint endpoint)
    {
        return new HttpMethod(endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods[0]);
    }

    internal static string ConcretePath(RouteEndpoint endpoint)
    {
        // "/api/v1/domains/{domain}" becomes "/api/v1/domains/x".
        return Regex.Replace(endpoint.RoutePattern.RawText!, @"\{[^}]+\}", "x");
    }

    // A session identifier can be presented by hand, for a client that keeps no cookies of its own.
    // A request can say which address it comes from, and which site sent it.
    internal static async Task<Answer> SendAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object? body = null,
        string? cookie = null,
        string? from = null,
        string? origin = null)
    {
        using HttpRequestMessage request = new(method, new Uri(path, UriKind.Relative));

        if (cookie is not null)
        {
            request.Headers.Add("Cookie", $"{SessionCookie.Name}={cookie}");
        }

        if (from is not null)
        {
            request.Headers.Add(RemoteAddressHeader, from);
        }

        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using HttpResponseMessage response = await client.SendAsync(request);

        string raw = await response.Content.ReadAsStringAsync();

        using JsonDocument document = JsonDocument.Parse(raw.Length == 0 ? "null" : raw);

        return new Answer(response.StatusCode, document.RootElement.Clone(), raw, response.Headers);
    }
}

/// <summary>
/// Gives each request the remote address a real connection would have.
/// </summary>
/// <remarks>
/// The in-memory server leaves the address unknown, and an unknown address is
/// not the loopback: every password would be refused. Placed before anything
/// else in the pipeline, so that what the host sees is what Kestrel would
/// have given it.
/// </remarks>
internal sealed class RemoteAddressFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (context, following) =>
            {
                string? declared = context.Request.Headers[PieApplication.RemoteAddressHeader];

                context.Connection.RemoteIpAddress = declared switch
                {
                    null => IPAddress.Loopback,
                    "unknown" => null,
                    _ => IPAddress.Parse(declared),
                };

                await following(context);
            });

            next(app);
        };
    }
}

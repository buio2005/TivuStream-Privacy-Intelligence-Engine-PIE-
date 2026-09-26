// TivuStream Privacy Intelligence Engine (PIE)
// Application host and composition root.
//
// Started by hand during development, or as a system service once installed
// (Installation Specification). A command given on the terminal, such as
// reset-password, runs and stops without starting the service.

using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.Options;
using TivuStream.Pie.Adapters.Technitium;
using TivuStream.Pie.Api;
using TivuStream.Pie.Api.Acquisition;
using TivuStream.Pie.Api.Authentication;
using TivuStream.Pie.Api.Classification;
using TivuStream.Pie.Api.Installation;
using TivuStream.Pie.Api.Contracts;
using TivuStream.Pie.Api.Storage;
using TivuStream.Pie.Api.Transport;
using TivuStream.Pie.Core;
using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Storage;
using TivuStream.Pie.Storage.Schema;

HostingMode hosting = HostingMode.Detect();

// A service is started from a folder of the system's choosing. The program's
// own files, the interface among them, are found beside the program.
WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = hosting.AsService ? AppContext.BaseDirectory : null,
});

builder.Services.AddWindowsService(options => options.ServiceName = HostingMode.ServiceName);

// Under the name the installation registers, so that what the service reports
// is found in the Event Viewer under the name of the service.
if (OperatingSystem.IsWindows())
{
    builder.Services.Configure<EventLogSettings>(HostingMode.NameEventSource);
}

builder.Services.AddSystemd();
builder.Services.AddSingleton(hosting);

// Installed, the data and the configuration holding the credentials live in
// a folder of their own, apart from the program.
string? dataDirectory = DataDirectory.From(builder.Configuration);

// Credentials never belong to a versioned file.
builder.Configuration.AddJsonFile(
    DataDirectory.Resolve(dataDirectory, DataDirectory.LocalSettingsFile),
    optional: true,
    reloadOnChange: false);

TransportOptions transport = builder.Configuration.GetSection("Transport").Get<TransportOptions>() ?? new();

transport.CertificateDirectory = DataDirectory.Resolve(dataDirectory, transport.CertificateDirectory);
transport.Certificate.Path = DataDirectory.Resolve(dataDirectory, transport.Certificate.Path);
transport.Certificate.KeyPath = DataDirectory.Resolve(dataDirectory, transport.Certificate.KeyPath);

// With the encrypted channel open, PIE is reached by this computer's own name
// and addresses. They are what the certificate names, and what is accepted.
MachineNames? machine = transport.HttpsPort > 0 ? MachineNames.Discover(transport.Names) : null;

// Read here, so that a configuration accepting any name stops the start
// rather than being discovered later.
string[] allowedHosts = HostPolicy.AllowedHosts(builder.Configuration, machine);

// A retention below its minimum stops the start rather than being corrected:
// consolidation cannot be undone.
RetentionOptions retention = builder.Configuration.GetSection(RetentionOptions.SectionName).Get<RetentionOptions>() ?? new();
retention.Validate();

// A certificate of the operator's that cannot be used stops the start here,
// with the reason, before anything else happens.
X509Certificate2? provided = transport.Certificate.IsProvided
    ? OperatorCertificate.Load(transport.Certificate, TimeProvider.System.GetUtcNow())
    : null;

builder.Services.AddSingleton(new TransportPolicy(StrictTransportSecurity: provided is not null));

// Forwarding headers are believed only from the proxies the operator names.
// A proxy is never trusted by default, the loopback included: on this
// machine, any process could otherwise say which client and which channel a
// request came from.
IPAddress[] trustedProxies = TrustedProxies.Parse(transport.TrustedProxies);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    foreach (IPAddress proxy in trustedProxies)
    {
        options.KnownProxies.Add(proxy);
    }
});

if (machine is not null)
{
    builder.WebHost.UseTransport(transport, machine, provided);
}
else
{
    builder.WebHost.ConfigureKestrel(kestrel => kestrel.ListenLocalhost(transport.HttpPort));
}

builder.Services.PostConfigure<HostFilteringOptions>(options => options.AllowedHosts = allowedHosts);

builder.Services.Configure<TechnitiumOptions>(builder.Configuration.GetSection("Technitium"));
builder.Services.Configure<AcquisitionOptions>(builder.Configuration.GetSection("Acquisition"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.PostConfigure<StorageOptions>(options =>
{
    options.DatabasePath = DataDirectory.Resolve(dataDirectory, options.DatabasePath);
    options.ListDirectoryPath = DataDirectory.Resolve(dataDirectory, options.ListDirectoryPath);
});
builder.Services.Configure<ClassificationOptions>(builder.Configuration.GetSection("Classification"));

builder.Services.AddSingleton(
    serviceProvider => serviceProvider.GetRequiredService<IOptions<TechnitiumOptions>>().Value);

// The Data Source is reached directly, never through the proxy configured in
// the system. Its answers carry the domains the network contacted, and a
// proxy, often left behind by a VPN client, may forward them off the device.
// A proxy that is configured but not running would also make a reachable
// source look unreachable.
builder.Services
    .AddHttpClient<TechnitiumAdapter>()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseProxy = false });

builder.Services.AddSingleton(
    serviceProvider => serviceProvider.GetRequiredService<IOptions<StorageOptions>>().Value);

builder.Services.AddSingleton<SqliteConnectionFactory>();
builder.Services.AddSingleton<SchemaMigrator>();
builder.Services.AddSingleton<AcquisitionRepository>();
builder.Services.AddSingleton<ScoreRepository>();
builder.Services.AddSingleton<AccountRepository>();
builder.Services.AddSingleton(PasswordHasher.Standard);
builder.Services.AddSingleton<SessionRepository>();
builder.Services.AddSingleton<SessionService>();
builder.Services.AddSingleton<CredentialVerifier>();
builder.Services.AddSingleton<AttemptLimiter>();
builder.Services.AddSingleton(new SetupOutput(Console.Out));
builder.Services.AddSingleton<SetupService>();
builder.Services.AddSingleton<RecoveryCommand>();

// Nothing answers without an identity. The framework is asked to refuse by
// default and each endpoint says what it requires, rather than the other way
// round.
builder.Services
    .AddAuthentication(SessionAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
        SessionAuthenticationHandler.SchemeName,
        configureOptions: null);

builder.Services.AddAuthorization(AuthorizationPolicies.Configure);
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationResultHandler>();
builder.Services.AddSingleton<ClassificationListRepository>();
builder.Services.AddSingleton<ClassificationListStore>();
builder.Services.AddSingleton<ClassificationProvider>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<NpssEngine>();

builder.Services.AddSingleton<AcquisitionState>();
builder.Services.AddHostedService<AcquisitionService>();

builder.Services.AddSingleton(serviceProvider => new PeriodConsolidator(
    serviceProvider.GetRequiredService<SqliteConnectionFactory>(),
    retention,
    TimeZoneInfo.Local));
builder.Services.AddHostedService<RetentionService>();

// Reaching a list is the only request PIE makes outside its own Data Source.
// It carries nothing about the network being observed.
builder.Services
    .AddHttpClient(
        nameof(ClassificationUpdateService),
        client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TivuStream-PIE");
        });

builder.Services.AddHostedService<ClassificationUpdateService>();

// Enumerations travel as names rather than as numbers: a number would be
// meaningless to anyone reading the answer.
// A body that cannot be read is reported the same way in every environment,
// through the handler of failures, instead of as a bare status in some.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

builder.Services.ConfigureHttpJsonOptions(
    options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

WebApplication app = builder.Build();

// The schema is brought up to date before anything else runs. Serving
// requests against a database of the wrong shape would produce failures far
// from their cause.
MigrationOutcome migration = app.Services.GetRequiredService<SchemaMigrator>().Migrate();

if (migration.DatabaseWasCreated)
{
    SchemaLog.Created(app.Logger, migration.FinalVersion);
}
else if (migration.SchemaChanged)
{
    if (migration.BackupPath is not null)
    {
        SchemaLog.BackedUp(app.Logger, migration.BackupPath);
    }

    SchemaLog.Updated(app.Logger, migration.InitialVersion, migration.FinalVersion);
}
else
{
    SchemaLog.Unchanged(app.Logger, migration.FinalVersion);
}

string productVersion = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "unknown";
string startedAs = hosting.AsService ? "running as a service" : "started by hand";
string dataLocation = dataDirectory ?? Directory.GetCurrentDirectory();

InstallationLog.Started(app.Logger, productVersion, startedAs, dataLocation);

RetentionLog.Configured(app.Logger, retention.HourlyDays, retention.DailyMonths, retention.MonthlyYears);

// Restoring access to an account is done at the terminal, by someone who has
// the machine, and never starts the service. It runs once the schema is ready
// and before anything slow is read.
string[] command = CommandLine.Positional(args);

if (command.Length > 0 && command[0] == "reset-password")
{
    if (command.Length != 2)
    {
        Console.Error.WriteLine("Usage: reset-password <name>");

        return RecoveryCommand.NotAcceptable;
    }

    return app.Services.GetRequiredService<RecoveryCommand>().Run(command[1], new ConsolePasswordPrompt(), Console.Out);
}

// Connecting to the Data Source and finding the way in are done at the
// terminal as well, during the installation (Installation Specification,
// Commands).
if (command.Length > 0 && command[0] == "configure")
{
    ConfigureCommand configure = new(
        options => new TechnitiumAdapter(
            new HttpClient(new SocketsHttpHandler { UseProxy = false }) { Timeout = TimeSpan.FromSeconds(15) },
            options),
        DataDirectory.Resolve(dataDirectory, DataDirectory.LocalSettingsFile));

    return await configure.RunAsync(Console.In, new ConsolePasswordPrompt(), Console.Out, CancellationToken.None);
}

if (command.Length > 0 && command[0] == "access")
{
    return new AccessCommand(transport, machine, provided).Run(Console.Out);
}

// A mistyped command must not start the service in its place.
if (command.Length > 0)
{
    Console.Error.WriteLine("Usage: tivustream-pie [configure | access | reset-password <name>]");

    return RecoveryCommand.NotAcceptable;
}

// The lists are put in place and read once the schema is ready. Reading them
// means opening files holding hundreds of thousands of names, which is done
// when the lists change and not when a domain is classified.
ClassificationProvider classification = app.Services.GetRequiredService<ClassificationProvider>();

classification.EnsureDefaults();
classification.Reload();

// First, so that everything below sees the client and the channel a trusted
// proxy declared. With no proxy named it changes nothing.
if (trustedProxies.Length > 0)
{
    app.UseForwardedHeaders();
}

// Outermost: whatever fails below answers in the common structure.
app.UseFailureAnswers();

// Before anything recognises the person: a request from another site is
// refused whoever it claims to be.
app.UseRequestProtection();

// The interface, open to everyone: the page that asks for a password has to
// load before anyone has given one. It carries no data.
app.UseInterface();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthentication();
app.MapAccounts();

// The state reports what has just happened, including failures. The
// repository reports what is known. The two answer different questions and
// are kept apart on purpose.
app.MapGet("/api/v1/health", (AcquisitionState state, AcquisitionRepository repository) =>
{
    AcquisitionResult? last = state.Current;
    StoredAcquisition? stored = repository.GetLatest();

    HealthReport report = new()
    {
        Api = "Online",
        Core = "NotImplemented",
        Adapter = last is null ? "Idle" : last.Succeeded ? "Online" : "Failing",
        Storage = "Ready",
        SchemaVersion = migration.FinalVersion,
        StoredPeriods = repository.CountPeriods(),
        Backend = stored?.DataSource ?? last?.DataSource,
        LastAcquisitionAt = last?.AttemptedAt,
        LastFailure = last?.Failure,
    };

    return Results.Ok(ApiResponse.Ok(report));
}).RequireAuthorization(AuthorizationPolicies.Viewer);

// Over the same window as domains and devices, declared. A window without any
// period answers with no statistics rather than a set of zeros, and rather
// than "nothing acquired yet", which is false when older acquisitions exist.
app.MapGet("/api/v1/statistics", (AcquisitionRepository repository, TimeProvider time) =>
{
    DateTimeOffset since = ObservationWindow.StartFor(time.GetUtcNow());

    return Results.Ok(ApiResponse.Ok(new ObservedStatistics
    {
        Period = repository.GetPeriodRangeSince(since),
        PeriodsObserved = repository.CountPeriodsSince(since),
        PeriodsRequested = ObservationWindow.RequestedHours,
        Statistics = repository.GetStatisticsSince(since),
    }));
}).RequireAuthorization(AuthorizationPolicies.Viewer);

app.MapGet("/api/v1/npss", (ScoreRepository scores, AcquisitionRepository repository) =>
{
    Npss? score = scores.GetLatest();

    if (score is null || scores.GetLatestScoredPeriod() is not ObservationPeriod produced)
    {
        return Results.Json(
            ApiResponse.Failed<ObservedScore>(
                "ScorePending",
                "No score has been produced yet."),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    // The window the score evaluated: the twenty-four hours ending with the
    // period it was produced in, which is not the present when acquisitions
    // have stopped.
    DateTimeOffset since = ObservationWindow.StartFor(produced.Start);

    return Results.Ok(ApiResponse.Ok(new ObservedScore
    {
        Period = repository.GetPeriodRangeSince(since, produced.Start),
        PeriodsObserved = repository.CountPeriodsSince(since, produced.Start),
        PeriodsRequested = ObservationWindow.RequestedHours,
        Score = score,
    }));
}).RequireAuthorization(AuthorizationPolicies.Viewer);

app.MapGet("/api/v1/devices", (AcquisitionRepository repository, TimeProvider time) =>
{
    DateTimeOffset since = ObservationWindow.StartFor(time.GetUtcNow());

    return Results.Ok(ApiResponse.Ok(new ObservedDevices
    {
        Period = repository.GetPeriodRangeSince(since),
        PeriodsObserved = repository.CountPeriodsSince(since),
        PeriodsRequested = ObservationWindow.RequestedHours,
        Devices = repository.GetDevicesSince(since),
    }));
}).RequireAuthorization(AuthorizationPolicies.Administrator);

// The window travels with the list. An empty list on its own cannot be told
// apart from an hour that has only just begun.
app.MapGet("/api/v1/domains", (AcquisitionRepository repository, TimeProvider time) =>
{
    DateTimeOffset since = ObservationWindow.StartFor(time.GetUtcNow());

    return Results.Ok(ApiResponse.Ok(new ObservedDomains
    {
        Period = repository.GetPeriodRangeSince(since),
        PeriodsObserved = repository.CountPeriodsSince(since),
        PeriodsRequested = ObservationWindow.RequestedHours,
        Domains = repository.GetDomainsSince(since),
    }));
}).RequireAuthorization(AuthorizationPolicies.Viewer);

app.MapGet("/api/v1/domains/{domain}", (
    string domain,
    ClaimsPrincipal user,
    AcquisitionRepository repository,
    TimeProvider time) =>
{
    DateTimeOffset since = ObservationWindow.StartFor(time.GetUtcNow());

    Domain? found = repository.GetDomainsSince(since)
        .Find(candidate => string.Equals(candidate.Name, domain, StringComparison.OrdinalIgnoreCase));

    if (found is null)
    {
        return Results.Json(
            ApiResponse.Failed<DomainDetail>(
                "DomainNotObserved",
                "The domain was not observed during the requested interval."),
            statusCode: StatusCodes.Status404NotFound);
    }

    StoredAcquisition? stored = repository.GetLatest();

    bool offered =
        stored?.DataSource.Capabilities.Contains(nameof(DomainActivity), StringComparer.Ordinal) == true;

    // What the source does not offer is unavailable to everyone. Saying it was
    // withheld would claim that the source offers it.
    ActivityAccess access =
        !offered ? ActivityAccess.Unavailable
        : user.IsInRole(nameof(AccountRole.Administrator)) ? ActivityAccess.Available
        : ActivityAccess.Withheld;

    DomainDetail detail = new()
    {
        Period = repository.GetPeriodRangeSince(since),
        PeriodsObserved = repository.CountPeriodsSince(since),
        PeriodsRequested = ObservationWindow.RequestedHours,
        Domain = found,

        // Read only when it may be shown, so that what is withheld is never
        // read at all, and an empty list is never mistaken for an absence of
        // activity.
        Activities = access == ActivityAccess.Available ? repository.GetActivitiesSince(found.Name, since) : [],
        ActivityAccess = access,
    };

    return Results.Ok(ApiResponse.Ok(detail));
}).RequireAuthorization(AuthorizationPolicies.Viewer);

// Shown last, so that it is what is on the screen when the service is ready.
// Under a service there is no screen, and the setup code does not exist: the
// first administrator is created at the terminal (Authentication
// Specification, First Run).
if (app.Services.GetRequiredService<HostingMode>().AsService)
{
    if (app.Services.GetRequiredService<SetupService>().IsRequired)
    {
        InstallationLog.AdministratorMissing(app.Logger);
    }
}
else
{
    app.Services.GetRequiredService<SetupService>().AnnounceIfRequired();
}

app.Run();

return 0;

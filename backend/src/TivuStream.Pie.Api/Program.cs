// TivuStream Privacy Intelligence Engine (PIE)
// Application host and composition root.
//
// Milestone M3.4 builds the first vertical slice: the Acquisition Flow
// reaches Technitium, the result is held in memory, and the Query Flow reads
// it. The Core does not exist yet, so no analysis takes place.
//
// One deviation from the API Specification is accepted for local use only and
// is recorded in the changelog: the host serves plain HTTP. Every endpoint
// requires a signed in account, as the Authentication Specification says.

using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.Extensions.Options;
using TivuStream.Pie.Adapters.Technitium;
using TivuStream.Pie.Api.Acquisition;
using TivuStream.Pie.Api.Authentication;
using TivuStream.Pie.Api.Classification;
using TivuStream.Pie.Api.Contracts;
using TivuStream.Pie.Api.Storage;
using TivuStream.Pie.Core;
using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Storage;
using TivuStream.Pie.Storage.Schema;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Credentials never belong to a versioned file.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

// Read here, so that a configuration accepting any name stops the start
// rather than being discovered later.
string[] allowedHosts = HostPolicy.AllowedHosts(builder.Configuration);

builder.Services.PostConfigure<HostFilteringOptions>(options => options.AllowedHosts = allowedHosts);

builder.Services.Configure<TechnitiumOptions>(builder.Configuration.GetSection("Technitium"));
builder.Services.Configure<AcquisitionOptions>(builder.Configuration.GetSection("Acquisition"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<ClassificationOptions>(builder.Configuration.GetSection("Classification"));

builder.Services.AddSingleton(
    serviceProvider => serviceProvider.GetRequiredService<IOptions<TechnitiumOptions>>().Value);

builder.Services.AddHttpClient<TechnitiumAdapter>();

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
    SchemaLog.Updated(app.Logger, migration.InitialVersion, migration.FinalVersion);
}
else
{
    SchemaLog.Unchanged(app.Logger, migration.FinalVersion);
}

// Restoring access to an account is done at the terminal, by someone who has
// the machine, and never starts the service. It runs once the schema is ready
// and before anything slow is read.
if (args.Length > 0 && args[0] == "reset-password")
{
    if (args.Length != 2)
    {
        Console.Error.WriteLine("Usage: reset-password <name>");

        return RecoveryCommand.NotAcceptable;
    }

    return app.Services.GetRequiredService<RecoveryCommand>().Run(args[1], new ConsolePasswordPrompt(), Console.Out);
}

// The lists are put in place and read once the schema is ready. Reading them
// means opening files holding hundreds of thousands of names, which is done
// when the lists change and not when a domain is classified.
ClassificationProvider classification = app.Services.GetRequiredService<ClassificationProvider>();

classification.EnsureDefaults();
classification.Reload();

// Before anything recognises the person: a request from another site is
// refused whoever it claims to be.
app.UseRequestProtection();

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

app.MapGet("/api/v1/statistics", (AcquisitionRepository repository) =>
{
    StoredAcquisition? stored = repository.GetLatest();

    if (stored is null)
    {
        return Results.Json(
            ApiResponse.Failed<Statistics>(
                "AcquisitionPending",
                "No acquisition has been recorded yet."),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(ApiResponse.Ok(stored.Statistics));
}).RequireAuthorization(AuthorizationPolicies.Viewer);

app.MapGet("/api/v1/npss", (ScoreRepository scores) =>
{
    Npss? score = scores.GetLatest();

    if (score is null)
    {
        return Results.Json(
            ApiResponse.Failed<Npss>(
                "ScorePending",
                "No score has been produced yet."),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(ApiResponse.Ok(score));
}).RequireAuthorization(AuthorizationPolicies.Viewer);

app.MapGet("/api/v1/devices", (AcquisitionRepository repository) =>
{
    return Results.Ok(ApiResponse.Ok(repository.GetLatestDevices()));
}).RequireAuthorization(AuthorizationPolicies.Administrator);

// The window travels with the list. An empty list on its own cannot be told
// apart from an hour that has only just begun.
//
// A whole day rather than the current hour: a fixed hourly bucket empties at
// every turn of the clock, which is the opposite of what someone asking what
// their network is doing wants to see.
app.MapGet("/api/v1/domains", (AcquisitionRepository repository, TimeProvider time) =>
{
    const int RequestedHours = 24;

    DateTimeOffset since = ObservationPeriod
        .Containing(time.GetUtcNow())
        .Start
        .AddHours(-(RequestedHours - 1));

    return Results.Ok(ApiResponse.Ok(new ObservedDomains
    {
        Period = repository.GetPeriodRangeSince(since),
        PeriodsObserved = repository.CountPeriodsSince(since),
        PeriodsRequested = RequestedHours,
        Domains = repository.GetDomainsSince(since),
    }));
}).RequireAuthorization(AuthorizationPolicies.Viewer);

app.MapGet("/api/v1/domains/{domain}", (string domain, ClaimsPrincipal user, AcquisitionRepository repository) =>
{
    Domain? found = repository.GetLatestDomains()
        .FirstOrDefault(candidate => string.Equals(candidate.Name, domain, StringComparison.OrdinalIgnoreCase));

    if (found is null)
    {
        return Results.Json(
            ApiResponse.Failed<DomainDetail>(
                "DomainNotObserved",
                "The domain was not observed during the last recorded period."),
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
        Domain = found,

        // Read only when it may be shown, so that what is withheld is never
        // read at all, and an empty list is never mistaken for an absence of
        // activity.
        Activities = access == ActivityAccess.Available ? repository.GetLatestActivitiesFor(found.Name) : [],
        ActivityAccess = access,
    };

    return Results.Ok(ApiResponse.Ok(detail));
}).RequireAuthorization(AuthorizationPolicies.Viewer);

// Shown last, so that it is what is on the screen when the service is ready.
app.Services.GetRequiredService<SetupService>().AnnounceIfRequired();

app.Run();

return 0;

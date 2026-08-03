// TivuStream Privacy Intelligence Engine (PIE)
// Application host and composition root.
//
// Milestone M3.4 builds the first vertical slice: the Acquisition Flow
// reaches Technitium, the result is held in memory, and the Query Flow reads
// it. The Core does not exist yet, so no analysis takes place.
//
// Two deviations from the API Specification are accepted for local use only
// and are recorded in the changelog: the host serves plain HTTP, and no
// endpoint verifies permissions.

using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TivuStream.Pie.Adapters.Technitium;
using TivuStream.Pie.Api.Acquisition;
using TivuStream.Pie.Api.Contracts;
using TivuStream.Pie.Api.Storage;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Storage;
using TivuStream.Pie.Storage.Schema;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Credentials never belong to a versioned file.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

builder.Services.Configure<TechnitiumOptions>(builder.Configuration.GetSection("Technitium"));
builder.Services.Configure<AcquisitionOptions>(builder.Configuration.GetSection("Acquisition"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));

builder.Services.AddSingleton(
    serviceProvider => serviceProvider.GetRequiredService<IOptions<TechnitiumOptions>>().Value);

builder.Services.AddHttpClient<TechnitiumAdapter>();

builder.Services.AddSingleton(
    serviceProvider => serviceProvider.GetRequiredService<IOptions<StorageOptions>>().Value);

builder.Services.AddSingleton<SqliteConnectionFactory>();
builder.Services.AddSingleton<SchemaMigrator>();

builder.Services.AddSingleton<AcquisitionState>();
builder.Services.AddHostedService<AcquisitionService>();

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

app.MapGet("/api/v1/health", (AcquisitionState state) =>
{
    AcquisitionResult? last = state.Current;

    HealthReport report = new()
    {
        Api = "Online",
        Core = "NotImplemented",
        Adapter = last is null ? "Idle" : last.Succeeded ? "Online" : "Failing",
        Storage = "Ready",
        SchemaVersion = migration.FinalVersion,
        Backend = last?.DataSource,
        LastAcquisitionAt = last?.AttemptedAt,
        LastFailure = last?.Failure,
    };

    return Results.Ok(ApiResponse.Ok(report));
});

app.MapGet("/api/v1/statistics", (AcquisitionState state) =>
{
    AcquisitionResult? last = state.Current;

    if (last is null)
    {
        return Results.Json(
            ApiResponse.Failed<Statistics>(
                "AcquisitionPending",
                "No acquisition has taken place yet."),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!last.Succeeded || last.Statistics is null)
    {
        return Results.Json(
            ApiResponse.Failed<Statistics>(
                "BackendUnavailable",
                last.Failure ?? "The last acquisition did not complete."),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(ApiResponse.Ok(last.Statistics));
});

app.Run();

using System.Diagnostics;
using TivuStream.Pie.Api.Authentication;
using TivuStream.Pie.Storage;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies that the recovery command works from the executable itself.
/// </summary>
/// <remarks>
/// The command is dispatched by the composition root before anything else
/// starts. Nothing else covers that wiring: a command that worked in a test
/// and not from the terminal would be of no use to whoever has lost access.
/// </remarks>
public sealed class RecoveryProcessTests : IDisposable
{
    private const string Password = "a-password-for-the-terminal";

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "pie-process-" + Guid.NewGuid().ToString("N"));

    public RecoveryProcessTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        // Pooled connections hold the file open. Only this database's: other tests
        // run at the same time on databases of their own.
        SqliteConnectionFactory.ReleaseConnections(Path.Combine(_directory, "pie.db"));

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task The_executable_creates_an_administrator_from_the_terminal_and_stops()
    {
        (int exit, string output, _) = await Run(["reset-password", "Maria"], $"{Password}\n{Password}\n");

        Assert.Equal(0, exit);
        Assert.Contains("Administrator 'maria' created", output, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, output, StringComparison.Ordinal);

        // What the executable wrote is what the real parameters would verify.
        AccountRepository accounts = new(new SqliteConnectionFactory(new StorageOptions { DatabasePath = DatabasePath }));

        StoredAccount maria = accounts.FindByUsername("maria")!;

        Assert.Equal(AccountRole.Administrator, maria.Role);
        // The first account of an empty installation: the password was just
        // chosen by whoever installed it.
        Assert.False(maria.PasswordChangeRequired);
        Assert.Equal(PasswordVerification.Valid, PasswordHasher.Standard.Verify(Password, maria.PasswordHash));
    }

    [Fact]
    public async Task Without_a_name_the_executable_says_how_to_use_it_and_does_not_start_the_service()
    {
        // A service that started instead would never return, and the timeout
        // of the run would be what fails.
        (int exit, _, string error) = await Run(["reset-password"], string.Empty);

        Assert.Equal(RecoveryCommand.NotAcceptable, exit);
        Assert.Contains("Usage: reset-password", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Installed_the_executable_keeps_its_database_in_the_data_folder()
    {
        string data = Path.Combine(_directory, "installed-data");
        Directory.CreateDirectory(data);

        // A relative path, as the default configuration has: taken from the
        // data folder, not from the folder the program was started in.
        (int exit, _, _) = await Run(
            ["reset-password", "maria", $"--DataDirectory={data}", "--Storage:DatabasePath=pie.db"],
            $"{Password}\n{Password}\n");

        string database = Path.Combine(data, "pie.db");

        Assert.Equal(0, exit);
        Assert.True(File.Exists(database));

        AccountRepository accounts = new(new SqliteConnectionFactory(new StorageOptions { DatabasePath = database }));

        Assert.NotNull(accounts.FindByUsername("maria"));

        SqliteConnectionFactory.ReleaseConnections(database);
    }

    private string DatabasePath => Path.Combine(_directory, "pie.db");

    private async Task<(int Exit, string Output, string Error)> Run(string[] arguments, string input)
    {
        ProcessStartInfo start = new("dotnet")
        {
            WorkingDirectory = _directory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        start.ArgumentList.Add(BuiltProduct());

        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        // Its own database, and no file of the developer's: the working folder
        // is empty, so neither appsettings file is read.
        start.Environment["Storage__DatabasePath"] = DatabasePath;
        start.Environment["Storage__ListDirectoryPath"] = Path.Combine(_directory, "lists");

        using Process process = Process.Start(start)!;

        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();

        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(45));

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            // A service that started instead of stopping would go on listening
            // long after this test has failed.
            process.Kill(entireProcessTree: true);

            throw new TimeoutException("The executable did not stop: it started the service instead of restoring access.");
        }

        return (process.ExitCode, await output, await error);
    }

    /// <summary>
    /// The product as it was built, beside the tests, not a copy of it.
    /// </summary>
    private static string BuiltProduct()
    {
        // .../backend/tests/<tests>/bin/<configuration>/<framework>/
        DirectoryInfo framework = new(AppContext.BaseDirectory.TrimEnd('\\', '/'));
        DirectoryInfo backend = framework.Parent!.Parent!.Parent!.Parent!.Parent!;

        string path = Path.Combine(
            backend.FullName,
            "src",
            "TivuStream.Pie.Api",
            "bin",
            framework.Parent.Name,
            framework.Name,
            "tivustream-pie.dll");

        Assert.True(File.Exists(path), $"the built product was not found at {path}");

        return path;
    }
}

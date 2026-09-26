using System.Text.Json;
using System.Text.Json.Nodes;
using TivuStream.Pie.Adapters;
using TivuStream.Pie.Adapters.Technitium;
using TivuStream.Pie.Api.Authentication;
using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Api.Installation;

/// <summary>
/// Connects PIE to its Data Source from the terminal.
/// </summary>
/// <remarks>
/// Installation Specification, Commands. Asks for the address and the token,
/// tries them, says which capabilities the Data Source offers and which it
/// does not, and only then writes them. A configuration that does not work is
/// never written: finding out at the first acquisition, in a log nobody
/// reads, is what this command exists to prevent.
/// <para>
/// The token is read without being shown and appears in no message.
/// </para>
/// </remarks>
internal sealed class ConfigureCommand
{
    internal const int Done = 0;

    internal const int Refused = 1;

    private const string DefaultAddress = "http://localhost:5380";

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    private readonly Func<TechnitiumOptions, TechnitiumAdapter> _adapterFor;
    private readonly string _settingsPath;

    /// <param name="adapterFor">Builds an Adapter for the settings being tried.</param>
    /// <param name="settingsPath">The file the settings are written to.</param>
    public ConfigureCommand(Func<TechnitiumOptions, TechnitiumAdapter> adapterFor, string settingsPath)
    {
        _adapterFor = adapterFor;
        _settingsPath = settingsPath;
    }

    internal async Task<int> RunAsync(TextReader input, IPasswordPrompt secret, TextWriter output, CancellationToken cancellationToken)
    {
        // Rewriting a file that cannot be read would lose whatever else the
        // person had put in it.
        if (File.Exists(_settingsPath) && ReadSettings() is null)
        {
            output.WriteLine($"The settings file {_settingsPath} exists but cannot be read. Correct it or move it away, then try again.");

            return Refused;
        }

        output.WriteLine("Connect PIE to your Technitium DNS Server.");
        output.WriteLine();

        while (true)
        {
            output.Write($"Address of Technitium [{DefaultAddress}]: ");
            output.Flush();

            string? typed = input.ReadLine();

            if (typed is null)
            {
                output.WriteLine();
                output.WriteLine("Nothing was written.");

                return Refused;
            }

            string address = string.IsNullOrWhiteSpace(typed) ? DefaultAddress : typed.Trim();

            if (!Uri.TryCreate(address, UriKind.Absolute, out Uri? baseAddress)
                || (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
            {
                output.WriteLine("That is not an address PIE can use. It looks like http://192.168.1.10:5380");

                continue;
            }

            string? token = secret.Read("API token (it is not shown while you type)");

            if (string.IsNullOrWhiteSpace(token))
            {
                output.WriteLine("No token was given. Nothing was written.");

                return Refused;
            }

            TechnitiumOptions options = new()
            {
                DataSourceId = ExistingDataSourceId() ?? Guid.NewGuid(),
                BaseAddress = baseAddress,
                ApiToken = token.Trim(),
            };

            output.WriteLine();
            output.WriteLine($"Trying {baseAddress} ...");

            DataSource? described = await TryAsync(options, output, cancellationToken).ConfigureAwait(false);

            if (described is not null && Explain(described, output))
            {
                Write(options);

                output.WriteLine();
                output.WriteLine($"Settings written to {_settingsPath}");

                return Done;
            }

            output.WriteLine();
            output.Write("Try again? [Y/n]: ");
            output.Flush();

            string? again = input.ReadLine();

            if (again is null || again.Trim().StartsWith('n') || again.Trim().StartsWith('N'))
            {
                output.WriteLine("Nothing was written.");

                return Refused;
            }
        }
    }

    private async Task<DataSource?> TryAsync(TechnitiumOptions options, TextWriter output, CancellationToken cancellationToken)
    {
        try
        {
            return await _adapterFor(options).DescribeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (AdapterException exception)
        {
            output.WriteLine(exception.Failure switch
            {
                AdapterFailure.Unreachable =>
                    "Nothing answered at that address. Check that Technitium is running, and the address and port.",
                AdapterFailure.NoAnswer =>
                    "Something is at that address, but it did not answer in time. Check the address and port.",
                AdapterFailure.CredentialsRefused =>
                    "Technitium answered, and refused the token. Check that you copied all of it, and that it was not deleted.",
                _ =>
                    "Technitium could not be used: " + exception.Message,
            });

            return null;
        }
    }

    /// <summary>
    /// Says what the Data Source offers and what it does not.
    /// </summary>
    /// <remarks>
    /// Installation Specification, Capability Detection and Privacy
    /// Disclosure: each missing capability with what will not be available
    /// and what would make it available; the device activity with what it
    /// costs in privacy, whether present or not.
    /// </remarks>
    /// <returns>Whether the connection is usable at all.</returns>
    private static bool Explain(DataSource described, TextWriter output)
    {
        IReadOnlyCollection<string> offered = described.Capabilities;

        output.WriteLine($"Connected to {described.Name}, Technitium {described.Version}.");
        output.WriteLine();

        if (!offered.Contains(nameof(Statistics)))
        {
            output.WriteLine("The account of this token cannot read the Dashboard, so PIE would see nothing.");
            output.WriteLine("In Technitium, give that account permission to view the Dashboard, then try again.");

            return false;
        }

        output.WriteLine("Available: totals of the network, devices, domains.");

        if (!offered.Contains(nameof(SourceConfiguration)))
        {
            output.WriteLine();
            output.WriteLine("Missing: the settings of the server.");
            output.WriteLine("  Without them, PIE cannot tell whether DNS security features are enabled, and that part of the score stays out.");
            output.WriteLine("  To add them, give the account of this token permission to view Settings in Technitium.");
        }

        output.WriteLine();

        if (offered.Contains(nameof(DomainActivity)))
        {
            output.WriteLine("Available: which device contacted which domain, through the Query Logs app of Technitium.");
            output.WriteLine("  That app makes Technitium keep every single query, for as long as its own settings say.");
            output.WriteLine("  PIE keeps only hourly totals of them.");
        }
        else
        {
            output.WriteLine("Missing: which device contacted which domain.");
            output.WriteLine("  To add it, install the 'Query Logs (Sqlite)' app in Technitium, and give the account of this token");
            output.WriteLine("  permission to view Apps and Logs.");
            output.WriteLine("  Be aware that the app makes Technitium keep every single query, for as long as its own settings say;");
            output.WriteLine("  PIE would keep only hourly totals of them.");
        }

        return true;
    }

    private Guid? ExistingDataSourceId()
    {
        JsonObject? settings = ReadSettings();

        string? existing = settings?["Technitium"]?["DataSourceId"]?.GetValue<string>();

        return Guid.TryParse(existing, out Guid id) && id != Guid.Empty ? id : null;
    }

    /// <summary>
    /// Writes the connection into the local settings, keeping everything else
    /// the file holds.
    /// </summary>
    private void Write(TechnitiumOptions options)
    {
        JsonObject settings = ReadSettings() ?? [];

        settings["Technitium"] = new JsonObject
        {
            ["DataSourceId"] = options.DataSourceId.ToString(),
            ["BaseAddress"] = options.BaseAddress!.ToString(),
            ["ApiToken"] = options.ApiToken,
        };

        string? directory = Path.GetDirectoryName(Path.GetFullPath(_settingsPath));

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_settingsPath, settings.ToJsonString(Indented));

        // The file holds the token. On Windows it inherits the permissions of
        // the data folder, which the installation restricts.
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(_settingsPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    private JsonObject? ReadSettings()
    {
        if (!File.Exists(_settingsPath))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(
                File.ReadAllText(_settingsPath),
                documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

using TivuStream.Pie.Api.Installation;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies where an installed PIE looks for its data, and how it reads the
/// command it is given.
/// </summary>
/// <remarks>
/// Installation Specification 1.3.0, Where Things Live and Commands.
/// </remarks>
public sealed class InstallationTests
{
    [Fact]
    public void A_relative_path_is_taken_from_the_data_folder()
    {
        string data = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "pie-data"));

        Assert.Equal(Path.Combine(data, "data", "pie.db"), DataDirectory.Resolve(data, Path.Combine("data", "pie.db")));
    }

    [Fact]
    public void A_full_path_is_kept_as_it_is()
    {
        string elsewhere = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "elsewhere", "pie.db"));

        Assert.Equal(elsewhere, DataDirectory.Resolve(Path.GetTempPath(), elsewhere));
    }

    [Fact]
    public void Without_a_data_folder_paths_stay_as_during_development()
    {
        Assert.Equal("data/pie.db", DataDirectory.Resolve(null, "data/pie.db"));
    }

    [Fact]
    public void An_empty_path_stays_empty_rather_than_becoming_the_data_folder()
    {
        // An operator certificate that is not configured must stay not
        // configured.
        Assert.Equal(string.Empty, DataDirectory.Resolve(Path.GetTempPath(), string.Empty));
    }

    [Theory]
    [InlineData(new[] { "reset-password", "maria" }, new[] { "reset-password", "maria" })]
    [InlineData(new[] { "reset-password", "maria", "--DataDirectory=/srv/pie" }, new[] { "reset-password", "maria" })]
    [InlineData(new[] { "--DataDirectory", "/var/lib/pie", "reset-password", "maria" }, new[] { "reset-password", "maria" })]
    [InlineData(new[] { "--DataDirectory", "/var/lib/pie" }, new string[0])]
    public void Settings_passed_with_a_command_are_not_taken_for_its_arguments(string[] args, string[] expected)
    {
        Assert.Equal(expected, CommandLine.Positional(args));
    }
}

using System.Net;
using System.Text.Json;
using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;
using TivuStream.Pie.Storage;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies what each role may read, and that what is withheld says so.
/// </summary>
/// <remarks>
/// Authentication Specification, Authorization and V6. A <c>Viewer</c> reads
/// the aggregated data and nothing that identifies a single device; an empty
/// list because of the role is declared as such, not presented as an absence
/// of activity.
/// </remarks>
public sealed class RoleTests : IDisposable
{
    private readonly PieApplication _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    [Theory]
    [InlineData("/api/v1/health")]
    [InlineData("/api/v1/statistics")]
    [InlineData("/api/v1/npss")]
    [InlineData("/api/v1/domains")]
    [InlineData("/api/v1/domains/a.example")]
    public async Task A_viewer_reads_the_aggregated_data(string path)
    {
        using HttpClient viewer = await _app.SignedInAsync(AccountRole.Viewer, "maria");

        Answer answer = await _app.GetAsync(path, viewer);

        // Some of these have nothing to show yet. What matters is that the
        // role is not the reason.
        Assert.NotEqual(HttpStatusCode.Unauthorized, answer.Status);
        Assert.NotEqual(HttpStatusCode.Forbidden, answer.Status);
    }

    [Theory]
    [InlineData("GET", "/api/v1/devices")]
    [InlineData("GET", "/api/v1/accounts")]
    [InlineData("POST", "/api/v1/accounts")]
    [InlineData("PATCH", "/api/v1/accounts/maria")]
    [InlineData("DELETE", "/api/v1/accounts/maria")]
    public async Task A_viewer_is_refused_what_identifies_a_device_and_the_management_of_accounts(string method, string path)
    {
        _app.AddAccount("root", AccountRole.Administrator);

        using HttpClient viewer = await _app.SignedInAsync(AccountRole.Viewer, "maria");

        Answer answer = await PieApplication.SendAsync(viewer, new HttpMethod(method), path, method is "POST" or "PATCH" ? new { } : null);

        Assert.Equal(HttpStatusCode.Forbidden, answer.Status);
        Assert.Equal("Forbidden", answer.Body.GetProperty("error").GetProperty("code").GetString());

        // Refused before anything was done.
        Assert.NotNull(_app.Account("maria"));
        Assert.True(_app.Account("maria")!.Enabled);
    }

    [Fact]
    public async Task Activity_the_role_does_not_cover_is_declared_withheld_and_not_shown_as_empty()
    {
        ObservationPeriod period = Seed.HoursAgo(0);
        DomainActivity activity = Seed.Activity("a.example", period, 5);
        Device device = Seed.Device("10.0.0.77", DeviceIdentityBasis.NetworkAddress, period) with
        {
            DeviceId = activity.DeviceId,
            Hostname = "laptop-maria",
        };

        Seed.Acquisition(
            _app,
            period,
            domains: [Seed.Domain("a.example", period, 5)],
            devices: [device],
            activities: [activity]);

        using HttpClient viewer = await _app.SignedInAsync(AccountRole.Viewer, "maria");

        Answer answer = await _app.GetAsync("/api/v1/domains/a.example", viewer);

        JsonElement data = answer.Body.GetProperty("data");

        Assert.Equal(HttpStatusCode.OK, answer.Status);
        Assert.Equal("Withheld", data.GetProperty("activityAccess").GetString());
        Assert.Equal(0, data.GetProperty("activities").GetArrayLength());

        // Nothing of the device travels, in any field.
        Assert.DoesNotContain(activity.DeviceId.ToString(), answer.Raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("10.0.0.77", answer.Raw, StringComparison.Ordinal);
        Assert.DoesNotContain("laptop-maria", answer.Raw, StringComparison.Ordinal);

        // The same domain, to an administrator, has the activity.
        Answer administrator = await _app.GetAsync("/api/v1/domains/a.example");

        Assert.Equal("Available", administrator.Body.GetProperty("data").GetProperty("activityAccess").GetString());
        Assert.Equal(1, administrator.Body.GetProperty("data").GetProperty("activities").GetArrayLength());
    }

    [Fact]
    public async Task Activity_the_source_does_not_offer_is_unavailable_to_a_viewer_too_and_not_withheld()
    {
        ObservationPeriod period = Seed.HoursAgo(0);

        Seed.Acquisition(
            _app,
            period,
            domains: [Seed.Domain("a.example", period, 5)],
            activities: [Seed.Activity("a.example", period, 5)],
            offersActivity: false);

        using HttpClient viewer = await _app.SignedInAsync(AccountRole.Viewer, "maria");

        Answer answer = await _app.GetAsync("/api/v1/domains/a.example", viewer);

        // "Withheld" would claim that the source offers it.
        Assert.Equal("Unavailable", answer.Body.GetProperty("data").GetProperty("activityAccess").GetString());
    }

    [Fact]
    public async Task A_demotion_takes_effect_on_the_next_request()
    {
        _app.AddAccount("root", AccountRole.Administrator);

        using HttpClient maria = await _app.SignedInAsync(AccountRole.Administrator, "maria");

        Assert.Equal(HttpStatusCode.OK, (await _app.GetAsync("/api/v1/devices", maria)).Status);

        Answer demoted = await PieApplication.SendAsync(
            await _app.AdministratorAsync(),
            HttpMethod.Patch,
            "/api/v1/accounts/maria",
            new { role = "Viewer" });

        Assert.Equal(HttpStatusCode.OK, demoted.Status);

        // The same session, not a new one: the role is read at every request.
        Assert.Equal(HttpStatusCode.Forbidden, (await _app.GetAsync("/api/v1/devices", maria)).Status);
    }
}

using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TivuStream.Pie.Api.Installation;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies what changes when PIE runs as a system service.
/// </summary>
/// <remarks>
/// Installation Specification 1.3.0, First Administrator Under A Service, and
/// Authentication Specification 1.6.0, First Run: a service has no window, so
/// no setup code exists, none is accepted, and the log says how to create the
/// first administrator instead.
/// </remarks>
public sealed partial class ServiceTests : IDisposable
{
    private readonly PieApplication _base = new();

    private readonly WebApplicationFactory<Program> _service;

    public ServiceTests()
    {
        _service = _base.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<HostingMode>();
            services.AddSingleton(new HostingMode(AsService: true));
        }));
    }

    public void Dispose()
    {
        _service.Dispose();
        _base.Dispose();
    }

    [Fact]
    public void Under_a_service_no_setup_code_is_shown_and_the_log_says_how_to_create_the_administrator()
    {
        _ = _service.Services;

        Assert.DoesNotMatch(SetupCodePattern(), _base.Output.ToString());
        Assert.Contains(_base.Logs, line => line.Contains("tivustream-pie reset-password <name>", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Under_a_service_no_code_opens_the_installation()
    {
        using HttpClient client = _service.CreateClient();

        Answer answer = await PieApplication.SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/setup",
            new { code = "ABCD-EFGH-JKLM", username = "maria", password = "a-password-worth-twelve" });

        Assert.Equal(HttpStatusCode.Unauthorized, answer.Status);
        Assert.Equal("SetupCodeRejected", answer.Body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public void The_version_of_the_product_is_stated_when_it_starts()
    {
        _ = _base.Services;

        Assert.Contains(_base.Logs, line => line.StartsWith("TivuStream PIE 0.1.0, started by hand", StringComparison.Ordinal));
    }

    [GeneratedRegex("[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}")]
    private static partial Regex SetupCodePattern();
}

using System.Net;
using System.Text.Json;
using TivuStream.Pie.Storage;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies that the engine serves the interface on its own address.
/// </summary>
/// <remarks>
/// Transport Security Specification, T1. One address for the interface and
/// the API: one certificate, no rules between origins.
/// </remarks>
public sealed class InterfaceTests : IDisposable
{
    private readonly PieApplication _app = new();

    public InterfaceTests()
    {
        // An installation with an account: the page loads before signing in
        // whatever state the installation is in.
        _app.AddAccount("someone", AccountRole.Viewer);
    }

    public void Dispose()
    {
        _app.Dispose();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/domains")]
    [InlineData("/domains/tracker.example")]
    [InlineData("/accounts")]
    public async Task An_address_of_the_interface_receives_its_page_without_signing_in(string path)
    {
        using HttpClient client = _app.NewClient();

        HttpResponseMessage response = await client.GetAsync(path);

        // Routes of the interface exist only in the browser: the page answers
        // them, and it decides what to show.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PieApplication.InterfacePage, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_page_is_never_cached_so_that_an_update_reaches_the_browser()
    {
        using HttpClient client = _app.NewClient();

        HttpResponseMessage response = await client.GetAsync("/domains");

        Assert.True(response.Headers.CacheControl?.NoCache);
    }

    [Fact]
    public async Task A_file_of_the_interface_is_served_as_it_is()
    {
        using HttpClient client = _app.NewClient();

        HttpResponseMessage response = await client.GetAsync("/assets/app.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PieApplication.InterfaceScript, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_page_carries_the_protection_headers_of_every_answer()
    {
        using HttpClient client = _app.NewClient();

        HttpResponseMessage response = await client.GetAsync("/");

        Assert.Contains(
            "frame-ancestors 'none'",
            response.Headers.GetValues("Content-Security-Policy").Single(),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/api/v1/nowhere")]
    [InlineData("/api/nowhere/at/all")]
    public async Task An_address_under_api_that_does_not_exist_is_an_api_error_and_not_the_page(string path)
    {
        using HttpClient client = _app.NewClient();

        HttpResponseMessage response = await client.GetAsync(path);

        // A client expecting data must not be handed HTML.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.False(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("NotFound", body.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_wrong_method_on_an_existing_endpoint_is_not_answered_as_missing()
    {
        using HttpClient root = await _app.SignedInAsync(AccountRole.Administrator, "root");

        HttpResponseMessage response = await root.DeleteAsync("/api/v1/domains");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task A_wrong_content_type_on_an_existing_endpoint_is_not_answered_as_missing()
    {
        // Signed in: the framework's answers to a mismatch carry no role, and
        // the rule that refuses what declares nothing would answer first.
        using HttpClient root = await _app.SignedInAsync(AccountRole.Administrator, "root");

        HttpResponseMessage response = await root.PostAsync(
            "/api/v1/auth/login",
            new StringContent("name=someone", System.Text.Encoding.UTF8, "text/plain"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task An_existing_endpoint_still_requires_signing_in()
    {
        Answer answer = await _app.GetAnonymouslyAsync("/api/v1/domains");

        // The open fallback must not open what exists.
        Assert.Equal(HttpStatusCode.Unauthorized, answer.Status);
    }
}

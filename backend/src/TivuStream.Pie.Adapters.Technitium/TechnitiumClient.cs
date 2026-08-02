using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using TivuStream.Pie.Adapters.Technitium.Responses;

namespace TivuStream.Pie.Adapters.Technitium;

/// <summary>
/// Transport towards the public HTTP API of Technitium DNS Server.
/// </summary>
/// <remarks>
/// This type is responsible for reaching the server, authenticating the
/// request and translating a failure into the vocabulary of the project. It
/// performs no mapping towards the Unified Data Model.
/// </remarks>
internal sealed class TechnitiumClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly string _apiToken;

    internal TechnitiumClient(HttpClient httpClient, TechnitiumOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        if (options.BaseAddress is null)
        {
            throw new AdapterException("The address of the Technitium instance is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiToken))
        {
            throw new AdapterException("The API token of the Technitium instance is not configured.");
        }

        httpClient.BaseAddress = options.BaseAddress;

        _httpClient = httpClient;
        _apiToken = options.ApiToken;
    }

    /// <summary>
    /// Formats an instant the way the Technitium API expects it.
    /// </summary>
    internal static string FormatInstant(DateTimeOffset instant)
    {
        return instant.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Performs a call whose payload is nested inside a dedicated property.
    /// </summary>
    /// <typeparam name="TResponse">Payload expected from the call.</typeparam>
    /// <param name="relativeUrl">Path and query of the call.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    internal async Task<TResponse> GetAsync<TResponse>(string relativeUrl, CancellationToken cancellationToken)
        where TResponse : class
    {
        TechnitiumEnvelope<TResponse> envelope = await ReadAsync<TechnitiumEnvelope<TResponse>>(
            relativeUrl,
            cancellationToken).ConfigureAwait(false);

        EnsureSucceeded(envelope.Status, envelope.ErrorMessage);

        return envelope.Response
            ?? throw new AdapterException("The Technitium instance returned no payload.");
    }

    /// <summary>
    /// Performs a call whose payload is returned at the root of the answer.
    /// </summary>
    /// <typeparam name="TResponse">Shape expected from the call.</typeparam>
    /// <param name="relativeUrl">Path and query of the call.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    internal async Task<TResponse> GetRootAsync<TResponse>(string relativeUrl, CancellationToken cancellationToken)
        where TResponse : TechnitiumRootResponse
    {
        TResponse response = await ReadAsync<TResponse>(relativeUrl, cancellationToken).ConfigureAwait(false);

        EnsureSucceeded(response.Status, response.ErrorMessage);

        return response;
    }

    /// <summary>
    /// Turns the outcome reported by the server into a failure when needed.
    /// </summary>
    /// <remarks>
    /// Only the message meant for people is carried over. The stack trace and
    /// the inner error are left behind on purpose.
    /// </remarks>
    private static void EnsureSucceeded(string? status, string? errorMessage)
    {
        if (string.Equals(status, "ok", StringComparison.Ordinal))
        {
            return;
        }

        string detail = string.IsNullOrWhiteSpace(errorMessage)
            ? "no description was provided"
            : errorMessage;

        throw new AdapterException($"The Technitium instance reported a failure: {detail}");
    }

    private async Task<TPayload> ReadAsync<TPayload>(string relativeUrl, CancellationToken cancellationToken)
        where TPayload : class
    {
        using HttpRequestMessage request = new(HttpMethod.Get, relativeUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

        HttpResponseMessage response;

        try
        {
            response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw new AdapterException("The Technitium instance could not be reached.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AdapterException("The Technitium instance did not answer in time.", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new AdapterException(
                    $"The Technitium instance answered with status {(int)response.StatusCode}.");
            }

            TPayload? payload;

            try
            {
                using Stream content = await response.Content
                    .ReadAsStreamAsync(cancellationToken)
                    .ConfigureAwait(false);

                payload = await JsonSerializer
                    .DeserializeAsync<TPayload>(content, SerializerOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (JsonException exception)
            {
                throw new AdapterException("The answer of the Technitium instance could not be read.", exception);
            }

            return payload
                ?? throw new AdapterException("The Technitium instance returned an empty answer.");
        }
    }
}

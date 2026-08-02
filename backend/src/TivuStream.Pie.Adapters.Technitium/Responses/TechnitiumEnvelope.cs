namespace TivuStream.Pie.Adapters.Technitium.Responses;

/// <summary>
/// Envelope wrapping the responses that carry their payload in a dedicated
/// property.
/// </summary>
/// <remarks>
/// The Technitium API uses two shapes. Most calls, among them every dashboard
/// call, nest the payload inside a <c>response</c> property. The calls
/// concerning the session return their fields at the root instead, next to
/// the outcome, and are represented by <see cref="TechnitiumRootResponse"/>.
/// <para>
/// The outcome of a call is carried by <see cref="Status"/> and not by the
/// HTTP status code: a failed call can arrive with HTTP 200.
/// </para>
/// <para>
/// The API also returns a stack trace and an inner error message. They are
/// deliberately not mapped, because diagnostic details of the backend must
/// not travel beyond the Adapter.
/// </para>
/// </remarks>
/// <typeparam name="TResponse">Payload carried by the response.</typeparam>
internal sealed class TechnitiumEnvelope<TResponse>
{
    /// <summary>
    /// Outcome of the call, as reported by the server.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Description of the failure, suitable for being shown to a person.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Payload of the response.
    /// </summary>
    public TResponse? Response { get; set; }
}

/// <summary>
/// Base of the responses that carry their payload at the root, next to the
/// outcome of the call.
/// </summary>
internal abstract class TechnitiumRootResponse
{
    /// <summary>
    /// Outcome of the call, as reported by the server.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Description of the failure, suitable for being shown to a person.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

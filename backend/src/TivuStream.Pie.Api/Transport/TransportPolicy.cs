namespace TivuStream.Pie.Api.Transport;

/// <summary>
/// What the encrypted channel allows the answers to promise.
/// </summary>
/// <param name="StrictTransportSecurity">
/// Whether answers over an encrypted connection carry
/// <c>Strict-Transport-Security</c>. Only with a certificate provided by the
/// operator, presumed recognised by browsers. With the generated one, a
/// browser that had received it would no longer let the person accept the
/// warning, and the first renewal would lock them out (Transport Security
/// Specification, Strict Transport Security).
/// </param>
internal sealed record TransportPolicy(bool StrictTransportSecurity);

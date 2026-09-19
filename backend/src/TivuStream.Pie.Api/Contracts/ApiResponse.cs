using System.Text.Json.Serialization;

namespace TivuStream.Pie.Api.Contracts;

/// <summary>
/// Common shape of every answer of the REST API.
/// </summary>
/// <remarks>
/// Defined by the API Specification.
/// </remarks>
/// <typeparam name="TData">Payload carried by the answer.</typeparam>
public sealed record ApiResponse<TData>
{
    /// <summary>
    /// Indicates whether the request was served.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Version of the API that produced the answer.
    /// </summary>
    public string ApiVersion { get; init; } = "v1";

    /// <summary>
    /// Moment the answer was produced.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Payload of the answer.
    /// </summary>
    public TData? Data { get; init; }

    /// <summary>
    /// Description of the failure, when the request could not be served.
    /// </summary>
    public ApiError? Error { get; init; }
}

/// <summary>
/// Builds the answers of the REST API.
/// </summary>
/// <remarks>
/// The builders live on a type of their own rather than on the generic
/// answer: a factory declared on a generic type has to be called stating the
/// type argument explicitly, which reads poorly at every call site.
/// </remarks>
public static class ApiResponse
{
    /// <summary>
    /// Builds an answer carrying a payload.
    /// </summary>
    /// <typeparam name="TData">Payload carried by the answer.</typeparam>
    /// <param name="data">Payload of the answer.</param>
    public static ApiResponse<TData> Ok<TData>(TData data)
    {
        return new ApiResponse<TData>
        {
            Success = true,
            Timestamp = DateTimeOffset.UtcNow,
            Data = data,
        };
    }

    /// <summary>
    /// Builds an answer describing a failure.
    /// </summary>
    /// <typeparam name="TData">Payload the answer would have carried.</typeparam>
    /// <param name="code">Identifier of the failure.</param>
    /// <param name="message">Description of the failure.</param>
    /// <param name="reason">Precise cause, in a form a program can read, when there is one.</param>
    public static ApiResponse<TData> Failed<TData>(string code, string message, string? reason = null)
    {
        return new ApiResponse<TData>
        {
            Success = false,
            Timestamp = DateTimeOffset.UtcNow,
            Error = new ApiError { Code = code, Message = message, Reason = reason },
        };
    }
}

/// <summary>
/// Description of a failure returned by the REST API.
/// </summary>
public sealed record ApiError
{
    /// <summary>
    /// Identifier of the failure.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Description of the failure, suitable for being shown to a person.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Precise cause of the failure, in a form a program can read, so that an
    /// interface can say it in the language of whoever is reading. Absent, and
    /// not null, when there is none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; init; }
}

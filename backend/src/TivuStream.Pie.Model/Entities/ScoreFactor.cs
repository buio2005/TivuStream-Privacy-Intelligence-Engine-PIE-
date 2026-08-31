namespace TivuStream.Pie.Model.Entities;

/// <summary>
/// A reason that determined the score of an area.
/// </summary>
/// <remarks>
/// Defined by the Data Model Specification. The codes and their meaning are
/// listed in the NPSS Specification.
/// <para>
/// A factor carries no text. The Core produces results, not prose: a sentence
/// already written belongs to one language, and would make it impossible to
/// present the same result in another without changing the Core.
/// </para>
/// <para>
/// Numbers travel as numbers rather than as text already formatted. How a
/// percentage is written belongs to the language, not to the measurement.
/// </para>
/// </remarks>
public sealed record ScoreFactor
{
    /// <summary>
    /// Identifies the statement.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Values that complete the statement.
    /// </summary>
    public IReadOnlyDictionary<string, object> Values { get; init; } =
        new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>
    /// Creates a factor that needs no values.
    /// </summary>
    /// <param name="code">Code of the factor.</param>
    /// <returns>The factor.</returns>
    public static ScoreFactor Of(string code)
    {
        return new ScoreFactor { Code = code };
    }

    /// <summary>
    /// Creates a factor carrying a single value.
    /// </summary>
    /// <param name="code">Code of the factor.</param>
    /// <param name="name">Name of the value.</param>
    /// <param name="value">The value itself.</param>
    /// <returns>The factor.</returns>
    public static ScoreFactor Of(string code, string name, object value)
    {
        return new ScoreFactor
        {
            Code = code,
            Values = new Dictionary<string, object>(StringComparer.Ordinal) { [name] = value },
        };
    }

    /// <summary>
    /// Creates a factor carrying two values.
    /// </summary>
    /// <param name="code">Code of the factor.</param>
    /// <param name="firstName">Name of the first value.</param>
    /// <param name="first">The first value.</param>
    /// <param name="secondName">Name of the second value.</param>
    /// <param name="second">The second value.</param>
    /// <returns>The factor.</returns>
    public static ScoreFactor Of(
        string code,
        string firstName,
        object first,
        string secondName,
        object second)
    {
        return new ScoreFactor
        {
            Code = code,
            Values = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [firstName] = first,
                [secondName] = second,
            },
        };
    }
}

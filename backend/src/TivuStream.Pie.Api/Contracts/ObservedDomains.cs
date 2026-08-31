using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Api.Contracts;

/// <summary>
/// The domains observed, together with the period they were observed in.
/// </summary>
/// <remarks>
/// An empty list without its period is ambiguous: whoever reads it cannot
/// tell "the network contacted nothing" from "the hour has just begun". The
/// first is a statement about the network, the second about the moment of
/// looking, and presenting them alike is false information.
/// </remarks>
public sealed record ObservedDomains
{
    /// <summary>
    /// Period the list refers to, absent when nothing was ever observed.
    /// </summary>
    public required ObservationPeriod? Period { get; init; }

    /// <summary>
    /// Domains observed in that period.
    /// </summary>
    public required IReadOnlyList<Domain> Domains { get; init; }
}

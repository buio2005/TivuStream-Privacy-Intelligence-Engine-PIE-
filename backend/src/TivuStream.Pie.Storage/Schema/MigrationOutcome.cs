namespace TivuStream.Pie.Storage.Schema;

/// <summary>
/// Describes what the migrator found and what it did.
/// </summary>
/// <remarks>
/// A change to the shape of stored data is not something to carry out
/// silently. The outcome is reported so that it can be logged and, when
/// relevant, shown.
/// </remarks>
public sealed record MigrationOutcome
{
    /// <summary>
    /// Version the database was at before the migrator ran.
    /// </summary>
    /// <remarks>
    /// Zero means the database had no schema at all.
    /// </remarks>
    public required int InitialVersion { get; init; }

    /// <summary>
    /// Version the database is at afterwards.
    /// </summary>
    public required int FinalVersion { get; init; }

    /// <summary>
    /// Migrations that were applied, in order.
    /// </summary>
    public IReadOnlyList<string> AppliedMigrations { get; init; } = [];

    /// <summary>
    /// Indicates whether the database was created by this run.
    /// </summary>
    public bool DatabaseWasCreated => InitialVersion == 0;

    /// <summary>
    /// Indicates whether anything changed.
    /// </summary>
    public bool SchemaChanged => AppliedMigrations.Count > 0;
}

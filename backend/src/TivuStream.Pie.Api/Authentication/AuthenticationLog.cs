namespace TivuStream.Pie.Api.Authentication;

/// <summary>
/// What is recorded about who signed in and what was done to the accounts.
/// </summary>
/// <remarks>
/// Authentication Specification, Logging. Events, outcomes and counts. Never a
/// password, a session identifier, the setup code or a source address.
/// <para>
/// A refused sign in records a count and not the name tried: a person who
/// types their password into the name field would otherwise find it in the
/// logs. A limit reached records which kind of counter, not whose.
/// </para>
/// </remarks>
internal static partial class AuthenticationLog
{
    /// <summary>
    /// The logger the endpoints write to. They are static functions, and
    /// cannot ask for a logger of their own type.
    /// </summary>
    internal static ILogger For(ILoggerFactory loggers)
    {
        return loggers.CreateLogger("TivuStream.Pie.Api.Authentication");
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "First run setup completed. Administrator '{Account}' created.")]
    internal static partial void SetupCompleted(ILogger logger, string account);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "'{Account}' signed in.")]
    internal static partial void SignedIn(ILogger logger, string account);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "A sign in was refused. Refused since the service started: {Count}.")]
    internal static partial void SignInRefused(ILogger logger, long count);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Too many failed attempts from one {Counter}. Further attempts wait {Seconds} seconds.")]
    internal static partial void LimitReached(ILogger logger, string counter, int seconds);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "'{Account}' changed their own password.")]
    internal static partial void PasswordChanged(ILogger logger, string account);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "'{Actor}' performed '{Operation}' on account '{Account}'.")]
    internal static partial void AccountChanged(ILogger logger, string actor, string operation, string account);
}

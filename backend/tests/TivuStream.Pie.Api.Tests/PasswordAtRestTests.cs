using System.Text;
using Microsoft.Extensions.DependencyInjection;
using TivuStream.Pie.Api.Authentication;
using TivuStream.Pie.Storage;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies that a password, once given, is nowhere to be found.
/// </summary>
/// <remarks>
/// Authentication Specification, Principles and Persistence: a secret is never
/// kept in clear. Whoever obtains the database file, for instance from a
/// forgotten backup, must not find in it anything a person typed.
/// </remarks>
public sealed class PasswordAtRestTests : IDisposable
{
    private const string Password = "a-password-nobody-should-ever-read";

    private readonly PieApplication _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    [Fact]
    public void The_password_is_not_anywhere_in_the_database_file()
    {
        AccountRepository accounts = _app.Services.GetRequiredService<AccountRepository>();
        PasswordHasher hasher = _app.Services.GetRequiredService<PasswordHasher>();

        accounts.Create("maria", AccountRole.Administrator, hasher.Hash(Password), true, PieApplication.Now);

        byte[] file = ReadDatabaseFile();

        // Positive control: the search is looking at the right file, which does
        // hold the account. Without it, an empty read would pass for a clean
        // database.
        Assert.True(Contains(file, "maria"), "the account should be in the file");
        Assert.True(Contains(file, "pbkdf2-sha512$"), "the hash should be in the file");

        Assert.False(Contains(file, Password), "the password must not be in the file");
        Assert.False(Contains(file, "nobody-should"), "no part of the password must be in the file");
    }

    private byte[] ReadDatabaseFile()
    {
        string path = _app.Services.GetRequiredService<SqliteConnectionFactory>().DatabasePath;

        // The database may still be open in this process. Reading must not
        // insist on exclusive access.
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using MemoryStream copy = new();

        stream.CopyTo(copy);

        return copy.ToArray();
    }

    private static bool Contains(byte[] haystack, string needle)
    {
        return haystack.AsSpan().IndexOf(Encoding.UTF8.GetBytes(needle)) >= 0;
    }
}

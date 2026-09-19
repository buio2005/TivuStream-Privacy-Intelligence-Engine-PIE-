using TivuStream.Pie.Api.Authentication;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies what is kept of a password, and how a password is recognised.
/// </summary>
/// <remarks>
/// Authentication Specification, Credentials: a salted hash and nothing from
/// which the password could be read back, the parameters recorded alongside
/// it so that they can grow, and the same password recognised however the
/// keyboard produced it.
/// </remarks>
public sealed class PasswordHasherTests
{
    private const string Password = "correct horse battery staple";

    private static readonly PasswordHasher Current = PasswordHasher.Standard;

    [Fact]
    public void The_stored_value_does_not_contain_the_password()
    {
        string stored = Current.Hash(Password);

        Assert.DoesNotContain(Password, stored, StringComparison.Ordinal);
        Assert.DoesNotContain("horse", stored, StringComparison.Ordinal);
    }

    [Fact]
    public void The_same_password_never_produces_the_same_stored_value_twice()
    {
        // The salt is per password. Two people with the same password must not
        // be recognisable as such to whoever reads the database.
        Assert.NotEqual(Current.Hash(Password), Current.Hash(Password));
    }

    [Fact]
    public void The_algorithm_and_its_parameters_travel_with_the_hash()
    {
        string stored = Current.Hash(Password);

        Assert.StartsWith("pbkdf2-sha512$210000$", stored, StringComparison.Ordinal);
    }

    [Fact]
    public void The_right_password_is_recognised()
    {
        string stored = Current.Hash(Password);

        Assert.Equal(PasswordVerification.Valid, Current.Verify(Password, stored));
    }

    [Theory]
    [InlineData("correct horse battery stapl")]
    [InlineData("Correct horse battery staple")]
    [InlineData("correct horse battery staple ")]
    [InlineData("")]
    public void A_wrong_password_is_not_recognised(string attempt)
    {
        string stored = Current.Hash(Password);

        Assert.Equal(PasswordVerification.Failed, Current.Verify(attempt, stored));
    }

    [Fact]
    public void The_same_password_typed_on_another_keyboard_is_recognised()
    {
        // "é" as one character, and as an "e" followed by an accent. They look
        // identical and differ in the bytes.
        string composed = "caffè corretto sempre";
        string decomposed = "caffè corretto sempre";

        string stored = Current.Hash(composed);

        Assert.NotEqual(composed, decomposed);
        Assert.Equal(PasswordVerification.Valid, Current.Verify(decomposed, stored));
    }

    [Fact]
    public void A_hash_made_with_lighter_parameters_is_recognised_and_asks_to_be_made_again()
    {
        string old = new PasswordHasher(iterations: 1_000).Hash(Password);

        // Still valid: the person must be let in. But the parameters have
        // grown since, and the next login is the only moment the password is
        // at hand to compute the new hash.
        Assert.Equal(PasswordVerification.ValidButOutdated, Current.Verify(Password, old));
    }

    [Fact]
    public void A_wrong_password_against_an_outdated_hash_is_simply_wrong()
    {
        string old = new PasswordHasher(iterations: 1_000).Hash(Password);

        Assert.Equal(PasswordVerification.Failed, Current.Verify("something else entirely", old));
    }

    [Fact]
    public void A_correct_hash_under_an_algorithm_the_software_does_not_know_is_refused()
    {
        // The hash and the password are right; only the label was changed. The
        // record is not what this software wrote, and is not to be trusted.
        string stored = Current.Hash(Password);
        string relabelled = "md5" + stored["pbkdf2-sha512".Length..];

        Assert.Equal(PasswordVerification.Failed, Current.Verify(Password, relabelled));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a hash")]
    [InlineData("pbkdf2-sha512$999999999$c2FsdA==$aGFzaA==")]
    [InlineData("pbkdf2-sha512$$$")]
    [InlineData("pbkdf2-sha512$abc$c2FsdA==$aGFzaA==")]
    [InlineData("pbkdf2-sha512$0$c2FsdA==$aGFzaA==")]
    [InlineData("pbkdf2-sha512$-5$c2FsdA==$aGFzaA==")]
    [InlineData("pbkdf2-sha512$210000$!!!$aGFzaA==")]
    [InlineData("md5$210000$c2FsdA==$aGFzaA==")]
    public void A_stored_value_that_cannot_be_read_never_lets_anyone_in(string damaged)
    {
        // A damaged record must fail closed. Nothing about it may be read as
        // "no password required".
        Assert.Equal(PasswordVerification.Failed, Current.Verify(Password, damaged));
        Assert.Equal(PasswordVerification.Failed, Current.Verify(string.Empty, damaged));
    }
}

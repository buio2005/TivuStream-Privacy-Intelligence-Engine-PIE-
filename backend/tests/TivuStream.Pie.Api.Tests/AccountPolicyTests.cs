using TivuStream.Pie.Api.Authentication;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies what is accepted as a name and as a password.
/// </summary>
/// <remarks>
/// Authentication Specification, Accounts And Roles and Credentials: length is
/// what counts, composition rules are deliberately absent, and a password
/// cannot be the name.
/// </remarks>
public sealed class AccountPolicyTests
{
    // ------------------------------------------------------------------
    // Passwords
    // ------------------------------------------------------------------

    [Fact]
    public void Twelve_characters_are_enough_and_eleven_are_not()
    {
        Assert.Equal(PasswordProblem.TooShort, AccountPolicy.CheckPassword(new string('a', 11), "maria"));
        Assert.Null(AccountPolicy.CheckPassword(new string('a', 12), "maria"));
    }

    [Fact]
    public void A_hundred_and_twenty_eight_characters_are_the_most()
    {
        Assert.Null(AccountPolicy.CheckPassword(new string('a', 128), "maria"));
        Assert.Equal(PasswordProblem.TooLong, AccountPolicy.CheckPassword(new string('a', 129), "maria"));
    }

    [Fact]
    public void No_rule_of_composition_is_imposed()
    {
        // Twelve lowercase letters, no capital, no digit, no symbol. Requiring
        // them produces predictable passwords, not stronger ones.
        Assert.Null(AccountPolicy.CheckPassword("abcdefghijkl", "maria"));
        Assert.Null(AccountPolicy.CheckPassword("correct horse battery staple", "maria"));
    }

    [Theory]
    [InlineData("mariamariamaria", "mariamariamaria")]
    [InlineData("MariaMariaMaria", "mariamariamaria")]
    public void A_password_cannot_be_the_name(string password, string username)
    {
        Assert.Equal(PasswordProblem.EqualsUsername, AccountPolicy.CheckPassword(password, username));
    }

    [Fact]
    public void Length_is_counted_in_characters_and_not_in_bytes()
    {
        // Twelve characters, of which each needs more than one byte. A rule
        // counted in bytes would penalise whoever writes in an accented
        // language.
        Assert.Null(AccountPolicy.CheckPassword(new string('è', 12), "maria"));
    }

    [Fact]
    public void Length_is_counted_after_the_same_normalisation_the_hash_uses()
    {
        // The single character U+FB03 becomes three letters when normalised.
        // Four of them are twelve characters to whoever compares passwords.
        string ligatures = string.Concat(Enumerable.Repeat("ﬃ", 4));

        Assert.Null(AccountPolicy.CheckPassword(ligatures, "maria"));
    }

    // ------------------------------------------------------------------
    // Names
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("maria")]
    [InlineData("m.rossi")]
    [InlineData("anna-lisa_2")]
    [InlineData("abc")]
    [InlineData("a23456789012345678901234567890bc")]
    public void An_acceptable_name_is_accepted(string name)
    {
        Assert.True(AccountPolicy.IsValidUsername(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("a234567890123456789012345678901bc")]
    [InlineData("Maria")]
    [InlineData("ma ria")]
    [InlineData("maria@casa")]
    [InlineData("marìa")]
    [InlineData("../etc")]
    public void An_unacceptable_name_is_refused(string name)
    {
        // Capitals are refused here because the name is given in its
        // canonical form. Comparison ignores case, but only after this.
        Assert.False(AccountPolicy.IsValidUsername(name));
    }
}

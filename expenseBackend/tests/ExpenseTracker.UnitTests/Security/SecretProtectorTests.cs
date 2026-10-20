using System.Security.Cryptography;
using ExpenseTracker.Infrastructure.Security;

namespace ExpenseTracker.UnitTests.Security;

public class SecretProtectorTests
{
    private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    [Fact]
    public void ProtectThenUnprotect_ReturnsOriginalText()
    {
        var protector = new SecretProtector(NewKey());

        var stored = protector.Protect("gsk_example_key");

        Assert.Equal("gsk_example_key", protector.Unprotect(stored));
    }

    // The stored value does not reveal the text, and a new nonce is used every time.
    [Fact]
    public void Protect_HidesText_AndGivesDifferentValueEachTime()
    {
        var protector = new SecretProtector(NewKey());

        var first = protector.Protect("gsk_example_key");
        var second = protector.Protect("gsk_example_key");

        Assert.DoesNotContain("gsk_example_key", first);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Unprotect_WithDifferentKey_Throws()
    {
        var stored = new SecretProtector(NewKey()).Protect("gsk_example_key");
        var otherProtector = new SecretProtector(NewKey());

        Assert.ThrowsAny<CryptographicException>(() => otherProtector.Unprotect(stored));
    }

    [Fact]
    public void Unprotect_ChangedValue_Throws()
    {
        var protector = new SecretProtector(NewKey());
        var bytes = Convert.FromBase64String(protector.Protect("gsk_example_key"));
        bytes[^1] ^= 1;

        Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect(Convert.ToBase64String(bytes)));
    }

    [Theory]
    [InlineData("not base64!")]
    [InlineData("c2hvcnQ=")] // valid base64, but only 5 bytes
    public void Constructor_InvalidKey_Throws(string key)
    {
        Assert.Throws<InvalidOperationException>(() => new SecretProtector(key));
    }
}

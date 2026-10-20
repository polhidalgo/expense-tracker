using System.Security.Cryptography;
using System.Text;

namespace ExpenseTracker.Infrastructure.Security;

public sealed class SecretProtector
{
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public SecretProtector(string base64Key)
    {
        try
        {
            _key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("The encryption key is not valid base64.");
        }

        if (_key.Length != KeySize)
            throw new InvalidOperationException($"The encryption key must be {KeySize} bytes.");
    }

    public string Protect(string text)
    {
        var plain = Encoding.UTF8.GetBytes(text);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var cipher = new byte[plain.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        return Convert.ToBase64String([.. nonce, .. tag, .. cipher]);
    }

    public string Unprotect(string value)
    {
        var data = Convert.FromBase64String(value);
        if (data.Length < NonceSize + TagSize)
            throw new CryptographicException("The encrypted value is too short.");

        var nonce = data[..NonceSize];
        var tag = data[NonceSize..(NonceSize + TagSize)];
        var cipher = data[(NonceSize + TagSize)..];
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }
}

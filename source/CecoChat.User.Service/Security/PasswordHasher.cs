using System.Security.Cryptography;
using System.Text;

namespace CecoChat.User.Service.Security;

public interface IPasswordHasher
{
    /// <summary>
    /// Returns the hashed password and salt in a single string.
    /// </summary>
    string Hash(string password);

    bool Verify(string password, string hashAndSalt);
}

internal class PasswordHasher : IPasswordHasher
{
    // based on the following resources:
    // https://stackoverflow.com/a/59338614/608971
    // https://security.stackexchange.com/a/27971/45802
    // https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html#pbkdf2

    // salt size is appropriate to be 128 bits, or 16 bytes
    private const int SaltSizeInBytes = 16;
    // password hash size is appropriate to be 128 bits, or 16 bytes
    private const int PasswordHashSizeInBytes = 16;
    // recommended iterations by OWASP in Jan 2023 is at least 210 000 with SHA512
    // but it is not a bad idea to safeguard and increase it
    private const int Iterations = 1_000_000;
    // SHA-512 is appropriate for 64-bit arithmetic operations
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA512;

    private const char Separator = ':';
    private const string Version1 = "1";

    public string Hash(string password)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSizeInBytes);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, Iterations, HashAlgorithm, PasswordHashSizeInBytes);

        string result = $"{Version1}{Separator}{Convert.ToBase64String(saltBytes)}{Separator}{Convert.ToBase64String(hashBytes)}";
        return result;
    }

    public bool Verify(string password, string hashAndSalt)
    {
        string[] split = hashAndSalt.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length != 3)
        {
            throw new InvalidOperationException($"Invalid format for hash-and-salt string - separated parts should be exactly 3.");
        }

        string version = split[0];
        if (version != Version1)
        {
            throw new NotSupportedException($"Unsupported version '{version}' of hash-and-salt string.");
        }

        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] saltBytes = Convert.FromBase64String(split[1]);
        byte[] expectedHashBytes = Convert.FromBase64String(split[2]);
        byte[] actualHashBytes = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, Iterations, HashAlgorithm, outputLength: PasswordHashSizeInBytes);

        bool areEqual = CryptographicOperations.FixedTimeEquals(expectedHashBytes, actualHashBytes);
        return areEqual;
    }
}

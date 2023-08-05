using System.Security.Cryptography;
using System.Text;

namespace CecoChat.Data.User.Security;

public interface IPasswordHasher
{
    /// <summary>
    /// Returns the hashed password and salt in a single string.
    /// </summary>
    string Hash(string password);

    bool Verify(string password, string hashAndSalt);
}

public class PasswordHasher : IPasswordHasher
{
    private const int KeySize = 128;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA512;
    private const char Separator = ':';
    private const string Version1 = "1";

    public string Hash(string password)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(KeySize);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, Iterations, HashAlgorithm, outputLength: KeySize);

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
        byte[] actualHashBytes = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, Iterations, HashAlgorithm, outputLength: KeySize);

        bool areEqual = CryptographicOperations.FixedTimeEquals(expectedHashBytes, actualHashBytes);
        return areEqual;
    }
}

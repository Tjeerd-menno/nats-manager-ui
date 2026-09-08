using System.Globalization;
using System.Security.Cryptography;
using NatsManager.Application.Modules.Auth.Ports;

namespace NatsManager.Infrastructure.Auth;

/// <summary>
/// PBKDF2 password hasher producing a self-describing, versioned hash string of the form
/// <c>{prf}.{iterations}.{saltBase64}.{hashBase64}</c> (e.g. <c>pbkdf2-sha512.100000....</c>).
/// The embedded parameters allow the PRF/iteration count to be upgraded over time while still
/// verifying previously stored hashes, and enable rehash-on-login via <see cref="NeedsRehash"/>.
/// Legacy <c>{saltBase64}.{hashBase64}</c> hashes remain verifiable for backward compatibility.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int CurrentIterations = 100_000;
    private const string CurrentPrf = "pbkdf2-sha512";
    private static readonly HashAlgorithmName CurrentAlgorithm = HashAlgorithmName.SHA512;

    // Parameters assumed for legacy two-part hashes created before the versioned format existed.
    private const int LegacyIterations = 100_000;
    private static readonly HashAlgorithmName LegacyAlgorithm = HashAlgorithmName.SHA512;

    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, CurrentIterations, CurrentAlgorithm, HashSize);

        return string.Join(
            '.',
            CurrentPrf,
            CurrentIterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public static bool Verify(string password, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        if (!TryParse(passwordHash, out var parsed))
        {
            return false;
        }

        var testHash = Rfc2898DeriveBytes.Pbkdf2(
            password, parsed.Salt, parsed.Iterations, parsed.Algorithm, parsed.Hash.Length);

        return CryptographicOperations.FixedTimeEquals(parsed.Hash, testHash);
    }

    /// <summary>
    /// Returns true when the supplied hash should be re-computed with current parameters
    /// (legacy/unparseable format, weaker algorithm, or fewer iterations than current).
    /// </summary>
    public static bool NeedsRehash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || !TryParse(passwordHash, out var parsed))
        {
            return true;
        }

        return !parsed.IsCurrentFormat
            || parsed.Algorithm != CurrentAlgorithm
            || parsed.Iterations < CurrentIterations;
    }

    private static bool TryParse(string passwordHash, out ParsedHash parsed)
    {
        parsed = default;
        var parts = passwordHash.Split('.');

        try
        {
            switch (parts.Length)
            {
                case 4:
                    {
                        var algorithm = PrfToAlgorithm(parts[0]);
                        if (algorithm is null)
                        {
                            return false;
                        }

                        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iterations)
                            || iterations < 1)
                        {
                            return false;
                        }

                        var salt = Convert.FromBase64String(parts[2]);
                        var hash = Convert.FromBase64String(parts[3]);
                        if (!HasExpectedSizes(salt, hash))
                        {
                            return false;
                        }

                        parsed = new ParsedHash(
                            salt,
                            hash,
                            iterations,
                            algorithm.Value,
                            IsCurrentFormat: true);
                        return true;
                    }

                case 2:
                    {
                        var salt = Convert.FromBase64String(parts[0]);
                        var hash = Convert.FromBase64String(parts[1]);
                        if (!HasExpectedSizes(salt, hash))
                        {
                            return false;
                        }

                        parsed = new ParsedHash(
                            salt,
                            hash,
                            LegacyIterations,
                            LegacyAlgorithm,
                            IsCurrentFormat: false);
                        return true;
                    }

                default:
                    return false;
            }
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool HasExpectedSizes(byte[] salt, byte[] hash) =>
        salt.Length == SaltSize && hash.Length == HashSize;

    private static HashAlgorithmName? PrfToAlgorithm(string prf) => prf switch
    {
        "pbkdf2-sha512" => HashAlgorithmName.SHA512,
        "pbkdf2-sha256" => HashAlgorithmName.SHA256,
        _ => null
    };

    private readonly record struct ParsedHash(
        byte[] Salt,
        byte[] Hash,
        int Iterations,
        HashAlgorithmName Algorithm,
        bool IsCurrentFormat);

    string IPasswordHasher.Hash(string password) => Hash(password);
    bool IPasswordHasher.Verify(string password, string passwordHash) => Verify(password, passwordHash);
    bool IPasswordHasher.NeedsRehash(string passwordHash) => NeedsRehash(passwordHash);
}

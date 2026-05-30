namespace NatsManager.Application.Modules.Auth.Ports;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);

    /// <summary>
    /// Returns true when a stored hash was produced with outdated parameters and should be
    /// re-hashed (e.g. on next successful login) using current parameters.
    /// </summary>
    bool NeedsRehash(string passwordHash);
}

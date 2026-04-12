namespace NatsManager.Application.Modules.Auth.Ports;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

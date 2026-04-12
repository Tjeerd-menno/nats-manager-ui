using NatsManager.Application.Modules.Environments.Ports;

namespace NatsManager.Infrastructure.Auth;

public sealed class NoOpCredentialEncryptionService : ICredentialEncryptionService
{
    public string Encrypt(string plainText) => plainText;
    public string Decrypt(string cipherText) => cipherText;
}

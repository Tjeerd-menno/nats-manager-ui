namespace NatsManager.Application.Modules.Environments.Ports;

public interface ICredentialEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

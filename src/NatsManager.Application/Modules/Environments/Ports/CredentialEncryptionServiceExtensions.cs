using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.Environments.Ports;

/// <summary>
/// Convenience helpers for <see cref="ICredentialEncryptionService"/> shared by the environment
/// register/update use cases.
/// </summary>
public static class CredentialEncryptionServiceExtensions
{
    /// <summary>
    /// Encrypts the supplied credential when one is required and present; returns <c>null</c> when the
    /// credential type is <see cref="CredentialType.None"/> or no credential value was provided.
    /// </summary>
    public static string? EncryptCredential(
        this ICredentialEncryptionService encryptionService,
        CredentialType credentialType,
        string? credential)
    {
        ArgumentNullException.ThrowIfNull(encryptionService);

        return credentialType != CredentialType.None && !string.IsNullOrEmpty(credential)
            ? encryptionService.Encrypt(credential)
            : null;
    }
}

using NATS.Client.Core;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Infrastructure.Nats;

internal static class NatsAuthHelper
{
    public static NatsAuthOpts BuildAuthOpts(CredentialType credentialType, string? credential)
    {
        if (credential is null)
            return NatsAuthOpts.Default;

        return credentialType switch
        {
            CredentialType.Token => new NatsAuthOpts { Token = credential },
            CredentialType.UserPassword => BuildUserPasswordAuth(credential),
            CredentialType.NKey => new NatsAuthOpts { NKeyFile = credential },
            CredentialType.CredsFile => new NatsAuthOpts { CredsFile = credential },
            _ => NatsAuthOpts.Default
        };
    }

    private static NatsAuthOpts BuildUserPasswordAuth(string credential)
    {
        var separatorIndex = credential.IndexOf(':');
        if (separatorIndex < 0)
            return new NatsAuthOpts { Token = credential };

        return new NatsAuthOpts
        {
            Username = credential[..separatorIndex],
            Password = credential[(separatorIndex + 1)..]
        };
    }
}

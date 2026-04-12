namespace NatsManager.Domain.Modules.Common;

public enum ConnectionStatus
{
    Unknown,
    Available,
    Degraded,
    Unavailable
}

public enum CredentialType
{
    None,
    Token,
    UserPassword,
    NKey,
    CredsFile
}

public enum ActionType
{
    Create,
    Update,
    Delete,
    TestInvoke,
    Publish,
    Subscribe,
    Login,
    Logout,
    PermissionChange
}

public enum ResourceType
{
    Environment,
    Stream,
    Consumer,
    KvBucket,
    KvKey,
    ObjectBucket,
    ObjectItem,
    Service,
    User,
    Role
}

public enum Outcome
{
    Success,
    Failure,
    Warning
}

public enum AuditSource
{
    UserInitiated,
    SystemGenerated
}

public enum KeyOperation
{
    Put,
    Delete,
    Purge
}

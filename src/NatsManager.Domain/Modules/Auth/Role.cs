namespace NatsManager.Domain.Modules.Auth;

public sealed class Role
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private Role() { }

    public static Role Create(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty
        };
    }

    public static class PredefinedNames
    {
        public const string ReadOnly = "ReadOnly";
        public const string Operator = "Operator";
        public const string Administrator = "Administrator";
        public const string Auditor = "Auditor";
    }
}

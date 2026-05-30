namespace NatsManager.Application.Modules.Environments.Ports;

/// <summary>
/// Implemented by in-memory stores that retain per-environment data. Allows that data to be released
/// when an environment is deleted, preventing unbounded growth for environments that no longer exist.
/// </summary>
public interface IEnvironmentScopedStore
{
    /// <summary>Releases all retained data for the given environment. A no-op when nothing is retained.</summary>
    void EvictEnvironment(Guid environmentId);
}

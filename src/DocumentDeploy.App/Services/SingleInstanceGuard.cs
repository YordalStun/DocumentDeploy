namespace DocumentDeploy.App.Services;

/// <summary>Stops a second copy of the tray app from starting (e.g. double-clicking the exe
/// while it's already running in the tray).</summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;

    public bool IsFirstInstance { get; }

    public SingleInstanceGuard()
    {
        _mutex = new Mutex(initiallyOwned: true, "DocumentDeploy-SingleInstance-Guard", out var createdNew);
        IsFirstInstance = createdNew;
    }

    public void Dispose() => _mutex.Dispose();
}

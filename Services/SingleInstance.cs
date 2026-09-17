using System.Net.Sockets;
using System.Runtime.Versioning;

namespace CursorPace.Services;

public static class SingleInstance
{
    public const string MutexName = "CursorPace_SingleInstance";
    public const string EventName = MutexName + "_Event";

    // Set in Program.Main after a successful acquire, before Avalonia starts.
    // A second process must not reach Initialize() (TrayIcon lives in App.axaml).
    public static ISingleInstance? Current { get; internal set; }

    public static ISingleInstance Create() =>
        OperatingSystem.IsWindows()
            ? new WindowsSingleInstance()
            : new UnixSingleInstance(WebViewProfilePaths.AppDataDirectory);
}

[SupportedOSPlatform("windows")]
internal sealed class WindowsSingleInstance : ISingleInstance
{
    private Mutex? _mutex;
    private EventWaitHandle? _eventWaitHandle;
    private Thread? _namedEventThread;

    public bool TryAcquire()
    {
        _mutex = new Mutex(true, SingleInstance.MutexName, out var createdNew);
        if (createdNew)
            return true;

        _mutex.Dispose();
        _mutex = null;
        return false;
    }

    public void Listen(Action onActivated)
    {
        _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, SingleInstance.EventName);
        _namedEventThread = new Thread(() =>
        {
            while (true)
            {
                try
                {
                    _eventWaitHandle.WaitOne();
                    onActivated();
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
            }
        })
        { IsBackground = true };
        _namedEventThread.Start();
    }

    public void SignalExisting()
    {
        try
        {
            using var handle = EventWaitHandle.OpenExisting(SingleInstance.EventName);
            handle.Set();
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        _namedEventThread?.Interrupt();
        _eventWaitHandle?.Dispose();
        _mutex?.Dispose();
        _namedEventThread = null;
        _eventWaitHandle = null;
        _mutex = null;
    }
}

internal sealed class UnixSingleInstance : ISingleInstance
{
    private readonly string _lockPath;
    private readonly string _socketPath;
    private FileStream? _lockStream;
    private Socket? _listener;
    private CancellationTokenSource? _listenCts;

    public UnixSingleInstance(string appDataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appDataDirectory);
        _lockPath = Path.Combine(appDataDirectory, "instance.lock");
        _socketPath = Path.Combine(appDataDirectory, "instance.sock");
    }

    public bool TryAcquire()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_lockPath)!);
        try
        {
            _lockStream = new FileStream(
                _lockPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None);
            return true;
        }
        catch (IOException)
        {
            _lockStream?.Dispose();
            _lockStream = null;
            return false;
        }
    }

    public void Listen(Action onActivated)
    {
        _listenCts = new CancellationTokenSource();
        try
        {
            if (File.Exists(_socketPath))
                File.Delete(_socketPath);
        }
        catch
        {
        }

        _listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        _listener.Bind(new UnixDomainSocketEndPoint(_socketPath));
        _listener.Listen(1);

        var token = _listenCts.Token;
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var client = await _listener.AcceptAsync(token);
                    onActivated();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    if (token.IsCancellationRequested)
                        break;
                }
            }
        }, token);
    }

    public void SignalExisting()
    {
        try
        {
            using var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            client.Connect(new UnixDomainSocketEndPoint(_socketPath));
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        _listenCts?.Cancel();
        _listener?.Dispose();
        _lockStream?.Dispose();
        try
        {
            if (File.Exists(_socketPath))
                File.Delete(_socketPath);
        }
        catch
        {
        }
        _listenCts = null;
        _listener = null;
        _lockStream = null;
    }
}

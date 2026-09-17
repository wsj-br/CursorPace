using System.Net.Sockets;
using CursorPace.Services;

namespace CursorPace.Tests;

public class SingleInstanceTests
{
    [Fact]
    public void TryAcquire_SecondInstanceFailsUntilFirstDisposes()
    {
        var dir = CreateTempDir();
        try
        {
            using var first = new UnixSingleInstance(dir);
            Assert.True(first.TryAcquire());

            using (var second = new UnixSingleInstance(dir))
                Assert.False(second.TryAcquire());

            first.Dispose();

            using var third = new UnixSingleInstance(dir);
            Assert.True(third.TryAcquire());
        }
        finally
        {
            TryDeleteDir(dir);
        }
    }

    [Fact]
    public async Task SignalExisting_InvokesListenCallback()
    {
        if (!Socket.OSSupportsUnixDomainSockets)
            return;

        var dir = CreateTempDir();
        try
        {
            using var first = new UnixSingleInstance(dir);
            Assert.True(first.TryAcquire());

            var activated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            first.Listen(() => activated.TrySetResult());

            using var second = new UnixSingleInstance(dir);
            Assert.False(second.TryAcquire());
            second.SignalExisting();

            await activated.Task.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            TryDeleteDir(dir);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "CursorPaceSingleInstance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void TryDeleteDir(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch
        {
        }
    }
}

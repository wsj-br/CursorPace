using CursorUsageProgress.ViewModels;

namespace CursorUsageProgress.Tests;

public class AsyncRelayCommandTests
{
    [Fact]
    public async Task Execute_ObservesExceptions()
    {
        var command = new AsyncRelayCommand(() => throw new InvalidOperationException("boom"));

        command.Execute(null);
        await Task.Delay(50);

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task Execute_BlocksReentrancyUntilComplete()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runs = 0;
        var command = new AsyncRelayCommand(async () =>
        {
            runs++;
            started.TrySetResult();
            await release.Task;
        });

        command.Execute(null);
        await started.Task;
        Assert.False(command.CanExecute(null));
        command.Execute(null);
        release.TrySetResult();
        await Task.Delay(50);

        Assert.Equal(1, runs);
        Assert.True(command.CanExecute(null));
    }
}

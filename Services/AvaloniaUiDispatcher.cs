using Avalonia.Threading;

namespace CursorPace.Services;

public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => Dispatcher.UIThread.Post(action);

    public IUiTimer CreateTimer() => new AvaloniaUiTimer();

    private sealed class AvaloniaUiTimer : IUiTimer
    {
        private readonly DispatcherTimer _timer = new();

        public AvaloniaUiTimer()
        {
            _timer.Tick += OnTick;
        }

        public TimeSpan Interval
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        public bool IsRepeating { get; set; } = true;

        public event EventHandler? Tick;

        public void Start() => _timer.Start();

        public void Stop() => _timer.Stop();

        private void OnTick(object? sender, EventArgs e)
        {
            if (!IsRepeating)
                _timer.Stop();
            Tick?.Invoke(this, EventArgs.Empty);
        }
    }
}

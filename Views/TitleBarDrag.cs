using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace CursorUsageProgress.Views;

internal sealed class TitleBarDrag
{
    private readonly Window _window;
    private readonly Control _dragSurface;
    private bool _dragging;
    private PixelPoint _windowOrigin;
    private PixelPoint _pointerOrigin;

    public TitleBarDrag(Window window, Control dragSurface)
    {
        _window = window;
        _dragSurface = dragSurface;
    }

    public void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (IsInteractiveChild(e.Source))
            return;

        if (!e.GetCurrentPoint(_window).Properties.IsLeftButtonPressed)
            return;

        if (OperatingSystem.IsWindows())
        {
            _window.BeginMoveDrag(e);
            return;
        }

        _dragging = true;
        _windowOrigin = _window.Position;
        _pointerOrigin = GetPointerScreenPosition(e);
        e.Pointer.Capture(_dragSurface);
        e.Handled = true;
    }

    public void OnPointerMoved(PointerEventArgs e)
    {
        if (!_dragging)
            return;

        var screen = GetPointerScreenPosition(e);
        var dx = screen.X - _pointerOrigin.X;
        var dy = screen.Y - _pointerOrigin.Y;
        _window.Position = new PixelPoint(_windowOrigin.X + dx, _windowOrigin.Y + dy);
        e.Handled = true;
    }

    public void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!_dragging)
            return;

        EndDrag(e.Pointer);
        e.Handled = true;
    }

    public void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        if (!_dragging)
            return;

        EndDrag(e.Pointer);
    }

    private void EndDrag(IPointer pointer)
    {
        _dragging = false;
        if (pointer.Captured == _dragSurface)
            pointer.Capture(null);
    }

    private PixelPoint GetPointerScreenPosition(PointerEventArgs e)
    {
        var local = e.GetCurrentPoint(_dragSurface).Position;
        return _dragSurface.PointToScreen(local);
    }

    private static bool IsInteractiveChild(object? source)
    {
        for (var control = source as Control; control != null; control = control.Parent as Control)
        {
            if (control is Button)
                return true;
        }

        return false;
    }
}

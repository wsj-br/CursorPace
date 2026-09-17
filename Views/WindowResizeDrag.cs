using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CursorPace.Services;

namespace CursorPace.Views;

internal sealed class WindowResizeDrag
{
    private readonly Window _window;
    private bool _resizing;
    private WindowEdge _edge;
    private Control? _grip;
    private PixelPoint _windowOrigin;
    private PixelPoint _pointerOrigin;
    private double _originWidth;
    private double _originHeight;

    public WindowResizeDrag(Window window)
    {
        _window = window;
    }

    public void OnPointerPressed(WindowEdge edge, Control grip, PointerPressedEventArgs e)
    {
        if (!_window.CanResize || _window.WindowState != WindowState.Normal)
            return;

        if (!e.GetCurrentPoint(_window).Properties.IsLeftButtonPressed)
            return;

        if (OperatingSystem.IsWindows())
        {
            _window.BeginResizeDrag(edge, e);
            return;
        }

        _resizing = true;
        _edge = edge;
        _grip = grip;
        _windowOrigin = _window.Position;
        _pointerOrigin = grip.PointToScreen(e.GetPosition(grip));
        _originWidth = _window.Width;
        _originHeight = _window.Height;
        e.Pointer.Capture(grip);
        e.Handled = true;
    }

    public void OnPointerMoved(PointerEventArgs e)
    {
        if (!_resizing || _grip is null)
            return;

        var screen = _grip.PointToScreen(e.GetPosition(_grip));
        var (x, y, width, height) = WindowResize.Apply(
            _windowOrigin.X,
            _windowOrigin.Y,
            _originWidth,
            _originHeight,
            screen.X - _pointerOrigin.X,
            screen.Y - _pointerOrigin.Y,
            _window.RenderScaling,
            MinLimit(_window.MinWidth),
            MinLimit(_window.MinHeight),
            west: IsWest(_edge),
            east: IsEast(_edge),
            north: IsNorth(_edge),
            south: IsSouth(_edge));

        _window.Width = width;
        _window.Height = height;
        if (IsWest(_edge) || IsNorth(_edge))
            _window.Position = new PixelPoint(x, y);

        e.Handled = true;
    }

    public void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!_resizing)
            return;

        EndDrag(e.Pointer);
        e.Handled = true;
    }

    public void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        if (!_resizing)
            return;

        EndDrag(e.Pointer);
    }

    private void EndDrag(IPointer pointer)
    {
        _resizing = false;
        if (_grip is not null && pointer.Captured == _grip)
            pointer.Capture(null);
        _grip = null;
    }

    private static double MinLimit(double value) =>
        double.IsNaN(value) || value < 0 ? 0 : value;

    private static bool IsWest(WindowEdge edge) =>
        edge is WindowEdge.West or WindowEdge.NorthWest or WindowEdge.SouthWest;

    private static bool IsEast(WindowEdge edge) =>
        edge is WindowEdge.East or WindowEdge.NorthEast or WindowEdge.SouthEast;

    private static bool IsNorth(WindowEdge edge) =>
        edge is WindowEdge.North or WindowEdge.NorthWest or WindowEdge.NorthEast;

    private static bool IsSouth(WindowEdge edge) =>
        edge is WindowEdge.South or WindowEdge.SouthWest or WindowEdge.SouthEast;
}

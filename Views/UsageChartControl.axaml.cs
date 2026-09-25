using System.Globalization;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using CursorPace.Models;
using CursorPace.Services;

namespace CursorPace.Views;

public partial class UsageChartControl : UserControl
{
    private const double PlotLeft = 38;
    private const double PlotRightPad = 8;
    private const double PlotTop = 28;
    private const double PlotBottomPad = 28;
    private const double MinTickSpacing = 16;
    private const double EstimatedStrokeThickness = 2;
    private const double UsageStrokeThickness = 4;
    private const double SampleMarkerDiameter = 7;
    private const double EndpointLabelOffsetX = 6;
    private const double MinZoomPixelSpan = 4;
    private static readonly (double X, double Y)[] HaloOffsets =
    [
        (-1, -1), (0, -1), (1, -1),
        (-1, 0),           (1, 0),
        (-1, 1),  (0, 1),  (1, 1)
    ];
    private bool _rebuilding;
    private bool _plotReady;
    private bool _selecting;
    private double _selectionOriginX;
    private double _selectionCurrentX;
    private IPointer? _selectionPointer;
    private Rect _plot;
    private decimal _xMin;
    private decimal _xMax;

    private static readonly Color ExpectedUsageColor = Color.FromArgb(255, 100, 116, 139);
    private static readonly Color CursorEstimatedColor = Color.FromArgb(255, 21, 128, 61);
    private static readonly Color OtherEstimatedColor = Color.FromArgb(255, 2, 132, 199);

    public static readonly StyledProperty<UsageChartDocument?> DocumentProperty =
        AvaloniaProperty.Register<UsageChartControl, UsageChartDocument?>(nameof(Document));

    public static readonly StyledProperty<UsageChartRange> SelectedRangeProperty =
        AvaloniaProperty.Register<UsageChartControl, UsageChartRange>(
            nameof(SelectedRange),
            UsageChartRange.OneMonth);

    public static readonly StyledProperty<UsageChartViewport?> CustomViewportProperty =
        AvaloniaProperty.Register<UsageChartControl, UsageChartViewport?>(nameof(CustomViewport));

    public UsageChartControl()
    {
        InitializeComponent();
        DocumentProperty.Changed.AddClassHandler<UsageChartControl>((control, _) => control.RebuildPlot());
        SelectedRangeProperty.Changed.AddClassHandler<UsageChartControl>((control, _) => control.UpdateRangeButtons());
        ActualThemeVariantChanged += (_, _) => RebuildPlot();
        Loaded += (_, _) =>
        {
            UpdateRangeButtons();
            RebuildPlot();
        };
        SizeChanged += (_, _) => RebuildPlot();
        IsVisibleProperty.Changed.AddClassHandler<UsageChartControl>((control, _) => control.RebuildPlot());
    }

    public UsageChartDocument? Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public UsageChartRange SelectedRange
    {
        get => GetValue(SelectedRangeProperty);
        set => SetValue(SelectedRangeProperty, value);
    }

    public UsageChartViewport? CustomViewport
    {
        get => GetValue(CustomViewportProperty);
        set => SetValue(CustomViewportProperty, value);
    }

    private Size PlotSize
    {
        get
        {
            var host = PlotHost?.Bounds.Size ?? default;
            if (host.Width >= 80 && host.Height >= 60)
                return host;
            var self = Bounds.Size;
            if (self.Width >= 80 && self.Height >= 60)
                return new Size(self.Width, Math.Max(60, self.Height - 40));
            return host;
        }
    }

    private void OnPlotSizeChanged(object? sender, SizeChangedEventArgs e) => RebuildPlot();

    private void RebuildPlot()
    {
        if (_rebuilding)
            return;
        _rebuilding = true;
        try
        {
        CancelSelection();
        if (PlotCanvas == null || HoverCanvas == null || SelectionCanvas == null || HoverBox == null || LegendPanel == null || EmptyPlotText == null)
            return;

        PlotCanvas.Children.Clear();
        HoverCanvas.Children.Clear();
        SelectionCanvas.Children.Clear();
        HoverBox.IsVisible = false;
        _plotReady = false;
        LegendPanel.Children.Clear();
        var document = Document;
        var hostWidth = PlotSize.Width;
        var hostHeight = PlotSize.Height;
        var hasSeries = document != null && document.ExpectedUsage.Count >= 2;
        EmptyPlotText.IsVisible = document == null || !hasSeries;
        if (document == null || !IsEffectivelyVisible || hostWidth < 80 || hostHeight < 60)
            return;

        PlotCanvas.Width = hostWidth;
        PlotCanvas.Height = hostHeight;
        HoverCanvas.Width = hostWidth;
        HoverCanvas.Height = hostHeight;

        var plot = new Rect(
            PlotLeft,
            PlotTop,
            Math.Max(40, hostWidth - PlotLeft - PlotRightPad),
            Math.Max(40, hostHeight - PlotTop - PlotBottomPad));

        var xMin = document.VisibleStartX;
        var xMax = document.VisibleEndX > xMin ? document.VisibleEndX : xMin + 1m;
        var yMin = document.YMin;
        var yMax = document.YMax > yMin ? document.YMax : yMin + UsageChartSeriesBuilder.YTickStep;
        _plot = plot;
        _xMin = xMin;
        _xMax = xMax;

        var mutedBrush = ThemeBrush("ThemeForegroundLowBrush", Color.FromArgb(255, 120, 120, 120));
        var gridBrush = ThemeBrush("CardStrokeBrush", Color.FromArgb(60, 128, 128, 128));
        var verticalBrush = new SolidColorBrush(ThemeColor("ChartGridLineColor", Color.FromArgb(40, 160, 160, 160)));
        var boxBrush = ThemeBrush("CardStrokeBrush", Color.FromArgb(140, 140, 140, 140));
        var limitBrush = ThemeBrush("CalendarMutedForegroundBrush", Color.FromArgb(180, 128, 128, 128));
        var expectedUsage = ThemeColor("ChartExpectedUsageColor", ExpectedUsageColor);
        var cursorColor = ThemeColor("ChartCursorEstimatedColor", CursorEstimatedColor);
        var otherColor = ThemeColor("ChartOtherEstimatedColor", OtherEstimatedColor);

        var intradayMarks = document.UsesIntradayAxis
            ? IntradayMarkedSlots(document, plot, xMin, xMax)
            : null;
        DrawGrid(document, plot, xMin, xMax, yMin, yMax, gridBrush, verticalBrush, mutedBrush, limitBrush, intradayMarks);
        DrawPlotBox(plot, boxBrush);
        DrawPolyline(document.ExpectedUsage, plot, xMin, xMax, yMin, yMax, expectedUsage, dashed: true, EstimatedStrokeThickness);
        if (document.HasCursorUsage)
            DrawPolyline(document.CursorUsage, plot, xMin, xMax, yMin, yMax, cursorColor, dashed: false, UsageStrokeThickness);
        if (document.HasOtherUsage)
            DrawPolyline(document.OtherUsage, plot, xMin, xMax, yMin, yMax, otherColor, dashed: false, UsageStrokeThickness);
        if (document.HasCursorEstimated)
            DrawPolyline(document.CursorEstimated, plot, xMin, xMax, yMin, yMax, cursorColor, dashed: false, EstimatedStrokeThickness);
        if (document.HasOtherEstimated)
            DrawPolyline(document.OtherEstimated, plot, xMin, xMax, yMin, yMax, otherColor, dashed: false, EstimatedStrokeThickness);
        if (document.UsesIntradayAxis)
        {
            DrawSampleMarkers(document.CursorUsage, plot, xMin, xMax, yMin, yMax, cursorColor);
            DrawSampleMarkers(document.OtherUsage, plot, xMin, xMax, yMin, yMax, otherColor);
        }
        DrawLastSampleAnnotations(document, plot, xMin, xMax, yMin, yMax, expectedUsage, cursorColor, otherColor);
        DrawAxes(document, plot, xMin, xMax, mutedBrush, intradayMarks);
        DrawLegend(document, mutedBrush);
        _plotReady = true;
        UpdateRangeButtons();
        }
        finally
        {
            _rebuilding = false;
        }
    }

    private void OnRangeClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: UsageChartRange range })
            return;

        SelectedRange = range;
        CustomViewport = null;
    }

    private void UpdateRangeButtons()
    {
        if (RangeButtons == null)
            return;

        foreach (var child in RangeButtons.Children)
        {
            if (child is not Button button)
                continue;

            var selected = !_selecting
                && Document?.IsCustomViewport != true
                && button.Tag is UsageChartRange range
                && range == SelectedRange;
            button.Background = selected
                ? ThemeBrush("ThemeAccentBrush", Color.FromArgb(255, 0, 120, 212))
                : Brushes.Transparent;
            button.Foreground = selected
                ? Brushes.White
                : ThemeBrush("ThemeForegroundLowBrush", Color.FromArgb(255, 120, 120, 120));
        }
    }

    private void OnPlotPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(PlotHost);
        if (point.Properties.IsRightButtonPressed)
        {
            CancelSelection();
            SelectedRange = UsageChartRange.OneMonth;
            CustomViewport = null;
            e.Handled = true;
            return;
        }

        if (!point.Properties.IsLeftButtonPressed || !_plotReady || _plot.Width <= 0)
            return;

        var position = e.GetPosition(PlotCanvas);
        if (!_plot.Contains(position))
            return;

        _selecting = true;
        _selectionOriginX = ClampPlotX(position.X);
        _selectionCurrentX = _selectionOriginX;
        _selectionPointer = e.Pointer;
        e.Pointer.Capture(PlotHost);
        HideHover();
        UpdateSelectionVisual();
        UpdateRangeButtons();
        e.Handled = true;
    }

    private void OnPlotPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_selecting || e.InitialPressMouseButton != MouseButton.Left)
            return;

        var origin = _selectionOriginX;
        var current = ClampPlotX(e.GetPosition(PlotCanvas).X);
        var pixelSpan = Math.Abs(current - origin);
        _selecting = false;
        _selectionPointer = null;
        SelectionCanvas?.Children.Clear();
        if (e.Pointer.Captured == PlotHost)
            e.Pointer.Capture(null);

        var zoom = UsageChartMath.ViewportFromDrag(
            AxisXFromPlot(origin),
            AxisXFromPlot(current),
            _xMin,
            _xMax,
            pixelSpan,
            MinZoomPixelSpan);
        if (zoom is { } viewport)
            CustomViewport = viewport;
        else
            UpdateRangeButtons();
        e.Handled = true;
    }

    private void OnPlotPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!_selecting)
            return;

        CancelSelection();
    }

    private void OnPlotContextRequested(object? sender, ContextRequestedEventArgs e) =>
        e.Handled = true;

    private void OnPlotPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_selecting)
        {
            _selectionCurrentX = ClampPlotX(e.GetPosition(PlotCanvas).X);
            UpdateSelectionVisual();
            e.Handled = true;
            return;
        }

        var document = Document;
        if (!_plotReady || document == null || _plot.Width <= 0)
        {
            HideHover();
            return;
        }

        var position = e.GetPosition(PlotCanvas);
        if (!_plot.Contains(position))
        {
            HideHover();
            return;
        }

        var x = AxisXFromPlot(position.X);
        var readout = UsageChartMath.Read(document, x);
        var px = MapX(x, _plot, _xMin, _xMax);

        HoverCanvas.Children.Clear();
        HoverCanvas.Children.Add(new Line
        {
            StartPoint = new Point(px, _plot.Top),
            EndPoint = new Point(px, _plot.Bottom),
            Stroke = ThemeBrush("ThemeForegroundLowBrush", Color.FromArgb(255, 120, 120, 120)),
            StrokeThickness = 1
        });

        var cursorBrush = new SolidColorBrush(ThemeColor("ChartCursorEstimatedColor", CursorEstimatedColor));
        var otherBrush = new SolidColorBrush(ThemeColor("ChartOtherEstimatedColor", OtherEstimatedColor));
        var expectedBrush = new SolidColorBrush(ThemeColor("ChartExpectedUsageColor", ExpectedUsageColor));
        var showZoomChange = document.IsCustomViewport;
        HoverTimeText.Text = readout.LocalTime.ToString("dd-MMM HH:mm", CultureInfo.CurrentCulture);
        HoverCursorText.Text = "Cursor " + FormatHoverPercent(readout.CursorPercent);
        HoverOtherText.Text = "Other " + FormatHoverPercent(readout.OtherPercent);
        HoverExpectedText.Text = "Expected usage " + UsageChartSeriesBuilder.FormatEndpointPercent(readout.ExpectedPercent);
        HoverCursorDeltaText.IsVisible = showZoomChange;
        HoverOtherDeltaText.IsVisible = showZoomChange;
        HoverExpectedDeltaText.IsVisible = showZoomChange;
        if (showZoomChange)
        {
            HoverCursorDeltaText.Text = FormatRangeChange(readout.CursorStartPercent, readout.CursorDelta);
            HoverOtherDeltaText.Text = FormatRangeChange(readout.OtherStartPercent, readout.OtherDelta);
            HoverExpectedDeltaText.Text = FormatRangeChange(readout.ExpectedStartPercent, readout.ExpectedDelta);
        }
        HoverCursorText.Foreground = cursorBrush;
        HoverCursorDeltaText.Foreground = cursorBrush;
        HoverOtherText.Foreground = otherBrush;
        HoverOtherDeltaText.Foreground = otherBrush;
        HoverExpectedText.Foreground = expectedBrush;
        HoverExpectedDeltaText.Foreground = expectedBrush;
        HoverBox.IsVisible = true;
    }

    private void OnPlotPointerExited(object? sender, PointerEventArgs e) => HideHover();

    private void HideHover()
    {
        if (HoverCanvas == null || HoverBox == null)
            return;
        HoverCanvas.Children.Clear();
        HoverBox.IsVisible = false;
    }

    private static string FormatHoverPercent(decimal? percent) =>
        percent.HasValue ? UsageChartSeriesBuilder.FormatEndpointPercent(percent.Value) : "—";

    private static string FormatRangeChange(decimal? start, decimal? delta)
    {
        var change = delta.HasValue
            ? UsageChartSeriesBuilder.FormatSignedEndpointPercent(delta.Value)
            : "—";
        return "start " + FormatHoverPercent(start) + ", " + change;
    }

    private static string FormatRangeChange(decimal start, decimal delta) =>
        "start " + UsageChartSeriesBuilder.FormatEndpointPercent(start) + ", "
        + UsageChartSeriesBuilder.FormatSignedEndpointPercent(delta);

    private void CancelSelection()
    {
        var pointer = _selectionPointer;
        var wasSelecting = _selecting;
        _selecting = false;
        _selectionPointer = null;
        if (SelectionCanvas != null)
            SelectionCanvas.Children.Clear();
        if (pointer?.Captured == PlotHost)
            pointer.Capture(null);
        if (wasSelecting)
            UpdateRangeButtons();
    }

    private void UpdateSelectionVisual()
    {
        if (SelectionCanvas == null || _plot.Width <= 0 || _plot.Height <= 0)
            return;

        var left = Math.Min(_selectionOriginX, _selectionCurrentX);
        var right = Math.Max(_selectionOriginX, _selectionCurrentX);
        var accent = ThemeColor("ThemeAccentBrush", Color.FromArgb(255, 0, 120, 212));
        var rect = new Rectangle
        {
            Width = Math.Max(1, right - left),
            Height = _plot.Height,
            Fill = new SolidColorBrush(Color.FromArgb(56, accent.R, accent.G, accent.B)),
            Stroke = new SolidColorBrush(accent),
            StrokeThickness = 1
        };
        SelectionCanvas.Children.Clear();
        SelectionCanvas.Children.Add(rect);
        Canvas.SetLeft(rect, left);
        Canvas.SetTop(rect, _plot.Top);
    }

    private decimal AxisXFromPlot(double x)
    {
        if (_plot.Width <= 0)
            return _xMin;

        var t = (decimal)((x - _plot.Left) / _plot.Width);
        if (t < 0)
            t = 0;
        else if (t > 1)
            t = 1;
        return _xMin + t * (_xMax - _xMin);
    }

    private double ClampPlotX(double x)
    {
        if (x < _plot.Left)
            return _plot.Left;
        if (x > _plot.Right)
            return _plot.Right;
        return x;
    }

    private void DrawGrid(
        UsageChartDocument document,
        Rect plot,
        decimal xMin,
        decimal xMax,
        decimal yMin,
        decimal yMax,
        IBrush gridBrush,
        IBrush verticalBrush,
        IBrush mutedBrush,
        IBrush limitBrush,
        IReadOnlyList<UsageChartSlot>? intradayMarks)
    {
        var verticalSlots = intradayMarks ?? (IReadOnlyList<UsageChartSlot>)document.Slots;
        foreach (var slot in verticalSlots)
        {
            if (slot.StartX <= xMin)
                continue;

            var px = MapX(slot.StartX, plot, xMin, xMax);
            PlotCanvas.Children.Add(new Line
            {
                StartPoint = new Point(px, plot.Top),
                EndPoint = new Point(px, plot.Bottom),
                Stroke = verticalBrush,
                StrokeThickness = 1
            });
        }

        var step = document.YTickStep > 0 ? document.YTickStep : UsageChartSeriesBuilder.YTickStep;
        for (var y = yMin; y <= yMax; y += step)
        {
            var py = MapY(y, plot, yMin, yMax);
            PlotCanvas.Children.Add(new Line
            {
                StartPoint = new Point(plot.Left, py),
                EndPoint = new Point(plot.Right, py),
                Stroke = y == document.UsageLimitPercent ? limitBrush : gridBrush,
                StrokeThickness = y == document.UsageLimitPercent ? 1.6 : 1
            });
            AddLabel($"{y:0}%", plot.Left - 6, py - 8, mutedBrush, 10, alignRight: true);
        }
    }

    private void DrawPlotBox(Rect plot, IBrush boxBrush)
    {
        var box = new Rectangle
        {
            Width = plot.Width,
            Height = plot.Height,
            Stroke = boxBrush,
            StrokeThickness = 1,
            Fill = null
        };
        PlotCanvas.Children.Add(box);
        Canvas.SetLeft(box, plot.Left);
        Canvas.SetTop(box, plot.Top);
    }

    private void DrawAxes(
        UsageChartDocument document,
        Rect plot,
        decimal xMin,
        decimal xMax,
        IBrush mutedBrush,
        IReadOnlyList<UsageChartSlot>? intradayMarks)
    {
        var slots = document.Slots;
        if (slots.Count == 0)
            return;

        var tickSlots = intradayMarks ?? (IReadOnlyList<UsageChartSlot>)slots;
        foreach (var slot in tickSlots)
        {
            if (slot.StartX <= xMin)
                continue;

            var px = MapX(slot.StartX, plot, xMin, xMax);
            PlotCanvas.Children.Add(new Line
            {
                StartPoint = new Point(px, plot.Bottom),
                EndPoint = new Point(px, plot.Bottom + 4),
                Stroke = mutedBrush,
                StrokeThickness = 1
            });
        }

        var labelled = slots.Where(slot => !slot.IsLeadingPartial).ToList();
        if (labelled.Count == 0)
            labelled = slots.ToList();

        if (intradayMarks != null)
            DrawIntradayLabels(intradayMarks, plot, xMin, xMax, mutedBrush);
        else
            DrawDayLabels(document, labelled, plot, xMin, xMax, mutedBrush);
        DrawDateLabels(document, labelled, plot, xMin, xMax, mutedBrush);
    }

    private List<UsageChartSlot> IntradayMarkedSlots(
        UsageChartDocument document,
        Rect plot,
        decimal xMin,
        decimal xMax)
    {
        var labelled = document.Slots.Where(slot => !slot.IsLeadingPartial).ToList();
        if (labelled.Count == 0)
            labelled = document.Slots.ToList();
        if (labelled.Count == 0)
            return [];

        var sample = new DateTime(2000, 1, 1, 22, 0, 0).ToString("t", CultureInfo.CurrentCulture);
        var minSpacing = MinimumTextSpacing(sample);
        var step = UsageChartMath.IntradayLabelStep(labelled.Count, plot.Width, minSpacing);
        var indexes = UsageChartMath.IntradayLabelIndexes(labelled.Count, step);
        var lastIndex = indexes[^1];
        var lastX = MapX(labelled[lastIndex].StartX, plot, xMin, xMax);
        var marked = new List<UsageChartSlot>();
        double? previousX = null;
        foreach (var index in indexes)
        {
            var x = MapX(labelled[index].StartX, plot, xMin, xMax);
            var isLast = index == lastIndex;
            if (!isLast && (lastX - x < minSpacing || previousX is { } prior && x - prior < minSpacing))
                continue;
            if (isLast && previousX is { } priorLast && x - priorLast < minSpacing && marked.Count > 0)
                marked.RemoveAt(marked.Count - 1);
            marked.Add(labelled[index]);
            previousX = x;
        }

        return marked;
    }

    private void DrawIntradayLabels(
        IReadOnlyList<UsageChartSlot> marked,
        Rect plot,
        decimal xMin,
        decimal xMax,
        IBrush mutedBrush)
    {
        foreach (var slot in marked)
        {
            if (slot.StartX < xMin)
                continue;

            AddLabel(
                slot.Date.ToString("t", CultureInfo.CurrentCulture),
                MapX(slot.StartX, plot, xMin, xMax),
                plot.Bottom + 6,
                mutedBrush,
                10);
        }
    }

    private static double MinimumTextSpacing(string sample)
    {
        var block = new TextBlock
        {
            Text = sample,
            FontSize = 10
        };
        block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return block.DesiredSize.Width + 8;
    }

    private void DrawDayLabels(
        UsageChartDocument document,
        List<UsageChartSlot> labelled,
        Rect plot,
        decimal xMin,
        decimal xMax,
        IBrush mutedBrush)
    {
        var step = Math.Max(1, (int)Math.Ceiling(MinTickSpacing * labelled.Count / plot.Width));
        var lastIndex = labelled.Count - 1;
        var lastX = MapX(labelled[lastIndex].MidX, plot, xMin, xMax);

        for (var i = 0; i < labelled.Count; i++)
        {
            var x = MapX(labelled[i].MidX, plot, xMin, xMax);
            if (i != lastIndex && (i % step != 0 || lastX - x < MinTickSpacing))
                continue;

            var text = document.UsesIntradayAxis
                ? labelled[i].Date.ToString("t", CultureInfo.CurrentCulture)
                : labelled[i].Date.Day.ToString(CultureInfo.CurrentCulture);
            AddLabel(text, x, plot.Bottom + 6, mutedBrush, 10);
        }
    }

    private void DrawDateLabels(
        UsageChartDocument document,
        List<UsageChartSlot> labelled,
        Rect plot,
        decimal xMin,
        decimal xMax,
        IBrush mutedBrush)
    {
        const double dateMinSpacing = 56;

        var lastIndex = labelled.Count - 1;
        var lastX = MapX(labelled[lastIndex].MidX, plot, xMin, xMax);
        var placedX = double.MinValue;
        DateTime? placedDate = null;

        for (var i = 0; i < lastIndex; i++)
        {
            if (!document.UsesIntradayAxis && i % 7 != 0)
                continue;
            if (document.UsesIntradayAxis && placedDate == labelled[i].Date.Date)
                continue;

            var x = MapX(labelled[i].MidX, plot, xMin, xMax);
            if (x - placedX < dateMinSpacing || lastX - x < dateMinSpacing)
                continue;

            placedX = x;
            placedDate = labelled[i].Date.Date;
            AddLabel(labelled[i].Date.ToString("d", CultureInfo.CurrentCulture), x, plot.Top - 18, mutedBrush, 10);
        }

        AddLabel(labelled[lastIndex].Date.ToString("d", CultureInfo.CurrentCulture), plot.Right, plot.Top - 18, mutedBrush, 10, alignRight: true);
    }

    private void DrawPolyline(
        IReadOnlyList<UsageChartPoint> points,
        Rect plot,
        decimal xMin,
        decimal xMax,
        decimal yMin,
        decimal yMax,
        Color color,
        bool dashed,
        double strokeThickness)
    {
        if (points.Count < 2)
            return;

        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(color),
            StrokeThickness = strokeThickness,
            StrokeJoin = PenLineJoin.Round,
            Fill = null
        };
        if (dashed)
            polyline.StrokeDashArray = new AvaloniaList<double> { 5, 3 };

        foreach (var point in points)
        {
            polyline.Points.Add(new Point(
                MapX(point.X, plot, xMin, xMax),
                MapY(point.Y, plot, yMin, yMax)));
        }

        PlotCanvas.Children.Add(polyline);
    }

    private void DrawSampleMarkers(
        IReadOnlyList<UsageChartPoint> points,
        Rect plot,
        decimal xMin,
        decimal xMax,
        decimal yMin,
        decimal yMax,
        Color color)
    {
        var fill = new SolidColorBrush(color);
        foreach (var point in points)
        {
            if (!point.IsMeasured)
                continue;

            var marker = new Ellipse
            {
                Width = SampleMarkerDiameter,
                Height = SampleMarkerDiameter,
                Fill = fill
            };
            PlotCanvas.Children.Add(marker);
            Canvas.SetLeft(marker, MapX(point.X, plot, xMin, xMax) - SampleMarkerDiameter / 2);
            Canvas.SetTop(marker, MapY(point.Y, plot, yMin, yMax) - SampleMarkerDiameter / 2);
        }
    }

    private void DrawLastSampleAnnotations(
        UsageChartDocument document,
        Rect plot,
        decimal xMin,
        decimal xMax,
        decimal yMin,
        decimal yMax,
        Color expectedColor,
        Color cursorColor,
        Color otherColor)
    {
        UsageChartPoint? lastCursor = document.LastMeasuredCursor;
        UsageChartPoint? lastOther = document.LastMeasuredOther;
        if (lastCursor is null && lastOther is null)
            return;

        var lastX = lastCursor?.X ?? lastOther!.X;
        if (lastCursor is not null && lastOther is not null)
            lastX = lastCursor.X >= lastOther.X ? lastCursor.X : lastOther.X;

        var expectedY = UsageChartSeriesBuilder.LinearExpectedPercent(document.CycleSeconds, lastX);
        var px = MapX(lastX, plot, xMin, xMax);
        var expectedPy = MapY(expectedY, plot, yMin, yMax);

        PlotCanvas.Children.Add(new Line
        {
            StartPoint = new Point(px, plot.Bottom),
            EndPoint = new Point(px, expectedPy),
            Stroke = new SolidColorBrush(expectedColor),
            StrokeThickness = 1,
            StrokeDashArray = new AvaloniaList<double> { 2, 2 }
        });

        var labelX = px + EndpointLabelOffsetX;
        if (lastCursor is not null)
        {
            AddLabel(
                UsageChartSeriesBuilder.FormatEndpointPercent(lastCursor.Y),
                labelX,
                MapY(lastCursor.Y, plot, yMin, yMax),
                new SolidColorBrush(cursorColor),
                11,
                alignLeft: true,
                centerVertically: true,
                halo: true);
        }

        if (lastOther is not null)
        {
            AddLabel(
                UsageChartSeriesBuilder.FormatEndpointPercent(lastOther.Y),
                labelX,
                MapY(lastOther.Y, plot, yMin, yMax),
                new SolidColorBrush(otherColor),
                11,
                alignLeft: true,
                centerVertically: true,
                halo: true);
        }

        AddLabel(
            UsageChartSeriesBuilder.FormatEndpointPercent(expectedY),
            labelX,
            expectedPy,
            new SolidColorBrush(expectedColor),
            11,
            alignLeft: true,
            centerVertically: true,
            halo: true);
    }

    private void DrawLegend(UsageChartDocument document, IBrush mutedBrush)
    {
        var primary = CreateLegendRow();
        var estimated = CreateLegendRow();
        AddLegendItem(primary, mutedBrush, "Expected usage", ThemeColor("ChartExpectedUsageColor", ExpectedUsageColor), dashed: true, thin: true);
        if (document.HasCursorUsage)
            AddLegendItem(primary, mutedBrush, "Cursor", ThemeColor("ChartCursorEstimatedColor", CursorEstimatedColor), dashed: false, thin: false);
        if (document.HasOtherUsage)
            AddLegendItem(primary, mutedBrush, "Other Models", ThemeColor("ChartOtherEstimatedColor", OtherEstimatedColor), dashed: false, thin: false);
        if (document.HasCursorEstimated)
            AddLegendItem(estimated, mutedBrush, "Cursor (estimated)", ThemeColor("ChartCursorEstimatedColor", CursorEstimatedColor), dashed: false, thin: true);
        if (document.HasOtherEstimated)
            AddLegendItem(estimated, mutedBrush, "Other Models (estimated)", ThemeColor("ChartOtherEstimatedColor", OtherEstimatedColor), dashed: false, thin: true);

        LegendPanel.Children.Add(primary);
        if (estimated.Children.Count > 0)
            LegendPanel.Children.Add(estimated);
    }

    private static StackPanel CreateLegendRow() =>
        new()
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

    private static void AddLegendItem(Panel row, IBrush mutedBrush, string label, Color color, bool dashed, bool thin)
    {
        var entry = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(16, 2, 0, 2),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        var line = new Rectangle
        {
            Width = 18,
            Height = thin ? 2 : 4,
            Fill = dashed ? null : new SolidColorBrush(color),
            Stroke = new SolidColorBrush(color),
            StrokeThickness = dashed ? 1.5 : 0,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        if (dashed)
            line.StrokeDashArray = new AvaloniaList<double> { 3, 2 };

        entry.Children.Add(line);
        entry.Children.Add(new SelectableTextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = mutedBrush,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });
        row.Children.Add(entry);
    }

    private void AddLabel(
        string text,
        double x,
        double y,
        IBrush brush,
        double fontSize,
        bool alignRight = false,
        bool alignLeft = false,
        bool centerVertically = false,
        bool halo = false)
    {
        var block = new SelectableTextBlock
        {
            Text = text,
            FontSize = fontSize,
            Foreground = brush
        };
        block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var width = block.DesiredSize.Width;
        var height = block.DesiredSize.Height;
        var left = alignRight ? x - width : alignLeft ? x : x - width / 2;
        var top = centerVertically ? y - height / 2 : y;
        var canvasWidth = PlotCanvas.Width;
        var canvasHeight = PlotCanvas.Height;
        if (canvasWidth > 0 && width > 0)
            left = Math.Clamp(left, 0, Math.Max(0, canvasWidth - width));
        if (centerVertically && canvasHeight > 0 && height > 0)
            top = Math.Clamp(top, 0, Math.Max(0, canvasHeight - height));
        if (halo)
            AddLabelHalo(text, fontSize, left, top);
        Canvas.SetLeft(block, left);
        Canvas.SetTop(block, top);
        PlotCanvas.Children.Add(block);
    }

    private void AddLabelHalo(string text, double fontSize, double left, double top)
    {
        var outline = new SolidColorBrush(PlotBackgroundColor());
        foreach (var offset in HaloOffsets)
        {
            var halo = new SelectableTextBlock
            {
                Text = text,
                FontSize = fontSize,
                Foreground = outline
            };
            Canvas.SetLeft(halo, left + offset.X);
            Canvas.SetTop(halo, top + offset.Y);
            PlotCanvas.Children.Add(halo);
        }
    }

    private Color PlotBackgroundColor()
    {
        for (StyledElement? current = this; current != null; current = current.Parent as StyledElement)
        {
            IBrush? background = current switch
            {
                Panel panel => panel.Background,
                Border border => border.Background,
                TemplatedControl templated => templated.Background,
                _ => null
            };
            if (background is ISolidColorBrush solid && solid.Color.A == 255)
                return solid.Color;
        }

        return ThemeColor(
            "ThemeBackgroundBrush",
            ActualThemeVariant == ThemeVariant.Dark
                ? Colors.Black
                : Colors.White);
    }

    private static double MapX(decimal x, Rect plot, decimal xMin, decimal xMax)
    {
        var span = xMax - xMin;
        if (span <= 0)
            return plot.Left;
        var t = (double)((x - xMin) / span);
        return plot.Left + t * plot.Width;
    }

    private static double MapY(decimal y, Rect plot, decimal yMin, decimal yMax)
    {
        var span = yMax - yMin;
        if (span <= 0)
            return plot.Bottom;
        var t = (double)((y - yMin) / span);
        return plot.Bottom - t * plot.Height;
    }

    private IBrush ThemeBrush(string key, Color fallback)
    {
        if (Application.Current?.TryGetResource(key, ActualThemeVariant, out var value) == true
            && value is IBrush brush)
        {
            return brush;
        }

        return new SolidColorBrush(fallback);
    }

    private Color ThemeColor(string key, Color fallback)
    {
        if (Application.Current?.TryGetResource(key, ActualThemeVariant, out var value) != true)
            return fallback;

        return value switch
        {
            Color color => color,
            ISolidColorBrush brush => brush.Color,
            _ => fallback
        };
    }
}

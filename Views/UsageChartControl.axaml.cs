using System.Globalization;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using CursorUsageProgress.Models;
using CursorUsageProgress.Services;

namespace CursorUsageProgress.Views;

public partial class UsageChartControl : UserControl
{
    private const double PlotLeft = 38;
    private const double PlotRightPad = 8;
    private const double PlotTop = 28;
    private const double PlotBottomPad = 28;
    private const double MinTickSpacing = 16;
    private const double MarkerSize = 7;
    private bool _rebuilding;

    private static readonly Color CursorExpectedColor = Color.FromArgb(255, 37, 99, 235);
    private static readonly Color OtherExpectedColor = Color.FromArgb(255, 234, 88, 12);
    private static readonly Color CursorEstimatedColor = Color.FromArgb(255, 21, 128, 61);
    private static readonly Color OtherEstimatedColor = Color.FromArgb(255, 2, 132, 199);

    public static readonly StyledProperty<UsageChartDocument?> DocumentProperty =
        AvaloniaProperty.Register<UsageChartControl, UsageChartDocument?>(nameof(Document));

    public UsageChartControl()
    {
        InitializeComponent();
        DocumentProperty.Changed.AddClassHandler<UsageChartControl>((control, _) => control.RebuildPlot());
        ActualThemeVariantChanged += (_, _) => RebuildPlot();
        Loaded += (_, _) => RebuildPlot();
        SizeChanged += (_, _) => RebuildPlot();
        IsVisibleProperty.Changed.AddClassHandler<UsageChartControl>((control, _) => control.RebuildPlot());
    }

    public UsageChartDocument? Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
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
        PlotCanvas.Children.Clear();
        LegendPanel.Children.Clear();
        var document = Document;
        var hostWidth = PlotSize.Width;
        var hostHeight = PlotSize.Height;
        var hasSeries = document != null
            && (document.CursorExpected.Count >= 2
                || document.OtherExpected.Count >= 2
                || document.Markers.Count > 0);
        EmptyPlotText.IsVisible = document == null || !hasSeries;
        if (document == null || !IsEffectivelyVisible || hostWidth < 80 || hostHeight < 60)
            return;

        PlotCanvas.Width = hostWidth;
        PlotCanvas.Height = hostHeight;

        var plot = new Rect(
            PlotLeft,
            PlotTop,
            Math.Max(40, hostWidth - PlotLeft - PlotRightPad),
            Math.Max(40, hostHeight - PlotTop - PlotBottomPad));

        var xMin = 0m;
        var xMax = document.CycleSeconds > xMin ? document.CycleSeconds : 1m;
        var yMin = 0m;
        var yMax = document.YMax <= 0 ? UsageChartSeriesBuilder.DefaultYMax : document.YMax;

        var mutedBrush = ThemeBrush("ThemeForegroundLowBrush", Color.FromArgb(255, 120, 120, 120));
        var gridBrush = ThemeBrush("CardStrokeBrush", Color.FromArgb(60, 128, 128, 128));
        var verticalBrush = new SolidColorBrush(ThemeColor("ChartGridLineColor", Color.FromArgb(40, 160, 160, 160)));
        var boxBrush = ThemeBrush("CardStrokeBrush", Color.FromArgb(140, 140, 140, 140));
        var limitBrush = ThemeBrush("CalendarMutedForegroundBrush", Color.FromArgb(180, 128, 128, 128));
        var cursorExpected = ThemeColor("ChartCursorExpectedColor", CursorExpectedColor);
        var otherExpected = ThemeColor("ChartOtherExpectedColor", OtherExpectedColor);
        var cursorEstimated = ThemeColor("ChartCursorEstimatedColor", CursorEstimatedColor);
        var otherEstimated = ThemeColor("ChartOtherEstimatedColor", OtherEstimatedColor);

        DrawGrid(document, plot, xMin, xMax, yMin, yMax, gridBrush, verticalBrush, mutedBrush, limitBrush);
        DrawPlotBox(plot, boxBrush);
        DrawPolyline(document.CursorExpected, plot, xMin, xMax, yMin, yMax, cursorExpected, dashed: true);
        DrawPolyline(document.OtherExpected, plot, xMin, xMax, yMin, yMax, otherExpected, dashed: true);
        if (document.HasCursorEstimated)
            DrawPolyline(document.CursorEstimated, plot, xMin, xMax, yMin, yMax, cursorEstimated, dashed: false);
        if (document.HasOtherEstimated)
            DrawPolyline(document.OtherEstimated, plot, xMin, xMax, yMin, yMax, otherEstimated, dashed: false);
        DrawMarkers(document, plot, xMin, xMax, yMin, yMax);
        DrawAxes(document, plot, xMin, xMax, mutedBrush);
        DrawLegend(document, mutedBrush);
        }
        finally
        {
            _rebuilding = false;
        }
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
        IBrush limitBrush)
    {
        foreach (var slot in document.Slots)
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

        for (var y = yMin; y <= yMax; y += 20m)
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
        IBrush mutedBrush)
    {
        var slots = document.Slots;
        if (slots.Count == 0)
            return;

        foreach (var slot in slots)
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

        var labelled = slots.Where(s => !s.IsLeadingPartial).ToList();
        if (labelled.Count == 0)
            return;

        DrawDayLabels(labelled, plot, xMin, xMax, mutedBrush);
        DrawDateLabels(labelled, plot, xMin, xMax, mutedBrush);
    }

    private void DrawDayLabels(
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

            AddLabel(
                labelled[i].Date.Day.ToString(CultureInfo.CurrentCulture),
                x,
                plot.Bottom + 6,
                mutedBrush,
                10);
        }
    }

    private void DrawDateLabels(
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

        for (var i = 0; i < lastIndex; i += 7)
        {
            var x = MapX(labelled[i].MidX, plot, xMin, xMax);
            if (x - placedX < dateMinSpacing || lastX - x < dateMinSpacing)
                continue;

            placedX = x;
            AddLabel(labelled[i].Date.ToString("d", CultureInfo.CurrentCulture), x, plot.Top - 18, mutedBrush, 10);
        }

        AddLabel(labelled[lastIndex].Date.ToString("d", CultureInfo.CurrentCulture), lastX, plot.Top - 18, mutedBrush, 10);
    }

    private void DrawPolyline(
        IReadOnlyList<UsageChartPoint> points,
        Rect plot,
        decimal xMin,
        decimal xMax,
        decimal yMin,
        decimal yMax,
        Color color,
        bool dashed)
    {
        if (points.Count < 2)
            return;

        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 2,
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

    private void DrawMarkers(
        UsageChartDocument document,
        Rect plot,
        decimal xMin,
        decimal xMax,
        decimal yMin,
        decimal yMax)
    {
        foreach (var marker in document.Markers)
        {
            if (marker.X < xMin || marker.X > xMax)
                continue;

            var color = MarkerColor(marker);
            var ellipse = new Ellipse
            {
                Width = MarkerSize,
                Height = MarkerSize,
                Fill = new SolidColorBrush(color),
                Stroke = ThemeBrush("SystemControlBackgroundChromeMediumLowBrush", Color.FromArgb(255, 255, 255, 255)),
                StrokeThickness = 1
            };
            Canvas.SetLeft(ellipse, MapX(marker.X, plot, xMin, xMax) - MarkerSize / 2);
            Canvas.SetTop(ellipse, MapY(marker.Y, plot, yMin, yMax) - MarkerSize / 2);
            ToolTip.SetTip(ellipse, MarkerTooltip(marker));
            PlotCanvas.Children.Add(ellipse);
        }
    }

    private void DrawLegend(UsageChartDocument document, IBrush mutedBrush)
    {
        var expectedRow = CreateLegendRow(mutedBrush,
            ("Cursor (expected)", ThemeColor("ChartCursorExpectedColor", CursorExpectedColor), true),
            ("Other Models (expected)", ThemeColor("ChartOtherExpectedColor", OtherExpectedColor), true));
        LegendPanel.Children.Add(expectedRow);

        if (!document.HasCursorEstimated && !document.HasOtherEstimated)
            return;

        var estimated = new List<(string Label, Color Color, bool Dashed)>();
        if (document.HasCursorEstimated)
            estimated.Add(("Cursor (estimated)", ThemeColor("ChartCursorEstimatedColor", CursorEstimatedColor), false));
        if (document.HasOtherEstimated)
            estimated.Add(("Other Models (estimated)", ThemeColor("ChartOtherEstimatedColor", OtherEstimatedColor), false));
        LegendPanel.Children.Add(CreateLegendRow(mutedBrush, estimated.ToArray()));
    }

    private static StackPanel CreateLegendRow(IBrush mutedBrush, params (string Label, Color Color, bool Dashed)[] items)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 16 };
        foreach (var item in items)
        {
            var entry = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
            var line = new Rectangle
            {
                Width = 18,
                Height = 2,
                Fill = item.Dashed ? null : new SolidColorBrush(item.Color),
                Stroke = new SolidColorBrush(item.Color),
                StrokeThickness = item.Dashed ? 1.5 : 0,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            if (item.Dashed)
                line.StrokeDashArray = new AvaloniaList<double> { 3, 2 };

            entry.Children.Add(line);
            entry.Children.Add(new TextBlock
            {
                Text = item.Label,
                FontSize = 11,
                Foreground = mutedBrush,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            });
            row.Children.Add(entry);
        }

        return row;
    }

    private void AddLabel(string text, double x, double y, IBrush brush, double fontSize, bool alignRight = false)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            Foreground = brush
        };
        block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var left = alignRight ? x - block.DesiredSize.Width : x - block.DesiredSize.Width / 2;
        Canvas.SetLeft(block, left);
        Canvas.SetTop(block, y);
        PlotCanvas.Children.Add(block);
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

    private Color MarkerColor(UsageChartMarker marker)
    {
        if (marker.MarkerKind == ChartMarkerKind.Origin)
            return ThemeColor("CalendarMutedForegroundBrush", Color.FromArgb(255, 80, 80, 80));

        return marker.QuotaKind switch
        {
            QuotaKind.CursorModels => marker.MarkerKind == ChartMarkerKind.Edit
                ? ThemeColor("ChartCursorExpectedColor", CursorExpectedColor)
                : ThemeColor("ChartCursorEstimatedColor", CursorEstimatedColor),
            QuotaKind.OtherModels => marker.MarkerKind == ChartMarkerKind.Edit
                ? ThemeColor("ChartOtherExpectedColor", OtherExpectedColor)
                : ThemeColor("ChartOtherEstimatedColor", OtherEstimatedColor),
            null => Color.FromArgb(255, 80, 80, 80),
            _ => throw new ArgumentOutOfRangeException(nameof(marker.QuotaKind), marker.QuotaKind, null)
        };
    }

    private static string MarkerTooltip(UsageChartMarker marker)
    {
        var instant = marker.Instant.ToString("g", CultureInfo.CurrentCulture);
        var percent = marker.Y.ToString("0.##", CultureInfo.CurrentCulture);
        return marker.MarkerKind switch
        {
            ChartMarkerKind.Origin => $"Cycle start  {percent}%\n{instant}",
            ChartMarkerKind.Sample => $"{QuotaLabel(marker.QuotaKind)} sample  {percent}%\n{instant}",
            ChartMarkerKind.Edit => $"{QuotaLabel(marker.QuotaKind)} edit  {percent}%\n{instant}",
            _ => throw new ArgumentOutOfRangeException(nameof(marker.MarkerKind), marker.MarkerKind, null)
        };
    }

    private static string QuotaLabel(QuotaKind? kind) =>
        kind switch
        {
            QuotaKind.CursorModels => "Cursor",
            QuotaKind.OtherModels => "Other Models",
            null => "Usage",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

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

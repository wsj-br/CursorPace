using System.Globalization;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
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
    private bool _rebuilding;

    private static readonly Color ExpectedUsageColor = Color.FromArgb(255, 100, 116, 139);
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
        var hasSeries = document != null && document.ExpectedUsage.Count >= 2;
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
        var expectedUsage = ThemeColor("ChartExpectedUsageColor", ExpectedUsageColor);
        var cursorColor = ThemeColor("ChartCursorEstimatedColor", CursorEstimatedColor);
        var otherColor = ThemeColor("ChartOtherEstimatedColor", OtherEstimatedColor);

        DrawGrid(document, plot, xMin, xMax, yMin, yMax, gridBrush, verticalBrush, mutedBrush, limitBrush);
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

    private void DrawLegend(UsageChartDocument document, IBrush mutedBrush)
    {
        LegendPanel.Children.Add(CreateLegendRow(mutedBrush,
            ("Expected usage", ThemeColor("ChartExpectedUsageColor", ExpectedUsageColor), true, true)));

        var usage = new List<(string Label, Color Color, bool Dashed, bool Thin)>();
        if (document.HasCursorUsage)
            usage.Add(("Cursor", ThemeColor("ChartCursorEstimatedColor", CursorEstimatedColor), false, false));
        if (document.HasOtherUsage)
            usage.Add(("Other Models", ThemeColor("ChartOtherEstimatedColor", OtherEstimatedColor), false, false));
        if (usage.Count > 0)
            LegendPanel.Children.Add(CreateLegendRow(mutedBrush, usage.ToArray()));

        var estimated = new List<(string Label, Color Color, bool Dashed, bool Thin)>();
        if (document.HasCursorEstimated)
            estimated.Add(("Cursor (estimated)", ThemeColor("ChartCursorEstimatedColor", CursorEstimatedColor), false, true));
        if (document.HasOtherEstimated)
            estimated.Add(("Other Models (estimated)", ThemeColor("ChartOtherEstimatedColor", OtherEstimatedColor), false, true));
        if (estimated.Count > 0)
            LegendPanel.Children.Add(CreateLegendRow(mutedBrush, estimated.ToArray()));
    }

    private static StackPanel CreateLegendRow(
        IBrush mutedBrush,
        params (string Label, Color Color, bool Dashed, bool Thin)[] items)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 16 };
        foreach (var item in items)
        {
            var entry = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
            var height = item.Thin ? 2.0 : 4.0;
            var line = new Rectangle
            {
                Width = 18,
                Height = height,
                Fill = item.Dashed ? null : new SolidColorBrush(item.Color),
                Stroke = new SolidColorBrush(item.Color),
                StrokeThickness = item.Dashed ? 1.5 : 0,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            if (item.Dashed)
                line.StrokeDashArray = new AvaloniaList<double> { 3, 2 };

            entry.Children.Add(line);
            entry.Children.Add(new SelectableTextBlock
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
        var block = new SelectableTextBlock
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

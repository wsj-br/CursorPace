using System.Globalization;
using CursorUsageProgress.Models;
using CursorUsageProgress.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace CursorUsageProgress.Views;

public sealed partial class UsageChartControl : UserControl
{
    private const double PlotLeft = 38;
    private const double PlotRightPad = 8;
    private const double PlotTop = 28;
    private const double PlotBottomPad = 28;
    private const double MinTickSpacing = 16;
    private const double MarkerSize = 7;

    private static readonly Color CursorExpectedColor = Color.FromArgb(255, 37, 99, 235);
    private static readonly Color OtherExpectedColor = Color.FromArgb(255, 234, 88, 12);
    private static readonly Color CursorEstimatedColor = Color.FromArgb(255, 21, 128, 61);
    private static readonly Color OtherEstimatedColor = Color.FromArgb(255, 2, 132, 199);

    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register(
            nameof(Document),
            typeof(UsageChartDocument),
            typeof(UsageChartControl),
            new PropertyMetadata(null, OnDocumentChanged));

    public UsageChartControl()
    {
        InitializeComponent();
        ActualThemeChanged += (_, _) => RebuildPlot();
        Loaded += (_, _) => RebuildPlot();
    }

    public UsageChartDocument? Document
    {
        get => (UsageChartDocument?)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((UsageChartControl)d).RebuildPlot();
    }

    private void OnPlotSizeChanged(object sender, SizeChangedEventArgs e) => RebuildPlot();

    private void RebuildPlot()
    {
        PlotCanvas.Children.Clear();
        LegendPanel.Children.Clear();
        var document = Document;
        if (document == null || ActualWidth < 80 || PlotCanvas.ActualHeight < 60)
            return;

        var plot = new Rect(
            PlotLeft,
            PlotTop,
            Math.Max(40, PlotCanvas.ActualWidth - PlotLeft - PlotRightPad),
            Math.Max(40, PlotCanvas.ActualHeight - PlotTop - PlotBottomPad));

        var xMin = 1m;
        var xMax = document.PlotEndX > xMin ? document.PlotEndX : xMin + 1m;
        var clipMin = UsageChartSeriesBuilder.ToAxisX(document.CycleStart, document.CycleStart);
        var clipMax = document.RenewalX > clipMin ? document.RenewalX : xMax;
        var yMin = 0m;
        var yMax = document.YMax <= 0 ? UsageChartSeriesBuilder.DefaultYMax : document.YMax;

        var mutedBrush = ThemeBrush("TextFillColorSecondaryBrush", Color.FromArgb(255, 120, 120, 120));
        var gridBrush = ThemeBrush("DividerStrokeColorDefaultBrush", Color.FromArgb(60, 128, 128, 128));
        var verticalBrush = new SolidColorBrush(Color.FromArgb(40, 160, 160, 160));
        var boxBrush = ThemeBrush("ControlStrokeColorDefaultBrush", Color.FromArgb(140, 140, 140, 140));
        var limitBrush = new SolidColorBrush(Color.FromArgb(180, 128, 128, 128));

        DrawGrid(document, plot, xMin, xMax, yMin, yMax, gridBrush, verticalBrush, mutedBrush, limitBrush);
        DrawPlotBox(plot, boxBrush);
        DrawPolyline(document.CursorExpected, plot, xMin, xMax, yMin, yMax, clipMin, clipMax, CursorExpectedColor, dashed: true);
        DrawPolyline(document.OtherExpected, plot, xMin, xMax, yMin, yMax, clipMin, clipMax, OtherExpectedColor, dashed: true);
        if (document.HasCursorEstimated)
            DrawPolyline(document.CursorEstimated, plot, xMin, xMax, yMin, yMax, clipMin, clipMax, CursorEstimatedColor, dashed: false);
        if (document.HasOtherEstimated)
            DrawPolyline(document.OtherEstimated, plot, xMin, xMax, yMin, yMax, clipMin, clipMax, OtherEstimatedColor, dashed: false);
        DrawMarkers(document, plot, xMin, xMax, yMin, yMax);
        DrawAxes(document, plot, xMin, xMax, mutedBrush);
        DrawLegend(document, mutedBrush);
    }

    private void DrawGrid(
        UsageChartDocument document,
        Rect plot,
        decimal xMin,
        decimal xMax,
        decimal yMin,
        decimal yMax,
        Brush gridBrush,
        Brush verticalBrush,
        Brush mutedBrush,
        Brush limitBrush)
    {
        foreach (var tick in document.DayTicks)
        {
            var px = MapX(tick.X, plot, xMin, xMax);
            PlotCanvas.Children.Add(new Line
            {
                X1 = px,
                Y1 = plot.Top,
                X2 = px,
                Y2 = plot.Bottom,
                Stroke = verticalBrush,
                StrokeThickness = 1
            });
        }

        var slotEndPx = MapX(document.SlotEndX, plot, xMin, xMax);
        if (document.SlotEndX > xMin && slotEndPx < plot.Right - 0.5)
        {
            PlotCanvas.Children.Add(new Line
            {
                X1 = slotEndPx,
                Y1 = plot.Top,
                X2 = slotEndPx,
                Y2 = plot.Bottom,
                Stroke = verticalBrush,
                StrokeThickness = 1
            });
        }

        for (var y = yMin; y <= yMax; y += 20m)
        {
            var py = MapY(y, plot, yMin, yMax);
            PlotCanvas.Children.Add(new Line
            {
                X1 = plot.Left,
                Y1 = py,
                X2 = plot.Right,
                Y2 = py,
                Stroke = y == document.UsageLimitPercent ? limitBrush : gridBrush,
                StrokeThickness = y == document.UsageLimitPercent ? 1.6 : 1
            });
            AddLabel($"{y:0}%", plot.Left - 6, py - 8, mutedBrush, 10, alignRight: true);
        }
    }

    private void DrawPlotBox(Rect plot, Brush boxBrush)
    {
        PlotCanvas.Children.Add(new Rectangle
        {
            Width = plot.Width,
            Height = plot.Height,
            Stroke = boxBrush,
            StrokeThickness = 1,
            Fill = null
        });
        Canvas.SetLeft(PlotCanvas.Children[^1], plot.Left);
        Canvas.SetTop(PlotCanvas.Children[^1], plot.Top);
    }

    private void DrawAxes(
        UsageChartDocument document,
        Rect plot,
        decimal xMin,
        decimal xMax,
        Brush mutedBrush)
    {
        var ticks = document.DayTicks;
        if (ticks.Count == 0)
            return;

        var slotCount = ticks.Count;
        var step = Math.Max(1, (int)Math.Ceiling(MinTickSpacing * Math.Max(slotCount, 1) / plot.Width));
        var lastDateX = double.MinValue;
        const double dateMinSpacing = 56;

        foreach (var tick in ticks)
        {
            var px = MapX(tick.X, plot, xMin, xMax);
            PlotCanvas.Children.Add(new Line
            {
                X1 = px,
                Y1 = plot.Bottom,
                X2 = px,
                Y2 = plot.Bottom + 4,
                Stroke = mutedBrush,
                StrokeThickness = 1
            });

            var isEdge = tick.DayNumber == 1 || tick.DayNumber == slotCount;
            var showDay = isEdge || tick.DayNumber % step == 0;
            if (!showDay)
                continue;

            var midX = MapX(tick.X + 0.5m, plot, xMin, xMax);
            AddLabel(tick.DayNumber.ToString(CultureInfo.InvariantCulture), midX, plot.Bottom + 6, mutedBrush, 10);

            var showDate = isEdge || (tick.DayNumber - 1) % 7 == 0;
            if (!showDate || midX - lastDateX < dateMinSpacing)
                continue;

            lastDateX = midX;
            AddLabel(tick.Date.ToString("d", CultureInfo.CurrentCulture), midX, plot.Top - 18, mutedBrush, 10);
        }
    }

    private void DrawPolyline(
        IReadOnlyList<UsageChartPoint> points,
        Rect plot,
        decimal xMin,
        decimal xMax,
        decimal yMin,
        decimal yMax,
        decimal clipMin,
        decimal clipMax,
        Color color,
        bool dashed)
    {
        if (points.Count < 2)
            return;

        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 2,
            StrokeLineJoin = PenLineJoin.Round,
            Fill = null
        };
        if (dashed)
            polyline.StrokeDashArray = new DoubleCollection { 5, 3 };

        foreach (var point in ClipToXRange(points, clipMin, clipMax))
        {
            polyline.Points.Add(new Point(
                MapX(point.X, plot, xMin, xMax),
                MapY(point.Y, plot, yMin, yMax)));
        }

        if (polyline.Points.Count >= 2)
            PlotCanvas.Children.Add(polyline);
    }

    private static List<UsageChartPoint> ClipToXRange(
        IReadOnlyList<UsageChartPoint> points,
        decimal xMin,
        decimal xMax)
    {
        var clipped = new List<UsageChartPoint>(points.Count);
        UsageChartPoint? previous = null;
        foreach (var point in points)
        {
            if (point.X < xMin)
            {
                previous = point;
                continue;
            }

            if (previous is { } before && clipped.Count == 0 && before.X < xMin)
                clipped.Add(InterpolateX(before, point, xMin));

            if (point.X > xMax)
            {
                if (previous is { } inside && inside.X <= xMax)
                    clipped.Add(InterpolateX(inside, point, xMax));
                break;
            }

            clipped.Add(point);
            previous = point;
        }

        return clipped;
    }

    private static UsageChartPoint InterpolateX(UsageChartPoint left, UsageChartPoint right, decimal x)
    {
        var span = right.X - left.X;
        var t = span == 0 ? 0m : (x - left.X) / span;
        return new UsageChartPoint { X = x, Y = left.Y + t * (right.Y - left.Y) };
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
                Stroke = ThemeBrush("CardBackgroundFillColorDefaultBrush", Color.FromArgb(255, 255, 255, 255)),
                StrokeThickness = 1
            };
            Canvas.SetLeft(ellipse, MapX(marker.X, plot, xMin, xMax) - MarkerSize / 2);
            Canvas.SetTop(ellipse, MapY(marker.Y, plot, yMin, yMax) - MarkerSize / 2);
            ToolTipService.SetToolTip(ellipse, MarkerTooltip(marker));
            PlotCanvas.Children.Add(ellipse);
        }
    }

    private void DrawLegend(UsageChartDocument document, Brush mutedBrush)
    {
        var expectedRow = CreateLegendRow(mutedBrush,
            ("Cursor (expected)", CursorExpectedColor, true),
            ("Other Models (expected)", OtherExpectedColor, true));
        LegendPanel.Children.Add(expectedRow);

        if (!document.HasCursorEstimated && !document.HasOtherEstimated)
            return;

        var estimated = new List<(string Label, Color Color, bool Dashed)>();
        if (document.HasCursorEstimated)
            estimated.Add(("Cursor (estimated)", CursorEstimatedColor, false));
        if (document.HasOtherEstimated)
            estimated.Add(("Other Models (estimated)", OtherEstimatedColor, false));
        LegendPanel.Children.Add(CreateLegendRow(mutedBrush, estimated.ToArray()));
    }

    private static StackPanel CreateLegendRow(Brush mutedBrush, params (string Label, Color Color, bool Dashed)[] items)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        foreach (var item in items)
        {
            var entry = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            var line = new Rectangle
            {
                Width = 18,
                Height = 2,
                Fill = item.Dashed ? null : new SolidColorBrush(item.Color),
                Stroke = new SolidColorBrush(item.Color),
                StrokeThickness = item.Dashed ? 1.5 : 0,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (item.Dashed)
                line.StrokeDashArray = new DoubleCollection { 3, 2 };

            entry.Children.Add(line);
            entry.Children.Add(new TextBlock
            {
                Text = item.Label,
                FontSize = 11,
                Foreground = mutedBrush,
                VerticalAlignment = VerticalAlignment.Center
            });
            row.Children.Add(entry);
        }

        return row;
    }

    private void AddLabel(string text, double x, double y, Brush brush, double fontSize, bool alignRight = false)
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

    private static Color MarkerColor(UsageChartMarker marker)
    {
        if (marker.MarkerKind == ChartMarkerKind.Origin)
            return Color.FromArgb(255, 80, 80, 80);

        return marker.QuotaKind switch
        {
            QuotaKind.CursorModels => marker.MarkerKind == ChartMarkerKind.Edit
                ? CursorExpectedColor
                : CursorEstimatedColor,
            QuotaKind.OtherModels => marker.MarkerKind == ChartMarkerKind.Edit
                ? OtherExpectedColor
                : OtherEstimatedColor,
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

    private Brush ThemeBrush(string key, Color fallback)
    {
        if (ActualTheme == ElementTheme.Dark && key == "CardBackgroundFillColorDefaultBrush")
            fallback = Color.FromArgb(255, 32, 32, 32);

        if (Application.Current.Resources.TryGetValue(key, out var value) && value is Brush brush)
            return brush;
        return new SolidColorBrush(fallback);
    }
}

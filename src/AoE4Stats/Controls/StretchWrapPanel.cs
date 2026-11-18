using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace AoE4Stats.Controls;

/// <summary>
/// Equal-width wrapping rows. ItemsRepeater's UniformGridLayout returns E_FAIL
/// ("Unspecified error") when items are added under a ScrollViewer.
/// </summary>
public sealed class StretchWrapPanel : Panel
{
    private double _measuredItemWidth;

    public static readonly DependencyProperty MinItemWidthProperty = DependencyProperty.Register(
        nameof(MinItemWidth),
        typeof(double),
        typeof(StretchWrapPanel),
        new PropertyMetadata(280d, OnLayoutPropertyChanged));

    public static readonly DependencyProperty ColumnSpacingProperty = DependencyProperty.Register(
        nameof(ColumnSpacing),
        typeof(double),
        typeof(StretchWrapPanel),
        new PropertyMetadata(0d, OnLayoutPropertyChanged));

    public static readonly DependencyProperty RowSpacingProperty = DependencyProperty.Register(
        nameof(RowSpacing),
        typeof(double),
        typeof(StretchWrapPanel),
        new PropertyMetadata(0d, OnLayoutPropertyChanged));

    public double MinItemWidth
    {
        get => (double)GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public double ColumnSpacing
    {
        get => (double)GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    public double RowSpacing
    {
        get => (double)GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count == 0)
        {
            return new Size(0, 0);
        }

        var (columns, itemWidth) = GetLayout(availableSize.Width);
        _measuredItemWidth = itemWidth;
        var height = MeasureRows(columns, itemWidth);
        var width = double.IsFinite(availableSize.Width) && availableSize.Width > 0
            ? availableSize.Width
            : columns * itemWidth + ColumnSpacing * (columns - 1);
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0)
        {
            return finalSize;
        }

        var (columns, itemWidth) = GetLayout(finalSize.Width);
        if (Math.Abs(itemWidth - _measuredItemWidth) > 0.5)
        {
            MeasureRows(columns, itemWidth);
        }

        var columnSpacing = ColumnSpacing;
        var rowSpacing = RowSpacing;
        double y = 0;
        var rowStart = 0;
        for (var i = 0; i < Children.Count; i++)
        {
            if (i % columns != columns - 1 && i != Children.Count - 1)
            {
                continue;
            }

            double rowHeight = 0;
            for (var j = rowStart; j <= i; j++)
            {
                rowHeight = Math.Max(rowHeight, Children[j].DesiredSize.Height);
            }

            double x = 0;
            for (var j = rowStart; j <= i; j++)
            {
                Children[j].Arrange(new Rect(x, y, itemWidth, rowHeight));
                x += itemWidth + columnSpacing;
            }

            y += rowHeight + rowSpacing;
            rowStart = i + 1;
        }

        return finalSize;
    }

    private double MeasureRows(int columns, double itemWidth)
    {
        var slot = new Size(itemWidth, double.PositiveInfinity);
        var rowSpacing = RowSpacing;
        double height = 0;
        double rowHeight = 0;
        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].Measure(slot);
            rowHeight = Math.Max(rowHeight, Children[i].DesiredSize.Height);
            if (i % columns == columns - 1 || i == Children.Count - 1)
            {
                height += rowHeight;
                if (i != Children.Count - 1)
                {
                    height += rowSpacing;
                }

                rowHeight = 0;
            }
        }

        return height;
    }

    private (int Columns, double ItemWidth) GetLayout(double availableWidth)
    {
        var min = MinItemWidth > 0 ? MinItemWidth : 1;
        var spacing = Math.Max(0, ColumnSpacing);
        if (!double.IsFinite(availableWidth) || availableWidth <= 0)
        {
            return (1, min);
        }

        var columns = Math.Max(1, (int)Math.Floor((availableWidth + spacing) / (min + spacing)));
        var itemWidth = (availableWidth - spacing * (columns - 1)) / columns;
        if (itemWidth < 1)
        {
            return (1, Math.Max(availableWidth, 1));
        }

        return (columns, itemWidth);
    }

    private static void OnLayoutPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is StretchWrapPanel panel)
        {
            panel.InvalidateMeasure();
        }
    }
}

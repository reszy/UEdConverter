using System.Windows;
using System.Windows.Controls;

namespace UedConverter.Components;

public class CenterFirstPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        Size total = new(0, 0);
        foreach (UIElement child in InternalChildren)
        {
            child.Measure(availableSize);
            var sz = child.DesiredSize;
            total.Height = Math.Max(total.Height, sz.Height);
            total.Width += sz.Width;
        }

        total.Width = Math.Min(total.Width, availableSize.Width);
        return new Size(total.Width, Math.Min(total.Height, availableSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (InternalChildren.Count == 0) return finalSize;

        // first child centered horizontally
        UIElement first = InternalChildren[0];
        Size firstSize = first.DesiredSize;
        double firstX = (finalSize.Width - firstSize.Width) / 2.0;
        double y = (finalSize.Height - firstSize.Height) / 2.0;
        first.Arrange(new Rect(firstX, y, firstSize.Width, firstSize.Height));

        // place following children to the right of the centered first child
        double x = firstX + firstSize.Width;
        for (int i = 1; i < InternalChildren.Count; i++)
        {
            UIElement child = InternalChildren[i];
            Size sz = child.DesiredSize;
            double childY = (finalSize.Height - sz.Height) / 2.0;
            // If there's no space to the right, still arrange within bounds
            child.Arrange(new Rect(x, childY, sz.Width, sz.Height));
            x += sz.Width;
        }

        return finalSize;
    }
}

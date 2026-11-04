using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace ShaderStudio.App.Ui;

/// <summary>Styles for the custom control classes used by the hand-built UI.</summary>
internal static class AppStyles
{
    public static Styles Create()
    {
        var styles = new Styles();

        // Sidebar tab button.
        styles.Add(new Style(selector => selector.OfType<Button>().Class("nav"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Brushes.Transparent),
                new Setter(Button.BorderThicknessProperty, new Thickness(0)),
                new Setter(Button.PaddingProperty, new Thickness(12, 9)),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Left),
                new Setter(Button.FontSizeProperty, 13.0),
                new Setter(Button.ForegroundProperty, Palette.TextMutedBrush),
            },
        });

        styles.Add(new Style(selector => selector.OfType<Button>().Class("nav").Class(":pointerover"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Palette.SurfaceAltBrush),
                new Setter(Button.ForegroundProperty, Palette.TextPrimaryBrush),
            },
        });

        styles.Add(new Style(selector => selector.OfType<Button>().Class("nav").Class("active"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0F2B3A"))),
                new Setter(Button.ForegroundProperty, Palette.AccentBrush),
            },
        });

        // Primary action button.
        styles.Add(new Style(selector => selector.OfType<Button>().Class("primary"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Palette.AccentBrush),
                new Setter(Button.ForegroundProperty, new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#04121A"))),
                new Setter(Button.FontWeightProperty, Avalonia.Media.FontWeight.SemiBold),
                new Setter(Button.PaddingProperty, new Thickness(16, 8)),
            },
        });

        styles.Add(new Style(selector => selector.OfType<Button>().Class("primary").Class(":pointerover"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Palette.AccentDimBrush),
            },
        });

        // Quiet button.
        styles.Add(new Style(selector => selector.OfType<Button>().Class("ghost"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Palette.SurfaceAltBrush),
                new Setter(Button.ForegroundProperty, Palette.TextPrimaryBrush),
                new Setter(Button.PaddingProperty, new Thickness(12, 7)),
            },
        });

        // Small icon-only button.
        styles.Add(new Style(selector => selector.OfType<Button>().Class("icon"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Brushes.Transparent),
                new Setter(Button.BorderThicknessProperty, new Thickness(0)),
                new Setter(Button.PaddingProperty, new Thickness(0)),
                new Setter(Button.WidthProperty, 26.0),
                new Setter(Button.HeightProperty, 26.0),
                new Setter(Button.ForegroundProperty, Palette.TextMutedBrush),
                new Setter(Button.FontSizeProperty, 14.0),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center),
            },
        });

        styles.Add(new Style(selector => selector.OfType<Button>().Class("icon").Class(":pointerover"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Palette.BorderBrush),
                new Setter(Button.ForegroundProperty, Palette.TextPrimaryBrush),
            },
        });

        return styles;
    }
}

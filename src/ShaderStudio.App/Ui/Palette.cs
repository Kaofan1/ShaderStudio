using Avalonia.Media;

namespace ShaderStudio.App.Ui;

/// <summary>Colour palette and shared brushes of the dark UI theme.</summary>
internal static class Palette
{
    public static readonly Color Background = Color.Parse("#0B0E13");
    public static readonly Color Surface = Color.Parse("#131922");
    public static readonly Color SurfaceAlt = Color.Parse("#1A2230");
    public static readonly Color Border = Color.Parse("#242E3C");
    public static readonly Color Accent = Color.Parse("#0AC2FF");
    public static readonly Color AccentDim = Color.Parse("#11A7D6");
    public static readonly Color TextPrimary = Color.Parse("#E8EEF7");
    public static readonly Color TextMuted = Color.Parse("#8794A6");
    public static readonly Color Danger = Color.Parse("#FF6B6B");
    public static readonly Color Success = Color.Parse("#46D39A");
    public static readonly Color Warning = Color.Parse("#FFB300");

    public static readonly IBrush BackgroundBrush = new SolidColorBrush(Background);
    public static readonly IBrush SurfaceBrush = new SolidColorBrush(Surface);
    public static readonly IBrush SurfaceAltBrush = new SolidColorBrush(SurfaceAlt);
    public static readonly IBrush BorderBrush = new SolidColorBrush(Border);
    public static readonly IBrush AccentBrush = new SolidColorBrush(Accent);
    public static readonly IBrush AccentDimBrush = new SolidColorBrush(AccentDim);
    public static readonly IBrush TextPrimaryBrush = new SolidColorBrush(TextPrimary);
    public static readonly IBrush TextMutedBrush = new SolidColorBrush(TextMuted);
    public static readonly IBrush DangerBrush = new SolidColorBrush(Danger);
    public static readonly IBrush SuccessBrush = new SolidColorBrush(Success);
    public static readonly IBrush WarningBrush = new SolidColorBrush(Warning);
}

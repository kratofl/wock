using MudBlazor;

namespace Wock.Components.Theme;

public static class WockTheme
{
    public const string Brand = "#ff906c";
    public const string SecondaryAccent = "#5af8fb";

    private static readonly string[] FontStack =
    [
        "-apple-system",
        "BlinkMacSystemFont",
        "SF Pro Text",
        "Segoe UI",
        "Arial",
        "sans-serif"
    ];

    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Brand,
            PrimaryContrastText = "#2d150d",
            PrimaryDarken = "#e26f4f",
            PrimaryLighten = "#fff1ec",
            Secondary = SecondaryAccent,
            SecondaryContrastText = "#082426",
            SecondaryDarken = "#20dfe4",
            SecondaryLighten = "#e6feff",
            Tertiary = "#1d1d1f",
            TertiaryContrastText = "#f5f5f7",
            Background = "#f3f4f6",
            BackgroundGray = "#e5e7eb",
            Surface = "#ffffff",
            DrawerBackground = "#ffffff",
            DrawerText = "#111827",
            DrawerIcon = "#4b5563",
            AppbarBackground = "#ffffff",
            AppbarText = "#111827",
            TextPrimary = "#111827",
            TextSecondary = "#4b5563",
            LinesDefault = "#d1d5db",
            LinesInputs = "#b8c0cc",
            TableLines = "#e5e7eb",
            TableHover = "#f9fafb",
            Divider = "#d1d5db",
            ActionDefault = "#4b5563",
            RippleOpacity = 0,
            RippleOpacitySecondary = 0,
            Error = "#dc2626",
            ErrorContrastText = "#fef2f2",
            ErrorLighten = "#fef2f2",
            ErrorDarken = "#991b1b"
        },
        PaletteDark = new PaletteDark
        {
            Primary = Brand,
            PrimaryContrastText = "#2d150d",
            PrimaryDarken = "#e26f4f",
            PrimaryLighten = "#4a2a23",
            Secondary = SecondaryAccent,
            SecondaryContrastText = "#082426",
            SecondaryDarken = "#20dfe4",
            SecondaryLighten = "#123b40",
            Tertiary = "#f8fafc",
            TertiaryContrastText = "#020817",
            Background = "#0b0f17",
            BackgroundGray = "#151a23",
            Surface = "#111722",
            DrawerBackground = "#0b0f17",
            DrawerText = "#f5f5f7",
            DrawerIcon = "#a1a1aa",
            AppbarBackground = "#0b0f17",
            AppbarText = "#f5f5f7",
            TextPrimary = "#f5f5f7",
            TextSecondary = "#a1a1aa",
            LinesDefault = "#273142",
            LinesInputs = "#343f52",
            TableLines = "#273142",
            TableHover = "#151a23",
            Divider = "#273142",
            ActionDefault = "#a1a1aa",
            RippleOpacity = 0,
            RippleOpacitySecondary = 0,
            Error = "#f87171",
            ErrorContrastText = "#450a0a",
            ErrorLighten = "#451a1a",
            ErrorDarken = "#ef4444"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "0.625rem",
            DrawerWidthLeft = "14.5rem",
            DrawerMiniWidthLeft = "3.5rem",
            AppbarHeight = "3.25rem"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = FontStack,
                FontSize = "0.875rem",
                LineHeight = "1.45"
            },
            H3 = new H3Typography
            {
                FontFamily = FontStack,
                FontWeight = "600",
                FontSize = "2rem",
                LineHeight = "1.15",
                LetterSpacing = "-0.025em"
            },
            H5 = new H5Typography
            {
                FontFamily = FontStack,
                FontWeight = "600",
                FontSize = "1.1rem",
                LineHeight = "1.25",
                LetterSpacing = "-0.01em"
            },
            H6 = new H6Typography
            {
                FontFamily = FontStack,
                FontWeight = "600",
                FontSize = "1rem",
                LineHeight = "1.25"
            },
            Button = new ButtonTypography
            {
                FontFamily = FontStack,
                FontWeight = "500",
                FontSize = "0.8125rem",
                TextTransform = "none"
            }
        }
    };
}

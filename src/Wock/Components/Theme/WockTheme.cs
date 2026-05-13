using MudBlazor;

namespace Wock.Components.Theme;

public static class WockTheme
{
    public const string Primary = "#ff906c";
    public const string Brand = Primary;
    public const string Action = Primary;
    public const string ChartAccent = Action;
    public const string Support = "#8b8ea3";

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
            PrimaryContrastText = "#ffffff",
            PrimaryDarken = "#e26f4f",
            PrimaryLighten = "#fff1ec",
            Secondary = Support,
            SecondaryContrastText = "#ffffff",
            SecondaryDarken = "#6f7286",
            SecondaryLighten = "#efe9fb",
            Tertiary = "#21172f",
            TertiaryContrastText = "#f5f5f7",
            Background = "#f7f3ff",
            BackgroundGray = "#efe8fb",
            Surface = "#fffefd",
            DrawerBackground = "#fffefd",
            DrawerText = "#21172f",
            DrawerIcon = "#70657d",
            AppbarBackground = "rgba(255,254,253,0.88)",
            AppbarText = "#21172f",
            TextPrimary = "#21172f",
            TextSecondary = "#70657d",
            LinesDefault = "#eadff4",
            LinesInputs = "#ddd0ec",
            TableLines = "#efe6f6",
            TableHover = "#fbf5ef",
            Divider = "#efe6f6",
            ActionDefault = "#70657d",
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
            PrimaryContrastText = "#f8fafc",
            PrimaryDarken = "#e26f4f",
            PrimaryLighten = "#4a2a23",
            Secondary = Support,
            SecondaryContrastText = "#f8fafc",
            SecondaryDarken = "#6f7286",
            SecondaryLighten = "#2b2e3d",
            Tertiary = "#f8fafc",
            TertiaryContrastText = "#020817",
            Background = "#120d1f",
            BackgroundGray = "#1b1429",
            Surface = "#1e172c",
            DrawerBackground = "#1a1228",
            DrawerText = "#f5f5f7",
            DrawerIcon = "#cfc2e0",
            AppbarBackground = "rgba(30,23,44,0.86)",
            AppbarText = "#f8f4ff",
            TextPrimary = "#f8f4ff",
            TextSecondary = "#cfc2e0",
            LinesDefault = "rgba(255,255,255,0.12)",
            LinesInputs = "rgba(255,255,255,0.16)",
            TableLines = "rgba(255,255,255,0.1)",
            TableHover = "rgba(255,144,108,0.08)",
            Divider = "rgba(255,255,255,0.1)",
            ActionDefault = "#cfc2e0",
            RippleOpacity = 0,
            RippleOpacitySecondary = 0,
            Error = "#f87171",
            ErrorContrastText = "#450a0a",
            ErrorLighten = "#451a1a",
            ErrorDarken = "#ef4444"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "1.25rem",
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

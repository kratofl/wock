using Microsoft.JSInterop;

namespace Wock.Components.Theme;

public sealed class ThemePreferenceService(IJSRuntime jsRuntime)
{
    private const string ThemePreferenceKey = "wock.theme";

    public event Action? Changed;

    public bool IsDarkMode { get; private set; }

    public bool IsLoaded { get; private set; }

    public async Task LoadAsync()
    {
        var savedTheme = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", ThemePreferenceKey);
        IsDarkMode = savedTheme == "dark";
        IsLoaded = true;
        Changed?.Invoke();
    }

    public async Task SetDarkModeAsync(bool isDarkMode)
    {
        IsDarkMode = isDarkMode;
        IsLoaded = true;
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", ThemePreferenceKey, isDarkMode ? "dark" : "light");
        Changed?.Invoke();
    }
}

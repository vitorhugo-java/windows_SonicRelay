using System.Text.Json;
using System.Text.Json.Serialization;

namespace SonicRelay.Windows.Core.Configuration;

/// <summary>Requested application theme.</summary>
public enum AppTheme
{
    System,
    Light,
    Dark
}

/// <summary>Window material behind the app content.</summary>
public enum AppBackdrop
{
    Mica,
    Acrylic,
    None
}

/// <summary>
/// Persists the user's appearance preferences (issue #30): theme (system/light/dark),
/// the window backdrop material (Mica/Acrylic/solid) and the acrylic tint opacity.
/// Mirrors <see cref="TrayBackgroundPreferenceStore"/>: a small user-scoped JSON file
/// cached in memory so the window can read it synchronously at startup, with safe
/// defaults when the file is missing or corrupt so it never blocks launch.
/// </summary>
public sealed class AppearancePreferenceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string DefaultPath => Path.Combine(UserConfigurationLoader.DefaultDirectory, "appearance.json");

    private readonly string _path;

    public AppearancePreferenceStore(string? path = null)
    {
        _path = path ?? DefaultPath;
        var document = Load();
        Theme = document.Theme;
        Backdrop = document.Backdrop;
        TintOpacity = ClampOpacity(document.TintOpacity);
    }

    /// <summary>Requested theme; <see cref="AppTheme.System"/> follows Windows.</summary>
    public AppTheme Theme { get; private set; }

    /// <summary>Window backdrop material.</summary>
    public AppBackdrop Backdrop { get; private set; }

    /// <summary>
    /// Acrylic tint opacity in the range 0.0–1.0. Higher is more opaque (less
    /// see-through). Only affects the Acrylic backdrop; ignored for Mica/None.
    /// </summary>
    public double TintOpacity { get; private set; }

    public async Task UpdateAsync(
        AppTheme theme,
        AppBackdrop backdrop,
        double tintOpacity,
        CancellationToken cancellationToken = default)
    {
        Theme = theme;
        Backdrop = backdrop;
        TintOpacity = ClampOpacity(tintOpacity);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        await File.WriteAllTextAsync(
            _path,
            JsonSerializer.Serialize(new AppearanceDocument(Theme, Backdrop, TintOpacity), JsonOptions),
            cancellationToken);
    }

    private static double ClampOpacity(double value) => value switch
    {
        < 0 => 0,
        > 1 => 1,
        _ => value
    };

    private AppearanceDocument Load()
    {
        try
        {
            if (!File.Exists(_path)) return AppearanceDocument.Default;
            return JsonSerializer.Deserialize<AppearanceDocument>(File.ReadAllText(_path), JsonOptions)
                ?? AppearanceDocument.Default;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            // A missing/corrupt appearance file must never block startup.
            return AppearanceDocument.Default;
        }
    }

    private sealed record AppearanceDocument(AppTheme Theme, AppBackdrop Backdrop, double TintOpacity)
    {
        public static AppearanceDocument Default { get; } = new(AppTheme.Dark, AppBackdrop.Mica, 0.85);
    }
}

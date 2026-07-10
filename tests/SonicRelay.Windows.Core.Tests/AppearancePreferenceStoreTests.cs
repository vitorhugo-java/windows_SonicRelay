using SonicRelay.Windows.Core.Configuration;

namespace SonicRelay.Windows.Core.Tests;

public sealed class AppearancePreferenceStoreTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"sonic-appearance-{Guid.NewGuid():N}.json");

    [Fact]
    public void Defaults_to_dark_mica()
    {
        var store = new AppearancePreferenceStore(path);

        Assert.Equal(AppTheme.Dark, store.Theme);
        Assert.Equal(AppBackdrop.Mica, store.Backdrop);
        Assert.Equal(0.85, store.TintOpacity);
    }

    [Fact]
    public async Task Round_trips_all_values()
    {
        var store = new AppearancePreferenceStore(path);

        await store.UpdateAsync(AppTheme.Light, AppBackdrop.Acrylic, 0.4);

        var reloaded = new AppearancePreferenceStore(path);
        Assert.Equal(AppTheme.Light, reloaded.Theme);
        Assert.Equal(AppBackdrop.Acrylic, reloaded.Backdrop);
        Assert.Equal(0.4, reloaded.TintOpacity);
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(2.0, 1.0)]
    public async Task Clamps_opacity_to_unit_range(double input, double expected)
    {
        var store = new AppearancePreferenceStore(path);

        await store.UpdateAsync(AppTheme.Dark, AppBackdrop.Acrylic, input);

        Assert.Equal(expected, store.TintOpacity);
    }

    [Fact]
    public async Task Corrupt_file_falls_back_to_defaults()
    {
        await File.WriteAllTextAsync(path, "not json {{");

        var store = new AppearancePreferenceStore(path);

        Assert.Equal(AppTheme.Dark, store.Theme);
        Assert.Equal(AppBackdrop.Mica, store.Backdrop);
    }

    public void Dispose()
    {
        if (File.Exists(path)) File.Delete(path);
    }
}

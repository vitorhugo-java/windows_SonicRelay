namespace SonicRelay.Windows.Desktop.ViewModels;

/// <summary>
/// A sidebar navigation entry. Phase 2's shell ships a single live surface (Dashboard);
/// the remaining destinations are declared so <c>SidebarNavigation</c> renders the full
/// prototype rail, and their pages are filled in with the later slices (audio, session,
/// diagnostics, settings — issue #32).
/// </summary>
public sealed class NavigationItem : ViewModelBase
{
    private bool isSelected;
    private bool isEnabled = true;

    public NavigationItem(string glyph, string label)
    {
        Glyph = glyph;
        Label = label;
    }

    /// <summary>An emoji/text glyph; the shell avoids an icon-font dependency in this phase.</summary>
    public string Glyph { get; }
    public string Label { get; }

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    public bool IsEnabled
    {
        get => isEnabled;
        set => SetProperty(ref isEnabled, value);
    }
}

using System.Collections.Generic;
using Godot;

public partial class SettingsPageManager : Node
{
    private List<Button> _titles = new();
    private List<CanvasItem> _pages = new();

    public int currentPage = 1;

    public override void _Ready()
    {
        // Boutons titres
        var hbox = GetNode("VBoxContainer/HBoxContainer");

        foreach (Node child in hbox.GetChildren())
        {
            if (child is Button btn)
                _titles.Add(btn);
        }

        // Pages dans MainContent
        var mainContent = GetParent().GetNode("MainContent");

        _pages.Add(mainContent.GetNode<CanvasItem>("PageSubtitle"));
        _pages.Add(mainContent.GetNode<CanvasItem>("PageVideo"));
        _pages.Add(mainContent.GetNode<CanvasItem>("PageVisual"));
        _pages.Add(mainContent.GetNode<CanvasItem>("PageCommand"));
        _pages.Add(mainContent.GetNode<CanvasItem>("PageAudio"));

        UpdateAll();
    }

    public void ChangePage(int pageIndex)
    {
        currentPage = pageIndex;
        UpdateAll();
    }

    private void UpdateAll()
    {
        // Couleur boutons
        for (int i = 0; i < _titles.Count; i++)
            _titles[i].Modulate = Colors.White;

        if (currentPage - 1 >= 0 && currentPage - 1 < _titles.Count)
            _titles[currentPage - 1].Modulate = Colors.Red;

        // Visibilité pages
        for (int i = 0; i < _pages.Count; i++)
            _pages[i].Visible = false;

        if (currentPage - 1 >= 0 && currentPage - 1 < _pages.Count)
            _pages[currentPage - 1].Visible = true;
    }

    /// <summary>
    /// Hides all pages and resets button highlights.
    /// Called by MenuManager when hiding the settings screen.
    /// </summary>
    public void HideAllPages()
    {
        foreach (var page in _pages)
            page.Visible = false;

        foreach (var title in _titles)
            title.Modulate = Colors.White;

        var vbox = GetNodeOrNull<Control>("VBoxContainer");
        if (vbox != null)
        {
            vbox.Visible = false;
            vbox.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
    }

    /// <summary>
    /// Shows the current page and highlights its button.
    /// Called by MenuManager when showing the settings screen.
    /// </summary>
    public void ShowCurrentPage()
    {
        var vbox = GetNodeOrNull<Control>("VBoxContainer");

        if (vbox != null)
        {
            vbox.Visible = true;
            vbox.MouseFilter = Control.MouseFilterEnum.Ignore;
        }

        UpdateAll();
    }

    // === Connexions boutons ===

    public void OnButtonSubtitlePressed() => ChangePage(1);
    public void OnButtonVideoPressed() => ChangePage(2);
    public void OnButtonVisualPressed() => ChangePage(3);
    public void OnButtonCommandPressed() => ChangePage(4);
    public void OnButtonAudioPressed() => ChangePage(5);
}

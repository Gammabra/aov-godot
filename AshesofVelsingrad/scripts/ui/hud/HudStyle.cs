using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Shared visual constants and helpers for every HUD widget — colours, panel styles,
///     bar styles, label / button theme overrides.
/// </summary>
/// <remarks>
///     Keeps the HUD's look-and-feel in one file. A future iteration can replace this with a
///     Godot <see cref="Theme" /> resource without changing widget code.
/// </remarks>
public static class HudStyle
{
    /// <summary>Translucent dark background used by every HUD panel.</summary>
    public static Color PanelBackground => new(0.08f, 0.07f, 0.10f, 0.78f);

    /// <summary>Subtle border colour around panels.</summary>
    public static Color PanelBorder => new(0.55f, 0.45f, 0.30f, 0.85f);

    /// <summary>Foreground text colour.</summary>
    public static Color TextColor => new(0.95f, 0.92f, 0.84f, 1f);

    /// <summary>Dimmed text colour (used for placeholders / labels).</summary>
    public static Color DimText => new(0.55f, 0.52f, 0.48f, 1f);

    /// <summary>Healthy HP bar fill.</summary>
    public static Color HpFill => new(0.78f, 0.18f, 0.20f, 1f);

    /// <summary>Mana bar fill.</summary>
    public static Color ManaFill => new(0.20f, 0.45f, 0.85f, 1f);

    /// <summary>Player chip colour for the turn queue.</summary>
    public static Color PlayerColor => new(0.30f, 0.80f, 0.95f, 1f);

    /// <summary>Enemy chip colour for the turn queue.</summary>
    public static Color EnemyColor => new(0.90f, 0.30f, 0.30f, 1f);

    // Font sizes ----------------------------------------------------------
    //
    // The HUD has four "tiers" of text — a hero title (Victory!, GameOver),
    // a section header (panel headings, unit name), the body size used by
    // most labels and buttons, and a footnote size for log entries / dim
    // metadata. They're declared here so widgets stop magic-numbering the
    // sizes and so a future redesign only touches one file.

    /// <summary>Hero font size (Victory / GameOver title).</summary>
    public const int FontSizeTitle = 36;

    /// <summary>Section-header font size (panel titles, unit name above HP).</summary>
    public const int FontSizeHeader = 16;

    /// <summary>Body font size (most labels, buttons).</summary>
    public const int FontSizeBody = 14;

    /// <summary>Footnote font size (log lines, dim subtitles).</summary>
    public const int FontSizeSmall = 13;

    /// <summary>
    ///     Returns <paramref name="baseSize" /> multiplied by the user's UI scale
    ///     setting (<see cref="SettingsManager.GetUiScale" />). Always returns at
    ///     least <c>1</c> so Godot's font renderer never gets a zero/negative size.
    /// </summary>
    /// <param name="baseSize">Design-time font size — one of the
    ///     <see cref="FontSizeTitle" />/<see cref="FontSizeHeader" />/
    ///     <see cref="FontSizeBody" />/<see cref="FontSizeSmall" /> constants.</param>
    /// <returns>Scaled, integer-rounded font size suitable for
    ///     <see cref="Control.AddThemeFontSizeOverride" />.</returns>
    public static int ScaledFontSize(int baseSize)
    {
        var scale = SettingsManager.Instance?.GetUiScale() ?? SettingsManager.UiScaleDefault;
        var scaled = Mathf.RoundToInt(baseSize * scale);
        return Mathf.Max(1, scaled);
    }

    // Live-rescale plumbing -----------------------------------------------
    //
    // BattleHud subscribes to SettingsManager.UiScaleChanged and forwards the
    // event to every styled control by walking the tree. Each control that
    // wants to participate stamps its design-time base size into a meta
    // entry; the walker reads that meta and re-applies the theme override.
    // Without the meta we'd have no way to know the original size — we'd
    // pull the *currently scaled* size and end up exponentially scaling on
    // every change.

    /// <summary>Meta key identifying the design-time font size of a styled control.</summary>
    public const string FontSizeMetaKey = "hud_base_font_size";

    /// <summary>Meta key identifying the theme override property used (e.g. "font_size", "normal_font_size").</summary>
    public const string FontSizePropertyMetaKey = "hud_font_size_property";

    /// <summary>
    ///     Apply a UI-scaled font size override to <paramref name="control" /> AND remember
    ///     the design-time base so a later <see cref="RefreshScaledFonts" /> walk can
    ///     re-apply it cleanly.
    /// </summary>
    /// <param name="control">Control receiving the override.</param>
    /// <param name="property">Theme override property name (<c>"font_size"</c> for Label/Button,
    ///     <c>"normal_font_size"</c> for RichTextLabel).</param>
    /// <param name="baseSize">Design-time font size.</param>
    public static void ApplyScaledFontSize(Control control, string property, int baseSize)
    {
        control.SetMeta(FontSizeMetaKey, baseSize);
        control.SetMeta(FontSizePropertyMetaKey, property);
        control.AddThemeFontSizeOverride(property, ScaledFontSize(baseSize));
    }

    /// <summary>
    ///     Walk <paramref name="root" />'s descendants and re-apply font-size overrides for
    ///     every control that was tagged via <see cref="ApplyScaledFontSize" /> or one of
    ///     the <c>StyleLabel</c>/<c>StyleButton</c> helpers. Cheap — only touches Controls
    ///     that opted in.
    /// </summary>
    /// <param name="root">Subtree root (typically the BattleHud CanvasLayer).</param>
    public static void RefreshScaledFonts(Node root)
    {
        if (root is Control control && control.HasMeta(FontSizeMetaKey))
        {
            var baseSize = (int)control.GetMeta(FontSizeMetaKey);
            var property = control.GetMeta(FontSizePropertyMetaKey, "font_size").AsString();
            control.AddThemeFontSizeOverride(property, ScaledFontSize(baseSize));
        }

        foreach (var child in root.GetChildren())
        {
            RefreshScaledFonts(child);
        }
    }

    /// <summary>Build a translucent rounded panel <see cref="StyleBoxFlat" />.</summary>
    /// <returns>A reusable stylebox.</returns>
    public static StyleBoxFlat MakePanelStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = PanelBackground,
            BorderColor = PanelBorder,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
    }

    /// <summary>
    ///     Wrap <paramref name="content" /> in a styled <see cref="PanelContainer" /> filling the parent.
    /// </summary>
    /// <param name="content">Control to host inside the panel.</param>
    /// <returns>A panel containing <paramref name="content" />, with <c>MouseFilter.Ignore</c>
    ///     so empty space lets clicks through to the map.</returns>
    public static PanelContainer MakePanel(Control content)
    {
        PanelContainer panel = new()
        {
            Name = "Panel",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride("panel", MakePanelStyle());
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        panel.AddChild(content);
        return panel;
    }

    /// <summary>
    ///     Apply default text colour and a UI-scaled font size to a <see cref="Label" />.
    /// </summary>
    /// <param name="label">Label to style.</param>
    /// <param name="baseSize">Design-time font size; defaults to <see cref="FontSizeBody" />.</param>
    public static void StyleLabel(Label label, int baseSize = FontSizeBody)
    {
        label.AddThemeColorOverride("font_color", TextColor);
        ApplyScaledFontSize(label, "font_size", baseSize);
    }

    /// <summary>
    ///     Apply default text colour and a UI-scaled font size to a <see cref="Button" />.
    /// </summary>
    /// <param name="button">Button to style.</param>
    /// <param name="baseSize">Design-time font size; defaults to <see cref="FontSizeBody" />.</param>
    public static void StyleButton(Button button, int baseSize = FontSizeBody)
    {
        button.AddThemeColorOverride("font_color", TextColor);
        button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.95f, 0.7f));
        ApplyScaledFontSize(button, "font_size", baseSize);
    }

    /// <summary>Build a HP/MP-style stylebox pair for a <see cref="ProgressBar" />.</summary>
    /// <param name="bar">The progress bar to style.</param>
    /// <param name="fillColor">Colour for the filled portion.</param>
    public static void ApplyBarStyle(ProgressBar bar, Color fillColor)
    {
        StyleBoxFlat bg = new()
        {
            BgColor = new Color(0.10f, 0.09f, 0.10f, 0.85f),
            BorderColor = new Color(0, 0, 0, 0.5f),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
        };
        StyleBoxFlat fill = new()
        {
            BgColor = fillColor,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
        };
        bar.AddThemeStyleboxOverride("background", bg);
        bar.AddThemeStyleboxOverride("fill", fill);
    }
}

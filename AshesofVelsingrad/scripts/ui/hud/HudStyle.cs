using Godot;

namespace AshesOfVelsingrad.ui.hud;

/// <summary>
///     Shared visual constants and helpers for the battle HUD widgets.
/// </summary>
/// <remarks>
///     Keeps every widget's look-and-feel in one place. Update colours / corner radii here
///     and every widget rebuilt from these helpers picks up the change. A future iteration can
///     replace this with a proper Godot <see cref="Theme" /> resource without changing widget code.
/// </remarks>
public static class HudStyle
{
    /// <summary>Semi-transparent dark background used by every panel.</summary>
    public static Color PanelBackground => new(0.08f, 0.07f, 0.10f, 0.78f);

    /// <summary>Subtle border colour to outline panels.</summary>
    public static Color PanelBorder => new(0.55f, 0.45f, 0.30f, 0.85f);

    /// <summary>Foreground text colour.</summary>
    public static Color TextColor => new(0.95f, 0.92f, 0.84f, 1f);

    /// <summary>Disabled / dimmed text colour.</summary>
    public static Color DimText => new(0.55f, 0.52f, 0.48f, 1f);

    /// <summary>Healthy HP bar colour.</summary>
    public static Color HpFill => new(0.78f, 0.18f, 0.20f, 1f);

    /// <summary>Mana bar colour.</summary>
    public static Color ManaFill => new(0.20f, 0.45f, 0.85f, 1f);

    /// <summary>Player faction chip colour for the turn queue.</summary>
    public static Color PlayerColor => new(0.30f, 0.80f, 0.95f, 1f);

    /// <summary>Ally faction chip colour.</summary>
    public static Color AllyColor => new(0.30f, 0.90f, 0.30f, 1f);

    /// <summary>Enemy faction chip colour.</summary>
    public static Color EnemyColor => new(0.90f, 0.30f, 0.30f, 1f);

    /// <summary>
    ///     Build a translucent rounded panel background.
    /// </summary>
    /// <returns>A reusable <see cref="StyleBoxFlat" /> instance.</returns>
    public static StyleBoxFlat MakePanelStyle()
    {
        StyleBoxFlat sb = new()
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
        return sb;
    }

    /// <summary>
    ///     Wrap <paramref name="content" /> in a styled <see cref="PanelContainer" /> filling the parent.
    /// </summary>
    /// <param name="content">The control to host inside the panel.</param>
    /// <returns>A styled panel containing <paramref name="content" />.</returns>
    /// <remarks>
    ///     The panel's <see cref="Control.MouseFilter" /> is set to
    ///     <see cref="Control.MouseFilterEnum.Ignore" /> so empty space inside HUD widgets
    ///     does not catch mouse events — interactive children (buttons etc.) keep their own
    ///     default <c>Stop</c> filter and continue to receive clicks.
    /// </remarks>
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
    ///     Apply default text colour to a label.
    /// </summary>
    /// <param name="label">The label to style.</param>
    public static void StyleLabel(Label label)
    {
        label.AddThemeColorOverride("font_color", TextColor);
        label.AddThemeFontSizeOverride("font_size", 14);
    }

    /// <summary>
    ///     Apply default text colour to a button.
    /// </summary>
    /// <param name="button">The button to style.</param>
    public static void StyleButton(Button button)
    {
        button.AddThemeColorOverride("font_color", TextColor);
        button.AddThemeColorOverride("font_hover_color", new Color(1, 0.95f, 0.7f));
        button.AddThemeFontSizeOverride("font_size", 14);
    }
}

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

    /// <summary>Apply default text colour and font size to a <see cref="Label" />.</summary>
    /// <param name="label">Label to style.</param>
    public static void StyleLabel(Label label)
    {
        label.AddThemeColorOverride("font_color", TextColor);
        label.AddThemeFontSizeOverride("font_size", 14);
    }

    /// <summary>Apply default text colour and font size to a <see cref="Button" />.</summary>
    /// <param name="button">Button to style.</param>
    public static void StyleButton(Button button)
    {
        button.AddThemeColorOverride("font_color", TextColor);
        button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.95f, 0.7f));
        button.AddThemeFontSizeOverride("font_size", 14);
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

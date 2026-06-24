using System.Collections.Generic;
using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Shared visual constants and helpers for every HUD widget — Souls-like dark-fantasy
///     palette, panel tiers, bar styling, scaled fonts, scaled metrics, layout helpers.
/// </summary>
public static class HudStyle
{
    // ── Palette ─────────────────────────────────────────────────────────
    /// <summary>Body / parchment text colour.</summary>
    public static Color Parchment => new(0.93f, 0.87f, 0.74f, 1f);
    /// <summary>Dim parchment for sub-text.</summary>
    public static Color ParchmentDim => new(0.66f, 0.60f, 0.50f, 1f);
    /// <summary>Title / header gold.</summary>
    public static Color Gold => new(0.86f, 0.70f, 0.36f, 1f);
    /// <summary>Bronze panel border.</summary>
    public static Color Bronze => new(0.55f, 0.42f, 0.22f, 1f);
    /// <summary>Faded bronze for inset lines.</summary>
    public static Color BronzeDim => new(0.34f, 0.26f, 0.14f, 0.95f);
    /// <summary>Heavy panel background.</summary>
    public static Color IronHeavy => new(0.07f, 0.06f, 0.06f, 0.94f);
    /// <summary>Lighter panel background.</summary>
    public static Color IronLight => new(0.12f, 0.10f, 0.10f, 0.85f);
    /// <summary>HP fill.</summary>
    public static Color Crimson => new(0.78f, 0.16f, 0.18f, 1f);
    /// <summary>HP fill darker stripe.</summary>
    public static Color CrimsonDeep => new(0.32f, 0.04f, 0.06f, 1f);
    /// <summary>MP fill.</summary>
    public static Color Azure => new(0.24f, 0.50f, 0.86f, 1f);
    /// <summary>MP fill darker stripe.</summary>
    public static Color AzureDeep => new(0.08f, 0.20f, 0.40f, 1f);
    /// <summary>Player faction chip.</summary>
    public static Color PlayerColor => new(0.28f, 0.78f, 0.94f, 1f);
    /// <summary>Ally faction chip.</summary>
    public static Color AllyColor => new(0.36f, 0.82f, 0.42f, 1f);
    /// <summary>Enemy faction chip.</summary>
    public static Color EnemyColor => new(0.86f, 0.22f, 0.22f, 1f);
    /// <summary>Hover gold.</summary>
    public static Color GoldHover => new(1f, 0.90f, 0.55f, 1f);
    /// <summary>Pressed deep crimson.</summary>
    public static Color PressedFill => new(0.20f, 0.06f, 0.06f, 0.95f);
    /// <summary>Disabled iron.</summary>
    public static Color DisabledFill => new(0.10f, 0.09f, 0.08f, 0.6f);
    /// <summary>Disabled text.</summary>
    public static Color DisabledText => new(0.45f, 0.42f, 0.36f, 0.75f);

    // Legacy aliases.
    /// <summary>Deprecated alias.</summary>
    public static Color PanelBackground => IronHeavy;
    /// <summary>Deprecated alias.</summary>
    public static Color PanelBorder => Bronze;
    /// <summary>Deprecated alias.</summary>
    public static Color TextColor => Parchment;
    /// <summary>Deprecated alias.</summary>
    public static Color DimText => ParchmentDim;
    /// <summary>Deprecated alias.</summary>
    public static Color HpFill => Crimson;
    /// <summary>Deprecated alias.</summary>
    public static Color ManaFill => Azure;

    // ── Font tiers ───────────────────────────────────────────────────────
    /// <summary>Hero title.</summary>
    public const int FontSizeTitle = 36;
    /// <summary>Section title.</summary>
    public const int FontSizeHeader = 18;
    /// <summary>Sub-header.</summary>
    public const int FontSizeSub = 16;
    /// <summary>Body. Kept at the 16px accessibility baseline for body text.</summary>
    public const int FontSizeBody = 16;
    /// <summary>Small footnote.</summary>
    public const int FontSizeSmall = 13;
    /// <summary>Tiny. Held at the 12px lower floor for readable on-screen text.</summary>
    public const int FontSizeTiny = 12;

    // ── Spacing tokens ──────────────────────────────────────────────────
    /// <summary>4 px.</summary>
    public const int PadXs = 4;
    /// <summary>6 px.</summary>
    public const int PadSm = 6;
    /// <summary>10 px.</summary>
    public const int PadMd = 10;
    /// <summary>14 px.</summary>
    public const int PadLg = 14;
    /// <summary>20 px.</summary>
    public const int PadXl = 20;

    // ── Component design sizes ──────────────────────────────────────────
    /// <summary>Action-bar button height.</summary>
    public const int ButtonHeight = 44;
    /// <summary>Skill-slot edge.</summary>
    public const int SkillSlotSize = 52;
    /// <summary>Default action-button icon max width (kept small so the text label fits).</summary>
    public const int ActionIconSize = 22;
    /// <summary>Slot icon max width.</summary>
    public const int SlotIconSize = 36;
    /// <summary>HP bar height.</summary>
    public const int HpBarHeight = 16;
    /// <summary>MP bar height.</summary>
    public const int MpBarHeight = 10;

    /// <summary>Player-status panel width.</summary>
    public const int PlayerStatusWidth = 340;
    /// <summary>Player-status panel height. Sized so name + class + HP/MP rows never clip.</summary>
    public const int PlayerStatusHeight = 210;
    /// <summary>Player-status portrait edge.</summary>
    public const int PlayerPortrait = 76;

    /// <summary>Action-bar width. Sized so all action labels (incl. Cancel) fit without clipping.</summary>
    public const int ActionBarWidth = 560;
    /// <summary>Action-bar height.</summary>
    public const int ActionBarHeight = 72;

    /// <summary>Skill bar width.</summary>
    public const int SkillBarWidth = 440;
    /// <summary>Skill bar height. Tall enough for the slot square plus the skill-name label.</summary>
    public const int SkillBarHeight = 100;

    /// <summary>Battle-log width.</summary>
    public const int LogWidth = 280;
    /// <summary>Battle-log height.</summary>
    public const int LogHeight = 130;

    /// <summary>Enemy roster width.</summary>
    public const int RosterWidth = 280;
    /// <summary>Enemy roster height.</summary>
    public const int RosterHeight = 250;
    /// <summary>Roster portrait edge.</summary>
    public const int RosterPortrait = 36;

    /// <summary>Turn-queue chip portrait size.</summary>
    public const int TurnChipSize = 38;
    /// <summary>Turn-queue width.</summary>
    public const int TurnQueueWidth = 480;
    /// <summary>Turn-queue height.</summary>
    public const int TurnQueueHeight = 76;

    /// <summary>Context info panel width.</summary>
    public const int ContextWidth = 260;
    /// <summary>Context info panel height. Tall enough for the multi-line skill-target text.</summary>
    public const int ContextHeight = 156;

    /// <summary>Corruption gauge width.</summary>
    public const int CorruptionWidth = 232;
    /// <summary>Corruption gauge height.</summary>
    public const int CorruptionHeight = 60;

    // ── Scaling primitives ──────────────────────────────────────────────
    /// <summary>Current UI scale multiplier.</summary>
    public static float UiScale =>
        SettingsManager.Instance?.GetUiScale() ?? SettingsManager.UiScaleDefault;

    /// <summary>Returns <paramref name="baseSize"/> × UI scale, ≥ 1.</summary>
    public static int ScaledFontSize(int baseSize)
        => Mathf.Max(1, Mathf.RoundToInt(baseSize * UiScale));

    /// <summary>Returns <paramref name="px"/> × UI scale.</summary>
    public static int ScaledPx(int px)
        => Mathf.RoundToInt(px * UiScale);

    // ── Live-rescale plumbing ───────────────────────────────────────────
    /// <summary>Meta key for design-time font size.</summary>
    public const string FontSizeMetaKey = "hud_base_font_size";
    /// <summary>Meta key for theme override property.</summary>
    public const string FontSizePropertyMetaKey = "hud_font_size_property";

    /// <summary>Apply a UI-scaled font size override and remember the design base.</summary>
    public static void ApplyScaledFontSize(Control control, string property, int baseSize)
    {
        control.SetMeta(FontSizeMetaKey, baseSize);
        control.SetMeta(FontSizePropertyMetaKey, property);
        control.AddThemeFontSizeOverride(property, ScaledFontSize(baseSize));
    }

    /// <summary>
    ///     Walk the tree, re-applying tagged font overrides and calling
    ///     <see cref="IHudWidget.Relayout"/> on every <see cref="IHudWidget"/>.
    /// </summary>
    public static void RefreshScaledFonts(Node root)
    {
        if (root is Control control && control.HasMeta(FontSizeMetaKey))
        {
            var baseSize = (int)control.GetMeta(FontSizeMetaKey);
            var property = control.GetMeta(FontSizePropertyMetaKey, "font_size").AsString();
            control.AddThemeFontSizeOverride(property, ScaledFontSize(baseSize));
        }
        if (root is IHudWidget widget) widget.Relayout();
        foreach (var child in root.GetChildren()) RefreshScaledFonts(child);
    }

    // ── Panel styles ────────────────────────────────────────────────────
    /// <summary>Panel weight tier.</summary>
    public enum PanelTier
    {
        /// <summary>Heavy iron panel.</summary>
        Heavy,
        /// <summary>Light iron panel.</summary>
        Light,
        /// <summary>Slot frame.</summary>
        Slot,
    }

    /// <summary>Build a stylebox for the given tier.</summary>
    public static StyleBoxFlat MakePanelStyle(PanelTier tier = PanelTier.Heavy)
    {
        return tier switch
        {
            PanelTier.Light => new StyleBoxFlat
            {
                BgColor = IronLight, BorderColor = BronzeDim,
                BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
                CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
                CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
                ContentMarginLeft = PadMd, ContentMarginRight = PadMd,
                ContentMarginTop = PadSm, ContentMarginBottom = PadSm,
                ShadowColor = new Color(0, 0, 0, 0.55f), ShadowSize = 4, ShadowOffset = new Vector2(0, 2),
            },
            PanelTier.Slot => new StyleBoxFlat
            {
                BgColor = new Color(0.06f, 0.05f, 0.04f, 0.95f), BorderColor = Bronze,
                BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
                CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
                CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
                ContentMarginLeft = PadXs, ContentMarginRight = PadXs,
                ContentMarginTop = PadXs, ContentMarginBottom = PadXs,
            },
            _ => new StyleBoxFlat
            {
                BgColor = IronHeavy, BorderColor = Bronze,
                BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
                CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
                CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
                ContentMarginLeft = PadMd, ContentMarginRight = PadMd,
                ContentMarginTop = PadSm, ContentMarginBottom = PadSm,
                ShadowColor = new Color(0, 0, 0, 0.7f), ShadowSize = 6, ShadowOffset = new Vector2(0, 2),
            },
        };
    }

    /// <summary>Wrap <paramref name="content"/> in a styled panel.</summary>
    public static PanelContainer MakePanel(Control content, PanelTier tier = PanelTier.Heavy)
    {
        PanelContainer panel = new()
        {
            Name = "Panel",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride("panel", MakePanelStyle(tier));
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        panel.AddChild(content);
        return panel;
    }

    // ── Label & button styling ─────────────────────────────────────────
    /// <summary>Apply parchment text + scaled font to a label and force MouseFilter.Ignore.</summary>
    public static void StyleLabel(Label label, int baseSize = FontSizeBody)
    {
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AddThemeColorOverride("font_color", Parchment);
        label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.85f));
        label.AddThemeConstantOverride("outline_size", 2);
        ApplyScaledFontSize(label, "font_size", baseSize);
    }

    /// <summary>Apply gold text + scaled font to a header label.</summary>
    public static void StyleHeader(Label label, int baseSize = FontSizeHeader)
    {
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AddThemeColorOverride("font_color", Gold);
        label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));
        label.AddThemeConstantOverride("outline_size", 3);
        ApplyScaledFontSize(label, "font_size", baseSize);
    }

    /// <summary>Default button styling.</summary>
    public static void StyleButton(Button button, int baseSize = FontSizeBody)
    {
        button.AddThemeColorOverride("font_color", Parchment);
        button.AddThemeColorOverride("font_hover_color", GoldHover);
        button.AddThemeColorOverride("font_pressed_color", Gold);
        button.AddThemeColorOverride("font_disabled_color", DisabledText);
        button.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.85f));
        button.AddThemeConstantOverride("outline_size", 2);
        ApplyScaledFontSize(button, "font_size", baseSize);

        button.AddThemeStyleboxOverride("normal", MakeButtonStylebox(IronHeavy, Bronze));
        button.AddThemeStyleboxOverride("hover", MakeButtonStylebox(new Color(0.18f, 0.14f, 0.10f, 0.95f), Gold, 3));
        button.AddThemeStyleboxOverride("pressed", MakeButtonStylebox(PressedFill, Gold, 3));
        button.AddThemeStyleboxOverride("disabled", MakeButtonStylebox(DisabledFill, BronzeDim));
        button.AddThemeStyleboxOverride("focus", MakeFocusStylebox());
    }

    /// <summary>Skill-slot styling.</summary>
    public static void StyleSlotButton(Button button, int baseSize = FontSizeSmall)
    {
        button.AddThemeColorOverride("font_color", Parchment);
        button.AddThemeColorOverride("font_hover_color", GoldHover);
        button.AddThemeColorOverride("font_pressed_color", Gold);
        button.AddThemeColorOverride("font_disabled_color", DisabledText);
        button.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));
        button.AddThemeConstantOverride("outline_size", 2);
        ApplyScaledFontSize(button, "font_size", baseSize);

        button.AddThemeStyleboxOverride("normal", MakeSlotStylebox(IronHeavy, Bronze));
        button.AddThemeStyleboxOverride("hover", MakeSlotStylebox(new Color(0.18f, 0.14f, 0.10f, 0.96f), Gold, 3));
        button.AddThemeStyleboxOverride("pressed", MakeSlotStylebox(PressedFill, Gold, 3));
        button.AddThemeStyleboxOverride("disabled", MakeSlotStylebox(DisabledFill, BronzeDim));
        button.AddThemeStyleboxOverride("focus", MakeFocusStylebox());
    }

    private static StyleBoxFlat MakeButtonStylebox(Color bg, Color border, int borderW = 2)
        => new() {
            BgColor = bg, BorderColor = border,
            BorderWidthLeft = borderW, BorderWidthRight = borderW,
            BorderWidthTop = borderW, BorderWidthBottom = borderW,
            CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5,
            CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5,
            ContentMarginLeft = PadSm, ContentMarginRight = PadSm,
            ContentMarginTop = PadXs, ContentMarginBottom = PadXs,
        };

    private static StyleBoxFlat MakeSlotStylebox(Color bg, Color border, int borderW = 2)
        => new() {
            BgColor = bg, BorderColor = border,
            BorderWidthLeft = borderW, BorderWidthRight = borderW,
            BorderWidthTop = borderW, BorderWidthBottom = borderW,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
            ContentMarginLeft = PadXs, ContentMarginRight = PadXs,
            ContentMarginTop = PadXs, ContentMarginBottom = PadXs,
        };

    private static StyleBoxFlat MakeFocusStylebox()
        => new() {
            BgColor = new Color(0, 0, 0, 0), BorderColor = GoldHover,
            BorderWidthLeft = 2, BorderWidthRight = 2,
            BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5,
            CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5,
        };

    // ── Bars ────────────────────────────────────────────────────────────
    /// <summary>Apply styled fill + dark inset background to a progress bar.</summary>
    public static void ApplyBarStyle(ProgressBar bar, Color fillColor)
    {
        StyleBoxFlat bg = new()
        {
            BgColor = new Color(0.10f, 0.09f, 0.10f, 0.85f),
            BorderColor = new Color(0, 0, 0, 0.5f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
        };
        StyleBoxFlat fill = new()
        {
            BgColor = fillColor,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
        };
        bar.AddThemeStyleboxOverride("background", bg);
        bar.AddThemeStyleboxOverride("fill", fill);
    }

    // ── Icon loader ─────────────────────────────────────────────────────
    private const string IconRoot = "res://assets/ui/hud/icons/";
    private static readonly Dictionary<string, Texture2D?> _iconCache = new();

    /// <summary>Load a HUD icon by short name. Null when not yet imported.</summary>
    public static Texture2D? LoadIcon(string name)
    {
        if (_iconCache.TryGetValue(name, out var cached)) return cached;
        string path = $"{IconRoot}icon_{name}.svg";
        Texture2D? tex = ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
        _iconCache[name] = tex;
        return tex;
    }

    /// <summary>
    ///     Set <paramref name="button"/>'s icon by short name AND cap its rendered width.
    ///     Without the cap a 64x64 SVG fills the entire button and crowds out the text.
    /// </summary>
    /// <param name="button">Button to receive the icon.</param>
    /// <param name="iconName">Short icon name (no "icon_" prefix, no extension).</param>
    /// <param name="maxWidthDesignPx">Design-time max icon width — multiplied by UI scale.</param>
    public static void SetButtonIcon(Button button, string iconName, int maxWidthDesignPx = ActionIconSize)
    {
        Texture2D? tex = LoadIcon(iconName);
        if (tex is not null)
        {
            button.Icon = tex;
            button.ExpandIcon = false;
        }
        // Apply the cap unconditionally — it kicks in as soon as the texture resolves.
        button.AddThemeConstantOverride("icon_max_width", ScaledPx(maxWidthDesignPx));
    }
}

/// <summary>HUD widgets implement this so anchor offsets can be recomputed on UI scale change.</summary>
public interface IHudWidget
{
    /// <summary>Recompute anchored offsets / min-size against current <see cref="HudStyle.UiScale"/>.</summary>
    void Relayout();
}

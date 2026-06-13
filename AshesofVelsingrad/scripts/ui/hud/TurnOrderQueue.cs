using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Top-centre strip showing the next units to act, rendered as portrait chips with a
///     faction-coloured frame.
/// </summary>
/// <remarks>
///     <para>
///         Cyan border = <see cref="Faction.Player" />, green = <see cref="Faction.Ally" />,
///         red = <see cref="Faction.Enemy" />. The first chip is highlighted with a thicker
///         gold halo as the currently acting unit. When a unit's <see cref="IUnitSystem.EntityProfile" />
///         has a portrait, it is shown inside the chip; otherwise the default silhouette icon
///         fills the space.
///     </para>
///     <para>
///         <see cref="UpdateOrder" /> is called by <c>GameManager</c> after every turn
///         transition with the upcoming units (typically from <c>TurnManager.GetUpcomingUnits</c>).
///     </para>
/// </remarks>
public sealed partial class TurnOrderQueue : Control, IHudWidget
{
    private HBoxContainer? _row;
    private Label? _header;
    private bool _built;

    /// <inheritdoc />
    public override void _Ready()
    {
        EnsureBuilt();
    }

    /// <summary>Idempotent build — safe to call before <c>_Ready</c> fires.</summary>
    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;
        BuildLayout();
    }

    /// <inheritdoc />
    public void Relayout() => ApplyAnchorOffsets();

    private void ApplyAnchorOffsets()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.CenterTop);
        int halfW = HudStyle.ScaledPx(HudStyle.TurnQueueWidth) / 2;
        int height = HudStyle.ScaledPx(HudStyle.TurnQueueHeight);
        OffsetLeft = -halfW;
        OffsetRight = halfW;
        OffsetTop = HudStyle.PadLg;
        OffsetBottom = HudStyle.PadLg + height;
        CustomMinimumSize = new Vector2(2 * halfW, height);
    }

    private void BuildLayout()
    {
        ApplyAnchorOffsets();
        MouseFilter = MouseFilterEnum.Ignore;

        Control panelContent = new() { MouseFilter = MouseFilterEnum.Ignore };
        panelContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(panelContent, HudStyle.PanelTier.Heavy));

        _row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _row.AddThemeConstantOverride("separation", HudStyle.PadSm);
        _row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panelContent.AddChild(_row);

        _header = new Label
        {
            Text = "TURN  ORDER",
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        HudStyle.StyleHeader(_header, HudStyle.FontSizeSub);
        _header.AddThemeColorOverride("font_color", HudStyle.Gold);
        _row.AddChild(_header);
    }

    /// <summary>Replace the current chip strip with the given upcoming units.</summary>
    public void UpdateOrder(IReadOnlyList<IUnitSystem> upcomingUnits)
    {
        if (_row is null) return;
        for (int i = _row.GetChildCount() - 1; i > 0; i--)
            _row.GetChild(i).QueueFree();

        for (int i = 0; i < upcomingUnits.Count; i++)
            _row.AddChild(BuildPortraitChip(upcomingUnits[i], isActive: i == 0));
    }

    private static Control BuildPortraitChip(IUnitSystem unit, bool isActive)
    {
        int chipSize = HudStyle.ScaledPx(HudStyle.TurnChipSize);
        Color borderColor = ColorFor(unit.Faction);
        if (isActive) borderColor = HudStyle.GoldHover;

        VBoxContainer wrapper = new()
        {
            CustomMinimumSize = new Vector2(
                chipSize + HudStyle.PadSm,
                chipSize + HudStyle.ScaledPx(HudStyle.FontSizeTiny) + HudStyle.PadSm),
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        wrapper.AddThemeConstantOverride("separation", 1);

        Panel border = new()
        {
            CustomMinimumSize = new Vector2(chipSize, chipSize),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        StyleBoxFlat sb = new()
        {
            BgColor = new Color(0, 0, 0, 0.55f),
            BorderColor = borderColor,
            BorderWidthLeft = isActive ? 4 : 2,
            BorderWidthRight = isActive ? 4 : 2,
            BorderWidthTop = isActive ? 4 : 2,
            BorderWidthBottom = isActive ? 4 : 2,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            ShadowColor = isActive ? new Color(1f, 0.7f, 0.3f, 0.45f) : new Color(0, 0, 0, 0),
            ShadowSize = isActive ? 6 : 0,
        };
        border.AddThemeStyleboxOverride("panel", sb);
        wrapper.AddChild(border);

        Texture2D? portrait = LoadPortrait(unit.EntityProfile?.PortraitPath)
                              ?? HudStyle.LoadIcon("portrait_default");
        if (portrait is not null)
        {
            TextureRect tex = new()
            {
                Texture = portrait,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            tex.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            int inset = isActive ? 5 : 3;
            tex.OffsetLeft = inset; tex.OffsetTop = inset;
            tex.OffsetRight = -inset; tex.OffsetBottom = -inset;
            border.AddChild(tex);
        }

        string display = unit.EntityProfile?.DisplayName is { Length: > 0 } n ? n : unit.UnitName;
        // Prefix a faction initial so faction is conveyed by a text channel, not colour alone
        // (WCAG 1.4.1). The letter leads the label, so it survives ClipText on long names.
        Label nameLabel = new()
        {
            Text = $"{FactionInitial(unit.Faction)} {display}",
            HorizontalAlignment = HorizontalAlignment.Center,
            ClipText = true,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        HudStyle.StyleLabel(nameLabel, HudStyle.FontSizeTiny);
        nameLabel.AddThemeColorOverride("font_color",
            isActive ? HudStyle.GoldHover : HudStyle.Parchment);
        wrapper.AddChild(nameLabel);

        wrapper.TooltipText = $"{display} ({unit.Faction}, Speed {unit.BaseSpeed:F0})";
        return wrapper;
    }

    private static Texture2D? LoadPortrait(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (!ResourceLoader.Exists(path)) return null;
        return ResourceLoader.Load<Texture2D>(path);
    }

    private static Color ColorFor(Faction f) => f switch
    {
        Faction.Player => HudStyle.PlayerColor,
        Faction.Ally => HudStyle.AllyColor,
        Faction.Enemy => HudStyle.EnemyColor,
        _ => Colors.Gray,
    };

    /// <summary>Single-letter faction tag — a non-colour cue for faction identity.</summary>
    private static string FactionInitial(Faction f) => f switch
    {
        Faction.Player => "P",
        Faction.Ally => "A",
        Faction.Enemy => "E",
        _ => "?",
    };
}

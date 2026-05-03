using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.systems.battle;
using Godot;

namespace AshesOfVelsingrad.ui.hud;

/// <summary>
///     Horizontal turn-order strip showing the next units to act.
/// </summary>
/// <remarks>
///     Tight 600×52 strip at the top centre. Each upcoming unit is rendered as a coloured
///     pill with its name, colour-coded by faction (cyan / green / red).
/// </remarks>
public sealed partial class TurnOrderQueue : Control
{
    private HBoxContainer? _row;

    /// <inheritdoc />
    public override void _Ready()
    {
        BuildLayout();

        _ = HudBusHelper.WhenReadyAsync(this, bus =>
        {
            bus.Subscribe<BattleEvents.TurnOrderChanged>(OnOrderChanged);
        });
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        BattleEventBus? bus = BattleEventBus.Instance;
        if (bus is null) return;
        bus.Unsubscribe<BattleEvents.TurnOrderChanged>(OnOrderChanged);
    }

    private void BuildLayout()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.CenterTop);
        OffsetLeft = -360;
        OffsetRight = 360;
        OffsetTop = 12;
        OffsetBottom = 90;
        MouseFilter = MouseFilterEnum.Ignore;

        Control panelContent = new() { MouseFilter = MouseFilterEnum.Ignore };
        panelContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(panelContent));

        _row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _row.AddThemeConstantOverride("separation", 4);
        _row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panelContent.AddChild(_row);

        Label header = new() { Text = "Next:" };
        HudStyle.StyleLabel(header);
        _row.AddChild(header);
    }

    private void OnOrderChanged(BattleEvents.TurnOrderChanged e)
    {
        if (_row is null) return;
        // Clear children except the header.
        for (int i = _row.GetChildCount() - 1; i > 0; i--)
            _row.GetChild(i).QueueFree();

        for (int i = 0; i < e.UpcomingUnits.Count; i++)
        {
            UnitSystem unit = e.UpcomingUnits[i];
            _row.AddChild(BuildPortraitChip(unit, isActive: i == 0));
        }
    }

    /// <summary>
    ///     Build a single portrait chip: a square panel with a faction-coloured border,
    ///     the unit's portrait inside, and a name label below.
    /// </summary>
    /// <param name="unit">The unit to render.</param>
    /// <param name="isActive">If true, the chip is highlighted as the currently-acting unit.</param>
    /// <returns>The composed chip control.</returns>
    private static Control BuildPortraitChip(UnitSystem unit, bool isActive)
    {
        const int size = 40;
        Color borderColor = ColorFor(unit.Faction);
        if (isActive) borderColor = borderColor.Lightened(0.25f);

        VBoxContainer wrapper = new()
        {
            CustomMinimumSize = new Vector2(size + 4, size + 18),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        wrapper.AddThemeConstantOverride("separation", 1);

        Panel border = new()
        {
            CustomMinimumSize = new Vector2(size, size),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        StyleBoxFlat sb = new()
        {
            BgColor = new Color(0, 0, 0, 0.4f),
            BorderColor = borderColor,
            BorderWidthLeft = isActive ? 3 : 2,
            BorderWidthRight = isActive ? 3 : 2,
            BorderWidthTop = isActive ? 3 : 2,
            BorderWidthBottom = isActive ? 3 : 2,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
        };
        border.AddThemeStyleboxOverride("panel", sb);
        wrapper.AddChild(border);

        // Portrait inside the border (or a colour fallback when no portrait is set).
        Texture2D? portrait = unit.EntityProfile?.Portrait;
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
            tex.OffsetLeft = 3; tex.OffsetTop = 3; tex.OffsetRight = -3; tex.OffsetBottom = -3;
            border.AddChild(tex);
        }
        else
        {
            ColorRect fallback = new()
            {
                Color = ColorFor(unit.Faction) with { A = 0.6f },
                MouseFilter = MouseFilterEnum.Ignore,
            };
            fallback.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            fallback.OffsetLeft = 3; fallback.OffsetTop = 3; fallback.OffsetRight = -3; fallback.OffsetBottom = -3;
            border.AddChild(fallback);
        }

        // Name label below the portrait.
        string display = unit.EntityProfile?.DisplayName is { Length: > 0 } n ? n : unit.UnitName;
        Label nameLabel = new()
        {
            Text = display,
            HorizontalAlignment = HorizontalAlignment.Center,
            ClipText = true,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        nameLabel.AddThemeColorOverride("font_color", HudStyle.TextColor);
        nameLabel.AddThemeFontSizeOverride("font_size", 10);
        wrapper.AddChild(nameLabel);

        wrapper.TooltipText = $"{display} ({unit.Faction}, Speed {unit.BaseSpeed:F0})";
        return wrapper;
    }

    private static Color ColorFor(Faction f) => f switch
    {
        Faction.Player => HudStyle.PlayerColor,
        Faction.Ally => HudStyle.AllyColor,
        Faction.Enemy => HudStyle.EnemyColor,
        _ => Colors.Gray,
    };
}

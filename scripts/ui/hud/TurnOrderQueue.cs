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
        OffsetLeft = -300;
        OffsetRight = 300;
        OffsetTop = 12;
        OffsetBottom = 60;
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
            Panel chip = new()
            {
                CustomMinimumSize = new Vector2(64, 28),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            StyleBoxFlat sb = new()
            {
                BgColor = ColorFor(unit.Faction),
                CornerRadiusBottomLeft = 6,
                CornerRadiusBottomRight = 6,
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
            };
            chip.AddThemeStyleboxOverride("panel", sb);

            Label l = new()
            {
                Text = unit.UnitName,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            l.AddThemeColorOverride("font_color", new Color(0, 0, 0, 1));
            l.AddThemeFontSizeOverride("font_size", 12);
            l.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            chip.AddChild(l);

            chip.TooltipText = $"{unit.UnitName} ({unit.Faction})";
            _row.AddChild(chip);
        }
    }

    private static Color ColorFor(Faction f) => f switch
    {
        Faction.Player => HudStyle.PlayerColor,
        Faction.Ally => HudStyle.AllyColor,
        Faction.Enemy => HudStyle.EnemyColor,
        _ => Colors.Gray,
    };
}

using System.Collections.Generic;
using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.systems.battle;
using Godot;

namespace AshesOfVelsingrad.ui.hud;

/// <summary>
///     Vertical stack of <see cref="UnitHealthBar" />s, one per alive enemy.
/// </summary>
/// <remarks>
///     Anchored to the top-right with a tight bounded width so map clicks pass through the
///     empty viewport beside the panel.
/// </remarks>
public sealed partial class EnemyRoster : Control
{
    private VBoxContainer? _box;
    private readonly List<UnitHealthBar> _bars = [];

    /// <inheritdoc />
    public override void _Ready()
    {
        BuildLayout();

        _ = HudBusHelper.WhenReadyAsync(this, bus =>
        {
            bus.Subscribe<BattleEvents.BattleStarted>(OnBattleStarted);
            bus.Subscribe<BattleEvents.UnitDied>(OnUnitDied);
            bus.Subscribe<BattleEvents.HpChanged>(_ => RefreshAll());
        });
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        BattleEventBus? bus = BattleEventBus.Instance;
        if (bus is null) return;
        bus.Unsubscribe<BattleEvents.BattleStarted>(OnBattleStarted);
        bus.Unsubscribe<BattleEvents.UnitDied>(OnUnitDied);
    }

    private void BuildLayout()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        OffsetLeft = -260;
        OffsetTop = 12;
        OffsetRight = -12;
        OffsetBottom = 12 + 320;
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer outer = new() { MouseFilter = MouseFilterEnum.Ignore };
        outer.AddThemeConstantOverride("separation", 4);
        outer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(outer));

        Label header = new() { Text = "Enemies" };
        HudStyle.StyleLabel(header);
        outer.AddChild(header);

        _box = new VBoxContainer { Name = "Bars", MouseFilter = MouseFilterEnum.Ignore };
        _box.AddThemeConstantOverride("separation", 2);
        outer.AddChild(_box);
    }

    private void OnBattleStarted(BattleEvents.BattleStarted e)
    {
        if (_box is null) return;
        foreach (UnitHealthBar b in _bars) b.QueueFree();
        _bars.Clear();

        foreach (UnitSystem enemy in e.Enemies)
        {
            UnitHealthBar bar = new() { Name = $"Bar_{enemy.Name}" };
            _box.AddChild(bar);
            bar.Bind(enemy);
            _bars.Add(bar);
        }
    }

    private void OnUnitDied(BattleEvents.UnitDied _)
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        foreach (UnitHealthBar bar in _bars) bar.Refresh();
    }
}

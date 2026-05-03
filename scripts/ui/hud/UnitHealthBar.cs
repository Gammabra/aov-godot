using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.systems.battle;
using Godot;

namespace AshesOfVelsingrad.ui.hud;

/// <summary>
///     Compact HP/MP indicator for a single unit (used inside the enemy roster
///     and as a player-status header).
/// </summary>
public sealed partial class UnitHealthBar : Control
{
    private UnitSystem? _bound;
    private Label? _name;
    private ProgressBar? _hp;
    private ProgressBar? _mp;

    /// <inheritdoc />
    public override void _Ready()
    {
        BuildLayout();

        _ = HudBusHelper.WhenReadyAsync(this, bus =>
        {
            bus.Subscribe<BattleEvents.HpChanged>(OnHp);
            bus.Subscribe<BattleEvents.ManaChanged>(OnMana);
        });
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        BattleEventBus? bus = BattleEventBus.Instance;
        if (bus is null) return;
        bus.Unsubscribe<BattleEvents.HpChanged>(OnHp);
        bus.Unsubscribe<BattleEvents.ManaChanged>(OnMana);
    }

    private void BuildLayout()
    {
        CustomMinimumSize = new Vector2(220, 64);
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer box = new() { MouseFilter = MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", 3);
        box.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(box));

        _name = new Label { Text = "—" };
        HudStyle.StyleLabel(_name);
        box.AddChild(_name);

        _hp = new ProgressBar { MinValue = 0, MaxValue = 1, Value = 1, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 12) };
        _hp.AddThemeColorOverride("fill_color", HudStyle.HpFill);
        box.AddChild(_hp);

        _mp = new ProgressBar { MinValue = 0, MaxValue = 1, Value = 1, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 8) };
        _mp.AddThemeColorOverride("fill_color", HudStyle.ManaFill);
        box.AddChild(_mp);
    }

    /// <summary>Bind the bar to a specific unit and refresh.</summary>
    /// <param name="unit">Unit to track.</param>
    public void Bind(UnitSystem unit)
    {
        _bound = unit;
        Refresh();
    }

    /// <summary>Force a redraw from the bound unit.</summary>
    public void Refresh()
    {
        if (_bound is null || _name is null || _hp is null || _mp is null) return;
        _name.Text = $"{_bound.UnitName}  ({_bound.Hp:F0}/{_bound.MaxHp:F0})";
        _hp.MaxValue = _bound.MaxHp;
        _hp.Value = _bound.Hp;
        _mp.MaxValue = _bound.MaxMana > 0 ? _bound.MaxMana : 1;
        _mp.Value = _bound.ManaPoint;
    }

    private void OnHp(BattleEvents.HpChanged e)
    {
        if (e.Unit == _bound) Refresh();
    }

    private void OnMana(BattleEvents.ManaChanged e)
    {
        if (e.Unit == _bound) Refresh();
    }
}

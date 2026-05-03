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
        ApplyBarStyle(_hp, HudStyle.HpFill);
        box.AddChild(_hp);

        _mp = new ProgressBar { MinValue = 0, MaxValue = 1, Value = 1, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 8) };
        ApplyBarStyle(_mp, HudStyle.ManaFill);
        box.AddChild(_mp);
    }

    /// <summary>
    ///     Apply background + fill stylebox overrides to a <see cref="ProgressBar" />.
    /// </summary>
    /// <param name="bar">The bar to style.</param>
    /// <param name="fillColor">Colour for the filled portion.</param>
    private static void ApplyBarStyle(ProgressBar bar, Color fillColor)
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

using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Compact HP/MP indicator bound to a single <see cref="IUnitSystem" />.
/// </summary>
/// <remarks>
///     Bind once with <see cref="Bind" /> and call <see cref="Refresh" /> from the parent
///     widget whenever an event source (a turn change, a damage tick, etc.) might have
///     altered the unit's state. Designed to be re-used by <see cref="EnemyRoster" /> and
///     other widgets that aggregate per-unit summaries.
/// </remarks>
public sealed partial class UnitHealthBar : Control
{
    private IUnitSystem? _bound;
    private Label? _name;
    private ProgressBar? _hp;
    private ProgressBar? _mp;

    /// <inheritdoc />
    public override void _Ready()
    {
        BuildLayout();
        // Auto-refresh each frame so HP/MP bars reflect damage taken without callers
        // having to invoke Refresh() manually after every event.
        SetProcess(true);
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (_bound is not null) Refresh();
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

        _hp = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            Value = 1,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 12),
        };
        HudStyle.ApplyBarStyle(_hp, HudStyle.HpFill);
        box.AddChild(_hp);

        _mp = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            Value = 1,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 8),
        };
        HudStyle.ApplyBarStyle(_mp, HudStyle.ManaFill);
        box.AddChild(_mp);
    }

    /// <summary>Bind the bar to a unit and immediately refresh.</summary>
    /// <param name="unit">Unit to track. Pass null to clear.</param>
    public void Bind(IUnitSystem? unit)
    {
        _bound = unit;
        Refresh();
    }

    /// <summary>Re-read the bound unit's HP/MP and update the visuals.</summary>
    public void Refresh()
    {
        if (_bound is null || _name is null || _hp is null || _mp is null)
        {
            if (_name is not null) _name.Text = "—";
            return;
        }

        _name.Text = $"{_bound.UnitName}  ({_bound.Hp:F0}/{_bound.MaxHp:F0})";
        _hp.MaxValue = _bound.MaxHp <= 0 ? 1 : _bound.MaxHp;
        _hp.Value = _bound.Hp;
        _mp.MaxValue = _bound.ManaMax > 0 ? _bound.ManaMax : 1;
        _mp.Value = _bound.Mana;
    }
}

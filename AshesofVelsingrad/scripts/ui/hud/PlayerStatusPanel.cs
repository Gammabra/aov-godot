using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Bottom-left panel showing the active controlled unit's name, HP and MP.
/// </summary>
/// <remarks>
///     Bound by <c>GameManager</c> on every player turn start (and after any HP/MP-changing
///     action). Light by design — the new branch doesn't yet have <c>EntityProfile</c>
///     (portrait/level/class metadata), so the panel just shows the name and the bars.
///     Add the portrait/level rendering back once a profile resource lands.
/// </remarks>
public sealed partial class PlayerStatusPanel : Control
{
    private IUnitSystem? _bound;
    private Label? _name;
    private ProgressBar? _hp;
    private Label? _hpLabel;
    private ProgressBar? _mp;
    private Label? _mpLabel;
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

    private void BuildLayout()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft);
        OffsetLeft = 12;
        OffsetTop = -160;
        OffsetRight = 320;
        OffsetBottom = -12;
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer outer = new() { MouseFilter = MouseFilterEnum.Ignore };
        outer.AddThemeConstantOverride("separation", 4);
        outer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(outer));

        _name = new Label { Text = "—" };
        _name.AddThemeFontSizeOverride("font_size", 16);
        HudStyle.StyleLabel(_name);
        outer.AddChild(_name);

        _hpLabel = new Label { Text = "HP —" };
        HudStyle.StyleLabel(_hpLabel);
        outer.AddChild(_hpLabel);

        _hp = new ProgressBar
        {
            MinValue = 0, MaxValue = 1, Value = 1, ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 12),
        };
        HudStyle.ApplyBarStyle(_hp, HudStyle.HpFill);
        outer.AddChild(_hp);

        _mpLabel = new Label { Text = "MP —" };
        HudStyle.StyleLabel(_mpLabel);
        outer.AddChild(_mpLabel);

        _mp = new ProgressBar
        {
            MinValue = 0, MaxValue = 1, Value = 1, ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 8),
        };
        HudStyle.ApplyBarStyle(_mp, HudStyle.ManaFill);
        outer.AddChild(_mp);
    }

    /// <summary>Bind the panel to a unit. Pass null to reset.</summary>
    /// <param name="unit">Unit to display, or null.</param>
    public void Bind(IUnitSystem? unit)
    {
        _bound = unit;
        Refresh();
    }

    /// <summary>Re-read the bound unit's stats and repaint.</summary>
    public void Refresh()
    {
        if (_bound is null)
        {
            if (_name is not null) _name.Text = "—";
            if (_hpLabel is not null) _hpLabel.Text = "HP —";
            if (_mpLabel is not null) _mpLabel.Text = "MP —";
            if (_hp is not null) { _hp.MaxValue = 1; _hp.Value = 0; }
            if (_mp is not null) { _mp.MaxValue = 1; _mp.Value = 0; }
            return;
        }

        if (_name is not null) _name.Text = _bound.UnitName;
        if (_hpLabel is not null) _hpLabel.Text = $"HP {_bound.Hp:F0}/{_bound.MaxHp:F0}";
        if (_mpLabel is not null) _mpLabel.Text = $"MP {_bound.Mana:F0}/{_bound.ManaMax:F0}";
        if (_hp is not null) { _hp.MaxValue = _bound.MaxHp <= 0 ? 1 : _bound.MaxHp; _hp.Value = _bound.Hp; }
        if (_mp is not null) { _mp.MaxValue = _bound.ManaMax <= 0 ? 1 : _bound.ManaMax; _mp.Value = _bound.Mana; }
    }
}

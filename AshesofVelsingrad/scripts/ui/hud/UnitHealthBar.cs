using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Compact portrait + HP/MP indicator bound to a single <see cref="IUnitSystem" />.
/// </summary>
/// <remarks>
///     <para>
///         A horizontal row: portrait inset on the left, name + HP/MP bars stacked on the right.
///         Designed to be reused by <see cref="EnemyRoster" /> and any other widget that
///         aggregates per-unit summaries.
///     </para>
///     <para>
///         Bind once with <see cref="Bind" /> and call <see cref="Refresh" /> when an event
///         source might have altered the unit's state.
///     </para>
/// </remarks>
public sealed partial class UnitHealthBar : Control, IHudWidget
{
    private IUnitSystem? _bound;
    private TextureRect? _portrait;
    private PanelContainer? _frame;
    private Label? _name;
    private ProgressBar? _hp;
    private Label? _hpLabel;
    private ProgressBar? _mp;

    /// <inheritdoc />
    public override void _Ready()
    {
        BuildLayout();
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (_bound is not null) _bound.OnStatsChanged -= Refresh;
        base._ExitTree();
    }

    /// <inheritdoc />
    public void Relayout() => ApplyMetrics();

    /// <summary>Recompute UI-scale-dependent sizes for this bar and its portrait.</summary>
    private void ApplyMetrics()
    {
        int portraitSize = HudStyle.ScaledPx(HudStyle.RosterPortrait);
        int inner = HudStyle.ScaledPx(HudStyle.RosterPortrait - HudStyle.PadXs * 2);
        CustomMinimumSize = new Vector2(0, portraitSize + HudStyle.PadXs);
        if (_frame is not null) _frame.CustomMinimumSize = new Vector2(portraitSize, portraitSize);
        if (_portrait is not null) _portrait.CustomMinimumSize = new Vector2(inner, inner);
    }

    private void BuildLayout()
    {
        int portraitSize = HudStyle.ScaledPx(HudStyle.RosterPortrait);
        CustomMinimumSize = new Vector2(0, portraitSize + HudStyle.PadXs);
        MouseFilter = MouseFilterEnum.Ignore;

        HBoxContainer row = new() { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", HudStyle.PadSm);
        row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(row);

        _frame = new PanelContainer
        {
            CustomMinimumSize = new Vector2(portraitSize, portraitSize),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _frame.AddThemeStyleboxOverride("panel", HudStyle.MakePanelStyle(HudStyle.PanelTier.Slot));
        row.AddChild(_frame);

        _portrait = new TextureRect
        {
            Texture = HudStyle.LoadIcon("portrait_default"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(
                HudStyle.ScaledPx(HudStyle.RosterPortrait - HudStyle.PadXs * 2),
                HudStyle.ScaledPx(HudStyle.RosterPortrait - HudStyle.PadXs * 2)),
        };
        _frame.AddChild(_portrait);

        VBoxContainer stats = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        stats.AddThemeConstantOverride("separation", 2);
        row.AddChild(stats);

        HBoxContainer nameRow = new() { MouseFilter = MouseFilterEnum.Ignore };
        nameRow.AddThemeConstantOverride("separation", HudStyle.PadSm);
        stats.AddChild(nameRow);

        _name = new Label
        {
            Text = "—",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ClipText = true,
        };
        HudStyle.StyleLabel(_name, HudStyle.FontSizeBody);
        nameRow.AddChild(_name);

        _hpLabel = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        HudStyle.StyleLabel(_hpLabel, HudStyle.FontSizeSmall);
        _hpLabel.AddThemeColorOverride("font_color", HudStyle.ParchmentDim);
        nameRow.AddChild(_hpLabel);

        _hp = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            Value = 1,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 12),
        };
        HudStyle.ApplyBarStyle(_hp, HudStyle.Crimson);
        stats.AddChild(_hp);

        _mp = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            Value = 1,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 8),
        };
        HudStyle.ApplyBarStyle(_mp, HudStyle.Azure);
        stats.AddChild(_mp);
    }

    /// <summary>Bind the bar to a unit and immediately refresh.</summary>
    public void Bind(IUnitSystem? unit)
    {
        if (ReferenceEquals(_bound, unit))
        {
            Refresh();
            return;
        }
        if (_bound is not null) _bound.OnStatsChanged -= Refresh;
        _bound = unit;
        if (_bound is not null) _bound.OnStatsChanged += Refresh;
        Refresh();
    }

    /// <summary>Re-read the bound unit's HP/MP and update the visuals.</summary>
    public void Refresh()
    {
        if (_bound is null || _name is null || _hp is null || _mp is null)
        {
            if (_name is not null) _name.Text = "—";
            if (_hpLabel is not null) _hpLabel.Text = "";
            return;
        }

        string display = _bound.EntityProfile?.DisplayName is { Length: > 0 } n ? n : _bound.UnitName;
        _name.Text = display;
        if (_hpLabel is not null) _hpLabel.Text = $"{_bound.Hp:F0}/{_bound.MaxHp:F0}";
        _hp.MaxValue = _bound.MaxHp <= 0 ? 1 : _bound.MaxHp;
        _hp.Value = _bound.Hp;
        _mp.MaxValue = _bound.ManaMax > 0 ? _bound.ManaMax : 1;
        _mp.Value = _bound.Mana;

        if (_portrait is not null)
        {
            string? path = _bound.EntityProfile?.PortraitPath;
            if (!string.IsNullOrEmpty(path) && ResourceLoader.Exists(path))
                _portrait.Texture = ResourceLoader.Load<Texture2D>(path);
            else
                _portrait.Texture = HudStyle.LoadIcon("portrait_default");
        }
    }
}

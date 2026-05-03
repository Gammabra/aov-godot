using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.systems.progression;
using Godot;

namespace AshesOfVelsingrad.ui.hud;

/// <summary>
///     Bottom-left panel showing the active controlled unit's portrait, name, level,
///     HP, MP and a mini corruption gauge.
/// </summary>
/// <remarks>
///     <para>
///         Driven by <see cref="BattleEvents.TurnStarted" />: when the active unit changes
///         the panel rebinds. While an enemy is acting it shows the most recently active
///         player or ally so the player still sees their own state.
///     </para>
///     <para>
///         Reads metadata from <see cref="UnitSystem.EntityProfile" /> when available;
///         falls back to <see cref="UnitSystem.UnitName" /> and a "Lv 1" placeholder.
///     </para>
/// </remarks>
public sealed partial class PlayerStatusPanel : Control
{
    private UnitSystem? _bound;

    private TextureRect? _portrait;
    private Label? _name;
    private Label? _classLevel;
    private ProgressBar? _hp;
    private Label? _hpLabel;
    private ProgressBar? _mp;
    private Label? _mpLabel;
    private HBoxContainer? _corruptionRow;

    /// <inheritdoc />
    public override void _Ready()
    {
        BuildLayout();

        _ = HudBusHelper.WhenReadyAsync(this, bus =>
        {
            bus.Subscribe<BattleEvents.TurnStarted>(OnTurnStarted);
            bus.Subscribe<BattleEvents.HpChanged>(OnHp);
            bus.Subscribe<BattleEvents.ManaChanged>(OnMana);
            bus.Subscribe<BattleEvents.CorruptionChanged>(_ => RefreshCorruption());
        });
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        BattleEventBus? bus = BattleEventBus.Instance;
        if (bus is null) return;
        bus.Unsubscribe<BattleEvents.TurnStarted>(OnTurnStarted);
        bus.Unsubscribe<BattleEvents.HpChanged>(OnHp);
        bus.Unsubscribe<BattleEvents.ManaChanged>(OnMana);
    }

    private void BuildLayout()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft);
        OffsetLeft = 12;
        OffsetTop = -160;
        OffsetRight = 280;
        OffsetBottom = -12;
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer outer = new() { MouseFilter = MouseFilterEnum.Ignore };
        outer.AddThemeConstantOverride("separation", 4);
        outer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(outer));

        // Header row: portrait + name + class/level.
        HBoxContainer header = new() { MouseFilter = MouseFilterEnum.Ignore };
        header.AddThemeConstantOverride("separation", 8);
        outer.AddChild(header);

        _portrait = new TextureRect
        {
            CustomMinimumSize = new Vector2(56, 56),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        header.AddChild(_portrait);

        VBoxContainer headerText = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        header.AddChild(headerText);

        _name = new Label { Text = "—" };
        _name.AddThemeFontSizeOverride("font_size", 16);
        HudStyle.StyleLabel(_name);
        headerText.AddChild(_name);

        _classLevel = new Label { Text = "Lv 1" };
        HudStyle.StyleLabel(_classLevel);
        _classLevel.AddThemeColorOverride("font_color", HudStyle.DimText);
        headerText.AddChild(_classLevel);

        // HP bar + numeric.
        _hpLabel = new Label { Text = "HP 0/0" };
        HudStyle.StyleLabel(_hpLabel);
        outer.AddChild(_hpLabel);
        _hp = new ProgressBar { MinValue = 0, MaxValue = 1, Value = 1, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 12) };
        ApplyBarStyle(_hp, HudStyle.HpFill);
        outer.AddChild(_hp);

        // MP bar + numeric.
        _mpLabel = new Label { Text = "MP 0/0" };
        HudStyle.StyleLabel(_mpLabel);
        outer.AddChild(_mpLabel);
        _mp = new ProgressBar { MinValue = 0, MaxValue = 1, Value = 1, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 8) };
        ApplyBarStyle(_mp, HudStyle.ManaFill);
        outer.AddChild(_mp);

        // Mini corruption strip.
        Label corrLabel = new() { Text = "Corruption" };
        HudStyle.StyleLabel(corrLabel);
        corrLabel.AddThemeColorOverride("font_color", HudStyle.DimText);
        outer.AddChild(corrLabel);

        _corruptionRow = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _corruptionRow.AddThemeConstantOverride("separation", 3);
        outer.AddChild(_corruptionRow);
        for (int i = 0; i < CharacterProfile.MaxCorruptionLevel; i++)
        {
            ColorRect seg = new()
            {
                CustomMinimumSize = new Vector2(60, 8),
                Color = new Color(0.18f, 0.18f, 0.18f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _corruptionRow.AddChild(seg);
        }
    }

    /// <summary>
    ///     Bind the panel to a unit. Pass null to clear.
    /// </summary>
    /// <param name="unit">Unit to display, or null to reset.</param>
    public void Bind(UnitSystem? unit)
    {
        _bound = unit;
        Refresh();
    }

    /// <summary>Force a redraw of every field from the bound unit.</summary>
    public void Refresh()
    {
        if (_bound is null)
        {
            if (_name is not null) _name.Text = "—";
            if (_classLevel is not null) _classLevel.Text = "";
            if (_portrait is not null) _portrait.Texture = null;
            if (_hp is not null) { _hp.Value = 0; _hp.MaxValue = 1; }
            if (_mp is not null) { _mp.Value = 0; _mp.MaxValue = 1; }
            if (_hpLabel is not null) _hpLabel.Text = "HP —";
            if (_mpLabel is not null) _mpLabel.Text = "MP —";
            return;
        }

        EntityProfile? ep = _bound.EntityProfile;
        string display = ep?.DisplayName is { Length: > 0 } n ? n : _bound.UnitName;
        int level = ep?.Level ?? _bound.Profile?.Level ?? 1;
        string klass = ep?.ClassName ?? "";

        if (_name is not null) _name.Text = display;
        if (_classLevel is not null) _classLevel.Text = string.IsNullOrEmpty(klass) ? $"Lv {level}" : $"{klass}  •  Lv {level}";
        if (_portrait is not null) _portrait.Texture = ep?.Portrait;

        if (_hp is not null) { _hp.MaxValue = _bound.MaxHp; _hp.Value = _bound.Hp; }
        if (_mp is not null) { _mp.MaxValue = _bound.MaxMana > 0 ? _bound.MaxMana : 1; _mp.Value = _bound.ManaPoint; }
        if (_hpLabel is not null) _hpLabel.Text = $"HP {_bound.Hp:F0}/{_bound.MaxHp:F0}";
        if (_mpLabel is not null) _mpLabel.Text = $"MP {_bound.ManaPoint:F0}/{_bound.MaxMana:F0}";

        RefreshCorruption();
    }

    private void RefreshCorruption()
    {
        if (_corruptionRow is null || _bound is null) return;
        int level = _bound.Profile?.CorruptionLevel ?? 0;
        for (int i = 0; i < _corruptionRow.GetChildCount(); i++)
        {
            if (_corruptionRow.GetChild(i) is not ColorRect cr) continue;
            cr.Color = i < level ? HudStyle.HpFill : new Color(0.18f, 0.18f, 0.18f);
        }
    }

    private void OnTurnStarted(BattleEvents.TurnStarted e)
    {
        // Show whoever is taking the turn; that's most useful while debugging both factions.
        // For ship-quality UI you might prefer to lock the panel to the player's controlled
        // unit and only update on PlayerTurn — easy follow-up, just guard on e.Faction.
        Bind(e.Unit);
    }

    private void OnHp(BattleEvents.HpChanged e)
    {
        if (e.Unit == _bound) Refresh();
    }

    private void OnMana(BattleEvents.ManaChanged e)
    {
        if (e.Unit == _bound) Refresh();
    }

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
}

using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Bottom-left panel showing the active controlled unit's portrait, name, HP and MP.
/// </summary>
/// <remarks>
///     <para>
///         Heavy iron frame with a portrait inset on the left, name + bars on the right. Uses
///         <see cref="HudStyle.ScaledPx" /> for every offset so the panel grows / shrinks with
///         the user's UI scale slider in lock-step with the fonts.
///     </para>
///     <para>
///         Bound by <c>GameManager</c> on every player turn start and after any HP/MP-changing
///         action. When the unit's <see cref="Data.EntityProfile" /> exposes a portrait path
///         the portrait is rendered; otherwise the default silhouette icon is shown.
///     </para>
/// </remarks>
public sealed partial class PlayerStatusPanel : Control, IHudWidget
{
    private IUnitSystem? _bound;
    private TextureRect? _portrait;
    private Label? _name;
    private Label? _classLine;
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

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (_bound is not null) _bound.OnStatsChanged -= Refresh;
        base._ExitTree();
    }

    /// <inheritdoc />
    public void Relayout() => ApplyAnchorOffsets();

    private void ApplyAnchorOffsets()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft);
        OffsetLeft = HudStyle.PadLg;
        OffsetTop = -HudStyle.ScaledPx(HudStyle.PlayerStatusHeight) - HudStyle.PadLg;
        OffsetRight = HudStyle.PadLg + HudStyle.ScaledPx(HudStyle.PlayerStatusWidth);
        OffsetBottom = -HudStyle.PadLg;
        CustomMinimumSize = new Vector2(
            HudStyle.ScaledPx(HudStyle.PlayerStatusWidth),
            HudStyle.ScaledPx(HudStyle.PlayerStatusHeight));
    }

    private void BuildLayout()
    {
        ApplyAnchorOffsets();
        MouseFilter = MouseFilterEnum.Ignore;

        Control panelContent = new() { MouseFilter = MouseFilterEnum.Ignore };
        panelContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(panelContent, HudStyle.PanelTier.Heavy));

        HBoxContainer row = new()
        {
            Name = "Row",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddThemeConstantOverride("separation", HudStyle.PadMd);
        row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panelContent.AddChild(row);

        // Portrait inset.
        PanelContainer portraitFrame = new()
        {
            Name = "PortraitFrame",
            CustomMinimumSize = new Vector2(
                HudStyle.ScaledPx(HudStyle.PlayerPortrait),
                HudStyle.ScaledPx(HudStyle.PlayerPortrait)),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        portraitFrame.AddThemeStyleboxOverride("panel",
            HudStyle.MakePanelStyle(HudStyle.PanelTier.Slot));
        row.AddChild(portraitFrame);

        _portrait = new TextureRect
        {
            Name = "Portrait",
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = HudStyle.LoadIcon("portrait_default"),
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(
                HudStyle.ScaledPx(HudStyle.PlayerPortrait - HudStyle.PadXs * 2),
                HudStyle.ScaledPx(HudStyle.PlayerPortrait - HudStyle.PadXs * 2)),
        };
        portraitFrame.AddChild(_portrait);

        // Stats column.
        VBoxContainer stats = new()
        {
            Name = "Stats",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        stats.AddThemeConstantOverride("separation", HudStyle.PadXs);
        row.AddChild(stats);

        // ClipText + ExpandFill so a long unit name clips inside the panel instead of
        // widening the stats column and pushing the HP/MP bars past the card edge.
        _name = new Label { Text = "—", ClipText = true, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        HudStyle.StyleHeader(_name);
        stats.AddChild(_name);

        _classLine = new Label { Text = "", ClipText = true, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        HudStyle.StyleLabel(_classLine, HudStyle.FontSizeSmall);
        _classLine.AddThemeColorOverride("font_color", HudStyle.ParchmentDim);
        stats.AddChild(_classLine);

        stats.AddChild(BuildIconLabel("heart", out _hpLabel, "HP —"));
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

        stats.AddChild(BuildIconLabel("droplet", out _mpLabel, "MP —"));
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

    private static HBoxContainer BuildIconLabel(string iconName, out Label labelOut, string initialText)
    {
        HBoxContainer row = new() { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", HudStyle.PadXs);

        TextureRect icon = new()
        {
            Texture = HudStyle.LoadIcon(iconName),
            CustomMinimumSize = new Vector2(
                HudStyle.ScaledPx(HudStyle.FontSizeSub),
                HudStyle.ScaledPx(HudStyle.FontSizeSub)),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddChild(icon);

        labelOut = new Label { Text = initialText, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        HudStyle.StyleLabel(labelOut, HudStyle.FontSizeSub);
        row.AddChild(labelOut);
        return row;
    }

    /// <summary>Bind the panel to a unit. Pass null to reset.</summary>
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

    /// <summary>Re-read the bound unit's stats and repaint.</summary>
    public void Refresh()
    {
        if (_bound is null)
        {
            if (_name is not null) _name.Text = "—";
            if (_classLine is not null) _classLine.Text = "";
            if (_hpLabel is not null) _hpLabel.Text = "HP —";
            if (_mpLabel is not null) _mpLabel.Text = "MP —";
            if (_hp is not null) { _hp.MaxValue = 1; _hp.Value = 0; }
            if (_mp is not null) { _mp.MaxValue = 1; _mp.Value = 0; }
            if (_portrait is not null) _portrait.Texture = HudStyle.LoadIcon("portrait_default");
            return;
        }

        if (_name is not null)
        {
            string display = _bound.EntityProfile?.DisplayName is { Length: > 0 } n
                ? n
                : _bound.UnitName;
            _name.Text = display;
        }
        if (_classLine is not null)
        {
            _classLine.Text = $"{_bound.Faction}  •  Speed {_bound.BaseSpeed:F0}";
        }
        if (_hpLabel is not null) _hpLabel.Text = $"HP {_bound.Hp:F0} / {_bound.MaxHp:F0}";
        if (_mpLabel is not null) _mpLabel.Text = $"MP {_bound.Mana:F0} / {_bound.ManaMax:F0}";
        if (_hp is not null) { _hp.MaxValue = _bound.MaxHp <= 0 ? 1 : _bound.MaxHp; _hp.Value = _bound.Hp; }
        if (_mp is not null) { _mp.MaxValue = _bound.ManaMax <= 0 ? 1 : _bound.ManaMax; _mp.Value = _bound.Mana; }

        if (_portrait is not null)
        {
            string? path = _bound.EntityProfile?.PortraitPath;
            if (!string.IsNullOrEmpty(path) && ResourceLoader.Exists(path))
            {
                _portrait.Texture = ResourceLoader.Load<Texture2D>(path);
            }
            else
            {
                _portrait.Texture = HudStyle.LoadIcon("portrait_default");
            }
        }
    }
}

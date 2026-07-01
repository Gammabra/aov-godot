using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Top-right column showing one <see cref="UnitHealthBar" /> per enemy.
/// </summary>
/// <remarks>
///     <para>
///         Heavy iron panel with a "ENEMIES" header in gold and a stack of bars below it. Each
///         bar carries a small portrait, the enemy name, and HP/MP. Panel grows downward to
///         fit the roster, capped at <see cref="HudStyle.RosterHeight" /> (scaled).
///     </para>
///     <para>
///         The owning <see cref="BattleHud" /> calls <see cref="Bind" /> once at battle start
///         with the enemy roster. The widget then refreshes itself when asked
///         (<see cref="RefreshAll" />).
///     </para>
/// </remarks>
public sealed partial class EnemyRoster : Control, IHudWidget
{
    private VBoxContainer? _box;
    private readonly List<UnitHealthBar> _bars = [];
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
    public void Relayout() => ApplyAnchorOffsets();

    private void ApplyAnchorOffsets()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        OffsetLeft = -HudStyle.ScaledPx(HudStyle.RosterWidth) - HudStyle.PadLg;
        OffsetTop = HudStyle.PadLg;
        OffsetRight = -HudStyle.PadLg;
        OffsetBottom = HudStyle.PadLg + HudStyle.ScaledPx(HudStyle.RosterHeight);
        CustomMinimumSize = new Vector2(
            HudStyle.ScaledPx(HudStyle.RosterWidth),
            HudStyle.ScaledPx(HudStyle.RosterHeight));
    }

    private void BuildLayout()
    {
        ApplyAnchorOffsets();
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer outer = new() { MouseFilter = MouseFilterEnum.Ignore };
        outer.AddThemeConstantOverride("separation", HudStyle.PadSm);
        outer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(outer, HudStyle.PanelTier.Heavy));

        Label header = new() { Text = "ENEMIES" };
        HudStyle.StyleHeader(header, HudStyle.FontSizeSub);
        outer.AddChild(header);

        ColorRect rule = new()
        {
            Color = HudStyle.Bronze,
            CustomMinimumSize = new Vector2(0, 1),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        outer.AddChild(rule);

        _box = new VBoxContainer
        {
            Name = "Bars",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _box.AddThemeConstantOverride("separation", HudStyle.PadXs);
        outer.AddChild(_box);
    }

    /// <summary>Replace the roster of tracked enemies. Called once at battle start.</summary>
    public void Bind(IReadOnlyList<IUnitSystem> enemies)
    {
        if (_box is null) return;

        foreach (UnitHealthBar bar in _bars)
            bar.QueueFree();
        _bars.Clear();

        foreach (IUnitSystem enemy in enemies)
        {
            UnitHealthBar bar = new() { Name = $"Bar_{((Node)enemy).Name}" };
            _box.AddChild(bar);
            bar.Bind(enemy);
            _bars.Add(bar);
        }
    }

    /// <summary>Force every bound bar to re-read its unit's state.</summary>
    public void RefreshAll()
    {
        foreach (UnitHealthBar bar in _bars) bar.Refresh();
    }
}

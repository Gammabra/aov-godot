using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Top-right column showing one <see cref="UnitHealthBar" /> per enemy.
/// </summary>
/// <remarks>
///     The owning <see cref="BattleHud" /> calls <see cref="Bind" /> once at battle start with
///     the enemy roster. The widget then refreshes itself when asked
///     (<see cref="RefreshAll" />) — typically from a TurnManager event hook in <c>GameManager</c>.
/// </remarks>
public sealed partial class EnemyRoster : Control
{
    private VBoxContainer? _box;
    private readonly List<UnitHealthBar> _bars = [];

    /// <inheritdoc />
    public override void _Ready()
    {
        BuildLayout();
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

    /// <summary>
    ///     Replace the roster of tracked enemies. Called once at battle start.
    /// </summary>
    /// <param name="enemies">Enemy unit list.</param>
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

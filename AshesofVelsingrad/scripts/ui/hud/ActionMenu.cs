using System;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.systems.battle;
using Godot;

namespace AshesOfVelsingrad.ui.hud;

/// <summary>
///     Bottom-centre action bar shown for the active player unit.
/// </summary>
/// <remarks>
///     <para>
///         A tight 520×56 strip anchored to the bottom centre of the viewport. Empty space
///         outside the strip is unoccupied so map clicks pass through the HUD.
///     </para>
///     <para>
///         Every button raises a C# event the rest of the HUD wires up:
///         <see cref="OnAttackPressed" />, <see cref="OnSkillPressed" />,
///         <see cref="OnItemPressed" />, <see cref="OnPassPressed" />, <see cref="OnFleePressed" />.
///         The "Pass" button additionally calls <c>UnitSystem.PassTurn</c> directly so the
///         turn loop unblocks.
///     </para>
/// </remarks>
public sealed partial class ActionMenu : Control
{
    /// <summary>Fired when the player chooses the basic attack.</summary>
    public event Action? OnAttackPressed;

    /// <summary>Fired when the player opens the skill submenu.</summary>
    public event Action? OnSkillPressed;

    /// <summary>Fired when the player opens the inventory.</summary>
    public event Action? OnItemPressed;

    /// <summary>Fired when the player passes the turn.</summary>
    public event Action? OnPassPressed;

    /// <summary>Fired when the player flees.</summary>
    public event Action? OnFleePressed;

    /// <summary>Fired when the player cancels skill targeting (Cancel button / Esc / right-click).</summary>
    public event Action? OnCancelPressed;

    private Button? _cancelButton;

    /// <inheritdoc />
    public override void _Ready()
    {
        BuildLayout();

        _ = HudBusHelper.WhenReadyAsync(this, bus =>
        {
            bus.Subscribe<BattleEvents.TurnStarted>(OnTurnStarted);
            bus.Subscribe<BattleEvents.TurnEnded>(OnTurnEnded);
        });
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        BattleEventBus? bus = BattleEventBus.Instance;
        if (bus is null) return;
        bus.Unsubscribe<BattleEvents.TurnStarted>(OnTurnStarted);
        bus.Unsubscribe<BattleEvents.TurnEnded>(OnTurnEnded);
    }

    private void BuildLayout()
    {
        // Bottom-center strip, fixed size — empty area around it lets map clicks through.
        SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
        OffsetLeft = -260;
        OffsetRight = 260;
        OffsetTop = -64;
        OffsetBottom = -8;
        MouseFilter = MouseFilterEnum.Ignore;

        Control panelContent = new();
        panelContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panelContent.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(HudStyle.MakePanel(panelContent));

        HBoxContainer row = new()
        {
            Name = "ButtonRow",
            // Allow empty gaps inside the bar to pass clicks through to the map.
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddThemeConstantOverride("separation", 6);
        row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panelContent.AddChild(row);

        AddButton(row, "Attack", () => OnAttackPressed?.Invoke());
        AddButton(row, "Skill", () => OnSkillPressed?.Invoke());
        AddButton(row, "Item", () => OnItemPressed?.Invoke());
        AddButton(row, "Pass", () =>
        {
            OnPassPressed?.Invoke();
            UnitSystem? u = TurnManager.Active?.CurrentUnit;
            u?.PassTurn();
        });
        AddButton(row, "Flee", () => OnFleePressed?.Invoke());

        // Cancel is hidden until the player enters skill-targeting mode.
        _cancelButton = new Button
        {
            Text = "Cancel",
            CustomMinimumSize = new Vector2(92, 40),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
        };
        HudStyle.StyleButton(_cancelButton);
        _cancelButton.AddThemeColorOverride("font_color", new Color(1f, 0.7f, 0.5f));
        _cancelButton.Pressed += () => OnCancelPressed?.Invoke();
        row.AddChild(_cancelButton);

        // Begin hidden until the first player turn starts.
        Visible = false;
    }

    /// <summary>
    ///     Toggle the visibility of the Cancel button (used when entering / leaving
    ///     skill-targeting mode).
    /// </summary>
    /// <param name="show">Whether to show the button.</param>
    public void ShowCancel(bool show)
    {
        if (_cancelButton is not null) _cancelButton.Visible = show;
    }

    private static void AddButton(Container parent, string label, Action onPressed)
    {
        Button b = new()
        {
            Text = label,
            CustomMinimumSize = new Vector2(92, 40),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        HudStyle.StyleButton(b);
        b.Pressed += () => onPressed();
        parent.AddChild(b);
    }

    private void OnTurnStarted(BattleEvents.TurnStarted e) => Visible = e.Faction == Faction.Player;

    private void OnTurnEnded(BattleEvents.TurnEnded _) => Visible = false;
}

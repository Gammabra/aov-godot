using System;
using Godot;
using AshesOfVelsingrad.UI.Inventory;
using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Bottom-centre action bar for the active player unit.
/// </summary>
/// <remarks>
///     Tight strip so empty viewport area lets map clicks through. Each button raises a C#
///     event the rest of the HUD wires up. The "Pass" button additionally calls
///     <c>IUnitSystem.PassTurn</c> directly so the turn loop unblocks even if the
///     consumer didn't subscribe. The Cancel button is hidden until <see cref="ShowCancel" />
///     is invoked by <c>GameManager</c> when entering skill-targeting mode.
/// </remarks>
public sealed partial class ActionMenu : Control
{
    /// <summary>Player chose the basic-attack action.</summary>
    public event Action? OnAttackPressed;

    /// <summary>Player asked to open / focus the skill submenu.</summary>
    public event Action? OnSkillPressed;

    /// <summary>Player chose the move action (returns to MoveUnit click context).</summary>
    public event Action? OnMovePressed;

    /// <summary>Player chose to pass the turn.</summary>
    public event Action? OnPassPressed;

    /// <summary>Player cancelled skill targeting (Cancel button / Esc / right-click).</summary>
    public event Action? OnCancelPressed;
    private Button? _useItemButton;
    private BattleInventoryUI? _inventoryUI;
    private SkillSelector? _skillSelector;

    private Button? _cancelButton;
    private bool _built;
    private Func<InventorySystem?>? _getActiveInventory;

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
        // Layout target: the project's canvas_items design viewport is 1152×648.
        // Every anchor here is computed against that, so widgets stay clear of
        // each other once the viewport is scaled to whatever the player's window
        // happens to be. The bottom action bar sits at viewport-center, ±180 wide,
        // safely between the 308-wide PlayerStatusPanel on the left and the
        // 260-wide BattleLog on the right.
        SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
        OffsetLeft = -180;
        OffsetRight = 180;
        OffsetTop = -64;
        OffsetBottom = -8;
        MouseFilter = MouseFilterEnum.Ignore;

        Control panelContent = new() { MouseFilter = MouseFilterEnum.Ignore };
        panelContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(panelContent));

        HBoxContainer row = new()
        {
            Name = "ButtonRow",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddThemeConstantOverride("separation", 6);
        row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panelContent.AddChild(row);

        AddButton(row, "Move", () => OnMovePressed?.Invoke());
        AddButton(row, "Attack", () => OnAttackPressed?.Invoke());
        AddButton(row, "Skill", () => OnSkillPressed?.Invoke());
        // Pass just emits the event — GameManager owns the actual TurnManager / unit and
        // calls PassTurn() in its handler. This avoids a TurnManager singleton accessor.
        AddButton(row, "Pass", () => OnPassPressed?.Invoke());

        Button useItem = new()
        {
            Text = "Item",
            CustomMinimumSize = new Vector2(86, 40),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        HudStyle.StyleButton(useItem);
        useItem.AddThemeColorOverride("font_color", new Color(0.6f, 0.9f, 0.6f, 1f)); // soft green
        useItem.Pressed += OnUseItemPressed;
        _useItemButton = useItem;
        row.AddChild(_useItemButton);

        _cancelButton = new Button
        {
            Text = "Cancel",
            CustomMinimumSize = new Vector2(86, 40),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
        };
        HudStyle.StyleButton(_cancelButton);
        _cancelButton.AddThemeColorOverride("font_color", new Color(1f, 0.7f, 0.5f));
        _cancelButton.Pressed += () => OnCancelPressed?.Invoke();
        row.AddChild(_cancelButton);
    }

    /// <summary>
    ///     Connect the action menu to the inventory panel and skill selector
    ///     so "Use Item" can swap them in place.
    /// </summary>
    public void SetInventoryUI(BattleInventoryUI inventoryUI, SkillSelector skillSelector, Func<InventorySystem?> getActiveInventory)
    {
        _inventoryUI  = inventoryUI;
        _skillSelector = skillSelector;
        _getActiveInventory = getActiveInventory;
    }

    private void OnUseItemPressed()
    {
        if (_inventoryUI == null || _skillSelector == null)
            return;

        bool opening = !_inventoryUI.Visible;
        if (opening && _getActiveInventory != null)
        {
            var inv = _getActiveInventory();
            if (inv != null) _inventoryUI.BindInventory(inv);
        }

        _inventoryUI.Visible = opening;
        _skillSelector.Visible = !opening;
    }

    // In every other button's press handler, close the inventory if open.
    // Replace AddButton calls for Move, Attack, Skill, Pass with this helper instead:
    private void AddButton(Container parent, string label, Action onPressed)
    {
        Button b = new()
        {
            Text = label,
            CustomMinimumSize = new Vector2(86, 40),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        HudStyle.StyleButton(b);
        b.Pressed += () =>
        {
            CloseInventory();
            onPressed();
        };
        parent.AddChild(b);
    }

    // Add the close helper:
    private void CloseInventory()
    {
        if (_inventoryUI is { Visible: true })
        {
            _inventoryUI.Visible = false;
            if (_skillSelector != null) _skillSelector.Visible = true;
        }
    }

    /// <summary>Show or hide the Cancel button. Called by <c>GameManager</c> when the player
    /// enters or leaves skill-targeting mode.</summary>
    /// <param name="show">True to show the Cancel button.</param>
    public void ShowCancel(bool show)
    {
        if (_cancelButton is not null) _cancelButton.Visible = show;
        if (show) CloseInventory();
    }
}

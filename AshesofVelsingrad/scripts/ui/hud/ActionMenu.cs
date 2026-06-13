using System;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Inventory;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Bottom-centre action bar for the active player unit.
/// </summary>
/// <remarks>
///     <para>
///         Iron + bronze frame, four icon-and-label buttons (Move / Attack / Skill / Pass)
///         plus a contextual Cancel that only appears while skill-targeting. Each button
///         carries a procedural icon from <c>res://assets/ui/hud/icons/</c>; if the icon
///         hasn't been imported yet the button still works as text-only.
///     </para>
///     <para>
///         Layout uses <see cref="HudStyle.ScaledPx"/> for the bar's anchor offsets so the
///         strip grows / shrinks with the user's UI scale slider in lock-step with the fonts.
///     </para>
/// </remarks>
public sealed partial class ActionMenu : Control, IHudWidget
{
    /// <summary>Player chose the basic-attack action.</summary>
    public event Action? OnAttackPressed;
    /// <summary>Player asked to open / focus the skill submenu.</summary>
    public event Action? OnSkillPressed;
    /// <summary>Player chose the move action.</summary>
    public event Action? OnMovePressed;
    /// <summary>Player chose to pass the turn.</summary>
    public event Action? OnPassPressed;
    /// <summary>Player cancelled skill targeting.</summary>
    public event Action? OnCancelPressed;
    private Button? _useItemButton;
    private BattleInventoryUI? _inventoryUI;
    private SkillSelector? _skillSelector;

    private Button? _cancelButton;
    private bool _built;
    private Func<InventorySystem?>? _getActiveInventory;

    /// <inheritdoc />
    public override void _Ready() => EnsureBuilt();

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
        SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
        int halfW = HudStyle.ScaledPx(HudStyle.ActionBarWidth) / 2;
        int height = HudStyle.ScaledPx(HudStyle.ActionBarHeight);
        OffsetLeft = -halfW;
        OffsetRight = halfW;
        OffsetTop = -height - HudStyle.PadLg;
        OffsetBottom = -HudStyle.PadLg;
        CustomMinimumSize = new Vector2(2 * halfW, height);
    }

    private void BuildLayout()
    {
        ApplyAnchorOffsets();
        // The widget itself MUST pass clicks through outside of buttons — players need to
        // click the battlefield through the gap between buttons.
        MouseFilter = MouseFilterEnum.Ignore;

        Control panelContent = new() { Name = "Content", MouseFilter = MouseFilterEnum.Ignore };
        panelContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(panelContent, HudStyle.PanelTier.Heavy));

        HBoxContainer row = new()
        {
            Name = "ButtonRow",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddThemeConstantOverride("separation", HudStyle.PadXs);
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

        _cancelButton = BuildButton("Cancel", "cancel", () => OnCancelPressed?.Invoke());
        _cancelButton.Visible = false;
        // Cancel uses an orange accent so it stands out from the four normal actions.
        _cancelButton.AddThemeColorOverride("font_color", new Color(1f, 0.72f, 0.45f, 1f));
        row.AddChild(_cancelButton);
    }

    /// <summary>
    ///     Connect the action menu to the inventory panel and skill selector
    ///     so "Use Item" can swap them in place.
    /// </summary>
    public void SetInventoryUI(BattleInventoryUI inventoryUI, SkillSelector skillSelector, Func<InventorySystem?> getActiveInventory)
    {
        _inventoryUI = inventoryUI;
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
        // Min width is ~70 design px so 5 buttons (4 + Cancel) fit in the 440-wide bar
        // even with the bronze content margins. The HBox shares any extra width via
        // SizeFlags.ExpandFill.
        Button b = new()
        {
            Text = label,
            CustomMinimumSize = new Vector2(
                HudStyle.ScaledPx(70),
                HudStyle.ScaledPx(HudStyle.ButtonHeight)),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.Fill,
            ClipText = true,
            // Icon on the LEFT, text follows. ExpandIcon=false keeps the icon at its
            // natural size instead of stretching it to fill the button.
            IconAlignment = HorizontalAlignment.Left,
            VerticalIconAlignment = VerticalAlignment.Center,
            ExpandIcon = false,
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

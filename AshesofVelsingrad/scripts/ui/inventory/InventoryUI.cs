// scripts/ui/hud/InventoryUI.cs
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Inventory panel spawned programmatically by <c>GameManager</c>,
///     following the same pattern as <see cref="BattleHud" />.
/// </summary>
public sealed partial class InventoryUI : CanvasLayer
{
    public const int InventoryLayer = 101; // above BattleHud

    private bool _built;
    private GridContainer? _slotContainer;
    private BattleInputSystem? _battleInputSystem;
    private InventorySystem? _inventory;
    private readonly List<InventorySlotUI> _slotUis = new();

    // ------------------------------------------------------------------ //
    //  Godot lifecycle                                                     //
    // ------------------------------------------------------------------ //

    public override void _Ready()
    {
        EnsureBuilt();
    }

    // ------------------------------------------------------------------ //
    //  Build                                                               //
    // ------------------------------------------------------------------ //

    /// <summary>
    ///     Build the panel layout synchronously. Idempotent — safe to call
    ///     before the node enters the tree (same guarantee as BattleHud.Build).
    /// </summary>
    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        Layer = InventoryLayer;
        Visible = false;

        // Root panel — dark translucent background
        var panel = new PanelContainer { Name = "Panel" };
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.CustomMinimumSize = new Vector2(480, 360);
        AddChild(panel);

        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        // Title bar
        var title = new Label { Text = "Inventory" };
        title.AddThemeFontSizeOverride("font_size", 20);
        vbox.AddChild(title);

        // Close button
        var closeBtn = new Button { Text = "Close" };
        closeBtn.Pressed += Toggle;
        vbox.AddChild(closeBtn);

        // Slot grid
        _slotContainer = new GridContainer { Columns = 5 };
        vbox.AddChild(_slotContainer);
    }

    // ------------------------------------------------------------------ //
    //  Public API (called by GameManager)                                  //
    // ------------------------------------------------------------------ //

    public void SetBattleInputSystem(BattleInputSystem bis)
    {
        _battleInputSystem = bis;
    }

    /// <summary>
    ///     Bind to a unit's inventory and rebuild the slot grid.
    ///     Safe to call every time the active unit changes.
    /// </summary>
    public void BindInventory(InventorySystem inventory)
    {
        // Unsubscribe from previous inventory to avoid stale callbacks
        if (_inventory != null)
            _inventory.SlotChanged -= OnSlotChanged;

        _inventory = inventory;
        _inventory.SlotChanged += OnSlotChanged;

        BuildSlots();
        RefreshAll();
    }

    /// <summary>Called by <see cref="InventorySlotUI" /> when the player clicks Use.</summary>
    public void NotifyUseItemPressed(int slotIndex)
    {
        _battleInputSystem?.EmitSignal(BattleInputSystem.SignalName.OnUseItemPressed, slotIndex);
        Toggle(); // close after using
    }

    public void Toggle()
    {
        Visible = !Visible;
    }

    // ------------------------------------------------------------------ //
    //  Private helpers                                                     //
    // ------------------------------------------------------------------ //

    private void BuildSlots()
    {
        if (_slotContainer == null || _inventory == null) return;

        foreach (Node child in _slotContainer.GetChildren())
            child.QueueFree();

        _slotUis.Clear();

        for (int i = 0; i < _inventory.Slots.Length; i++)
        {
            var slotUi = new InventorySlotUI();
            slotUi.Setup(i, this);
            _slotContainer.AddChild(slotUi);
            _slotUis.Add(slotUi);
        }
    }

    private void RefreshAll()
    {
        if (_inventory == null) return;
        for (int i = 0; i < _slotUis.Count; i++)
            _slotUis[i].Refresh(_inventory.Slots[i]);
    }

    private void OnSlotChanged(int index)
    {
        if (index < 0 || index >= _slotUis.Count || _inventory == null) return;
        _slotUis[index].Refresh(_inventory.Slots[index]);
    }
}

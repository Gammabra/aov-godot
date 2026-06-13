using System.Collections.Generic;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Inventory;

/// <summary>
///     Exploration inventory panel: 30-slot global grid on the left,
///     4 unit loadout rows on the right. Sized as a fraction of the viewport
///     so it scales cleanly at any resolution.
/// </summary>
public partial class ExplorationInventoryUI : CanvasLayer
{
    public const int InventoryLayer = 10;

    [ExportGroup("Internal Node Bindings")]
    [Export] private GridContainer _exploGrid = null!;
    [Export] private VBoxContainer _unitPanelColumn = null!;
    [Export] private Button _closeButton = null!;

    [ExportGroup("Prefab Scenes")]
    [Export] private PackedScene _slotScene = null!;
    [Export] private PackedScene _unitPanelScene = null!;

    private readonly List<ExplorationSlotUI> _exploSlots = new();
    private readonly List<UnitTransferPanel> _unitPanels = new();

    public override void _Ready()
    {
        Layer = InventoryLayer;
        Visible = false;

        // Connect editor-mapped close button
        if (_closeButton != null)
            _closeButton.Pressed += Toggle;

        BindGlobalInventory();
        RefreshUnitPanels();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("open_inventory"))
            Toggle();
    }

    public void Toggle()
    {
        if (!Visible)
        {
            if (PlayerInventoryManager.Instance is { } mgr)
                RefreshAll(mgr.GlobalInventory);
            RefreshUnitPanels();
        }
        Visible = !Visible;
    }

    public virtual void RefreshUnitPanels()
    {
        if (_unitPanelColumn == null || _unitPanelScene == null) return;
        if (PlayerInventoryManager.Instance is not { } mgr) return;

        foreach (Node child in _unitPanelColumn.GetChildren())
            child.QueueFree();

        _unitPanels.Clear();

        for (int i = 0; i < PlayerInventoryManager.MaxPartySize; i++)
        {
            var panel = _unitPanelScene.Instantiate<UnitTransferPanel>();
            _unitPanelColumn.AddChild(panel);

            panel.Bind(mgr.PartyNames[i], mgr.PartyLoadouts[i]);
            _unitPanels.Add(panel);
        }
    }

    private void BindGlobalInventory()
    {
        if (PlayerInventoryManager.Instance is not { } mgr)
        {
            GD.PrintErr("ExplorationInventoryUI: PlayerInventoryManager not found.");
            return;
        }

        mgr.GlobalInventory.SlotChanged += OnGlobalSlotChanged;
        BuildExploSlots(mgr.GlobalInventory);
    }

    private void BuildExploSlots(InventorySystem inventory)
    {
        if (_exploGrid == null || _slotScene == null) return;

        foreach (Node child in _exploGrid.GetChildren())
            child.QueueFree();

        _exploSlots.Clear();

        for (int i = 0; i < InventoryConstants.ExplorationCapacity; i++)
        {
            var slot = _slotScene.Instantiate<ExplorationSlotUI>();
            _exploGrid.AddChild(slot);

            slot.Setup(i, inventory);
            _exploSlots.Add(slot);
        }

        RefreshAll(inventory);
    }

    private void RefreshAll(InventorySystem inventory)
    {
        for (int i = 0; i < _exploSlots.Count; i++)
            _exploSlots[i].Refresh(inventory.GetSlot(i));
    }

    private void OnGlobalSlotChanged(int index)
    {
        if (PlayerInventoryManager.Instance is not { } mgr) return;
        if (index < 0 || index >= _exploSlots.Count) return;
        _exploSlots[index].Refresh(mgr.GlobalInventory.GetSlot(index));
    }

    public override void _ExitTree()
    {
        if (PlayerInventoryManager.Instance is { } mgr)
            mgr.GlobalInventory.SlotChanged -= OnGlobalSlotChanged;

        base._ExitTree();
    }
}

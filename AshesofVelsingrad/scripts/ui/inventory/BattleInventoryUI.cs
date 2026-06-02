using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Inventory;

/// <summary>
///    Battle inventory panel: horizontal row of 5 slots. Pops up as a modal overlay during battle when the player presses the inventory key.
///    Binds directly to the live inventory of the currently controlled unit, so it requires a reference to the battle's input system to signal item use and close itself.
/// </summary>
public sealed partial class BattleInventoryUI : Control
{
    [Export] private HBoxContainer _slotRow = null!;
    [Export] private PackedScene _battleSlotScene = null!; // Your BattleSlotUI scene

    private BattleInputSystem? _battleInputSystem;
    private InventorySystem? _inventory;
    private readonly List<BattleSlotUI> _slots = new();

    public override void _Ready()
    {
        Visible = false;
        BuildBattleSlots();
    }

    private void BuildBattleSlots()
    {
        if (_slotRow == null || _battleSlotScene == null) return;

        foreach (Node child in _slotRow.GetChildren()) 
            child.QueueFree();
            
        _slots.Clear();

        for (int i = 0; i < InventoryConstants.BattleCapacity; i++)
        {
            var slot = _battleSlotScene.Instantiate<BattleSlotUI>();
            _slotRow.AddChild(slot);
            
            slot.Setup(i, this);
            _slots.Add(slot);
        }
    }

    public void SetBattleInputSystem(BattleInputSystem bis) => _battleInputSystem = bis;

    public void BindInventory(InventorySystem inventory)
    {
        if (_inventory != null) 
            _inventory.SlotChanged -= OnSlotChanged;
            
        _inventory = inventory;
        _inventory.SlotChanged += OnSlotChanged;
        RefreshAll();
    }

    public void NotifyUseItemPressed(int slotIndex)
    {
        _battleInputSystem?.EmitSignal(BattleInputSystem.SignalName.OnUseItemPressed, slotIndex);
        Toggle();
    }

    public void Toggle() => Visible = !Visible;

    private void RefreshAll()
    {
        if (_inventory == null) return;
        for (int i = 0; i < _slots.Count; i++)
            _slots[i].Refresh(_inventory.GetSlot(i));
    }

    private void OnSlotChanged(int index)
    {
        if (_inventory == null || index < 0 || index >= _slots.Count) return;
        _slots[index].Refresh(_inventory.GetSlot(index));
    }
}

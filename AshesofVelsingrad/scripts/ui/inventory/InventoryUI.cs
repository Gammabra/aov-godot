using Godot;
using AshesOfVelsingrad.Systems;
using System.Collections.Generic;

public partial class InventoryUI : Control
{
    [Export] private GridContainer? _slotContainer;
    [Export] private PackedScene? _slotScene;

    // ADD: reference to input system so we can emit through it
    private BattleInputSystem? _battleInputSystem;

    private InventorySystem? _inventory;
    private readonly List<InventorySlotUI> _slotUis = new();

    public override void _Ready()
    {
        Visible = false;
    }

    // ADD: called by GameManager during setup
    public void SetBattleInputSystem(BattleInputSystem bis)
    {
        _battleInputSystem = bis;
    }

    public void BindInventory(InventorySystem inventory)
    {
        _inventory = inventory;
        _inventory.SlotChanged += OnSlotChanged;
        BuildSlots();
        RefreshAll();
    }

    // Called by InventorySlotUI when the player clicks "Use"
    public void NotifyUseItemPressed(int slotIndex)
    {
        _battleInputSystem?.EmitSignal(BattleInputSystem.SignalName.OnUseItemPressed, slotIndex);
        Toggle(); // close the panel after using
    }

    private void BuildSlots()
    {
        if (_slotContainer == null || _slotScene == null || _inventory == null)
            return;

        foreach (Node child in _slotContainer.GetChildren())
            child.QueueFree();

        _slotUis.Clear();

        for (int i = 0; i < _inventory.Slots.Length; i++)
        {
            var slotUi = _slotScene.Instantiate<InventorySlotUI>();
            slotUi.Setup(i, this); // pass reference so slot can call back
            _slotContainer.AddChild(slotUi);
            _slotUis.Add(slotUi);
        }
    }

    private void RefreshAll()
    {
        if (_inventory == null) return;
        for (int i = 0; i < _inventory.Slots.Length; i++)
            _slotUis[i].Refresh(_inventory.Slots[i]);
    }

    private void OnSlotChanged(int index)
    {
        if (index < 0 || index >= _slotUis.Count || _inventory == null) return;
        _slotUis[index].Refresh(_inventory.Slots[index]);
    }

    public void Toggle()
    {
        Visible = !Visible;
    }
}

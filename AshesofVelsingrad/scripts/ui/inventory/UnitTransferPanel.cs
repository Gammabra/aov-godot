using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Inventory;

/// <summary>
///     Horizontal strip: unit name label + 5 drop-target slots.
///     Binds to a persistent <see cref="InventorySystem" /> party loadout,
///     so no live unit node is required during exploration.
/// </summary>
public sealed partial class UnitTransferPanel : Control
{
    [Export] private Label _nameLabel = null!;
    [Export] private HBoxContainer _slotRow = null!;
    [Export] private PackedScene _unitSlotScene = null!; // Your UnitSlotUI scene

    private readonly List<UnitSlotUI> _slots = new();
    private InventorySystem? _loadout;

    public void Bind(string unitName, InventorySystem loadout)
    {
        if (_loadout != null)
            _loadout.SlotChanged -= OnLoadoutSlotChanged;

        // Clear dynamic elements safely
        foreach (Node child in _slotRow.GetChildren())
            child.QueueFree();

        _slots.Clear();

        _loadout = loadout;
        _loadout.SlotChanged += OnLoadoutSlotChanged;

        if (_nameLabel != null)
            _nameLabel.Text = unitName;

        if (_slotRow != null && _unitSlotScene != null)
        {
            for (int i = 0; i < InventoryConstants.BattleCapacity; i++)
            {
                var slot = _unitSlotScene.Instantiate<UnitSlotUI>();
                _slotRow.AddChild(slot);

                slot.Setup(i, loadout);
                slot.Refresh(loadout.GetSlot(i));
                _slots.Add(slot);
            }
        }
    }

    public override void _ExitTree()
    {
        if (_loadout != null)
            _loadout.SlotChanged -= OnLoadoutSlotChanged;

        base._ExitTree();
    }

    private void OnLoadoutSlotChanged(int index)
    {
        if (!GodotObject.IsInstanceValid(this) || _loadout == null || index < 0 || index >= _slots.Count) return;
        _slots[index].Refresh(_loadout.GetSlot(index));
    }
}

using Godot;
using AshesOfVelsingrad.systems;

public partial class InventorySlotUI : PanelContainer
{
	[Export] private Label _label;

	private int _slotIndex;

	public void Setup(int slotIndex)
	{
		_slotIndex = slotIndex;
	}

	public void Refresh(InventorySlot slot)
	{
		if (slot.IsEmpty)
		{
			_label.Text = $"[{_slotIndex}] Empty";
			return;
		}

		var item = ItemCatalog.Get(slot.ItemId);
		_label.Text = $"[{_slotIndex}] {item.Name} x{slot.Quantity}";
	}
}

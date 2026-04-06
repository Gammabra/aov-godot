using Godot;
using AshesOfVelsingrad.systems;
using System.Collections.Generic;

public partial class InventoryUI : Control
{
	[Export] private GridContainer _slotContainer;
	[Export] private PackedScene _slotScene;

	private InventorySystem _inventory;
	private readonly List<InventorySlotUI> _slotUis = new();

	public override void _Ready()
	{
		Visible = false;
	}

	public void BindInventory(InventorySystem inventory)
	{
		_inventory = inventory;
		_inventory.SlotChanged += OnSlotChanged;

		BuildSlots();
		RefreshAll();
	}

	private void BuildSlots()
	{
		foreach (Node child in _slotContainer.GetChildren())
			child.QueueFree();

		_slotUis.Clear();

		for (int i = 0; i < _inventory.Slots.Length; i++)
		{
			var slotUi = _slotScene.Instantiate<InventorySlotUI>();
			slotUi.Setup(i);
			_slotContainer.AddChild(slotUi);
			_slotUis.Add(slotUi);
		}
	}

	private void RefreshAll()
	{
		for (int i = 0; i < _inventory.Slots.Length; i++)
			_slotUis[i].Refresh(_inventory.Slots[i]);
	}

	private void OnSlotChanged(int index)
	{
		if (index < 0 || index >= _slotUis.Count)
			return;

		_slotUis[index].Refresh(_inventory.Slots[index]);
	}

	public void Toggle()
	{
		Visible = !Visible;
	}
}

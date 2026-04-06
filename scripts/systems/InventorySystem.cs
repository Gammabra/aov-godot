using System;

namespace AshesOfVelsingrad.systems;

public struct InventorySlot
{
	public int ItemId;
	public int Quantity;

	public bool IsEmpty => ItemId == 0 || Quantity <= 0;

	public void Clear()
	{
		ItemId = 0;
		Quantity = 0;
	}
}

public sealed class InventorySystem
{
	public int Capacity { get; }
	public InventorySlot[] Slots { get; }

	public event Action<int>? SlotChanged;

	private readonly Func<ItemSystem, bool>? _accept;

	public InventorySystem(int capacity, Func<ItemSystem, bool>? acceptFilter = null)
	{
		if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
		Capacity = capacity;
		Slots = new InventorySlot[capacity];
		_accept = acceptFilter;
	}

	public bool CanAccept(int itemId)
	{
		if (!ItemCatalog.TryGet(itemId, out var item))
			return false;

		return _accept == null || _accept(item);
	}

	public int AddItem(int itemId, int amount)
	{
		if (amount <= 0) return 0;
		var item = ItemCatalog.Get(itemId);

		if (_accept != null && !_accept(item))
			return amount;

		if (item.IsStackable && item.MaxStack > 1)
		{
			for (int i = 0; i < Slots.Length && amount > 0; i++)
			{
				if (Slots[i].IsEmpty) continue;
				if (Slots[i].ItemId != itemId) continue;

				int space = item.MaxStack - Slots[i].Quantity;
				if (space <= 0) continue;

				int add = Math.Min(space, amount);
				Slots[i].Quantity += add;
				amount -= add;
				SlotChanged?.Invoke(i);
			}
		}

		for (int i = 0; i < Slots.Length && amount > 0; i++)
		{
			if (!Slots[i].IsEmpty) continue;

			if (item.IsStackable && item.MaxStack > 1)
			{
				int add = Math.Min(item.MaxStack, amount);
				Slots[i].ItemId = itemId;
				Slots[i].Quantity = add;
				amount -= add;
			}
			else
			{
				Slots[i].ItemId = itemId;
				Slots[i].Quantity = 1;
				amount -= 1;
			}

			SlotChanged?.Invoke(i);
		}

		return amount;
	}

	public bool RemoveItem(int itemId, int amount)
	{
		if (amount <= 0) return true;

		for (int i = 0; i < Slots.Length && amount > 0; i++)
		{
			if (Slots[i].IsEmpty) continue;
			if (Slots[i].ItemId != itemId) continue;

			int take = Math.Min(Slots[i].Quantity, amount);
			Slots[i].Quantity -= take;
			amount -= take;

			if (Slots[i].Quantity <= 0)
				Slots[i].Clear();

			SlotChanged?.Invoke(i);
		}

		return amount == 0;
	}

	public void Move(int fromIndex, int toIndex)
	{
		if (fromIndex < 0 || fromIndex >= Slots.Length) return;
		if (toIndex < 0 || toIndex >= Slots.Length) return;
		if (fromIndex == toIndex) return;

		var a = Slots[fromIndex];
		var b = Slots[toIndex];

		if (a.IsEmpty) return;

		if (!b.IsEmpty && a.ItemId == b.ItemId)
		{
			var item = ItemCatalog.Get(a.ItemId);
			if (item.IsStackable && item.MaxStack > 1)
			{
				int space = item.MaxStack - b.Quantity;
				if (space > 0)
				{
					int move = Math.Min(space, a.Quantity);
					b.Quantity += move;
					a.Quantity -= move;

					if (a.Quantity <= 0) a.Clear();

					Slots[fromIndex] = a;
					Slots[toIndex] = b;
					SlotChanged?.Invoke(fromIndex);
					SlotChanged?.Invoke(toIndex);
					return;
				}
			}
		}

		Slots[fromIndex] = b;
		Slots[toIndex] = a;
		SlotChanged?.Invoke(fromIndex);
		SlotChanged?.Invoke(toIndex);
	}
}

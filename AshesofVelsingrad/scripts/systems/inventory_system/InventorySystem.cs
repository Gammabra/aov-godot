using System;

namespace AshesOfVelsingrad.Systems;

public struct InventorySlot: IInventorySlot
{
	public int ItemId { get; set; }
	public int Quantity { get; set; }

	public bool IsEmpty => ItemId == 0 || Quantity <= 0;

	public void Clear()
	{
		ItemId = 0;
		Quantity = 0;
	}
}

public sealed class InventorySystem: IInventorySystem
{
	public int Capacity { get; }
	public IInventorySlot[] Slots { get; }

	public event Action<int>? SlotChanged;

	private readonly Func<ItemSystem, bool>? _accept;
    public IInventorySlot GetSlot(int index) => Slots[index];

	public InventorySystem(int capacity, Func<ItemSystem, bool>? acceptFilter = null)
	{
		if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
		Capacity = capacity;
		Slots = new IInventorySlot[capacity];
		_accept = acceptFilter;

		// Fill with empty structs so GetSlot never returns null
		for (int i = 0; i < capacity; i++)
			Slots[i] = new InventorySlot();
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

	/// <summary>
    /// Uses the item in the given slot. Returns false if the slot is empty or item use failed.
    /// Does NOT call ReportSystemUnitHasPlayed — that is the caller's responsibility.
    /// </summary>
    public bool TryUseItem(int slotIndex, IUnitSystem user, IUnitSystem? target, IMapSystem? map)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Length) return false;
        var slot = Slots[slotIndex];
        if (slot.IsEmpty) return false;
        if (!ItemCatalog.TryGet(slot.ItemId, out var item)) return false;

        item.Use(user, target, map);   // apply effect (heal, buff, etc.)
        RemoveItem(slot.ItemId, 1);    // consume one from stack
        return true;
    }

	// In InventorySystem.cs — add:
	/// <summary>
	///     Overwrites this inventory's slots with a shallow copy of <paramref name="source" />.
	///     Used to seed per-unit battle inventories from the global exploration inventory.
	/// </summary>
	public void CopyFrom(IInventorySystem source)
	{
		for (int i = 0; i < Slots.Length && i < source.Slots.Length; i++)
		{
			Slots[i] = source.Slots[i]; // InventorySlot is a struct — copy by value
			SlotChanged?.Invoke(i);
		}
	}
}

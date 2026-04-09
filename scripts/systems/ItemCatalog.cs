using System;
using System.Collections.Generic;

namespace AshesOfVelsingrad.systems;

public static class ItemCatalog
{
	private static readonly Dictionary<int, ItemSystem> _items = new();

	public static void Register(ItemSystem item)
	{
		if (_items.ContainsKey(item.Id))
			throw new InvalidOperationException($"Item id already registered: {item.Id}");
		_items[item.Id] = item;
	}

	public static ItemSystem Get(int id)
	{
		if (!_items.TryGetValue(id, out var item))
			throw new KeyNotFoundException($"Unknown item id: {id}");
		return item;
	}

	public static bool TryGet(int id, out ItemSystem item) => _items.TryGetValue(id, out item!);

	public static void Clear() => _items.Clear();
}

using System;

namespace AshesOfVelsingrad.Systems;

public interface IInventorySystem
{
    int Capacity { get; }
    event Action<int>? SlotChanged;

    bool TryUseItem(int slotIndex, IUnitSystem user, IUnitSystem? target, IMapSystem? map);
    IReadOnlyInventorySlot GetSlot(int index);
}

public interface IReadOnlyInventorySlot
{
    int ItemId { get; }
    int Quantity { get; }
    bool IsEmpty { get; }
}
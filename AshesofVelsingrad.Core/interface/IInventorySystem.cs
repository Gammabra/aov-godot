using System;

namespace AshesOfVelsingrad.Systems;

public interface IInventorySystem
{
    int Capacity { get; }
    IInventorySlot[] Slots { get; }
    event Action<int>? SlotChanged;

    bool TryUseItem(int slotIndex, IUnitSystem user, IUnitSystem? target, IMapSystem? map);
    IInventorySlot GetSlot(int index);
    void CopyFrom(IInventorySystem source);
}

public interface IInventorySlot
{
    int ItemId { get; set; }
    int Quantity { get; set; }
    bool IsEmpty { get; }

    void Clear();
}

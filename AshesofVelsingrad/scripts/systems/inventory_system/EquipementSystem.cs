using System;

namespace AshesOfVelsingrad.Systems;

public enum EquipSlot
{
    Weapon,
    Armor
}

public sealed class EquipmentSystem
{
    public int WeaponItemId { get; private set; }
    public int ArmorItemId { get; private set; }

    public event Action<EquipSlot>? EquipmentChanged;

    public bool TryEquip(int itemId)
    {
        var item = ItemCatalog.Get(itemId);

        switch (item.Category)
        {
            case ItemCategory.Weapon:
                WeaponItemId = itemId;
                EquipmentChanged?.Invoke(EquipSlot.Weapon);
                return true;

            case ItemCategory.Armor:
                ArmorItemId = itemId;
                EquipmentChanged?.Invoke(EquipSlot.Armor);
                return true;

            default:
                return false;
        }
    }

    public int Unequip(EquipSlot slot)
    {
        int removed;
        switch (slot)
        {
            case EquipSlot.Weapon:
                removed = WeaponItemId;
                WeaponItemId = 0;
                break;
            case EquipSlot.Armor:
                removed = ArmorItemId;
                ArmorItemId = 0;
                break;
            default:
                return 0;
        }

        EquipmentChanged?.Invoke(slot);
        return removed;
    }
}

using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.items;

public sealed class IronSwordItem : ItemSystem
{
    public IronSwordItem()
    {
        Id = 2;
        Name = "Iron Sword";
        Description = "A simple sword.";
        Category = ItemCategory.Weapon;
        IsStackable = false;
        MaxStack = 1;
    }

    public override void Use(IUnitSystem user, IUnitSystem? target, IMapSystem? map)
    {
        if (target == null)
            return;
    }
}

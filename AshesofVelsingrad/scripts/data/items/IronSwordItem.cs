using AshesOfVelsingrad.systems;

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
}

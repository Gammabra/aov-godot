using AshesOfVelsingrad.systems;

namespace AshesOfVelsingrad.items;

public sealed class LeatherArmorItem : ItemSystem
{
	public LeatherArmorItem()
	{
		Id = 3;
		Name = "Leather Armor";
		Description = "A light armor";
		Category = ItemCategory.Armor;
		IsStackable = false;
		MaxStack = 1;
	}
}

using AshesOfVelsingrad.Systems;

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

	public override void Use(IUnitSystem user, IUnitSystem? target, IMapSystem? map)
	{
		if (target == null)
			return;
	}
}

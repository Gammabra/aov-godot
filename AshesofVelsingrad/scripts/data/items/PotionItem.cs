using AshesOfVelsingrad.systems;

namespace AshesOfVelsingrad.items;

public sealed class PotionItem : ItemSystem
{
	public PotionItem()
	{
		Id = 1;
		Name = "Potion";
		Description = "Restores a small amount of HP.";
		Category = ItemCategory.Consumable;
		IsStackable = true;
		MaxStack = 10;
		TargetType = TargetType.Allies;
	}
}

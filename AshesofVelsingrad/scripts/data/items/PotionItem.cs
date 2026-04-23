using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

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
		TargetType = AovDataStructures.TargetTypes.SingleAlly;
	}

	public override void Use(IUnitSystem user, IUnitSystem? target, IMapSystem? map)
	{
		if (target == null)
			return;
	}
}

using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.items;

public sealed class PotionItem : ItemSystem
{
    /// <summary>HP restored per use.</summary>
    private const float _healAmount = 50f;

    public PotionItem()
    {
        Id = 1;
        Name = "Potion";
        Description = $"Restores {_healAmount} HP.";
        Category = ItemCategory.Consumable;
        IsStackable = true;
        MaxStack = 10;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem user, IUnitSystem? target, IMapSystem? map)
    {
        // Heal the target if provided, otherwise heal the user (self-use)
        var actualTarget = target ?? user;
        actualTarget.OnEffectHeal(_healAmount);
    }
}

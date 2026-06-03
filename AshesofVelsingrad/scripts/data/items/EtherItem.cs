using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.items;

/// <summary>Restores a small amount of mana.</summary>
public sealed class EtherItem : ItemSystem
{
    private const float _manaAmount = 40f;

    public EtherItem()
    {
        Id = 4;
        Name = "Ether";
        Description = $"Restores {_manaAmount} MP.";
        Category = ItemCategory.Consumable;
        IsStackable = true;
        MaxStack = 10;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem user, IUnitSystem? target, IMapSystem? map)
    {
        var actualTarget = target ?? user;
        actualTarget.RestoreMana(_manaAmount); // mana restore needs a dedicated method — see note below
    }
}
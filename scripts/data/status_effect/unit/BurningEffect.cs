using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace AshesOfVelsingrad.Data;

public sealed class BurningEffect(
    int duration,
    AovDataStructures.ModifierType modifierType,
    float amount
)
    : StatusEffect<UnitSystem>("Burning", "BurningEffect", duration, false, modifierType, amount)
{
    public override void OnTurnPassed(IEffectTarget<UnitSystem> target)
    {
        if (target is UnitSystem unit)
            unit.OnEffectDamage(ModifierType, Amount);
        base.OnTurnPassed(target);
    }
}

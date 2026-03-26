using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data;

public sealed class AtkBuffer(
    int duration,
    AovDataStructures.ModifierType modifierType,
    float amount
)
    : StatusEffect<UnitSystem>(
        "AtkBuffer",
        "Buff Atk",
        duration,
        false,
        modifierType,
        amount
    )
{
    public override void OnApply(IEffectTarget<UnitSystem> target)
    {
        if (target is UnitSystem unit)
            unit.OnEffectModifierApplied(AovDataStructures.StatTypeWithModifier.Atk, ModifierType, Amount);
        base.OnApply(target);
    }

    public override void OnRemove(IEffectTarget<UnitSystem> target)
    {
        if (target is UnitSystem unit)
            unit.OnEffectModifierRemoved(AovDataStructures.StatTypeWithModifier.Atk, ModifierType, Amount);
        base.OnRemove(target);
    }
}

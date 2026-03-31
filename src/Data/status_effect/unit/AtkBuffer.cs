using AshesOfVelsingrad.Utilities;
using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.Data;

public sealed class AtkBuffer(
    int duration,
    AovDataStructures.ModifierType modifierType,
    float amount
)
    : StatusEffect<IUnitSystem>(
        "AtkBuffer",
        "Buff Atk",
        duration,
        false,
        modifierType,
        amount
    )
{
    public override void OnApply(IUnitSystem target)
    {
        if (target is IUnitSystem unit)
            unit.OnEffectModifierApplied(AovDataStructures.StatTypeWithModifier.Atk, ModifierType, Amount);
        base.OnApply(target);
    }

    public override void OnRemove(IUnitSystem target)
    {
        if (target is IUnitSystem unit)
            unit.OnEffectModifierRemoved(AovDataStructures.StatTypeWithModifier.Atk, ModifierType, Amount);
        base.OnRemove(target);
    }
}

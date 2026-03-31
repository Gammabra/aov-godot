using AshesOfVelsingrad.Utilities;
using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.Data;

public sealed class BurningEffect(
    int duration,
    AovDataStructures.ModifierType modifierType,
    float amount
)
    : StatusEffect<IUnitSystem>("Burning", "BurningEffect", duration, false, modifierType, amount)
{
    public override void OnTurnPassed(IUnitSystem target)
    {
        if (target is IUnitSystem unit)
            unit.OnEffectDamage(ModifierType, Amount);
        base.OnTurnPassed(target);
    }
}

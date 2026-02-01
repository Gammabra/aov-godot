using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.Data;

public sealed class BurningEffect(int duration)
    : StatusEffect<UnitSystem>("Burning", "BurningEffect", duration, false)
{
    public override void OnTurnPassed(IEffectTarget<UnitSystem> target)
    {
        if (target is UnitSystem unit)
            unit.TakeDamage(5);
        base.OnTurnPassed(target);
    }
}

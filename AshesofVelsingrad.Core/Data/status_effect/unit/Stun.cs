using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.Data;

public sealed class Stun(
    int duration
)
    : StatusEffect<IUnitSystem>("Stun", "Stun", duration, false)
{
    public override bool ShouldApplyTwice => true;  // ADD THIS

    public override void OnApply(IUnitSystem target)
    {
        if (target is IUnitSystem unit)
            unit.OnEffectControlApplied();
        base.OnApply(target);
    }

    public override void OnRemove(IUnitSystem target)
    {
        if (target is IUnitSystem unit)
            unit.OnEffectControlRemoved();
        base.OnRemove(target);
    }
}

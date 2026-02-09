using AshesOfVelsingrad.Systems;
using Godot;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data;

public sealed class Stun(
    int duration
)
    : StatusEffect<UnitSystem>("Stun", "Stun", duration, false)
{
    public override void OnApply(IEffectTarget<UnitSystem> target)
    {
        if (target is UnitSystem unit)
            unit.OnEffectControlApplied();
        base.OnApply(target);
    }

    public override void OnRemove(IEffectTarget<UnitSystem> target)
    {
        if (target is UnitSystem unit)
            unit.OnEffectControlRemoved();
        base.OnRemove(target);
    }
}

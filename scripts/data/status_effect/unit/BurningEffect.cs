using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.Data;

public sealed class BurningEffect(int duration)
    : StatusEffect<UnitSystem>("Burning", "BurningEffect", duration, false)
{
    public override void OnApply(IEffectTarget<UnitSystem> target)
    {
        GD.Print($"Applying BurningEffect on {target}");
        base.OnApply(target);
    }

    public override void OnTurnPassed(IEffectTarget<UnitSystem> target)
    {
        if (target is UnitSystem unit)
            unit.OnEffectDamage(DataStructures.ModifierType.Flat, 10);
        base.OnTurnPassed(target);
    }

    public override void OnRemove(IEffectTarget<UnitSystem> target)
    {
        GD.Print($"Applying BurningEffect on {target}");
        base.OnRemove(target);
    }
}

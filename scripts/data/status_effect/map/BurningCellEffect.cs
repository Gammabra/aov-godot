using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.Data;

public sealed class BurningCellEffect : StatusEffect<MapSystem>
{
    public BurningCellEffect(int duration)
        : base("BurningCell", "BurningCellEffect", duration, false)
    {
        EffectToSpread = new BurningEffect(2);
    }
}

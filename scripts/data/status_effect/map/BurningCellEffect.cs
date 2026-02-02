using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.Data;

public sealed class BurningCellEffect : StatusEffect<CellInformation>
{
    public BurningCellEffect(int duration)
        : base("BurningCell", "BurningCellEffect", duration, false)
    {
        EffectToSpread = new BurningEffect(2);
    }
}

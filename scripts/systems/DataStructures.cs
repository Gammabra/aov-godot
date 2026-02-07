namespace AshesOfVelsingrad.Systems;

public sealed class DataStructures
{
    public enum EffectType
    {
        Damage,
        Heal,
        Revive,
        ApplyModifier,
        RemoveModifier,
        Control,
        Summon
    }

    public enum StatTypeWithModifier
    {
        Atk,
        Def
    }

    public enum ModifierType
    {
        Flat,
        Percent
    }
}

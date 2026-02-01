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
        ResourceChange,
        Summon
    }

    public enum StatType
    {
        Hp,
        MaxHp,
        Atk,
        Def,
        Speed,
        Intelligence,
        Mana,
        Curse
    }

    public enum ModifierType
    {
        Flat,
        Percent
    }

    public record EffectPayload
    {
        public EffectType EffectType;
        public StatType? StatType;
        public ModifierType? ModifierType;
        public float Value;
    }
}

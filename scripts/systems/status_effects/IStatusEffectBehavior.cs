namespace AshesOfVelsingrad.Systems;

public interface IStatusEffectBehavior
{
    void OnEffectDamage(DataStructures.ModifierType modifierType, float amount);
    void OnEffectHeal(float amount);
    void OnEffectRevive(DataStructures.ModifierType modifierType, float amount);

    void OnEffectModifierApplied(
        DataStructures.StatTypeWithModifier statTypeWithModifier,
        DataStructures.ModifierType modifierType,
        float amount
    );

    void OnEffectModifierRemoved(
        DataStructures.StatTypeWithModifier statTypeWithModifier,
        DataStructures.ModifierType modifierType,
        float amount
    );

    void OnEffectControlApplied();
    void OnEffectControlRemoved();
}

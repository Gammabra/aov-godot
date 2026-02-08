using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Systems;

public interface IStatusEffectBehavior
{
    void OnEffectDamage(AovDataStructures.ModifierType modifierType, float amount);
    void OnEffectHeal(float amount);
    void OnEffectRevive(AovDataStructures.ModifierType modifierType, float amount);

    void OnEffectModifierApplied(
        AovDataStructures.StatTypeWithModifier statTypeWithModifier,
        AovDataStructures.ModifierType modifierType,
        float amount
    );

    void OnEffectModifierRemoved(
        AovDataStructures.StatTypeWithModifier statTypeWithModifier,
        AovDataStructures.ModifierType modifierType,
        float amount
    );

    void OnEffectControlApplied();
    void OnEffectControlRemoved();
}

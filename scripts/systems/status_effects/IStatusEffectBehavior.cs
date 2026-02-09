using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Defines the behavior contract for status effects applied to an entity.
/// </summary>
/// <remarks>
///     <para>
///     <see cref="IStatusEffectBehavior"/> is implemented by classes that react to
///     status effect events such as damage, healing, stat modifiers, control effects,
///     or revival.
///     </para>
///     <para>
///     Each method is called by the status effect or combat system when a specific
///     effect-related event occurs, allowing custom logic to be executed
///     (visual feedback, stat updates, triggers, etc.).
///     </para>
/// </remarks>
public interface IStatusEffectBehavior
{
    /// <summary>
    ///     Called when a damage effect is applied to the entity.
    /// </summary>
    /// <param name="modifierType">
    ///     The type of modifier responsible for the damage (e.g. Flat, Percent).
    /// </param>
    /// <param name="amount">
    ///     The amount of damage applied.
    /// </param>
    void OnEffectDamage(
        AovDataStructures.ModifierType modifierType,
        float amount
    );

    /// <summary>
    ///     Called when a healing effect is applied to the entity.
    /// </summary>
    /// <param name="amount">
    ///     The amount of health restored.
    /// </param>
    void OnEffectHeal(float amount);

    /// <summary>
    ///     Called when a revive effect is applied to the entity.
    /// </summary>
    /// <param name="modifierType">
    ///     The type of modifier responsible for the revive effect.
    /// </param>
    /// <param name="amount">
    ///     The amount of health restored upon revival.
    /// </param>
    void OnEffectRevive(
        AovDataStructures.ModifierType modifierType,
        float amount
    );

    /// <summary>
    ///     Called when a stat modifier effect is applied to the entity.
    /// </summary>
    /// <param name="statTypeWithModifier">
    ///     The affected stat.
    /// </param>
    /// <param name="modifierType">
    ///     The type of modifier applied.
    /// </param>
    /// <param name="amount">
    ///     The value of the applied modifier.
    /// </param>
    void OnEffectModifierApplied(
        AovDataStructures.StatTypeWithModifier statTypeWithModifier,
        AovDataStructures.ModifierType modifierType,
        float amount
    );

    /// <summary>
    ///     Called when a stat modifier effect is removed from the entity.
    /// </summary>
    /// <param name="statTypeWithModifier">
    ///     The affected stat.
    /// </param>
    /// <param name="modifierType">
    ///     The type of modifier being removed.
    /// </param>
    /// <param name="amount">
    ///     The value of the removed modifier.
    /// </param>
    void OnEffectModifierRemoved(
        AovDataStructures.StatTypeWithModifier statTypeWithModifier,
        AovDataStructures.ModifierType modifierType,
        float amount
    );

    /// <summary>
    ///     Called when a control effect (e.g. stun, silence, root) is applied to the entity.
    /// </summary>
    void OnEffectControlApplied();

    /// <summary>
    ///     Called when a control effect is removed from the entity.
    /// </summary>
    void OnEffectControlRemoved();
}

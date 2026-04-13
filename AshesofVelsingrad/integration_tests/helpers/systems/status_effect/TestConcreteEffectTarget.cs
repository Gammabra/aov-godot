using AshesOfVelsingrad.Systems;

namespace AshesOfVelsingrad.Helpers.Systems;

/// <summary>
/// Test double for EffectTarget{TTarget}.
/// Allows verifying that effects are added/removed correctly.
/// </summary>
public class TestConcreteEffectTarget<TTarget> : EffectTarget<TTarget>
{
    public bool ApplyCalled { get; private set; }
    public bool RemoveCalled { get; private set; }

    public StatusEffect<TTarget>? LastAppliedEffect { get; private set; }
    public StatusEffect<TTarget>? LastRemovedEffect { get; private set; }

    public override void ApplyEffect(StatusEffect<TTarget> statusEffect)
    {
        ApplyCalled = true;
        LastAppliedEffect = statusEffect;

        base.ApplyEffect(statusEffect);
    }

    public override void RemoveEffect(StatusEffect<TTarget> statusEffect)
    {
        RemoveCalled = true;
        LastRemovedEffect = statusEffect;

        base.RemoveEffect(statusEffect);
    }

    public void ResetFlags()
    {
        ApplyCalled = false;
        RemoveCalled = false;
        LastAppliedEffect = null;
        LastRemovedEffect = null;
    }
}

using AshesOfVelsingrad.systems.Interfaces;

namespace AshesOfVelsingrad.systems;

public abstract class StatusEffectSystem
{
    public string Name { get; protected set; }
    public string Description { get; protected set; }
    public int Duration { get; protected set; }
    public int StackCount { get; protected set; } = 1;

    public virtual bool IsStackable => false;

    public virtual void OnApply(IEffectTarget target)
    {
    }

    public virtual void OnRemove(IEffectTarget target)
    {
    }

    public virtual void OnTurnPassed(IEffectTarget target)
    {
        Duration--;
        if (Duration == 0) target.RemoveEffect(this);
    }

    public virtual void AddStack()
    {
        if (IsStackable) StackCount++;
    }
}

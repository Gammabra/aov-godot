using System.Collections.Generic;

namespace AshesOfVelsingrad.systems.Interfaces;

public interface IEffectTarget
{
    void ApplyEffect(StatusEffectSystem statusEffectSystem);
    void RemoveEffect(StatusEffectSystem statusEffectSystem);
    List<StatusEffectSystem> GetActiveEffects();
}

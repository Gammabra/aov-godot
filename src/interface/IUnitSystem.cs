using System.Collections.Generic;
using System.Threading.Tasks;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Systems;

public interface IUnitSystem : IEffectTarget<IUnitSystem>, IStatusEffectBehavior
{
    // var from UnitStystem
    string UnitName { get; }
    string Description { get; }

    // var from UnitStystemMovements
    int PossibleMovesRange { get; }

    // var from UnitStystemStats
    float Hp { get; }
    float MaxHp { get; }
    float BaseAtk { get; }
    float BaseDef { get; }
    float BaseSpeed { get; }
    float Intelligence { get; }
    float ManaMax { get; }
    float Mana { get; }
    float Curse { get; }
    bool IsAlive { get; }
    AIPersonality Personality { get; }
    List<ISkillSystem> ActiveSkills { get; }

    //var from UnitSystemStatusEffectImplementation
    float TotalAtk { get; }
    float TotalDef { get; }
    bool IsControlled { get; }

    // Methods from IUnitSystem
    Task WaitForActionAsync();
    void PassTurn();

    // Methods from UnitSystemMovements
    List<(int, int,int)> GetPossibleMoves(IMapSystem map);
    void SetGridPosition(int x, int y, int z, IMapSystem map);
    bool CanMoveTo(int x, int y, int z, IMapSystem map);
    bool MoveTo(int x, int y, int z, IMapSystem map);

    // Methods from UnitSystemSkills
    List<(int, int, int)> GetReachableCellsForSkills(IMapSystem map, ISkillSystem skill);
    void Play(List<IUnitSystem> targets, IMapSystem? map, ISkillSystem skill);

    // Methods from UnitSystemStats
    void SetIsAlive(bool isAlive);
    void TakeDamage(float damage);
    void BypassDamage(float damage);

    // Methods from UnitSystemStatusEffectImplementation
    void OnEffectHeal(float amount);
    void OnEffectControlApplied();
    void OnEffectControlRemoved();
    void OnEffectDamage(AovDataStructures.ModifierType modifierType, float amount);
    void OnEffectModifierApplied(
        AovDataStructures.StatTypeWithModifier statType,
        AovDataStructures.ModifierType modifierType,
        float amount
    );
    void OnEffectModifierRemoved(
        AovDataStructures.StatTypeWithModifier statType,
        AovDataStructures.ModifierType modifierType,
        float amount
    );
    void InjectDependencies(StatusEffectSystem statusEffectSystem);
    List<StatusEffect<IUnitSystem>> GetActiveEffects();
    void RemoveEffect(StatusEffect<IUnitSystem> statusEffect);
    void OnEffectRevive(AovDataStructures.ModifierType modifierType, float amount);
    void SetStatusEffectOnUnit(StatusEffect<IUnitSystem> statusEffect);
}
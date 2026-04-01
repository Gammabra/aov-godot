// using System;
// using System.Collections.Generic;
// using System.Reflection;
// using AshesOfVelsingrad.Managers;
// using AshesOfVelsingrad.Systems;
// using AshesOfVelsingrad.Utilities;
// using Godot;

// namespace UnitTests;

// public partial class TestConcreteGameManager : GameManager
// {
//     public bool IsInitialized { get; private set; }
//     private List<IUnitSystem> _playerUnits = new();
//     private List<IUnitSystem> _enemyUnits = new();
//     private bool _unitMoved;

//     public int PlayerUnitsCount => _playerUnits.Count;
//     public int EnemyUnitsCount => _enemyUnits.Count;
//     public bool UnitMoved => _unitMoved;
//     public (int, int, int)? LastMovedToPosition { get; private set; }
//     public ISkillSystem? LastUsedSkill { get; private set; }
//     public IUnitSystem? LastSkillTarget { get; private set; }
//     public IUnitSystem? LastSkillSource { get; private set; }

//     public void SetNodePaths(
//         NodePath playerUnitsPath,
//         NodePath enemyUnitsPath,
//         NodePath mapSystemPath,
//         NodePath turnManagerPath,
//         NodePath battleInputSystemPath)
//     {
//         // Use reflection to set private fields
//         var playerField = typeof(GameManager).GetField("_playerUnitsPath",
//             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//         playerField?.SetValue(this, playerUnitsPath);

//         var enemyField = typeof(GameManager).GetField("_enemyUnitsPath",
//             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//         enemyField?.SetValue(this, enemyUnitsPath);

//         var mapField = typeof(GameManager).GetField("_mapSystemPath",
//             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//         mapField?.SetValue(this, mapSystemPath);

//         var turnField = typeof(GameManager).GetField("_turnManagerPath",
//             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//         turnField?.SetValue(this, turnManagerPath);

//         var inputField = typeof(GameManager).GetField("_battleInputSystemPath",
//             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//         inputField?.SetValue(this, battleInputSystemPath);
//     }

//     public void CallInitialize()
//     {
//         // Call protected Initialize method
//         var method = typeof(GameManager).GetMethod("Initialize",
//             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//         method?.Invoke(this, null);

//         IsInitialized = true;

//         // Capture unit lists using reflection
//         var playerUnitsField = typeof(GameManager).GetField("_playerUnits",
//             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//         _playerUnits = (List<IUnitSystem>)playerUnitsField?.GetValue(this)!;

//         var enemyUnitsField = typeof(GameManager).GetField("_enemyUnits",
//             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//         _enemyUnits = (List<IUnitSystem>)enemyUnitsField?.GetValue(this)!;
//     }

//     public IUnitSystem GetPlayerUnit(int index)
//     {
//         if (_playerUnits == null || index >= _playerUnits.Count)
//             throw new InvalidOperationException($"Cannot get player unit at index {index}. Count: {_playerUnits?.Count ?? 0}");
//         return _playerUnits[index];
//     }

//     public IUnitSystem GetEnemyUnit(int index)
//     {
//         if (_enemyUnits == null || index >= _enemyUnits.Count)
//             throw new InvalidOperationException($"Cannot get enemy unit at index {index}. Count: {_enemyUnits?.Count ?? 0}");
//         return _enemyUnits[index];
//     }

//     public void ClearMapSystem()
//     {
//         var field = typeof(GameManager).GetField("_mapSystemContainer",
//             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//         field?.SetValue(this, null);
//     }

//     public void ClearTurnManager()
//     {
//         var field = typeof(GameManager).GetField("_turnManagerContainer",
//             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//         field?.SetValue(this, null);
//     }

//     // Override MoveUnit to track if unit moved
//     public override void MoveUnit((int, int, int) cell)
//     {
//         LastMovedToPosition = cell;
//         base.MoveUnit(cell);

//         var field = typeof(GameManager).GetField("_unitMoved",
//             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//         _unitMoved = (bool)field?.GetValue(this)!;
//     }

//     public void SetCurrentUnitPossibleMoves(List<Vector3I> moves)
//     {
//         var field = typeof(GameManager).GetField("_currentUnitPossibleMoves",
//             BindingFlags.NonPublic | BindingFlags.Instance);
//         field?.SetValue(this, moves);
//     }

//     public int GetCurrentUnitPossibleMovesCount()
//     {
//         var field = typeof(GameManager).GetField("_currentUnitPossibleMoves",
//             BindingFlags.NonPublic | BindingFlags.Instance);
//         var moves = (List<Vector3I>)field?.GetValue(this)!;
//         return moves?.Count ?? 0;
//     }

//     public void SetSelectedSkill(ISkillSystem skill)
//     {
//         var field = typeof(GameManager).GetField("_selectedSkill",
//             BindingFlags.NonPublic | BindingFlags.Instance);
//         field?.SetValue(this, skill);
//     }

//     public void SetCurrentUnitReachableCells(List<Vector3I> cells)
//     {
//         var field = typeof(GameManager).GetField("_currentUnitReachableCellsForCurrentSelectedSkill",
//             BindingFlags.NonPublic | BindingFlags.Instance);
//         field?.SetValue(this, cells);
//     }

//     public void CallHandlePlayerUnitMove(Vector3I cell)
//     {
//         var method = typeof(GameManager).GetMethod("HandlePlayerUnitMove",
//             BindingFlags.NonPublic | BindingFlags.Instance);
//         method?.Invoke(this, new object[] { cell });
//     }

//     public void CallHandlePlayerSelectTarget(Vector3I cell)
//     {
//         var method = typeof(GameManager).GetMethod("HandlePlayerSelectTarget",
//             BindingFlags.NonPublic | BindingFlags.Instance);
//         method?.Invoke(this, new object[] { cell });
//     }

//     public void CallCheckUnitsLife(List<IUnitSystem> units)
//     {
//         var method = typeof(GameManager).GetMethod("CheckUnitsLife",
//             BindingFlags.NonPublic | BindingFlags.Instance);
//         method?.Invoke(this, new object[] { units });
//     }

//     public void CallCheckWinLoseCondition()
//     {
//         var method = typeof(GameManager).GetMethod("CheckWinLoseCondition",
//             BindingFlags.NonPublic | BindingFlags.Instance);
//         method?.Invoke(this, null);
//     }

//     public void CallCheckUnitTurnEnd()
//     {
//         var method = typeof(GameManager).GetMethod("CheckUnitTurnEnd",
//             BindingFlags.NonPublic | BindingFlags.Instance);
//         method?.Invoke(this, null);
//     }

//     public AovDataStructures.GameOutcome GetGameOutcome()
//     {
//         var field = typeof(GameManager).GetField("_gameOutcome",
//             BindingFlags.NonPublic | BindingFlags.Instance);
//         return (AovDataStructures.GameOutcome)field?.GetValue(this)!;
//     }

//     public List<IUnitSystem> GetPlayerUnitsList()
//     {
//         return _playerUnits;
//     }

//     public List<IUnitSystem> GetEnemyUnitsList()
//     {
//         return _enemyUnits;
//     }

//     // Override or add the UseSkill method
//     public override void UseSkill(IUnitSystem source, IUnitSystem target, ISkillSystem skill)
//     {
//         LastSkillSource = source;
//         LastSkillTarget = target;
//         LastUsedSkill = skill;
//         // Don't call base - just record the call for test verification
//         base.UseSkill(source, target, skill);
//     }

//     // Method to call base GameManager.UseSkill via reflection
//     public void CallUseSkill(IUnitSystem source, IUnitSystem target, ISkillSystem skill)
//     {
//         var method = typeof(GameManager).GetMethod("UseSkill",
//             BindingFlags.Public | BindingFlags.Instance);
//         method?.Invoke(this, new object[] { source, target, skill });
//     }
// }

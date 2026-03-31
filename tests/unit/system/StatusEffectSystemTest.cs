// using System;
// using System.Collections.Generic;
// using AshesOfVelsingrad.Systems;
// using GdUnit4;
// using Godot;
// using static GdUnit4.Assertions;

// namespace UnitTests;

// [TestSuite]
// [RequireGodotRuntime]
// public class StatusEffectSystemTest
// {
//     private Node? _root;
//     private readonly List<Node> _testNodes = new();

//     private T AddNode<T>(T node)
//         where T : Node
//     {
//         if (_root == null)
//             throw new InvalidOperationException("Root is not initialized.");

//         _root.AddChild(node);
//         _testNodes.Add(node);
//         return node;
//     }

//     [BeforeTest]
//     public void Setup()
//     {
//         _root = new Node { Name = "TestRoot" };
//         ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
//         _testNodes.Clear();
//         _testNodes.Add(_root);
//     }

//     [AfterTest]
//     public void Cleanup()
//     {
//         foreach (Node node in _testNodes)
//             node.QueueFree();
//         _testNodes.Clear();
//     }

//     // =====================================================
//     //  APPLY EFFECT
//     // =====================================================

//     [TestCase]
//     public void ApplyEffect_AddsTarget_AndEffect()
//     {
//         StatusEffectSystem sys = new();
//         TestConcreteEffectTarget<object> target = new();
//         TestConcreteStatusEffect<object> effect = new(duration: 2);

//         sys.ApplyEffect(target, effect);

//         AssertThat(target.GetActiveEffects().Count).IsEqual(1);
//         AssertThat(target.ApplyCalled).IsTrue();
//     }

//     [TestCase]
//     public void ApplyEffect_Stacks_WhenStackable()
//     {
//         StatusEffectSystem sys = new();
//         TestConcreteEffectTarget<IUnitSystem> target = new();
//         TestConcreteStatusEffect<IUnitSystem> effect1 = new(duration: 1, isStackable: true);
//         TestConcreteStatusEffect<IUnitSystem> effect2 = new(duration: 3, isStackable: true);

//         sys.ApplyEffect(target, effect1);
//         sys.ApplyEffect(target, effect2);

//         AssertThat(target.GetActiveEffects().Count).IsEqual(1);
//         AssertThat(target.GetActiveEffects()[0].StackCount).IsEqual(2);
//         AssertThat(target.GetActiveEffects()[0].Duration).IsEqual(3);
//     }

//     [TestCase]
//     public void ApplyEffect_DoesNotStack_WhenNotStackable()
//     {
//         StatusEffectSystem sys = new();
//         TestConcreteEffectTarget<object> target = new();
//         TestConcreteStatusEffect<object> e1 = new(duration: 2, isStackable: false);
//         TestConcreteStatusEffect<object> e2 = new(duration: 1, isStackable: false);

//         sys.ApplyEffect(target, e1);
//         sys.ApplyEffect(target, e2);

//         AssertThat(target.GetActiveEffects().Count).IsEqual(1);
//         AssertThat(target.GetActiveEffects()[0].StackCount).IsEqual(1);
//         AssertThat(target.GetActiveEffects()[0].Duration).IsEqual(2);
//     }

//     // =====================================================
//     //  PROCESS TARGET TURN END  (IUnitSystem only)
//     // =====================================================

//     [TestCase]
//     public void ProcessTargetTurnEnd_ProcessesUnitSystemEffects()
//     {
//         StatusEffectSystem sys = new();
//         TestConcreteEffectTarget<IUnitSystem> unitTarget = new();
//         TestConcreteStatusEffect<IUnitSystem> effect = new(duration: 2);

//         sys.ApplyEffect(unitTarget, effect);

//         sys.ProcessUnitTurnEnd(unitTarget);

//         AssertThat(effect.Duration).IsEqual(1);
//         AssertThat(effect.TurnPassedCalled).IsTrue();
//     }

//     [TestCase]
//     public void ProcessTargetTurnEnd_RemovesExpiredEffect_AndUntracksTarget()
//     {
//         StatusEffectSystem sys = new();
//         TestConcreteEffectTarget<IUnitSystem> unitTarget = new();
//         TestConcreteStatusEffect<IUnitSystem> effect = new(duration: 1);

//         sys.ApplyEffect(unitTarget, effect);

//         sys.ProcessUnitTurnEnd(unitTarget); // Duration -> 0
//         sys.ProcessUnitTurnEnd(unitTarget); // Should remove

//         AssertThat(unitTarget.GetActiveEffects().Count).IsEqual(0);
//     }

//     [TestCase]
//     public void ProcessTargetTurnEnd_IgnoresNonUnitSystemEffects()
//     {
//         StatusEffectSystem sys = new();
//         TestConcreteEffectTarget<object> target = new();
//         TestConcreteStatusEffect<object> effect = new(duration: 2);

//         sys.ApplyEffect(target, effect);

//         // Should do nothing because type mismatch
//         sys.ProcessUnitTurnEnd(target as IEffectTarget<IUnitSystem>);

//         AssertThat(effect.Duration).IsEqual(2);
//         AssertThat(effect.TurnPassedCalled).IsFalse();
//     }

//     // =====================================================
//     //  PROCESS TURN END (IMapSystem / CellInformation only)
//     // =====================================================

//     [TestCase]
//     public void ProcessTurnEnd_ProcessesOnlyCellInformationTargets()
//     {
//         StatusEffectSystem sys = new();

//         // Cell target : OK
//         TestConcreteEffectTarget<CellInformation> cellTarget = new();
//         TestConcreteStatusEffect<CellInformation> cellEffect = new(duration: 1);
//         sys.ApplyEffect(cellTarget, cellEffect);

//         // Unit target : should NOT be processed
//         TestConcreteUnitSystem unitTarget = AddNode(new TestConcreteUnitSystem());
//         TestConcreteStatusEffect<IUnitSystem> unitEffect = new(duration: 1);
//         sys.ApplyEffect(unitTarget, unitEffect);

//         // Run map processing
//         sys.ProcessTurnEnd();

//         // Cell effect must have ticked
//         AssertThat(cellEffect.Duration).IsEqual(0);
//         AssertThat(cellEffect.TurnPassedCalled).IsTrue();

//         // Unit effect should be untouched
//         AssertThat(unitEffect.Duration).IsEqual(1);
//         AssertThat(unitEffect.TurnPassedCalled).IsFalse();
//     }

//     [TestCase]
//     public void ProcessTurnEnd_RemovesExpiredEffects_AndUntracksCellTarget()
//     {
//         StatusEffectSystem sys = new();

//         TestConcreteEffectTarget<CellInformation> cellTarget = new();
//         TestConcreteStatusEffect<CellInformation> effect = new(duration: 1);

//         sys.ApplyEffect(cellTarget, effect);

//         // Process first turn → effect duration becomes 0
//         sys.ProcessTurnEnd();

//         // Process second turn → effect should be removed and target untracked
//         sys.ProcessTurnEnd();

//         AssertThat(cellTarget.GetActiveEffects().Count).IsEqual(0);
//     }
// }

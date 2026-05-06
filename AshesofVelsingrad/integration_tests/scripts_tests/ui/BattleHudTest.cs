using System;
using System.Collections.Generic;
using AshesOfVelsingrad.UI.Hud;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.UI;

/// <summary>
///     Coverage for <see cref="BattleHud" /> — the programmatic HUD root.
/// </summary>
/// <remarks>
///     <para>
///         Verifies the contract <c>GameManager.EnsureHud</c> relies on:
///         <list type="bullet">
///             <item><description><c>Build()</c> creates every child widget exactly once.</description></item>
///             <item><description><c>Build()</c> is idempotent — calling it twice (or before <c>_Ready</c> fires) doesn't double-spawn.</description></item>
///             <item><description><c>Build()</c> pins <c>Layer = HudLayer</c> and <c>Visible = true</c>.</description></item>
///             <item><description>Each child widget's own <c>_built</c> flag is set, so subsequent calls are no-ops.</description></item>
///         </list>
///     </para>
/// </remarks>
[TestSuite]
[RequireGodotRuntime]
public class BattleHudTest
{
    private Node? _root;
    private readonly List<Node> _testNodes = new();

    private T AddNode<T>(T node) where T : Node
    {
        if (_root == null)
            throw new InvalidOperationException("Root is not initialized.");
        _root.AddChild(node);
        _testNodes.Add(node);
        return node;
    }

    [BeforeTest]
    public void Setup()
    {
        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Clear();
        _testNodes.Add(_root);
    }

    [AfterTest]
    public void Cleanup()
    {
        foreach (Node node in _testNodes)
            node.QueueFree();
        _testNodes.Clear();
    }

    [TestCase]
    public void Build_CreatesAllSevenChildWidgets()
    {
        BattleHud hud = AddNode(new BattleHud { Name = "BattleHud" });

        hud.Build();

        AssertThat(hud.ActionMenu).IsNotNull();
        AssertThat(hud.SkillSelector).IsNotNull();
        AssertThat(hud.PlayerStatus).IsNotNull();
        AssertThat(hud.EnemyRoster).IsNotNull();
        AssertThat(hud.TurnQueue).IsNotNull();
        AssertThat(hud.ContextInfo).IsNotNull();
        AssertThat(hud.Log).IsNotNull();
        AssertThat(hud.GetChildCount()).IsEqual(7);
    }

    [TestCase]
    public void Build_PinsLayerAndVisibility()
    {
        BattleHud hud = AddNode(new BattleHud { Name = "BattleHud" });

        hud.Build();

        AssertThat(hud.Layer).IsEqual(BattleHud.HudLayer);
        AssertThat(hud.Visible).IsTrue();
    }

    [TestCase]
    public void Build_IsIdempotent_DoesNotDoubleSpawnChildren()
    {
        BattleHud hud = AddNode(new BattleHud { Name = "BattleHud" });

        hud.Build();
        int afterFirstBuild = hud.GetChildCount();

        hud.Build();
        int afterSecondBuild = hud.GetChildCount();

        AssertThat(afterSecondBuild).IsEqual(afterFirstBuild);
        AssertThat(afterSecondBuild).IsEqual(7);
    }

    [TestCase]
    public void Build_BeforeReadyFires_SafeToCallSynchronously()
    {
        // Reproduces the GameManager.EnsureHud pattern: build BEFORE the BattleHud's
        // own _Ready fires. Without the EnsureBuilt-cascade, calling RefreshHudForActiveUnit
        // immediately after AddChild would NRE because PlayerStatus is null. With it, the
        // child references are populated synchronously and Bind() is safe.
        BattleHud hud = new() { Name = "BattleHud" };
        // Add to tree but do NOT wait a frame; emulate "_Ready never fires" worst case.
        _root!.AddChild(hud);
        _testNodes.Add(hud);

        hud.Build();

        AssertThat(hud.PlayerStatus).IsNotNull();
        AssertThat(hud.SkillSelector).IsNotNull();
        AssertThat(hud.ActionMenu).IsNotNull();
    }
}

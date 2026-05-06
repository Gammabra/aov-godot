using System;
using System.Collections.Generic;
using AshesOfVelsingrad.UI.Hud;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.UI;

/// <summary>
///     Coverage for <see cref="GameOverScreen" /> — the Defeat overlay shown when the
///     entire player party falls. Specifically tests the spam-click guard added after
///     the user reported triple-firing the <em>Try Again</em> handler. Without it,
///     a fast triple-click queued three <c>ReloadCurrentScene</c> calls in a row.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class GameOverScreenTest
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
    public void EnsureBuilt_PinsLayerAboveHud()
    {
        GameOverScreen screen = AddNode(new GameOverScreen { Name = "GameOverScreen" });
        screen.EnsureBuilt();

        AssertThat(screen.Layer).IsEqual(GameOverScreen.GameOverLayer);
        AssertThat(screen.Layer > BattleHud.HudLayer).IsTrue();
        AssertThat(screen.Visible).IsTrue();
    }

    [TestCase]
    public void TryAgain_FiresOnceEvenWhenInvokedMultipleTimes()
    {
        // Regression for the bug where pressing Try Again multiple times queued
        // multiple ReloadCurrentScene calls. Use reflection to invoke the private
        // FireTryAgain handler directly so we don't have to simulate Godot button presses.
        GameOverScreen screen = AddNode(new GameOverScreen { Name = "GameOverScreen" });
        screen.EnsureBuilt();

        int handlerCallCount = 0;
        screen.OnTryAgainPressed += () => handlerCallCount++;

        InvokePrivate(screen, "FireTryAgain");
        InvokePrivate(screen, "FireTryAgain");
        InvokePrivate(screen, "FireTryAgain");

        AssertThat(handlerCallCount).IsEqual(1);
    }

    [TestCase]
    public void TryAgain_AfterFirstClick_DisablesBothButtons()
    {
        // Verify the visual side of the guard: both buttons go Disabled = true after
        // the first action, so the user gets feedback that their click landed.
        GameOverScreen screen = AddNode(new GameOverScreen { Name = "GameOverScreen" });
        screen.EnsureBuilt();
        screen.OnTryAgainPressed += () => { /* swallow */ };

        InvokePrivate(screen, "FireTryAgain");

        Button? tryAgainButton = GetPrivateField<Button>(screen, "_tryAgainButton");
        Button? forfeitButton = GetPrivateField<Button>(screen, "_forfeitButton");

        AssertThat(tryAgainButton).IsNotNull();
        AssertThat(forfeitButton).IsNotNull();
        AssertThat(tryAgainButton!.Disabled).IsTrue();
        AssertThat(forfeitButton!.Disabled).IsTrue();
    }

    [TestCase]
    public void Forfeit_FiresOnceAndBlocksTryAgain()
    {
        // Mirror test of the spam guard for the Forfeit path.
        GameOverScreen screen = AddNode(new GameOverScreen { Name = "GameOverScreen" });
        screen.EnsureBuilt();

        int forfeitCount = 0;
        int tryAgainCount = 0;
        screen.OnForfeitPressed += () => forfeitCount++;
        screen.OnTryAgainPressed += () => tryAgainCount++;

        InvokePrivate(screen, "FireForfeit");
        InvokePrivate(screen, "FireTryAgain"); // should be ignored — guard is shared
        InvokePrivate(screen, "FireForfeit");

        AssertThat(forfeitCount).IsEqual(1);
        AssertThat(tryAgainCount).IsEqual(0);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        target.GetType()
            .GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(target, null);
    }

    private static T? GetPrivateField<T>(object target, string fieldName) where T : class
    {
        return target.GetType()
            .GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(target) as T;
    }
}

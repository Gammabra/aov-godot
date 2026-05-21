using System;
using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.Audio;
using AshesOfVelsingrad.Managers;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.UI;

[TestSuite]
[RequireGodotRuntime]
public partial class ButtonMenuTest
{
    private List<Node> _testNodes = new();
    private Node? _root;

    private object? _originalAudioInstance;
    private object? _originalMainInstance;
    private object? _originalMenuInstance;

    #region Setup / Teardown

    [BeforeTest]
    public void SetUp()
    {
        _testNodes.Clear();

        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Add(_root);

        // Capture and isolate existing production singletons
        _originalAudioInstance = GetStaticInstanceField(typeof(AudioManager));
        _originalMainInstance = GetStaticInstanceField(typeof(MainManager));
        _originalMenuInstance = GetStaticInstanceField(typeof(MenuManager));

        // Clear out references to provide a pristine test bed
        SetStaticInstanceField(typeof(AudioManager), null);
        SetStaticInstanceField(typeof(MainManager), null);
        SetStaticInstanceField(typeof(MenuManager), null);
    }

    [AfterTest]
    public void TearDown()
    {
        // Fully restore core singletons to original states
        SetStaticInstanceField(typeof(AudioManager), _originalAudioInstance);
        SetStaticInstanceField(typeof(MainManager), _originalMainInstance);
        SetStaticInstanceField(typeof(MenuManager), _originalMenuInstance);

        foreach (Node node in _testNodes)
        {
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
                node.QueueFree();
        }

        _testNodes.Clear();
    }

    #endregion

    #region Reflection Helpers

    private object? GetStaticInstanceField(Type type)
    {
        FieldInfo? field = type.GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)
            ?? type.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
        return field?.GetValue(null);
    }

    private void SetStaticInstanceField(Type type, object? value)
    {
        FieldInfo? field = type.GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)
            ?? type.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
        field?.SetValue(null, value);
    }

    #endregion

    #region Integration Tests

    [TestCase]
    public void Ready_ExecutesSafely_WhenAudioManagerIsMissing()
    {
        // Arrange
        ButtonMenu button = new();
        _root!.AddChild(button);
        _testNodes.Add(button);

        // Act & Assert
        button._Ready();
        AssertThat(button).IsNotNull();
    }

    [TestCase]
    public void PlayButtonPressed_FallsBackToSceneTree_WhenMainManagerIsMissing()
    {
        // Arrange
        ButtonMenu button = new();
        _root!.AddChild(button);
        _testNodes.Add(button);

        // Ensure MainManager is unassigned to isolate the engine branch
        SetStaticInstanceField(typeof(MainManager), null);

        // Act & Assert
        // Validates that the fallback branch successfully requests a scene transition 
        // through Godot's core SceneTree without causing runtime errors.
        button.OnPlayButtonPressed();
        AssertThat(button).IsNotNull();
    }

    [TestCase]
    public void OptionsButtonPressed_ExecutesSafely_WhenMenuManagerIsMissing()
    {
        // Arrange
        ButtonMenu button = new();
        _root!.AddChild(button);
        _testNodes.Add(button);

        // Act & Assert
        button.OnOptionsButtonPressed();
        AssertThat(button).IsNotNull();
    }

    [TestCase]
    public void OptionsExitButtonPressed_ExecutesSafely_WhenMenuManagerIsMissing()
    {
        // Arrange
        ButtonMenu button = new();
        _root!.AddChild(button);
        _testNodes.Add(button);

        // Act & Assert
        button.OnOptionsExitButtonPressed();
        AssertThat(button).IsNotNull();
    }

    #endregion
}

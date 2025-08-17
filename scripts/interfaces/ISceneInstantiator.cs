using Godot;
using System;
using System.Collections.Generic;

namespace AshesOfVelsingrad;

public interface ISceneInstantiator
{
    Node InstantiateMainMenu();
    Node InstantiateOptionsMenu();
}

public class ProductionSceneInstantiator : ISceneInstantiator
{
    private readonly PackedScene _mainMenuScene;
    private readonly PackedScene _optionsMenuScene;

    public ProductionSceneInstantiator(PackedScene mainMenuScene, PackedScene optionsMenuScene)
    {
        _mainMenuScene = mainMenuScene;
        _optionsMenuScene = optionsMenuScene;
    }

    public Node InstantiateMainMenu()
    {
        return _mainMenuScene.Instantiate();
    }

    public Node InstantiateOptionsMenu()
    {
        return _optionsMenuScene.Instantiate();
    }
}

public class TestSceneInstantiator : ISceneInstantiator
{
    public int MainMenuInstantiateCount { get; private set; }
    public int OptionsMenuInstantiateCount { get; private set; }
    private readonly List<Node> _createdNodes = new();

    public Node InstantiateMainMenu()
    {
        MainMenuInstantiateCount++;
        var node = new Control { Name = "TestMainMenu" };
        _createdNodes.Add(node);
        GD.Print($"[TEST] TestSceneInstantiator created MainMenu. Count: {MainMenuInstantiateCount}");
        return node;
    }

    public Node InstantiateOptionsMenu()
    {
        OptionsMenuInstantiateCount++;
        var node = new Control { Name = "TestOptionsMenu" };
        _createdNodes.Add(node);
        GD.Print($"[TEST] TestSceneInstantiator created OptionsMenu. Count: {OptionsMenuInstantiateCount}");
        return node;
    }

    public void FreeAllNodes()
    {
        GD.Print($"[TEST] Freeing {_createdNodes.Count} nodes from TestSceneInstantiator");

        for (int i = _createdNodes.Count - 1; i >= 0; i--)
        {
            var node = _createdNodes[i];
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                // Retirer le nœud de son parent s'il en a un
                if (node.GetParent() != null)
                {
                    node.GetParent().RemoveChild(node);
                }
                node.QueueFree();
                GD.Print($"[TEST] Freed node: {node.Name}");
            }
        }
        _createdNodes.Clear();

        GD.Print("[TEST] TestSceneInstantiator cleanup completed");
    }

    public void Reset()
    {
        FreeAllNodes();
        MainMenuInstantiateCount = 0;
        OptionsMenuInstantiateCount = 0;
    }
}

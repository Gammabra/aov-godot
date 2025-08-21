using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.UI.Menus;
using Godot;
using System.Collections.Generic;

namespace UnitTests;

/// <summary>
/// Version testable de Main qui hérite de la classe de production
/// et override les méthodes nécessaires pour les tests
/// </summary>
public partial class TestableMain : AshesOfVelsingrad.Main
{
    private readonly List<Node> _createdNodes = new();
    public int MainMenuInstantiateCount { get; private set; }
    public int OptionsMenuInstantiateCount { get; private set; }

    /// <summary>
    /// Permet de définir le menu container pour les tests
    /// </summary>
    public void SetMenuContainer(Control? menuContainer)
    {
        _menuContainer = menuContainer;
    }

    /// <summary>
    /// Override la création des menus pour les tests
    /// </summary>
    protected override (Node mainMenu, Node optionsMenu) CreateMenus()
    {
        // Vérifier que le container existe avant de créer les menus (même logique que la classe parente)
        if (_menuContainer == null)
        {
            GD.Print("[TEST] MenuContainer is null, not creating test menus");
            return (null!, null!);
        }

        GD.Print("[TEST] Using test menu creation");

        MainMenuInstantiateCount++;
        var mainMenu = new Control { Name = "TestMainMenu" };
        _createdNodes.Add(mainMenu);
        GD.Print($"[TEST] TestableMain created MainMenu. Count: {MainMenuInstantiateCount}");

        OptionsMenuInstantiateCount++;
        var optionsMenu = new Control { Name = "TestOptionsMenu" };
        _createdNodes.Add(optionsMenu);
        GD.Print($"[TEST] TestableMain created OptionsMenu. Count: {OptionsMenuInstantiateCount}");

        return (mainMenu, optionsMenu);
    }

    /// <summary>
    /// Expose la méthode InitializeMenus pour les tests
    /// </summary>
    public new void InitializeMenus()
    {
        base.InitializeMenus();
    }

    /// <summary>
    /// Nettoie tous les nœuds créés pendant les tests
    /// </summary>
    public void FreeAllCreatedNodes()
    {
        GD.Print($"[TEST] Freeing {_createdNodes.Count} nodes from TestableMain");

        for (int i = _createdNodes.Count - 1; i >= 0; i--)
        {
            var node = _createdNodes[i];
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                if (node.GetParent() != null)
                {
                    node.GetParent().RemoveChild(node);
                }
                node.QueueFree();
                GD.Print($"[TEST] Freed node: {node.Name}");
            }
        }
        _createdNodes.Clear();

        GD.Print("[TEST] TestableMain cleanup completed");
    }

    /// <summary>
    /// Reset les compteurs pour les tests
    /// </summary>
    public void Reset()
    {
        FreeAllCreatedNodes();
        MainMenuInstantiateCount = 0;
        OptionsMenuInstantiateCount = 0;
    }
}

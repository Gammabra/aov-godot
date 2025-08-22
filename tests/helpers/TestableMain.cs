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
    private System.Func<Node, Node?>? _autoFreeCallback; // Callback pour AutoFree - retour nullable
    public int MainMenuInstantiateCount { get; private set; }
    public int OptionsMenuInstantiateCount { get; private set; }

    /// <summary>
    /// Permet de définir le callback AutoFree pour les tests
    /// </summary>
    public void SetAutoFreeCallback(System.Func<Node, Node?> callback)
    {
        _autoFreeCallback = callback;
    }

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
        
        // Utiliser AutoFree si disponible
        if (_autoFreeCallback != null)
        {
            _autoFreeCallback(mainMenu);
        }
        
        _createdNodes.Add(mainMenu);
        GD.Print($"[TEST] TestableMain created MainMenu. Count: {MainMenuInstantiateCount}");

        OptionsMenuInstantiateCount++;
        var optionsMenu = new Control { Name = "TestOptionsMenu" };
        
        // Utiliser AutoFree si disponible
        if (_autoFreeCallback != null)
        {
            _autoFreeCallback(optionsMenu);
        }
        
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
    /// Cette méthode retire les nœuds de leur parent mais ne les libère pas
    /// car ils sont gérés par AutoFree de GdUnit4
    /// </summary>
    public void FreeAllCreatedNodes()
    {
        GD.Print($"[TEST] Cleaning up {_createdNodes.Count} nodes from TestableMain");

        for (int i = _createdNodes.Count - 1; i >= 0; i--)
        {
            var node = _createdNodes[i];
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                // Retirer le nœud de son parent s'il en a un
                if (node.GetParent() != null)
                {
                    node.GetParent().RemoveChild(node);
                    GD.Print($"[TEST] Removed {node.Name} from parent");
                }
            }
        }
        _createdNodes.Clear();

        GD.Print("[TEST] TestableMain cleanup completed");
    }

    /// <summary>
    /// Reset les compteurs et libère les nœuds pour les tests
    /// </summary>
    public void Reset()
    {
        FreeAllCreatedNodes();
        MainMenuInstantiateCount = 0;
        OptionsMenuInstantiateCount = 0;
    }

    /// <summary>
    /// Getter pour accéder aux nœuds créés (utile pour les tests)
    /// </summary>
    public IReadOnlyList<Node> CreatedNodes => _createdNodes;
}
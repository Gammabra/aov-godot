using System.Collections.Generic;
using Godot;

namespace AshesOfVelsingrad.Helpers;

public partial class TestableMain : AshesOfVelsingrad.Main
{
    private readonly List<Node> _createdNodes = new();
    private System.Func<Node, Node?>? _autoFreeCallback;
    public int MainMenuInstantiateCount { get; private set; }
    public int OptionsMenuInstantiateCount { get; private set; }

    public void SetAutoFreeCallback(System.Func<Node, Node?> callback)
    {
        _autoFreeCallback = callback;
    }

    public void SetMenuContainer(Control? menuContainer)
    {
        _menuContainer = menuContainer;
    }

    protected override (Node mainMenu, Node optionsMenu) CreateMenus()
    {
        if (_menuContainer == null)
        {
            GD.Print("[TEST] MenuContainer is null, not creating test menus");
            return (null!, null!);
        }

        GD.Print("[TEST] Using test menu creation");

        MainMenuInstantiateCount++;
        var mainMenu = new Control { Name = "TestMainMenu" };

        if (_autoFreeCallback != null)
        {
            _autoFreeCallback(mainMenu);
        }

        _createdNodes.Add(mainMenu);
        GD.Print($"[TEST] TestableMain created MainMenu. Count: {MainMenuInstantiateCount}");

        OptionsMenuInstantiateCount++;
        var optionsMenu = new Control { Name = "TestOptionsMenu" };

        if (_autoFreeCallback != null)
        {
            _autoFreeCallback(optionsMenu);
        }

        _createdNodes.Add(optionsMenu);
        GD.Print($"[TEST] TestableMain created OptionsMenu. Count: {OptionsMenuInstantiateCount}");

        return (mainMenu, optionsMenu);
    }

    public new void InitializeMenus()
    {
        base.InitializeMenus();
    }

    public void FreeAllCreatedNodes()
    {
        GD.Print($"[TEST] Cleaning up {_createdNodes.Count} nodes from TestableMain");

        for (int i = _createdNodes.Count - 1; i >= 0; i--)
        {
            var node = _createdNodes[i];
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
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

    public void Reset()
    {
        FreeAllCreatedNodes();
        MainMenuInstantiateCount = 0;
        OptionsMenuInstantiateCount = 0;
    }

    public IReadOnlyList<Node> CreatedNodes => _createdNodes;
}

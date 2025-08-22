using Godot;
using System.Collections.Generic;
using AshesOfVelsingrad.Managers;

namespace UnitTests;
public partial class TestMenuManager : MenuManager
{
    public Dictionary<string, Control> RegisteredMenus { get; } = new();
    public string? LastShownMenu { get; private set; }
    public static new TestMenuManager? Instance { get; set; }

    public TestMenuManager()
    {
        Name = "TestMenuManager";
        GD.Print("[TEST] TestMenuManager constructor called");
    }

    protected override void Initialize()
    {
        Instance = this;

        var baseInstanceProperty = typeof(MenuManager).GetProperty("Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        baseInstanceProperty?.SetValue(null, this);

        GD.Print("[TEST] TestMenuManager initialized");
    }

    public override void RegisterMenu(string menuName, Control menuControl)
    {
        RegisteredMenus[menuName] = menuControl;
        GD.Print($"[TEST] TestMenuManager: Registered menu '{menuName}'");

        try
        {
            base.RegisterMenu(menuName, menuControl);
        }
        catch (System.Exception ex)
        {
            GD.Print($"[TEST] Exception in base.RegisterMenu: {ex.Message} - continuing with test");
        }
    }

    public override void ShowMenu(string menuName, bool addToHistory = true)
    {
        LastShownMenu = menuName;
        GD.Print($"[TEST] TestMenuManager: Showed menu '{menuName}'");

        try
        {
            base.ShowMenu(menuName, addToHistory);
        }
        catch (System.Exception ex)
        {
            GD.Print($"[TEST] Exception in base.ShowMenu: {ex.Message} - continuing with test");
        }
    }
}

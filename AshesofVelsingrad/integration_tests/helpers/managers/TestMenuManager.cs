using System.Collections.Generic;
using AshesOfVelsingrad.Managers;
using Godot;

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

        SetInstanceForTesting(this);

        GD.Print($"[TEST] TestMenuManager initialized - MenuManager.Instance: {MenuManager.Instance != null}");
        GD.Print($"[TEST] TestMenuManager initialized - MenuManager.Instance == this: {MenuManager.Instance == this}");
        GD.Print($"[TEST] TestMenuManager initialized - TestMenuManager.Instance: {Instance != null}");
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
        GD.Print($"[TEST] TestMenuManager: Request to show menu '{menuName}' (addToHistory: {addToHistory})");

        try
        {
            if (RegisteredMenus.TryGetValue(menuName, out var menuControl) &&
                IsInstanceValid(menuControl) &&
                !menuControl.IsQueuedForDeletion())
            {
                base.ShowMenu(menuName, addToHistory);
            }
            else
            {
                GD.Print($"[TEST] Skipping ShowMenu for '{menuName}' (menu not registered or invalid)");
            }
        }
        catch (System.Exception ex)
        {
            GD.Print($"[TEST] Exception in TestMenuManager.ShowMenu: {ex.Message} - ignoring in test mode");
        }
    }

    public void VerifyInstanceState()
    {
        GD.Print($"[TEST] VerifyInstanceState - MenuManager.Instance == this: {MenuManager.Instance == this}");
        GD.Print($"[TEST] VerifyInstanceState - TestMenuManager.Instance == this: {Instance == this}");
    }
}

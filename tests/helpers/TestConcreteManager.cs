using AshesOfVelsingrad.Managers;
using Godot;
using System.Reflection;

namespace UnitTests;

public partial class TestConcreteManager : BaseManager
{
    public static new TestConcreteManager? Instance { get; set; }
    public bool IsInitialized { get; private set; }
    public bool IsCleanedUp { get; private set; }
    public int InitializeCallCount { get; private set; }

    public TestConcreteManager()
    {
        Name = "TestConcreteManager";
        GD.Print("[TEST] TestConcreteManager constructor called");
    }

    protected override void Initialize()
    {
        if (Instance != null && Instance != this)
        {
            GD.PrintErr($"Multiple instances of {GetType().Name} detected.");
            QueueFree();
            return;
        }

        BaseManager.Instance = this;
        Instance = this;

        IsInitialized = true;
        InitializeCallCount++;

        GD.Print("[TEST] TestConcreteManager initialized");
    }

    protected override void Cleanup()
    {
        IsCleanedUp = true;

        if (Instance == this)
            Instance = null;

        if (BaseManager.Instance == this)
            BaseManager.Instance = null;

        GD.Print("[TEST] TestConcreteManager cleanup called");
    }

    public void CallInitialize() => Initialize();
    public void CallCleanup() => Cleanup();
    public void CallReady() => _Ready();
    public void CallExitTree() => _ExitTree();
}

public partial class AnotherTestManager : BaseManager
{
    public static new AnotherTestManager? Instance { get; set; }
    public bool IsInitialized { get; private set; }

    public AnotherTestManager()
    {
        Name = "AnotherTestManager";
    }

    protected override void Initialize()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }

        BaseManager.Instance = this;
        Instance = this;
        IsInitialized = true;

        GD.Print("[TEST] AnotherTestManager initialized");
    }

    public void CallInitialize() => Initialize();
}

public static class MethodInfoExtensions
{
    public static bool IsOverride(this MethodInfo method)
    {
        return method.GetBaseDefinition() != method;
    }
}

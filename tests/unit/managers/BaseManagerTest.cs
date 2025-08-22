using AshesOfVelsingrad.Managers;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
using System.Collections.Generic;
using System.Reflection;

namespace UnitTests;

[TestSuite]
[RequireGodotRuntime]
public class BaseManagerTest
{
    private readonly List<Node> _testNodes = new();
    private Node _root;

    [Before]
    public void SetUp()
    {
        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Godot.Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Add(_root);

        ResetAllManagerInstances();
    }

    [TestCase]
    public void BaseManager_IsAbstractClass_CannotBeInstantiated()
    {
        // Test that BaseManager is properly abstract
        var baseManagerType = typeof(BaseManager);
        
        AssertThat(baseManagerType.IsAbstract).IsTrue();
    }

    [TestCase]
    public void BaseManager_HasAbstractInitializeMethod()
    {
        // Verify Initialize method is abstract and must be implemented
        var baseManagerType = typeof(BaseManager);
        var initializeMethod = baseManagerType.GetMethod("Initialize", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        AssertThat(initializeMethod).IsNotNull();
        AssertThat(initializeMethod!.IsAbstract).IsTrue();
    }

    [TestCase]
    public void BaseManager_HasVirtualCleanupMethod()
    {
        // Verify Cleanup method is virtual and can be overridden
        var baseManagerType = typeof(BaseManager);
        var cleanupMethod = baseManagerType.GetMethod("Cleanup", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        AssertThat(cleanupMethod).IsNotNull();
        AssertThat(cleanupMethod!.IsVirtual).IsTrue();
    }

    [TestCase]
    public void BaseManager_HasStaticInstanceProperty()
    {
        // Verify BaseManager has the static Instance property
        var baseManagerType = typeof(BaseManager);
        var instanceProperty = baseManagerType.GetProperty("Instance", 
            BindingFlags.Public | BindingFlags.Static);
        
        AssertThat(instanceProperty).IsNotNull();
        AssertThat(instanceProperty!.PropertyType).IsEqual(typeof(BaseManager));
        AssertThat(instanceProperty.CanRead).IsTrue();
        AssertThat(instanceProperty.CanWrite).IsTrue(); // protected set
    }

    [TestCase]
    public void ConcreteManager_InitializeImplementation_SetsInstance()
    {
        // Act
        var manager = AddToTestScene(new TestConcreteManager());
        _testNodes.Add(manager);

        // Assert
        AssertThat(TestConcreteManager.Instance).IsEqual(manager);
        AssertThat(manager.IsInitialized).IsTrue();
    }

    [TestCase]
    public void ConcreteManager_CleanupImplementation_CanBeOverridden()
    {
        // Test that Cleanup can be overridden and is called
        var manager = AddToTestScene(new TestConcreteManager());
        _testNodes.Add(manager);
        
        manager.CallInitialize();
        AssertThat(manager.IsCleanedUp).IsFalse();
        
        // Act
        manager.CallCleanup();
        
        // Assert
        AssertThat(manager.IsCleanedUp).IsTrue();
    }

    [TestCase]
    public void SingletonPattern_OnlyOneInstanceAllowed()
    {
        var manager = AddToTestScene(new TestConcreteManager());
        manager.CallInitialize();

        var secondManager = AddToTestScene(new TestConcreteManager());
        secondManager.CallInitialize();

        AssertThat(TestConcreteManager.Instance).IsEqual(manager);
        AssertThat(secondManager.IsInitialized).IsFalse();
    }

    [TestCase]
    public void DifferentManagerTypes_CanHaveIndependentInstances()
    {
        // Test that different manager types maintain separate instances
        var concreteManager = AutoFree(new TestConcreteManager());
        var anotherManager = AutoFree(new AnotherTestManager());
        _testNodes.Add(concreteManager);
        _testNodes.Add(anotherManager);
        
        // Initialize both
        concreteManager.CallInitialize();
        anotherManager.CallInitialize();
        
        // Both should be initialized independently
        AssertThat(TestConcreteManager.Instance).IsEqual(concreteManager);
        AssertThat(AnotherTestManager.Instance).IsEqual(anotherManager);
        
        // Verify they're different objects
        AssertThat(TestConcreteManager.Instance).IsNotEqual(AnotherTestManager.Instance);
    }

    [TestCase]
    public void SettingsManager_InheritsFromBaseManager()
    {
        // Test that SettingsManager properly inherits BaseManager functionality
        AssertThat(typeof(SettingsManager).IsSubclassOf(typeof(BaseManager))).IsTrue();
        
        // Test that it has proper singleton behavior by using our test version
        SetSingletonInstance<SettingsManager>(null);
        var settingsManager = AddToTestScene(new TestSettingsManager());
        _testNodes.Add(settingsManager);
        
        // TestSettingsManager constructor should call Initialize
        // Verify it's now the singleton instance
        AssertThat(SettingsManager.Instance).IsNotNull();
        AssertThat(SettingsManager.Instance).IsInstanceOf<SettingsManager>();
    }

    [TestCase]
    public void MenuManager_InheritsFromBaseManager()
    {
        // Test that MenuManager properly inherits BaseManager functionality
        AssertThat(typeof(MenuManager).IsSubclassOf(typeof(BaseManager))).IsTrue();
        
        // Test singleton behavior with test version
        SetSingletonInstance<MenuManager>(null);
        var menuManager = AddToTestScene(new TestMenuManager());
        _testNodes.Add(menuManager);
        
        // TestMenuManager constructor should call Initialize
        AssertThat(MenuManager.Instance).IsNotNull();
        AssertThat(MenuManager.Instance).IsInstanceOf<MenuManager>();
    }

    [TestCase]
    public void BaseManager_ReadyMethod_CallsInitialize()
    {
        var manager = AddToTestScene(new TestConcreteManager());
        _testNodes.Add(manager);

        // Should be initialized now
        AssertThat(TestConcreteManager.Instance).IsEqual(manager);
        AssertThat(manager.IsInitialized).IsTrue();
    }

    [TestCase]
    public async System.Threading.Tasks.Task BaseManager_ExitTreeMethod_CallsCleanup()
    {
        var manager = AddToTestScene(new TestConcreteManager());
        _testNodes.Add(manager);

        manager.CallInitialize();
        AssertThat(manager.IsCleanedUp).IsFalse();

        manager.QueueFree();
        var tree = (SceneTree)Godot.Engine.GetMainLoop();
        await tree.ToSignal(tree, "process_frame"); // let Godot do its thing

        AssertThat(TestConcreteManager.Instance).IsNull();
        AssertThat(manager.IsCleanedUp).IsTrue();
    }

    [TestCase]
    public void BaseManager_PublicInterface_IsCorrect()
    {
        // Verify BaseManager has the expected public interface
        var baseManagerType = typeof(BaseManager);
        
        // Should inherit from Node
        AssertThat(baseManagerType.IsSubclassOf(typeof(Node))).IsTrue();
        
        // Should have _Ready override
        var readyMethod = baseManagerType.GetMethod("_Ready");
        AssertThat(readyMethod).IsNotNull();
        AssertThat(readyMethod!.IsPublic).IsTrue();
        AssertThat(readyMethod.IsOverride()).IsTrue();
        
        // Should have _ExitTree override
        var exitTreeMethod = baseManagerType.GetMethod("_ExitTree");
        AssertThat(exitTreeMethod).IsNotNull();
        AssertThat(exitTreeMethod!.IsPublic).IsTrue();
        AssertThat(exitTreeMethod.IsOverride()).IsTrue();
    }

    // Helper methods
    private T AddToTestScene<T>(T node) where T : Node
    {
        ((SceneTree)Godot.Engine.GetMainLoop()).Root.AddChild(node);
        _testNodes.Add(node);
        return node;
    }

    private void ResetAllManagerInstances()
    {
        TestConcreteManager.Instance = null;
        AnotherTestManager.Instance = null;
        SetSingletonInstance<SettingsManager>(null);
        SetSingletonInstance<MenuManager>(null);
    }

    private void SetSingletonInstance<T>(T? instance) where T : class
    {
        var instanceProperty = typeof(T).GetProperty("Instance",
            BindingFlags.Public | BindingFlags.Static);
        instanceProperty?.SetValue(null, instance);
    }

    [After]
    public void TearDown()
    {
        foreach (var node in _testNodes)
        {
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
            {
                node.QueueFree();
            }
        }
        _testNodes.Clear();
        ResetAllManagerInstances();
    }
}

/// <summary>
/// Concrete implementation of BaseManager for testing purposes
/// </summary>
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
            return;
        }

        // Assign both base and derived Instance
        BaseManager.Instance = this;
        Instance = this;

        IsInitialized = true;
        InitializeCallCount++;

        GD.Print("[TEST] TestConcreteManager initialized");
    }

    protected override void Cleanup()
    {
        IsCleanedUp = true;

        // Clear both references
        if (Instance == this)
            Instance = null;

        if (BaseManager.Instance == this)
            BaseManager.Instance = null;

        GD.Print("[TEST] TestConcreteManager cleanup called");
    }

    // Public methods to test protected functionality
    public void CallInitialize() => Initialize();
    public void CallCleanup() => Cleanup();
    public void CallReady() => _Ready();
    public void CallExitTree() => _ExitTree();
}

/// <summary>
/// Another concrete implementation to test multiple manager types
/// </summary>
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
            return;
        }

        Instance = this;
        IsInitialized = true;
    }

    public void CallInitialize() => Initialize();
}

// Extension method to check if method is override
public static class MethodInfoExtensions
{
    public static bool IsOverride(this MethodInfo method)
    {
        return method.GetBaseDefinition() != method;
    }
}

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
    private Node? _root;

    [BeforeTest]
    public void SetUp()
    {
        // Reset all manager instances first
        ResetAllManagerInstances();

        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Godot.Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Add(_root);
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
        // Arrange & Act
        var manager = CreateAndInitializeManager<TestConcreteManager>();

        // Assert
        AssertThat(TestConcreteManager.Instance).IsEqual(manager);
        AssertThat(manager.IsInitialized).IsTrue();
    }

    [TestCase]
    public void ConcreteManager_CleanupImplementation_CanBeOverridden()
    {
        // Arrange - ensure clean state
        TestConcreteManager.Instance = null;
        SetSingletonInstance<BaseManager>(null);

        var manager = AddToTestRoot(new TestConcreteManager());
        _testNodes.Add(manager);
        manager.CallInitialize(); // Manually initialize

        GD.Print($"[TEST] After initialize - IsInitialized: {manager.IsInitialized}, IsCleanedUp: {manager.IsCleanedUp}");
        AssertThat(manager.IsInitialized).IsTrue();
        AssertThat(manager.IsCleanedUp).IsFalse();

        // Act - manually call cleanup to test the override
        manager.CallCleanup();

        GD.Print($"[TEST] After cleanup - IsCleanedUp: {manager.IsCleanedUp}");
        // Assert
        AssertThat(manager.IsCleanedUp).IsTrue();
    }

    [TestCase]
    public void SingletonPattern_OnlyOneInstanceAllowed()
    {
        // Arrange - ensure clean state
        TestConcreteManager.Instance = null;
        SetSingletonInstance<BaseManager>(null);

        // Act - create and initialize first manager
        var manager = AddToTestRoot(new TestConcreteManager());
        _testNodes.Add(manager);
        manager.CallInitialize();

        GD.Print($"[TEST] First manager - Instance: {TestConcreteManager.Instance}, IsInitialized: {manager.IsInitialized}");
        AssertThat(TestConcreteManager.Instance).IsEqual(manager);
        AssertThat(manager.IsInitialized).IsTrue();

        // Create second manager and try to initialize
        var secondManager = AddToTestRoot(new TestConcreteManager());
        _testNodes.Add(secondManager);

        GD.Print($"[TEST] Before second initialize - QueuedForDeletion: {secondManager.IsQueuedForDeletion()}");
        secondManager.CallInitialize(); // This should queue the manager for deletion
        GD.Print($"[TEST] After second initialize - QueuedForDeletion: {secondManager.IsQueuedForDeletion()}");

        // Assert - singleton should still be the first manager
        AssertThat(TestConcreteManager.Instance).IsEqual(manager);

        // Second manager should be queued for deletion
        AssertThat(secondManager.IsQueuedForDeletion()).IsTrue();
    }

    [TestCase]
    public void DifferentManagerTypes_CanHaveIndependentInstances()
    {
        // Arrange & Act
        var concreteManager = CreateAndInitializeManager<TestConcreteManager>();
        var anotherManager = CreateAndInitializeManager<AnotherTestManager>();

        // Assert - both should be initialized independently
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

        // Test that it has proper singleton behavior
        var settingsManager = CreateAndInitializeManager<TestSettingsManager>();

        // Verify it's now the singleton instance
        AssertThat(SettingsManager.Instance).IsNotNull();
        AssertThat(SettingsManager.Instance).IsInstanceOf<SettingsManager>();
    }

    [TestCase]
    public void MenuManager_InheritsFromBaseManager()
    {
        // Test that MenuManager properly inherits BaseManager functionality
        AssertThat(typeof(MenuManager).IsSubclassOf(typeof(BaseManager))).IsTrue();

        // Test singleton behavior
        var menuManager = CreateAndInitializeManager<TestMenuManager>();

        AssertThat(MenuManager.Instance).IsNotNull();
        AssertThat(MenuManager.Instance).IsInstanceOf<MenuManager>();
    }

    [TestCase]
    public void BaseManager_ReadyMethod_CallsInitialize()
    {
        // Arrange - ensure clean state for this specific test
        TestConcreteManager.Instance = null;
        SetSingletonInstance<BaseManager>(null);

        var manager = AddToTestRoot(new TestConcreteManager());
        _testNodes.Add(manager);

        GD.Print($"[TEST] Before CallReady - BaseManager.Instance: {BaseManager.Instance}");
        GD.Print($"[TEST] Before CallReady - TestConcreteManager.Instance: {TestConcreteManager.Instance}");
        GD.Print($"[TEST] Before CallReady - IsInitialized: {manager.IsInitialized}");

        // Since BaseManager.Instance is null, _Ready should call Initialize
        manager.CallReady();

        GD.Print($"[TEST] After CallReady - BaseManager.Instance: {BaseManager.Instance}");
        GD.Print($"[TEST] After CallReady - TestConcreteManager.Instance: {TestConcreteManager.Instance}");
        GD.Print($"[TEST] After CallReady - IsInitialized: {manager.IsInitialized}");
        GD.Print($"[TEST] After CallReady - InitializeCallCount: {manager.InitializeCallCount}");

        AssertThat(TestConcreteManager.Instance).IsEqual(manager);
        AssertThat(manager.IsInitialized).IsTrue();
        AssertThat(manager.InitializeCallCount).IsEqual(1);
    }

    [TestCase]
    public async System.Threading.Tasks.Task BaseManager_ExitTreeMethod_CallsCleanup()
    {
        // Arrange - ensure clean state
        TestConcreteManager.Instance = null;
        SetSingletonInstance<BaseManager>(null);

        var manager = AddToTestRoot(new TestConcreteManager());
        _testNodes.Add(manager);
        manager.CallInitialize(); // Manually initialize

        GD.Print($"[TEST] After initialize - IsInitialized: {manager.IsInitialized}, IsCleanedUp: {manager.IsCleanedUp}");
        AssertThat(manager.IsInitialized).IsTrue();
        AssertThat(manager.IsCleanedUp).IsFalse();
        AssertThat(TestConcreteManager.Instance).IsEqual(manager);

        // Remove from test nodes list so TearDown doesn't interfere
        _testNodes.Remove(manager);

        // Act - queue for deletion
        manager.QueueFree();

        // Wait for the deletion to be processed
        var tree = (SceneTree)Godot.Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame); // Extra frame to ensure cleanup

        GD.Print($"[TEST] After QueueFree - IsCleanedUp: {manager.IsCleanedUp}");
        GD.Print($"[TEST] After QueueFree - TestConcreteManager.Instance: {TestConcreteManager.Instance}");

        // Assert - should be cleaned up and instance cleared
        AssertThat(manager.IsCleanedUp).IsTrue();
        AssertThat(TestConcreteManager.Instance).IsNull();
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
    private T CreateAndInitializeManager<T>() where T : BaseManager, new()
    {
        var manager = AddToTestRoot(new T());
        _testNodes.Add(manager);

        // Cast to get access to CallInitialize method
        if (manager is TestConcreteManager tcm)
        {
            tcm.CallInitialize();
        }
        else if (manager is AnotherTestManager atm)
        {
            atm.CallInitialize();
        }

        return manager;
    }

    private T AddToTestRoot<T>(T node) where T : Node
    {
        if (_root == null)
            throw new System.InvalidOperationException("Test root node is not initialized.");

        // Add to our test root instead of directly to scene root
        _root.AddChild(node);
        return node;
    }

    private void ResetAllManagerInstances()
    {
        // Reset test manager instances
        TestConcreteManager.Instance = null;
        AnotherTestManager.Instance = null;

        // Reset real manager instances
        SetSingletonInstance<SettingsManager>(null);
        SetSingletonInstance<MenuManager>(null);

        // Also reset the base BaseManager instance
        SetSingletonInstance<BaseManager>(null);
    }

    private void SetSingletonInstance<T>(T? instance) where T : class
    {
        var instanceProperty = typeof(T).GetProperty("Instance",
            BindingFlags.Public | BindingFlags.Static);
        instanceProperty?.SetValue(null, instance);
    }

    [AfterTest]
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

using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
using AshesofVelsingrad;

namespace Tests.Unit
{
    [TestSuite]
    public class UnitTestExample
    {
        [TestCase]
        [RequireGodotRuntime]
        public void TestTempCounter()
        {
            var main = AutoFree(new Main());
            int result = main.TempCounter(5);
            AssertThat(result).IsEqual(6);
        }

        [TestCase]
        public void TestBasicAssertion()
        {
            AssertThat(2 + 2).IsEqual(4);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void TestGodotNode()
        {
            var node = AutoFree(new Node());

            AssertThat(node).IsNotNull();
            AssertThat(node.Name).IsEqual("");

            node.Name = "TestNode";
            AssertThat(node.Name).IsEqual("TestNode");
        }

        [TestCase]
        [RequireGodotRuntime]
        public void TestGodotNodeWithManualCleanup()
        {
            Node node = null;
            try
            {
                node = new Node();
                AssertThat(node).IsNotNull();

                AssertThat(node.GetType().Name).IsEqual("Node");

                node.Name = "ManualTestNode";
                AssertThat(node.Name).IsEqual("ManualTestNode");
            }
            finally
            {
                node?.QueueFree();
            }
        }

        [TestCase]
        [RequireGodotRuntime]
        public void TestGodotNodeWithSceneTree()
        {
            var scene = AutoFree(new Node());
            var child = AutoFree(new Node());

            scene.AddChild(child);

            AssertThat(scene.GetChildCount()).IsEqual(1);
            AssertThat(scene.GetChild(0)).IsEqual(child);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void TestNodeProperties()
        {
            var node = AutoFree(new Node());

            AssertThat(node.GetInstanceId()).IsGreater(0);
            AssertThat(node.IsInsideTree()).IsFalse();

            node.Name = "TestNode";
            AssertThat(node.Name).IsEqual("TestNode");
        }

        [TestCase]
        [RequireGodotRuntime]
        public void TestNodeHierarchy()
        {
            var parent = AutoFree(new Node());
            var child1 = AutoFree(new Node());
            var child2 = AutoFree(new Node());

            parent.Name = "Parent";
            child1.Name = "Child1";
            child2.Name = "Child2";

            parent.AddChild(child1);
            parent.AddChild(child2);

            AssertThat(parent.GetChildCount()).IsEqual(2);
            AssertThat(child1.GetParent()).IsEqual(parent);
            AssertThat(child2.GetParent()).IsEqual(parent);
            AssertThat(parent.GetChild(0).Name).IsEqual("Child1");
            AssertThat(parent.GetChild(1).Name).IsEqual("Child2");
        }
    }
}

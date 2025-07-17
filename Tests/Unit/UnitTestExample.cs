using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace Tests.Unit
{
    [TestSuite]
    public class UnitTestExample
    {
        [TestCase]
        public void TestTempCounter()
        {
            var main = new Main();
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
            AssertThat(node.Name).IsEqual("Node");
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
                AssertThat(node.Name).IsEqual("Node");
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
    }
}

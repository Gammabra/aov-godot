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
            var node = new Node();
            AssertThat(node).IsNotNull();
            AssertThat(node.Name).IsEqual("Node");
        }
    }
}

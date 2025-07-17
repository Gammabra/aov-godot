using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

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
            // Utilisation d'AutoFree pour éviter les orphan nodes
            var node = AutoFree(new Node());

            AssertThat(node).IsNotNull();
            // Le nom par défaut d'un Node nouvellement créé est une chaîne vide
            AssertThat(node.Name).IsEqual("");

            // Test avec un nom explicite
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

                // Vérification du type plutôt que du nom
                AssertThat(node.GetType().Name).IsEqual("Node");

                // Test avec un nom explicite
                node.Name = "ManualTestNode";
                AssertThat(node.Name).IsEqual("ManualTestNode");
            }
            finally
            {
                // Nettoyage manuel si nécessaire
                node?.QueueFree();
            }
        }

        [TestCase]
        [RequireGodotRuntime]
        public void TestGodotNodeWithSceneTree()
        {
            // Pour les tests plus complexes avec scene tree
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

            // Test des propriétés de base
            AssertThat(node.GetInstanceId()).IsGreater(0);
            AssertThat(node.IsInsideTree()).IsFalse();

            // Test de modification de propriétés
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

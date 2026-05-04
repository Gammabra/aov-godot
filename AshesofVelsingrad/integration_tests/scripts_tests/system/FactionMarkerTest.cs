using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Systems.Battle;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace AshesOfVelsingrad.IntegrationTests.Systems;

/// <summary>
///     Coverage for <see cref="FactionMarker" /> — the small floating arrow above each
///     unit that's colour-coded by faction. The marker itself has no gameplay logic, but
///     it's how the player visually identifies whose side a unit is on, so a regression
///     here makes the battlefield unreadable. Tests verify:
///     <list type="bullet">
///         <item><description><see cref="FactionMarker.Bind" /> assigns a faction-distinct colour.</description></item>
///         <item><description><see cref="FactionMarker.SetActive" /> brightens the marker for the active turn.</description></item>
///         <item><description>The marker mounts a child <see cref="MeshInstance3D" /> and floats above its parent.</description></item>
///     </list>
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class FactionMarkerTest
{
    private Node? _root;
    private readonly List<Node> _testNodes = new();

    private T AddNode<T>(T node) where T : Node
    {
        if (_root == null)
            throw new InvalidOperationException("Root is not initialized.");
        _root.AddChild(node);
        _testNodes.Add(node);
        return node;
    }

    [BeforeTest]
    public void Setup()
    {
        _root = new Node { Name = "TestRoot" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_root);
        _testNodes.Clear();
        _testNodes.Add(_root);
    }

    [AfterTest]
    public void Cleanup()
    {
        foreach (Node node in _testNodes)
            node.QueueFree();
        _testNodes.Clear();
    }

    [TestCase]
    public void HoverHeight_IsAppliedAsLocalY()
    {
        FactionMarker marker = AddNode(new FactionMarker { Name = "Marker" });
        marker.Bind(Faction.Player);

        // _Ready sets Position.Y = HoverHeight so the marker floats above the parent unit.
        AssertThat(Mathf.IsEqualApprox(marker.Position.Y, FactionMarker.HoverHeight)).IsTrue();
        AssertThat(marker.Position.X).IsEqual(0f);
        AssertThat(marker.Position.Z).IsEqual(0f);
    }

    [TestCase]
    public void Build_CreatesArrowMeshChild()
    {
        FactionMarker marker = AddNode(new FactionMarker { Name = "Marker" });
        marker.Bind(Faction.Enemy);

        // _Ready spawns the arrow mesh as a child MeshInstance3D.
        Node? mesh = marker.GetNodeOrNull("ArrowMesh");
        AssertThat(mesh).IsNotNull();
        AssertThat(mesh is MeshInstance3D).IsTrue();
    }

    [TestCase]
    public void Bind_PlayerFaction_UsesBlueAlbedo()
    {
        FactionMarker marker = AddNode(new FactionMarker { Name = "Marker" });
        marker.Bind(Faction.Player);

        StandardMaterial3D material = GetMaterial(marker);
        // Blue rest colour: high B, moderate G, low-ish R.
        AssertThat(material.AlbedoColor.B > material.AlbedoColor.R).IsTrue();
    }

    [TestCase]
    public void Bind_EnemyFaction_UsesRedAlbedo()
    {
        FactionMarker marker = AddNode(new FactionMarker { Name = "Marker" });
        marker.Bind(Faction.Enemy);

        StandardMaterial3D material = GetMaterial(marker);
        // Red rest colour: high R, low G, low B.
        AssertThat(material.AlbedoColor.R > material.AlbedoColor.G).IsTrue();
        AssertThat(material.AlbedoColor.R > material.AlbedoColor.B).IsTrue();
    }

    [TestCase]
    public void Bind_AllyFaction_UsesGreenAlbedo()
    {
        FactionMarker marker = AddNode(new FactionMarker { Name = "Marker" });
        marker.Bind(Faction.Ally);

        StandardMaterial3D material = GetMaterial(marker);
        // Green rest colour: high G, low R, low B.
        AssertThat(material.AlbedoColor.G > material.AlbedoColor.R).IsTrue();
        AssertThat(material.AlbedoColor.G > material.AlbedoColor.B).IsTrue();
    }

    [TestCase]
    public void SetActive_BrightensTheMarker()
    {
        FactionMarker marker = AddNode(new FactionMarker { Name = "Marker" });
        marker.Bind(Faction.Player);

        Color rest = GetMaterial(marker).AlbedoColor;

        marker.SetActive(true);
        Color active = GetMaterial(marker).AlbedoColor;

        // The active state lightens the colour, so at least one channel should be brighter.
        bool brighter = active.R > rest.R || active.G > rest.G || active.B > rest.B;
        AssertThat(brighter).IsTrue();
    }

    [TestCase]
    public void SetActive_FalseAfterTrue_ReturnsToRestColor()
    {
        FactionMarker marker = AddNode(new FactionMarker { Name = "Marker" });
        marker.Bind(Faction.Player);

        Color rest = GetMaterial(marker).AlbedoColor;
        marker.SetActive(true);
        marker.SetActive(false);
        Color afterDeactivate = GetMaterial(marker).AlbedoColor;

        AssertThat(Mathf.IsEqualApprox(afterDeactivate.R, rest.R)).IsTrue();
        AssertThat(Mathf.IsEqualApprox(afterDeactivate.G, rest.G)).IsTrue();
        AssertThat(Mathf.IsEqualApprox(afterDeactivate.B, rest.B)).IsTrue();
    }

    private static StandardMaterial3D GetMaterial(FactionMarker marker)
    {
        MeshInstance3D mesh = marker.GetNode<MeshInstance3D>("ArrowMesh");
        return (StandardMaterial3D)mesh.MaterialOverride;
    }
}

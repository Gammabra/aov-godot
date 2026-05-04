using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.Systems.Battle;

/// <summary>
///     Small downward-pointing arrow that floats above a unit, colour-coded by faction.
/// </summary>
/// <remarks>
///     <para>
///         Spawned by <c>GameManager</c> for every loaded unit. Blue = <see cref="Faction.Player" />,
///         green = <see cref="Faction.Ally" />, red = <see cref="Faction.Enemy" />. The arrow
///         additionally pulses brighter while the parent unit is the currently-acting one,
///         so the player can identify whose turn it is from any camera angle.
///     </para>
///     <para>
///         Pure presentation node — has no gameplay state. Lives as a child of the unit's
///         <see cref="CharacterBody3D" /> so it inherits its world transform automatically,
///         then floats <see cref="HoverHeight" /> units above the unit's origin.
///     </para>
/// </remarks>
public sealed partial class FactionMarker : Node3D
{
    /// <summary>How far above the unit the arrow floats, in world units.</summary>
    public const float HoverHeight = 1.2f;

    /// <summary>Triangle (arrow) edge length, in world units.</summary>
    public const float Size = 0.35f;

    private MeshInstance3D? _mesh;
    private StandardMaterial3D? _material;
    private Color _restColor;
    private Color _activeColor;
    private bool _isActive;
    private double _pulseTime;

    /// <summary>
    ///     Configure the marker for a given faction. Call once after <c>AddChild</c>.
    /// </summary>
    /// <param name="faction">Faction the parent unit belongs to.</param>
    public void Bind(Faction faction)
    {
        _restColor = faction switch
        {
            Faction.Player => new Color(0.30f, 0.65f, 1.00f, 1f),     // blue
            Faction.Ally => new Color(0.30f, 0.90f, 0.30f, 1f),       // green
            Faction.Enemy => new Color(1.00f, 0.30f, 0.30f, 1f),      // red
            _ => new Color(0.80f, 0.80f, 0.80f, 1f),
        };
        _activeColor = _restColor.Lightened(0.4f);

        if (_material is not null)
            _material.AlbedoColor = _restColor;
    }

    /// <summary>
    ///     Toggle whether the marker should pulse brightly to indicate the active turn.
    /// </summary>
    /// <param name="active">True when the parent unit is currently acting.</param>
    public void SetActive(bool active)
    {
        _isActive = active;
        _pulseTime = 0;
        if (_material is not null)
            _material.AlbedoColor = active ? _activeColor : _restColor;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        Position = new Vector3(0, HoverHeight, 0);

        _mesh = new MeshInstance3D
        {
            Name = "ArrowMesh",
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        _mesh.Mesh = BuildArrowMesh();
        _material = new StandardMaterial3D
        {
            AlbedoColor = _restColor,
            Transparency = BaseMaterial3D.TransparencyEnum.Disabled,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            DisableReceiveShadows = true,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            EmissionEnabled = true,
            Emission = _restColor * 0.6f,
        };
        _mesh.MaterialOverride = _material;
        AddChild(_mesh);
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        // Slight bob even when idle so the arrow reads as a UI element rather than world geometry.
        _pulseTime += delta;
        float bob = (float)System.Math.Sin(_pulseTime * (_isActive ? 6.0 : 2.5)) * 0.08f;
        if (_mesh is not null)
            _mesh.Position = new Vector3(0, bob, 0);
    }

    /// <summary>Build a small downward-pointing tetrahedron-style arrow mesh.</summary>
    private static ArrayMesh BuildArrowMesh()
    {
        // Four-vertex pyramid: apex pointing DOWN at (0, -Size, 0), base square at +Size/2 above.
        const float h = Size;
        Vector3 apex = new(0, -h, 0);
        Vector3 a = new(-h * 0.5f, 0, -h * 0.5f);
        Vector3 b = new(h * 0.5f, 0, -h * 0.5f);
        Vector3 c = new(h * 0.5f, 0, h * 0.5f);
        Vector3 d = new(-h * 0.5f, 0, h * 0.5f);

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        // Four side faces, each pointing outward from apex to a base edge
        AddTri(st, apex, a, b);
        AddTri(st, apex, b, c);
        AddTri(st, apex, c, d);
        AddTri(st, apex, d, a);
        // Cap (top of pyramid) so the arrow looks solid from above too
        AddTri(st, a, b, c);
        AddTri(st, a, c, d);

        st.GenerateNormals();
        return st.Commit();
    }

    private static void AddTri(SurfaceTool st, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        st.AddVertex(v0);
        st.AddVertex(v1);
        st.AddVertex(v2);
    }
}

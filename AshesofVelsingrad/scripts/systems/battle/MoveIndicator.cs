using System.Collections.Generic;
using Godot;

namespace AshesOfVelsingrad.systems.battle;

/// <summary>
///     World-space overlay that highlights tiles a unit can move to.
/// </summary>
/// <remarks>
///     <para>
///         A pool of translucent quads spawned as children of the active map. Call
///         <see cref="Show" /> with the result of <c>UnitSystem.GetPossibleMoves(map)</c> to
///         display the indicators; call <see cref="Hide" /> to clear them.
///     </para>
///     <para>
///         Reuses meshes via a simple pool to avoid alloc churn between turns. The default
///         tile colour is friendly cyan-green; <see cref="TargetIndicator" /> uses the same
///         class with a hostile-red palette to highlight skill targets.
///     </para>
/// </remarks>
public partial class MoveIndicator : Node3D
{
    /// <summary>Colour of each move tile. Defaults to a translucent friendly green.</summary>
    [Export]
    public Color TileColor { get; set; } = new(0.30f, 0.95f, 0.55f, 0.50f);

    /// <summary>How high above the cell the indicator hovers (in world units).</summary>
    [Export]
    public float Height { get; set; } = 0.05f;

    private readonly List<MeshInstance3D> _pool = [];
    private MapSystem? _map;
    private StandardMaterial3D? _material;

    /// <summary>
    ///     Bind the indicator to the active map. Must be called once before <see cref="Show" />.
    /// </summary>
    /// <param name="map">The map whose cell coordinates we'll convert to world space.</param>
    public void Bind(MapSystem map)
    {
        _map = map;
    }

    /// <summary>Display indicators on every tile in <paramref name="tiles" />.</summary>
    /// <param name="tiles">Cell coordinates as (x, y, z) tuples.</param>
    public void Show(IReadOnlyList<(int X, int Y, int Z)> tiles)
    {
        if (_map is null)
        {
            GD.PrintErr($"{nameof(MoveIndicator)}: Bind(map) must be called first.");
            return;
        }

        EnsurePool(tiles.Count);

        for (int i = 0; i < _pool.Count; i++)
        {
            MeshInstance3D mesh = _pool[i];
            if (i < tiles.Count)
            {
                (int x, int y, int z) = tiles[i];
                Vector3 world = _map.MapToLocal(new Vector3I(x, y, z));
                world.Y += _map.CellSize.Y * 0.5f + Height;
                mesh.Position = world;
                mesh.Visible = true;
            }
            else
            {
                mesh.Visible = false;
            }
        }
    }

    /// <summary>Hide all indicators.</summary>
    public new void Hide()
    {
        foreach (MeshInstance3D mesh in _pool)
            mesh.Visible = false;
    }

    private void EnsurePool(int count)
    {
        while (_pool.Count < count)
        {
            MeshInstance3D mesh = BuildIndicator();
            _pool.Add(mesh);
            AddChild(mesh);
        }
    }

    private MeshInstance3D BuildIndicator()
    {
        Vector3 cell = _map?.CellSize ?? Vector3.One;
        PlaneMesh plane = new()
        {
            Size = new Vector2(cell.X * 0.92f, cell.Z * 0.92f),
        };

        _material ??= new StandardMaterial3D
        {
            AlbedoColor = TileColor,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            DisableReceiveShadows = true,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            // Faintly emissive so the tile reads at any lighting.
            EmissionEnabled = true,
            Emission = new Color(TileColor.R, TileColor.G, TileColor.B, 1f) * 0.4f,
        };
        plane.SurfaceSetMaterial(0, _material);

        return new MeshInstance3D
        {
            Mesh = plane,
            Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }
}

/// <summary>
///     Reuses <see cref="MoveIndicator" /> with a hostile-red palette to highlight skill targets.
/// </summary>
public sealed partial class TargetIndicator : MoveIndicator
{
    /// <inheritdoc />
    public override void _Ready()
    {
        TileColor = new Color(0.95f, 0.30f, 0.30f, 0.55f);
        base._Ready();
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] int gridHalfSize = 50; // map is -50..50 on X and Z

    readonly HashSet<Vector2Int> _occupied = new();

    bool         _gridVisible;
    GameObject   _gridVisual;
    NavMeshData  _navMeshData;
    NavMeshDataInstance _navMeshHandle;


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) ToggleGrid();
    }

    public void Initialize(MeshFilter _ = null) => BakeNavMesh();

    // ── Grid math ─────────────────────────────────────────────────────────────

    public Vector2Int WorldToGrid(Vector3 world) =>
        new(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.z));

    public Vector3 GridToWorld(Vector2Int cell) =>
        new(cell.x, 0f, cell.y);

    public bool CanPlace(BuildingDefinition def, Vector2Int origin)
    {
        for (int x = 0; x < def.widthCells; x++)
            for (int z = 0; z < def.depthCells; z++)
                if (_occupied.Contains(origin + new Vector2Int(x, z)))
                    return false;
        return true;
    }

    public void OccupyCells(BuildingDefinition def, Vector2Int origin)
        => OccupyCells(def.widthCells, def.depthCells, origin);

    public void OccupyCells(int widthCells, int depthCells, Vector2Int origin)
    {
        for (int x = 0; x < widthCells; x++)
            for (int z = 0; z < depthCells; z++)
                _occupied.Add(origin + new Vector2Int(x, z));
    }

    // ── Grid visual ───────────────────────────────────────────────────────────

    void ToggleGrid()
    {
        _gridVisible = !_gridVisible;
        if (_gridVisible) ShowGrid();
        else HideGrid();
    }

    void ShowGrid()
    {
        if (_gridVisual != null) { _gridVisual.SetActive(true); return; }

        _gridVisual = new GameObject("GridVisual");
        var mat = ResourceVisuals.CreateUnlit(new Color(0.5f, 0.5f, 0.5f, 0.3f));
        int size = gridHalfSize;

        for (int i = -size; i <= size; i++)
        {
            AddLine(_gridVisual, new Vector3(i, 0.01f, -size), new Vector3(i, 0.01f, size), mat);
            AddLine(_gridVisual, new Vector3(-size, 0.01f, i), new Vector3(size, 0.01f, i), mat);
        }
    }

    void HideGrid()
    {
        if (_gridVisual != null) _gridVisual.SetActive(false);
    }

    static void AddLine(GameObject parent, Vector3 a, Vector3 b, Material mat)
    {
        var go = new GameObject("GridLine");
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPositions(new[] { a, b });
        lr.startWidth = lr.endWidth = 0.02f;
        lr.material = mat;
        lr.useWorldSpace = true;
    }

    // ── NavMesh ───────────────────────────────────────────────────────────────

    public void RebakeNavMesh()
    {
        if (_navMeshHandle.valid) NavMesh.RemoveNavMeshData(_navMeshHandle);
        BakeNavMesh();
    }

    void BakeNavMesh()
    {
        // Use a flat box covering the entire map as the walkable source.
        // NavMeshBuildSourceShape.Mesh's sourceData is only available via the
        // AI Navigation package; Box works identically for a flat plane and
        // needs no extra reference. Buildings carve via NavMeshObstacle.
        var source = new NavMeshBuildSource
        {
            transform = Matrix4x4.identity,
            shape     = NavMeshBuildSourceShape.Box,
            size      = new Vector3(100f, 0.2f, 100f), // thin slab at y = 0
            area      = 0 // Walkable
        };

        var bounds   = new Bounds(Vector3.zero, new Vector3(110f, 10f, 110f));
        var settings = NavMesh.GetSettingsByID(0);

        _navMeshData   = NavMeshBuilder.BuildNavMeshData(
            settings,
            new List<NavMeshBuildSource> { source },
            bounds,
            Vector3.zero,
            Quaternion.identity);

        _navMeshHandle = NavMesh.AddNavMeshData(_navMeshData);
    }
}

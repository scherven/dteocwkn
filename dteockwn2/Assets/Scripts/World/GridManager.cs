using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] int gridHalfSize = 50; // map is -50..50 on X and Z

    readonly HashSet<Vector2Int> _occupied      = new();
    readonly HashSet<Vector2Int> _preparedCells = new();

    bool         _gridVisible;
    bool         _preparePlotActive;
    GameObject   _preparePreview;
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
        if (_preparePlotActive) UpdatePreparePlotCursor();
    }

    public void Initialize(MeshFilter _ = null) => BakeNavMesh();

    // ── Grid math ─────────────────────────────────────────────────────────────

    public Vector2Int WorldToGrid(Vector3 world) =>
        new(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.z));

    public Vector3 GridToWorld(Vector2Int cell) =>
        new(cell.x, 0f, cell.y);

    public bool CanPlace(BuildingDefinition def, Vector2Int origin)
    {
        bool needsTerrain = def.requiredTerrain != TerrainType.Field;

        for (int x = 0; x < def.widthCells; x++)
            for (int z = 0; z < def.depthCells; z++)
            {
                var cell = origin + new Vector2Int(x, z);
                if (_occupied.Contains(cell)) return false;

                if (needsTerrain)
                {
                    // Terrain-restricted buildings bypass the Clear Land requirement
                    // but ALL cells must match the required terrain type
                    if (TerrainManager.Instance.GetTerrainType(cell) != def.requiredTerrain)
                        return false;
                }
                else
                {
                    // Require prepared cells only once the player has used Clear Land at least once
                    if (_preparedCells.Count > 0 && !_preparedCells.Contains(cell)) return false;
                }
            }
        return true;
    }

    // ── Prepared plots ────────────────────────────────────────────────────────

    public bool HasAnyPreparedCells => _preparedCells.Count > 0;

    /// <summary>Enters the interactive prepare-plot selection mode.</summary>
    public void BeginPreparePlotSelection()
    {
        _preparePlotActive = true;
        _preparePreview = CreatePreparePlotPreview();
    }

    void UpdatePreparePlotCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPreparePlot();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) return;
        if (hit.normal.y < 0.8f) return;

        var origin   = WorldToGrid(hit.point);
        var worldPos = GridToWorld(origin);
        _preparePreview.transform.position = worldPos + new Vector3(0.5f, 0.01f, 0.5f);

        if (Input.GetMouseButtonDown(0))
            ConfirmPreparePlot(origin);
    }

    void ConfirmPreparePlot(Vector2Int origin)
    {
        for (int x = 0; x < 2; x++)
            for (int z = 0; z < 2; z++)
            {
                var cell = origin + new Vector2Int(x, z);
                TerrainManager.Instance?.ClearCell(cell);
                _preparedCells.Add(cell);
            }

        PreparedPlotVisual.Create(GridToWorld(origin) + new Vector3(0.5f, 0.01f, 0.5f));
        CancelPreparePlot();
    }

    void CancelPreparePlot()
    {
        _preparePlotActive = false;
        if (_preparePreview != null) { Object.Destroy(_preparePreview); _preparePreview = null; }
    }

    static GameObject CreatePreparePlotPreview()
    {
        var go  = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "PreparePlotPreview";
        go.transform.rotation   = Quaternion.Euler(90, 0, 0);
        go.transform.localScale = new Vector3(2f, 2f, 1f);
        go.GetComponent<Renderer>().material = ResourceVisuals.CreateUnlit(new Color(0.3f, 0.9f, 0.3f, 0.5f));
        Object.Destroy(go.GetComponent<Collider>());
        return go;
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
        // Road area (3) cost: 1/1.75 ≈ 0.571 — agents prefer roads when routing
        // because lower cost = faster in NavMesh A*. Must be set before building.
        const int   RoadArea     = 3;
        const float RoadCostMult = 1f / 1.75f;
        NavMesh.SetAreaCost(RoadArea, RoadCostMult);

        var sources = new List<NavMeshBuildSource>();

        // Base walkable terrain — full map, thin slab at y = 0
        sources.Add(new NavMeshBuildSource
        {
            transform = Matrix4x4.identity,
            shape     = NavMeshBuildSourceShape.Box,
            size      = new Vector3(100f, 0.2f, 100f),
            area      = 0 // Walkable
        });

        // Road cells — slightly taller (0.3 > 0.2) and added after terrain so they
        // take priority in the build and produce a distinct area-3 NavMesh polygon.
        if (RoadManager.Instance != null)
        {
            foreach (var cell in RoadManager.Instance.BuiltRoads)
            {
                sources.Add(new NavMeshBuildSource
                {
                    transform = Matrix4x4.Translate(new Vector3(cell.x, 0f, cell.y)),
                    shape     = NavMeshBuildSourceShape.Box,
                    size      = new Vector3(1f, 0.3f, 1f),
                    area      = RoadArea
                });
            }
        }

        var bounds   = new Bounds(Vector3.zero, new Vector3(110f, 10f, 110f));
        var settings = NavMesh.GetSettingsByID(0);

        _navMeshData   = NavMeshBuilder.BuildNavMeshData(
            settings,
            sources,
            bounds,
            Vector3.zero,
            Quaternion.identity);

        _navMeshHandle = NavMesh.AddNavMeshData(_navMeshData);
    }
}

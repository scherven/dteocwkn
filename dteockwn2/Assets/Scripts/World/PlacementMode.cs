using UnityEngine;

/// <summary>
/// Handles the interactive building placement flow after a market purchase.
/// Left-click to confirm placement; Escape to cancel.
/// </summary>
public class PlacementMode : MonoBehaviour
{
    public static PlacementMode Instance { get; private set; }

    BuildingDefinition _pending;
    GameObject         _preview;
    bool               _active;

    public bool IsActive => _active;

    static readonly Color ValidColor   = new(0.20f, 0.85f, 0.20f);
    static readonly Color InvalidColor = new(0.85f, 0.20f, 0.20f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (!_active) return;

        if (Input.GetKeyDown(KeyCode.Escape)) { Cancel(); return; }
        if (Camera.main == null || GridManager.Instance == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // Raycast to any surface; require an approximately horizontal hit (the flat terrain)
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) return;
        if (hit.normal.y < 0.8f) return; // ignore walls / building sides

        var grid   = GridManager.Instance;
        var cell   = grid.WorldToGrid(hit.point);
        var worldPos = grid.GridToWorld(cell);
        bool valid = grid.CanPlace(_pending, cell);

        // Update preview position and color
        _preview.transform.position = worldPos + Vector3.up;
        _preview.GetComponent<Renderer>().material.color = valid ? ValidColor : InvalidColor;

        if (Input.GetMouseButtonDown(0) && valid)
            Confirm(cell, worldPos);
    }

    public void Begin(BuildingDefinition def)
    {
        _pending = def;
        _active  = true;
        CreatePreview(def);
    }

    public void Cancel()
    {
        // Refund — resources were already consumed on purchase; refund them here
        if (_pending != null)
            foreach (var cost in _pending.materialCost)
                GameInventory.Instance.Add(cost.type, cost.amount);

        Cleanup();
    }

    void Confirm(Vector2Int cell, Vector3 worldPos)
    {
        GridManager.Instance.OccupyCells(_pending, cell);

        var siteGo    = new GameObject($"BuildSite_{_pending.buildingName}");
        var buildSite = siteGo.AddComponent<BuildSite>();
        buildSite.Initialize(_pending, worldPos);
        ConstructionQueue.Instance.Enqueue(buildSite);

        var buildings = GameObject.Find("Buildings");
        if (buildings != null) siteGo.transform.SetParent(buildings.transform);

        Cleanup();
    }

    void Cleanup()
    {
        _active  = false;
        _pending = null;
        if (_preview != null) { Destroy(_preview); _preview = null; }
    }

    void CreatePreview(BuildingDefinition def)
    {
        _preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _preview.name = "PlacementPreview";
        _preview.transform.localScale = new Vector3(def.widthCells, 2f, def.depthCells);
        Destroy(_preview.GetComponent<Collider>());
        // Use Unlit/Color — always available, no transparency needed for greybox
        _preview.GetComponent<Renderer>().material = ResourceVisuals.CreateUnlit(ValidColor);
    }
}

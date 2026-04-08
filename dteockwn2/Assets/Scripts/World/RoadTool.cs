using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Road-marking tool. Toggle with R key or its HUD button.
/// While active, left-click to plan/unplan road cells on the terrain.
/// Escape exits the tool.
/// </summary>
public class RoadTool : MonoBehaviour
{
    public static RoadTool Instance { get; private set; }

    public bool IsActive { get; private set; }

    GameObject _cursor;
    Image      _buttonImage; // injected by GameBootstrap

    static readonly Color BtnActive   = new(0.80f, 0.60f, 0.10f, 0.95f);
    static readonly Color BtnInactive = new(0.25f, 0.25f, 0.25f, 0.90f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) Toggle();
        if (IsActive) UpdateCursor();
    }

    /// <summary>Called by GameBootstrap to wire up the mode button's image for tinting.</summary>
    public void SetButtonRef(Image img)
    {
        _buttonImage = img;
        RefreshButton();
    }

    public void Toggle()
    {
        IsActive = !IsActive;
        if (!IsActive) HideCursor();
        RefreshButton();
    }

    // ── Cursor ────────────────────────────────────────────────────────────────

    void UpdateCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { Toggle(); return; }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) { HideCursor(); return; }
        if (hit.normal.y < 0.8f) return;

        var cell     = GridManager.Instance.WorldToGrid(hit.point);
        var worldPos = new Vector3(cell.x, 0.025f, cell.y);

        EnsureCursor();
        _cursor.transform.position = worldPos;

        // Colour indicates current state of the hovered cell
        bool built   = RoadManager.Instance.IsBuiltRoad(cell);
        bool planned = RoadManager.Instance.IsPlannedRoad(cell);
        var color = built   ? new Color(0.85f, 0.30f, 0.30f, 0.55f)  // red — already built
                  : planned ? new Color(0.90f, 0.55f, 0.10f, 0.60f)  // orange — will un-plan
                  :           new Color(1.00f, 0.90f, 0.20f, 0.60f); // yellow — will plan
        _cursor.GetComponent<Renderer>().material.color = color;

        if (Input.GetMouseButtonDown(0))
            RoadManager.Instance.TogglePlanned(cell);
    }

    void EnsureCursor()
    {
        if (_cursor != null) return;
        _cursor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _cursor.name = "RoadCursor";
        _cursor.transform.rotation   = Quaternion.Euler(90f, 0f, 0f);
        _cursor.transform.localScale = Vector3.one * 0.97f;
        _cursor.GetComponent<Renderer>().material =
            ResourceVisuals.CreateUnlit(new Color(1f, 0.9f, 0.2f, 0.6f));
        Object.Destroy(_cursor.GetComponent<Collider>());
    }

    void HideCursor()
    {
        if (_cursor == null) return;
        Destroy(_cursor);
        _cursor = null;
    }

    void RefreshButton()
    {
        if (_buttonImage != null)
            _buttonImage.color = IsActive ? BtnActive : BtnInactive;
    }
}

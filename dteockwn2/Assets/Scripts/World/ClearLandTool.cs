using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Clear Land action tool. Costs 1 hammer per 2×2 plot prepared.
/// Toggle with C or the HUD button. Shows a green/red cursor while active.
/// </summary>
public class ClearLandTool : MonoBehaviour
{
    public static ClearLandTool Instance { get; private set; }

    public bool IsActive { get; private set; }

    GameObject _cursor;
    Image      _buttonImage;

    static readonly Color BtnActive   = new(0.80f, 0.60f, 0.10f, 0.95f);
    static readonly Color BtnInactive = new(0.25f, 0.25f, 0.25f, 0.90f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) Toggle();
        if (IsActive) UpdateCursor();
    }

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
        if (Camera.main == null || GridManager.Instance == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) { HideCursor(); return; }
        if (hit.normal.y < 0.8f) return;

        var cell     = GridManager.Instance.WorldToGrid(hit.point);
        var worldPos = GridManager.Instance.GridToWorld(cell);

        EnsureCursor();
        // Centre the 2×2 preview on the plot origin
        _cursor.transform.position = worldPos + new Vector3(0.5f, 0.025f, 0.5f);

        bool canAfford = HammerManager.Instance != null && HammerManager.Instance.HammersAvailable >= 1;
        _cursor.GetComponent<Renderer>().material.color = canAfford
            ? new Color(0.30f, 0.90f, 0.30f, 0.55f)
            : new Color(0.90f, 0.30f, 0.30f, 0.55f);

        if (Input.GetMouseButtonDown(0) && canAfford)
        {
            if (HammerManager.Instance.TrySpend(1))
                GridManager.Instance.PreparePlot(cell);
        }
    }

    void EnsureCursor()
    {
        if (_cursor != null) return;
        _cursor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _cursor.name = "ClearLandCursor";
        _cursor.transform.rotation   = Quaternion.Euler(90f, 0f, 0f);
        _cursor.transform.localScale = new Vector3(2f, 2f, 1f);
        _cursor.GetComponent<Renderer>().material =
            ResourceVisuals.CreateUnlit(new Color(0.30f, 0.90f, 0.30f, 0.55f));
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

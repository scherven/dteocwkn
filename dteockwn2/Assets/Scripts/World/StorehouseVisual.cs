using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds an open-top storehouse (4 walls + floor) and maintains
/// a grid of resource pile objects on the floor that reflect the inventory.
/// </summary>
public class StorehouseVisual : MonoBehaviour
{
    // How many visual piles per resource type to show max
    const int MaxPilesPerType = 6;
    // Pile spacing inside the storehouse
    const float PileSpacing = 0.7f;

    // Resource visuals arranged on the floor:
    //   Wood  piles: left half  (negative X)
    //   Stone piles: right half (positive X)
    readonly List<GameObject> _woodPiles  = new();
    readonly List<GameObject> _stonePiles = new();

    void OnEnable()
    {
        GameEvents.OnResourceAdded    += OnInventoryChanged;
        GameEvents.OnResourceConsumed += OnInventoryChanged;
    }

    void OnDisable()
    {
        GameEvents.OnResourceAdded    -= OnInventoryChanged;
        GameEvents.OnResourceConsumed -= OnInventoryChanged;
    }

    void OnInventoryChanged(ResourceType _, int __)  => RefreshPiles();

    void Start() => RefreshPiles();

    void RefreshPiles()
    {
        if (GameInventory.Instance == null) return;

        int wood  = GameInventory.Instance.GetCount(ResourceType.Wood);
        int stone = GameInventory.Instance.GetCount(ResourceType.Stone);

        UpdatePiles(_woodPiles,  ResourceType.Wood,  wood,  leftSide: true);
        UpdatePiles(_stonePiles, ResourceType.Stone, stone, leftSide: false);
    }

    void UpdatePiles(List<GameObject> piles, ResourceType type, int count, bool leftSide)
    {
        int target = Mathf.Min(count, MaxPilesPerType);

        // Spawn missing piles
        while (piles.Count < target)
        {
            int idx = piles.Count;
            var pile = ResourceVisuals.Spawn(type, PilePosition(idx, leftSide));
            pile.transform.SetParent(transform, worldPositionStays: true);
            pile.transform.localScale = Vector3.one * 0.5f;
            if (pile.TryGetComponent<Collider>(out var col)) col.enabled = false;
            piles.Add(pile);
        }

        // Destroy excess piles
        while (piles.Count > target)
        {
            int last = piles.Count - 1;
            Destroy(piles[last]);
            piles.RemoveAt(last);
        }
    }

    Vector3 PilePosition(int idx, bool leftSide)
    {
        // Arrange in a 2-column × 3-row grid on each side of the floor
        int col = idx % 2;
        int row = idx / 2;

        float x = (leftSide ? -1f : 1f) * (0.5f + col * PileSpacing);
        float z = -PileSpacing + row * PileSpacing;
        float y = 0.15f; // just above the floor

        return transform.position + new Vector3(x, y, z);
    }

    // ── Static factory ─────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces the solid storehouse cube with 4 walls + floor and attaches
    /// StorehouseVisual. Call from GameBootstrap instead of CreatePrimitive(Cube).
    /// </summary>
    public static GameObject Create(Vector3 pos, GameObject parent)
    {
        var root = new GameObject("Storehouse");
        root.transform.SetParent(parent.transform);
        root.transform.position = pos;

        var mat = ResourceVisuals.CreateUnlit(ResourceVisuals.HexColor("555555"));

        float w = 3f, d = 3f, h = 2f, t = 0.15f; // width, depth, height, wall thickness

        // Floor
        AddBox(root, "Floor", new Vector3(0, 0, 0), new Vector3(w, t, d), mat);

        // 4 walls (positioned so inner face is flush with floor edges)
        float hy = h / 2f + t / 2f;
        AddBox(root, "Wall_N",  new Vector3(0,  hy,  d / 2f - t / 2f), new Vector3(w, h, t), mat);
        AddBox(root, "Wall_S",  new Vector3(0,  hy, -d / 2f + t / 2f), new Vector3(w, h, t), mat);
        AddBox(root, "Wall_E",  new Vector3( w / 2f - t / 2f, hy, 0),  new Vector3(t, h, d), mat);
        AddBox(root, "Wall_W",  new Vector3(-w / 2f + t / 2f, hy, 0),  new Vector3(t, h, d), mat);

        root.AddComponent<StorehouseVisual>();
        return root;
    }

    static void AddBox(GameObject parent, string name, Vector3 localPos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, worldPositionStays: false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = size;
        go.GetComponent<Renderer>().material = mat;
        // No dynamic colliders needed; NavMesh won't be affected by thin walls
        var col = go.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
}

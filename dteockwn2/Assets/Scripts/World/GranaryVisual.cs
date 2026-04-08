using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds an open-top granary (4 walls + floor) and maintains
/// a grid of food pile objects on the floor that reflect the food inventory.
/// </summary>
public class GranaryVisual : MonoBehaviour
{
    const int   MaxPilesPerType = 6;
    const float PileSpacing     = 0.7f;

    readonly List<GameObject> _foodPiles = new();

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

    void OnInventoryChanged(ResourceType type, int _)
    {
        if (type == ResourceType.Food) RefreshPiles();
    }

    void Start() => RefreshPiles();

    void RefreshPiles()
    {
        if (GameInventory.Instance == null) return;

        int food = GameInventory.Instance.GetCount(ResourceType.Food);
        int target = Mathf.Min(food, MaxPilesPerType);

        while (_foodPiles.Count < target)
        {
            int idx  = _foodPiles.Count;
            var pile = ResourceVisuals.Spawn(ResourceType.Food, PilePosition(idx));
            pile.transform.SetParent(transform, worldPositionStays: true);
            pile.transform.localScale = Vector3.one * 0.5f;
            if (pile.TryGetComponent<Collider>(out var col)) col.enabled = false;
            _foodPiles.Add(pile);
        }

        while (_foodPiles.Count > target)
        {
            int last = _foodPiles.Count - 1;
            Destroy(_foodPiles[last]);
            _foodPiles.RemoveAt(last);
        }
    }

    Vector3 PilePosition(int idx)
    {
        int col = idx % 2;
        int row = idx / 2;
        float x = -0.35f + col * PileSpacing;
        float z = -PileSpacing + row * PileSpacing;
        return transform.position + new Vector3(x, 0.15f, z);
    }

    // ── Static factory ─────────────────────────────────────────────────────────

    public static GameObject Create(Vector3 pos, GameObject parent)
    {
        var root = new GameObject("Granary");
        root.transform.SetParent(parent.transform);
        root.transform.position = pos;

        var mat = ResourceVisuals.CreateUnlit(ResourceVisuals.HexColor("C8881A"));

        float w = 3f, d = 3f, h = 2f, t = 0.15f;

        AddBox(root, "Floor",  new Vector3(0, 0, 0),                         new Vector3(w, t, d), mat);
        float hy = h / 2f + t / 2f;
        AddBox(root, "Wall_N", new Vector3(0,  hy,  d / 2f - t / 2f),       new Vector3(w, h, t), mat);
        AddBox(root, "Wall_S", new Vector3(0,  hy, -d / 2f + t / 2f),       new Vector3(w, h, t), mat);
        AddBox(root, "Wall_E", new Vector3( w / 2f - t / 2f, hy, 0),        new Vector3(t, h, d), mat);
        AddBox(root, "Wall_W", new Vector3(-w / 2f + t / 2f, hy, 0),        new Vector3(t, h, d), mat);

        root.AddComponent<GranaryVisual>();
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
        var col = go.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
}

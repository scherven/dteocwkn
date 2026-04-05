using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placed on a building's GameObject while it is under construction.
/// Manages material delivery; completes instantly once all materials arrive.
/// </summary>
public class BuildSite : MonoBehaviour
{
    public BuildingDefinition Definition { get; private set; }

    public bool IsComplete { get; private set; }

    int _materialsRequired;
    int _materialsDelivered;

    // Visual roots
    GameObject _scaffoldVisual;
    GameObject _completedVisual;

    // Materials piled up at the site (destroyed on completion)
    readonly List<GameObject> _pile = new();

    public void Initialize(BuildingDefinition def, Vector3 worldPos)
    {
        Definition = def;
        transform.position = worldPos;

        _scaffoldVisual  = CreateOpenBox("Scaffold",      def, worldPos, HexColor("F0C040"));
        _completedVisual = CreateOpenBox(def.buildingName, def, worldPos, HexColor("777777"),
                                         addObstacle: true);
        _completedVisual.SetActive(false);

        foreach (var cost in def.materialCost)
            _materialsRequired += cost.amount;

        SpawnMaterialFetchTasks(def);

        GameEvents.RaiseBuildingPlaced(def, worldPos);
    }

    void SpawnMaterialFetchTasks(BuildingDefinition def)
    {
        foreach (var cost in def.materialCost)
        {
            for (int i = 0; i < cost.amount; i++)
            {
                Vector3 srcPos = BuildingManager.Instance.StorehousePosition;
                GameObject matObj = ResourceVisuals.Spawn(cost.type, srcPos);

                var deliverTask = new VillagerTask
                {
                    Type                = TaskType.DeliverResource,
                    TargetPosition      = transform.position,
                    TargetObject        = gameObject,
                    DepositAtDestination = true,
                    OnComplete          = OnMaterialDelivered
                };

                var pickupTask = new VillagerTask
                {
                    Type           = TaskType.PickUpResource,
                    TargetPosition = srcPos,
                    TargetObject   = matObj,
                    FollowUp       = deliverTask
                };

                TaskDispatcher.Instance.EnqueueTask(pickupTask);
            }
        }
    }

    void OnMaterialDelivered()
    {
        _materialsDelivered++;
        if (_materialsDelivered >= _materialsRequired)
            Complete();
    }

    /// <summary>
    /// Called by VillagerAgent on deposit. Positions the object in a visible pile.
    /// </summary>
    public void ReceiveMaterial(GameObject mat)
    {
        mat.transform.SetParent(transform, worldPositionStays: false);
        mat.transform.localScale = Vector3.one * 0.4f;

        int idx = _pile.Count;
        int col = idx % 2;
        int row = idx / 2;
        mat.transform.localPosition = new Vector3(
            -0.35f + col * 0.7f,
            0.2f   + row * 0.45f,
            0f);

        _pile.Add(mat);
    }

    /// <summary>No-op — kept for API compatibility.</summary>
    public void AddProgress(float delta) { }

    void Complete()
    {
        IsComplete = true;

        if (_scaffoldVisual != null)  Destroy(_scaffoldVisual);
        if (_completedVisual != null) _completedVisual.SetActive(true);

        foreach (var mat in _pile) if (mat != null) Destroy(mat);
        _pile.Clear();

        BuildingManager.Instance.RegisterBuilding(Definition, transform.position);
        GridManager.Instance?.RebakeNavMesh();

        if (Definition.associatedCard != null)
            DeckManager.Instance.AddCardToDeck(Definition.associatedCard);

        GameEvents.RaiseBuildingCompleted(Definition);

        enabled = false;
    }

    // ── Visual helpers ────────────────────────────────────────────────────────
    // Open-top box (4 walls + floor, no roof) so resources inside are visible.

    static GameObject CreateOpenBox(string rootName, BuildingDefinition def, Vector3 pos,
                                    Color color, bool addObstacle = false)
    {
        float w = def.widthCells;
        float d = def.depthCells;
        const float h = 2f;
        const float t = 0.12f;

        var root = new GameObject(rootName);
        root.transform.position = pos;

        var mat = ResourceVisuals.CreateUnlit(color);

        // Floor (sits at ground level, thin)
        AddBox(root, "Floor", new Vector3(0, t / 2f, 0), new Vector3(w, t, d), mat);

        // Walls
        float hy = h / 2f + t;
        AddBox(root, "Wall_N", new Vector3(0,  hy,  d / 2f - t / 2f), new Vector3(w, h, t), mat);
        AddBox(root, "Wall_S", new Vector3(0,  hy, -d / 2f + t / 2f), new Vector3(w, h, t), mat);
        AddBox(root, "Wall_E", new Vector3( w / 2f - t / 2f, hy, 0),  new Vector3(t, h, d), mat);
        AddBox(root, "Wall_W", new Vector3(-w / 2f + t / 2f, hy, 0),  new Vector3(t, h, d), mat);

        if (addObstacle)
            root.AddComponent<UnityEngine.AI.NavMeshObstacle>().carving = true;

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
        Object.Destroy(go.GetComponent<Collider>());
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}

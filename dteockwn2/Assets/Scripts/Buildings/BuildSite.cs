using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Placed on a building's GameObject while it is under construction.
/// Manages material delivery; completes instantly once all materials arrive.
/// </summary>
public class BuildSite : MonoBehaviour
{
    public BuildingDefinition Definition { get; private set; }

    public bool IsComplete { get; private set; }

    public int HammerCost     { get; private set; }
    public int HammersApplied { get; private set; }

    public float HammerProgressFraction =>
        HammerCost > 0 ? (float)HammersApplied / HammerCost : 1f;

    int _materialsRequired;
    int _materialsDelivered;

    // Per-type tracking for the label
    readonly Dictionary<ResourceType, int> _requiredByType  = new();
    readonly Dictionary<ResourceType, int> _deliveredByType = new();

    // Visual roots
    GameObject         _scaffoldVisual;
    GameObject         _completedVisual;
    GameObject         _hammerProgressBar;
    TextMeshPro        _requirementsLabel;

    // Materials piled up at the site (destroyed on completion)
    readonly List<GameObject> _pile = new();

    public void Initialize(BuildingDefinition def, Vector3 worldPos)
    {
        Definition = def;
        transform.position = worldPos;
        HammerCost = def.hammerCost;

        // Trigger collider so the player can click this site to apply a hammer
        var clickCol = gameObject.AddComponent<BoxCollider>();
        clickCol.isTrigger = true;
        clickCol.size      = new Vector3(def.widthCells, 2.5f, def.depthCells);
        clickCol.center    = new Vector3(0f, 1.25f, 0f);

        _scaffoldVisual  = CreateOpenBox("Scaffold",      def, worldPos, HexColor("F0C040"));
        _completedVisual = CreateOpenBox(def.buildingName, def, worldPos, HexColor("777777"));
        _completedVisual.SetActive(false);

        CreateHammerProgressBar(def, worldPos);
        CreateRequirementsLabel(def, worldPos);

        foreach (var cost in def.materialCost)
        {
            _materialsRequired += cost.amount;
            _requiredByType[cost.type]  = cost.amount;
            _deliveredByType[cost.type] = 0;
        }

        UpdateRequirementsLabel();
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

                var resourceType = cost.type; // capture for closure
                var deliverTask = new VillagerTask
                {
                    Type                 = TaskType.DeliverResource,
                    TargetPosition       = transform.position,
                    TargetObject         = gameObject,
                    DepositAtDestination = true,
                    OnComplete           = () => OnMaterialDelivered(resourceType)
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

    void OnMaterialDelivered(ResourceType type)
    {
        _materialsDelivered++;
        if (_deliveredByType.ContainsKey(type))
            _deliveredByType[type]++;
        UpdateRequirementsLabel();
        CheckCompletion();
    }

    /// <summary>
    /// Called by ConstructionQueue each Evening with available Hammers.
    /// Returns the number of Hammers not consumed (overflow for next project).
    /// </summary>
    public int ApplyHammers(int amount)
    {
        int needed    = HammerCost - HammersApplied;
        int consumed  = Mathf.Min(amount, needed);
        HammersApplied += consumed;
        UpdateProgressBar();
        UpdateRequirementsLabel();
        CheckCompletion();
        return amount - consumed;
    }

    /// <summary>
    /// Spend one hammer from the player's bank and apply it here.
    /// Returns true if a hammer was available and applied.
    /// </summary>
    public bool TryApplyOneHammer()
    {
        if (IsComplete) return false;
        if (!HammerManager.Instance.TrySpend(1)) return false;
        ApplyHammers(1);
        return true;
    }

    void CheckCompletion()
    {
        if (_materialsDelivered >= _materialsRequired && HammersApplied >= HammerCost)
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

    void Complete()
    {
        IsComplete = true;

        // Remove click collider — building is done
        if (TryGetComponent<BoxCollider>(out var col)) Destroy(col);

        if (_scaffoldVisual != null)                        Destroy(_scaffoldVisual);
        if (_hammerProgressBar != null)                     Destroy(_hammerProgressBar);
        if (_requirementsLabel != null)                     Destroy(_requirementsLabel.gameObject);
        if (_completedVisual != null)                       _completedVisual.SetActive(true);

        foreach (var mat in _pile) if (mat != null) Destroy(mat);
        _pile.Clear();

        BuildingManager.Instance.RegisterBuilding(Definition, transform.position, _completedVisual);
        GridManager.Instance?.RegisterBuildingFootprint(
            transform.position, Definition.widthCells, Definition.depthCells);

        if (Definition.associatedCard != null)
            DeckManager.Instance.AddCardToDeck(
                Definition.associatedCard.CreateBoundInstance(transform.position));

        GameEvents.RaiseBuildingCompleted(Definition);

        enabled = false;
    }

    // ── Requirements label ────────────────────────────────────────────────────────

    void CreateRequirementsLabel(BuildingDefinition def, Vector3 worldPos)
    {
        var go = new GameObject("RequirementsLabel");
        go.transform.position = worldPos + new Vector3(0f, 3.6f, 0f);
        // Billboard: face camera each frame
        go.AddComponent<BillboardLabel>();

        _requirementsLabel = go.AddComponent<TextMeshPro>();
        _requirementsLabel.fontSize        = 2.2f;
        _requirementsLabel.alignment       = TextAlignmentOptions.Center;
        _requirementsLabel.color           = Color.white;
        _requirementsLabel.outlineWidth    = 0.2f;
        _requirementsLabel.outlineColor    = Color.black;
    }

    void UpdateRequirementsLabel()
    {
        if (_requirementsLabel == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<b>{Definition.buildingName}</b>");

        foreach (var cost in Definition.materialCost)
        {
            int delivered = _deliveredByType.TryGetValue(cost.type, out int d) ? d : 0;
            int needed    = cost.amount - delivered;
            string color  = needed <= 0 ? "#88FF88" : "#FFFFFF";
            sb.AppendLine($"<color={color}>{cost.type}: {delivered}/{cost.amount}</color>");
        }

        sb.Append($"<color=#FFD700>\u26d3 {HammersApplied}/{HammerCost}</color>");

        _requirementsLabel.text = sb.ToString();
    }

    // ── Hammer progress bar ───────────────────────────────────────────────────────

    void CreateHammerProgressBar(BuildingDefinition def, Vector3 worldPos)
    {
        float w = def.widthCells;
        const float barHeight = 0.15f;
        const float barDepth  = 0.15f;
        const float yPos      = 2.3f;

        _hammerProgressBar = new GameObject("HammerProgressBar");
        _hammerProgressBar.transform.position = worldPos;

        var track = GameObject.CreatePrimitive(PrimitiveType.Cube);
        track.name = "Track";
        track.transform.SetParent(_hammerProgressBar.transform, false);
        track.transform.localPosition = new Vector3(0, yPos, 0);
        track.transform.localScale    = new Vector3(w, barHeight, barDepth);
        track.GetComponent<Renderer>().material = ResourceVisuals.CreateUnlit(new Color(0.2f, 0.2f, 0.2f));
        Object.Destroy(track.GetComponent<Collider>());

        var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fill.name = "Fill";
        fill.transform.SetParent(_hammerProgressBar.transform, false);
        fill.transform.localPosition = new Vector3(-w / 2f, yPos, 0);
        fill.transform.localScale    = new Vector3(0f, barHeight * 1.05f, barDepth * 1.1f);
        fill.GetComponent<Renderer>().material = ResourceVisuals.CreateUnlit(new Color(1f, 0.85f, 0.1f));
        Object.Destroy(fill.GetComponent<Collider>());
    }

    void UpdateProgressBar()
    {
        if (_hammerProgressBar == null) return;
        var fill = _hammerProgressBar.transform.Find("Fill");
        if (fill == null) return;

        float w    = Definition.widthCells;
        float frac = HammerProgressFraction;
        float fillW = w * frac;
        fill.localScale    = new Vector3(fillW, fill.localScale.y, fill.localScale.z);
        fill.localPosition = new Vector3(-w / 2f + fillW / 2f, fill.localPosition.y, fill.localPosition.z);
    }

    // ── Visual helpers ────────────────────────────────────────────────────────
    // Open-top box (4 walls + floor, no roof) so resources inside are visible.

    static GameObject CreateOpenBox(string rootName, BuildingDefinition def, Vector3 pos,
                                    Color color)
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

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

/// <summary>
/// Bootstraps the entire game scene at runtime.
/// Attach to any GameObject in SampleScene; it creates all managers, world, villagers, and UI.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    // --- Optional inspector overrides for starter assets ---
    [Header("Starter Assets (optional — auto-created if null)")]
    [SerializeField] BuildingDefinition woodcutterDef;
    [SerializeField] BuildingDefinition cottageDef;
    [SerializeField] BuildingDefinition stonecutterDef;
    [SerializeField] BuildingDefinition farmDef;
    [SerializeField] BuildingDefinition granaryDef;
    [SerializeField] BuildingDefinition guildhallDef;
    [SerializeField] BuildingDefinition builderHutDef;

    Material   _whiteMat;

    void Awake()
    {
        CreateMaterials();
        CreateManagers();
    }

    void Start()
    {
        SetupCamera();

        var terrain = CreateTerrain();
        TerrainManager.Instance.Generate(Random.Range(0, 9999));
        CreateStorehouse();
        CreateGranary();
        EnsureBuildingAssets(); // needed before CreateBuildersHut
        CreateBuildersHut();

        // Spawn 5 starting villagers evenly distributed in a ring around the storehouse.
        var initialVillagers = new System.Collections.Generic.List<VillagerAgent>();
        for (int i = 0; i < 5; i++)
        {
            float angle = i * Mathf.PI * 2f / 5f;
            var   pos   = new Vector3(Mathf.Cos(angle) * 4f, 0f, Mathf.Sin(angle) * 4f);
            initialVillagers.Add(CreateVillager(pos, $"Villager_{i + 1:D2}"));
        }

        GridManager.Instance.Initialize(terrain.GetComponent<MeshFilter>());

        SetupStartingInventory();
        SetupStartingDeck(initialVillagers);
        SetupMarket();

        CreateUI();
    }

    // ── Managers ─────────────────────────────────────────────────────────────

    void CreateManagers()
    {
        var root = new GameObject("_GameManager");

        AddSingleton<GameEvents>(root);
        AddSingleton<GameInventory>(root);
        AddSingleton<HammerManager>(root);
        AddSingleton<DeckManager>(root);
        AddSingleton<TaskDispatcher>(root);
        AddSingleton<BuildingManager>(root);
        AddSingleton<ConstructionQueue>(root);
        AddSingleton<MarketManager>(root);
        AddSingleton<VillagerManager>(root);
        AddSingleton<GridManager>(root);
        AddSingleton<TerrainManager>(root);
        AddSingleton<SeasonManager>(root);
        AddSingleton<FoodConsumptionManager>(root);
        AddSingleton<RoadManager>(root);
        AddSingleton<RoadTool>(root);
        AddSingleton<ClearLandTool>(root);
        AddSingleton<HammerInputHandler>(root);
        AddSingleton<PlacementMode>(root);
    }

    static T AddSingleton<T>(GameObject parent) where T : Component
    {
        var go = new GameObject(typeof(T).Name);
        go.transform.SetParent(parent.transform);
        return go.AddComponent<T>();
    }

    // ── World ─────────────────────────────────────────────────────────────────

    GameObject CreateTerrain()
    {
        var worldRoot = new GameObject("World");

        var terrain = GameObject.CreatePrimitive(PrimitiveType.Plane);
        terrain.name = "Terrain";
        terrain.transform.SetParent(worldRoot.transform);
        terrain.transform.localScale = Vector3.one * 10f;
        terrain.GetComponent<Renderer>().material =
            ResourceVisuals.CreateUnlit(ResourceVisuals.HexColor("CCCCCC"));

        BuildingManager.Instance.SetStorehousePosition(Vector3.zero);

        return terrain;
    }

    void CreateStorehouse()
    {
        var buildings = new GameObject("Buildings");
        StorehouseVisual.Create(Vector3.zero, buildings);
        GridManager.Instance.OccupyCells(3, 3, new Vector2Int(-1, -1));
        GridManager.Instance.RegisterBuildingFootprint(Vector3.zero, 3, 3);
    }

    void CreateGranary()
    {
        var buildings = GameObject.Find("Buildings") ?? new GameObject("Buildings");
        var pos = new Vector3(5f, 0f, 0f);
        GranaryVisual.Create(pos, buildings);
        BuildingManager.Instance.SetGranaryPosition(pos);
        GridManager.Instance.OccupyCells(3, 3, new Vector2Int(2, -1));
        GridManager.Instance.RegisterBuildingFootprint(pos, 3, 3);
    }

    void CreateBuildersHut()
    {
        var buildings = GameObject.Find("Buildings") ?? new GameObject("Buildings");
        var pos       = new Vector3(-5f, 0f, 0f);

        // Simple visual: a brownish box representing the hut.
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Builder's Hut";
        go.transform.SetParent(buildings.transform);
        go.transform.position   = pos + new Vector3(0f, 0.75f, 0f);
        go.transform.localScale = new Vector3(1.8f, 1.5f, 1.8f);
        go.GetComponent<Renderer>().material =
            ResourceVisuals.CreateUnlit(ResourceVisuals.HexColor("8B7355"));
        if (go.TryGetComponent<Collider>(out var col)) Destroy(col);

        BuildingManager.Instance.RegisterBuilding(builderHutDef, pos, go);
        GridManager.Instance.OccupyCells(2, 2, new Vector2Int(-6, -1));
        GridManager.Instance.RegisterBuildingFootprint(pos, 2, 2);

        // All unassigned villagers automatically work here.
        VillagerManager.Instance.SetFallbackWorkplace(builderHutDef, pos);
    }

    // ── Villagers ─────────────────────────────────────────────────────────────

    VillagerAgent CreateVillager(Vector3 pos, string villageName)
    {
        var villagers = GameObject.Find("Villagers") ?? new GameObject("Villagers");

        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = villageName;
        go.transform.SetParent(villagers.transform);
        go.transform.position   = pos;
        go.transform.localScale = new Vector3(1f, 0.9f, 1f);

        go.GetComponent<Renderer>().material = _whiteMat;

        var navAgent = go.AddComponent<NavMeshAgent>();
        navAgent.radius           = 0.4f;
        navAgent.height           = 1.8f;
        navAgent.speed            = 3.5f;
        navAgent.angularSpeed     = 120f;
        navAgent.acceleration     = 8f;
        navAgent.stoppingDistance = 0.3f;

        go.AddComponent<VillagerNeeds>();
        go.AddComponent<VillagerAnimator>();
        return go.AddComponent<VillagerAgent>();
    }

    // ── Camera ────────────────────────────────────────────────────────────────

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam != null && cam.GetComponent<CameraController>() == null)
            cam.gameObject.AddComponent<CameraController>();
    }

    // ── Starting state ────────────────────────────────────────────────────────

    void SetupStartingInventory()
    {
        GameInventory.Instance.Add(ResourceType.Wood,  5);
        GameInventory.Instance.Add(ResourceType.Stone, 3);
        GameInventory.Instance.Add(ResourceType.Food,  20);
    }

    void SetupStartingDeck(System.Collections.Generic.List<VillagerAgent> initialVillagers)
    {
        var cards = new List<CardData>();

        // One personal Villager card per starting villager.
        // Each card is linked to its owner; VillagerManager will mutate it when a job is assigned.
        var hammerEffect = ScriptableObject.CreateInstance<GiveHammerEffect>();
        foreach (var agent in initialVillagers)
        {
            var c = ScriptableObject.CreateInstance<CardData>();
            c.cardName    = "Villager";
            c.description = "A villager puts in a day's work. +1 Hammer.";
            c.type        = CardType.Villager;
            c.effect      = hammerEffect;
            agent.OwnedCard = c;
            cards.Add(c);
        }

        DeckManager.Instance.SetStartingDeck(cards);
    }

    void SetupMarket()
    {
        EnsureBuildingAssets();

        var market = MarketManager.Instance;
        market.AddEntry(new MarketEntry { building = woodcutterDef,  unlocked = true });
        market.AddEntry(new MarketEntry { building = cottageDef,     unlocked = true });
        market.AddEntry(new MarketEntry { building = stonecutterDef, unlocked = true });
        market.AddEntry(new MarketEntry { building = farmDef,        unlocked = true });
        market.AddEntry(new MarketEntry { building = granaryDef,    unlocked = true });
        market.AddEntry(new MarketEntry { building = guildhallDef,  unlocked = true });
    }

    // ── Asset creation (runtime fallback) ─────────────────────────────────────

    void EnsureBuildingAssets()
    {
        if (woodcutterDef == null)
        {
            woodcutterDef = ScriptableObject.CreateInstance<BuildingDefinition>();
            woodcutterDef.buildingName  = "Woodcutter's Hut";
            woodcutterDef.description   = "Puts a villager to work felling timber.";
            woodcutterDef.widthCells    = 2;
            woodcutterDef.depthCells    = 2;
            woodcutterDef.materialCost  = new List<ResourceCost>
                { new(ResourceType.Wood, 2), new(ResourceType.Stone, 1) };
            woodcutterDef.hammerCost    = 4;
            woodcutterDef.type         = BuildingType.Job;
            woodcutterDef.jobSlots     = 1;

            var hutEffect = ScriptableObject.CreateInstance<WoodcutterHutEffect>();
            var hutCard              = ScriptableObject.CreateInstance<CardData>();
            hutCard.cardName         = "Woodcutter's Hut";
            hutCard.description      = "Produce 3 Logs into the Town Buffer.";
            hutCard.type             = CardType.Building;
            hutCard.effect           = hutEffect;
            woodcutterDef.associatedCard = hutCard;
        }

        if (cottageDef == null)
        {
            cottageDef = ScriptableObject.CreateInstance<BuildingDefinition>();
            cottageDef.buildingName  = "Cottage";
            cottageDef.description   = "Housing for two villagers.";
            cottageDef.widthCells    = 2;
            cottageDef.depthCells    = 2;
            cottageDef.materialCost  = new List<ResourceCost>
                { new(ResourceType.Wood, 3), new(ResourceType.Stone, 1) };
            cottageDef.hammerCost    = 2;
            cottageDef.type         = BuildingType.Housing;
            cottageDef.housingCapacity = 2;

            var cottageEffect            = ScriptableObject.CreateInstance<CottageEffect>();
            var cottageCard              = ScriptableObject.CreateInstance<CardData>();
            cottageCard.cardName         = "Cottage";
            cottageCard.description      = "Housing +2. Gain 1 villager next morning.";
            cottageCard.type             = CardType.Building;
            cottageCard.effect           = cottageEffect;
            cottageDef.associatedCard    = cottageCard;
        }

        if (stonecutterDef == null)
        {
            stonecutterDef = ScriptableObject.CreateInstance<BuildingDefinition>();
            stonecutterDef.buildingName    = "Stonecutter";
            stonecutterDef.description     = "Must be built on stone terrain. Cuts stone for the town.";
            stonecutterDef.widthCells      = 2;
            stonecutterDef.depthCells      = 2;
            stonecutterDef.materialCost    = new List<ResourceCost> { new(ResourceType.Wood, 4) };
            stonecutterDef.hammerCost      = 4;
            stonecutterDef.type            = BuildingType.Job;
            stonecutterDef.jobSlots        = 1;
            stonecutterDef.requiredTerrain = TerrainType.Stone;

            var stonecutterEffect        = ScriptableObject.CreateInstance<StonecutterEffect>();
            var stonecutterCard          = ScriptableObject.CreateInstance<CardData>();
            stonecutterCard.cardName     = "Stonecutter";
            stonecutterCard.description  = "Cut 3 Stone from the quarry.";
            stonecutterCard.type         = CardType.Building;
            stonecutterCard.effect       = stonecutterEffect;
            stonecutterDef.associatedCard = stonecutterCard;
        }

        if (farmDef == null)
        {
            farmDef = ScriptableObject.CreateInstance<BuildingDefinition>();
            farmDef.buildingName  = "Farm";
            farmDef.description   = "Plant seeds in Spring/Summer. Harvest 5 Food in Autumn.";
            farmDef.widthCells    = 2;
            farmDef.depthCells    = 2;
            farmDef.materialCost  = new List<ResourceCost> { new(ResourceType.Wood, 2) };
            farmDef.hammerCost    = 3;
            farmDef.type          = BuildingType.Farm;
            farmDef.jobSlots      = 2;

            var farmEffect       = ScriptableObject.CreateInstance<FarmEffect>();
            var farmCard         = ScriptableObject.CreateInstance<CardData>();
            farmCard.cardName    = "Farm";
            farmCard.description = "Spring/Summer: plant a seed. Autumn: harvest 5 Food.";
            farmCard.type        = CardType.Building;
            farmCard.effect      = farmEffect;
            farmDef.associatedCard = farmCard;
        }

        if (granaryDef == null)
        {
            granaryDef = ScriptableObject.CreateInstance<BuildingDefinition>();
            granaryDef.buildingName = "Granary";
            granaryDef.description  = "Stores food. Build near your farms.";
            granaryDef.widthCells   = 3;
            granaryDef.depthCells   = 3;
            granaryDef.materialCost = new List<ResourceCost>
                { new(ResourceType.Wood, 4), new(ResourceType.Stone, 2) };
            granaryDef.hammerCost   = 5;
            granaryDef.type         = BuildingType.Granary;
            // No associated card — granary is passive storage.
        }

        if (guildhallDef == null)
        {
            guildhallDef = ScriptableObject.CreateInstance<BuildingDefinition>();
            guildhallDef.buildingName = "Guildhall";
            guildhallDef.description  = "Replaces the Builder's Hut. All unoccupied villagers produce 2 Hammers each.";
            guildhallDef.widthCells   = 3;
            guildhallDef.depthCells   = 3;
            guildhallDef.materialCost = new List<ResourceCost>
                { new(ResourceType.Wood, 10), new(ResourceType.Stone, 10) };
            guildhallDef.hammerCost   = 10;
            guildhallDef.type         = BuildingType.Guildhall;
            guildhallDef.jobSlots     = -1; // unlimited
            // No associated card — job effects come through villager card mutation.
        }

        if (builderHutDef == null)
        {
            builderHutDef = ScriptableObject.CreateInstance<BuildingDefinition>();
            builderHutDef.buildingName = "Builder's Hut";
            builderHutDef.description  = "Any villager not assigned elsewhere helps with construction. +1 Hammer each.";
            builderHutDef.widthCells   = 2;
            builderHutDef.depthCells   = 2;
            builderHutDef.materialCost = new List<ResourceCost>();
            builderHutDef.hammerCost   = 0;
            builderHutDef.type         = BuildingType.BuildersHut;
            builderHutDef.jobSlots     = -1; // unlimited
        }
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    void CreateUI()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var canvasGo = new GameObject("UI");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>();

        CreateInventoryHUD(canvasGo);
        CreateDebugPanel(canvasGo);
        CreateTaskDebugPanel(canvasGo);
        CreateJobAssignmentPanel();
        CreateHandUI(canvasGo);
        CreateMarketUI(canvasGo);
        CreateEndDayButton(canvasGo);
        CreateRoadToolButton(canvasGo);
        CreateClearLandButton(canvasGo);

    }

    void CreateInventoryHUD(GameObject canvas)
    {
        var go = MakePanel("InventoryHUD", canvas,
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-10f, -10f), new Vector2(-180f, -10f),
            new Vector2(170f, 130f));
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        MakeText(go, "");
        go.AddComponent<InventoryHUD>();
        go.AddComponent<DraggablePanel>();
    }

    void CreateDebugPanel(GameObject canvas)
    {
        var go = MakePanel("DebugPanel", canvas,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -10f), new Vector2(10f, -10f),
            new Vector2(240f, 200f));
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        MakeText(go, "");
        go.AddComponent<DebugPanel>();
        go.AddComponent<DraggablePanel>();
    }

    void CreateTaskDebugPanel(GameObject canvas)
    {
        var go = MakePanel("TaskDebugPanel", canvas,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -220f), new Vector2(10f, -220f),
            new Vector2(320f, 300f));
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);
        MakeText(go, "");
        go.AddComponent<TaskDebugPanel>();
        go.AddComponent<DraggablePanel>();
    }

    void CreateHandUI(GameObject canvas)
    {
        var go = MakePanel("HandUI", canvas,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 10f), new Vector2(0f, 10f),
            new Vector2(900f, 160f));
        go.AddComponent<HandUI>();
    }

    void CreateEndDayButton(GameObject canvas)
    {
        var go = MakePanel("EndDayButton", canvas,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 180f), new Vector2(0f, 180f),
            new Vector2(160f, 50f));

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 0.2f, 0.9f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.8f, 0.3f, 0.9f);
        colors.pressedColor     = new Color(0.1f, 0.4f, 0.1f, 0.9f);
        btn.colors = colors;

        var label = MakeText(go, "End Day");
        label.fontSize  = 18f;
        label.alignment = TMPro.TextAlignmentOptions.Center;

        btn.onClick.AddListener(() => DeckManager.Instance?.EndDay());
    }

    /// <summary>
    /// Creates the IMGUI Job Assignment debug panel (not attached to the Canvas —
    /// JobAssignmentPanel draws itself with OnGUI and only needs a MonoBehaviour host).
    /// </summary>
    void CreateJobAssignmentPanel()
    {
        var go = new GameObject("JobAssignmentPanel");
        go.AddComponent<JobAssignmentPanel>();
    }

    void CreateRoadToolButton(GameObject canvas)
    {
        // Positioned to the right of the End Day button, same vertical level
        var go = MakePanel("RoadToolButton", canvas,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(190f, 180f), new Vector2(190f, 180f),
            new Vector2(130f, 50f));

        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.25f, 0.90f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var label = MakeText(go, "Road (R)");
        label.fontSize  = 16f;
        label.alignment = TMPro.TextAlignmentOptions.Center;

        btn.onClick.AddListener(() => RoadTool.Instance?.Toggle());

        // Wire button image into RoadTool so it highlights when the mode is active
        RoadTool.Instance?.SetButtonRef(img);
    }

    void CreateClearLandButton(GameObject canvas)
    {
        // Positioned to the right of the Road button
        var go = MakePanel("ClearLandButton", canvas,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(350f, 180f), new Vector2(350f, 180f),
            new Vector2(130f, 50f));

        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.25f, 0.90f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var label = MakeText(go, "Clear (C)");
        label.fontSize  = 16f;
        label.alignment = TMPro.TextAlignmentOptions.Center;

        btn.onClick.AddListener(() => ClearLandTool.Instance?.Toggle());

        ClearLandTool.Instance?.SetButtonRef(img);
    }

    void CreateMarketUI(GameObject canvas)
    {
        // Panel anchored to top-right, below InventoryHUD
        var panel = MakePanel("MarketUI", canvas,
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-260f, -150f), new Vector2(-260f, -150f),
            new Vector2(500f, 400f));
        panel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        panel.AddComponent<DraggablePanel>();

        var list = new GameObject("List", typeof(RectTransform));
        list.transform.SetParent(panel.transform, false);
        var listRt = list.GetComponent<RectTransform>();
        listRt.anchorMin = Vector2.zero;
        listRt.anchorMax = Vector2.one;
        listRt.offsetMin = new Vector2(10, 10);
        listRt.offsetMax = new Vector2(-10, -10);

        var vlg = list.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 8;
        vlg.childControlHeight   = false;
        vlg.childForceExpandHeight = false;

        panel.AddComponent<MarketUI>();
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    static GameObject MakePanel(string name, GameObject parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 pivot,
        Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = anchorMin;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        return go;
    }

    static TextMeshProUGUI MakeText(GameObject parent, string text)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(6, 6);
        rt.offsetMax = new Vector2(-6, -6);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text     = text;
        tmp.fontSize = 14f;
        tmp.color    = Color.white;
        return tmp;
    }

    void CreateMaterials() =>
        _whiteMat = ResourceVisuals.CreateUnlit(Color.white);
}

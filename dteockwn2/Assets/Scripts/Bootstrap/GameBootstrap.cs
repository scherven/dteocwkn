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
    // If left null, assets are created in-memory for Play mode.
    [Header("Starter Assets (optional — auto-created if null)")]
    [SerializeField] BuildingDefinition woodcutterDef;
    [SerializeField] BuildingDefinition cottageDef;
    [SerializeField] CardData woodCard;
    [SerializeField] CardData stoneCard;

    Material _whiteMat;

    void Awake()
    {
        CreateMaterials();
        CreateManagers();
    }

    void Start()
    {
        SetupCamera();

        // World must exist before NavMesh bake
        var terrain = CreateTerrain();
        CreateStorehouse();
        CreateVillager(new Vector3(3f, 0f, 0f),  "Villager_01");
        CreateVillager(new Vector3(-3f, 0f, 0f), "Villager_02");

        // Bake NavMesh after terrain is created
        GridManager.Instance.Initialize(terrain.GetComponent<MeshFilter>());

        // Set up starting state
        SetupStartingInventory();
        SetupStartingDeck();
        SetupMarket();

        // UI last (needs DeckManager etc. to be ready)
        CreateUI();
    }

    // ── Managers ─────────────────────────────────────────────────────────────

    void CreateManagers()
    {
        var root = new GameObject("_GameManager");

        AddSingleton<GameEvents>(root);
        AddSingleton<GameInventory>(root);
        AddSingleton<DeckManager>(root);
        AddSingleton<TaskDispatcher>(root);
        AddSingleton<BuildingManager>(root);
        AddSingleton<MarketManager>(root);
        AddSingleton<VillagerManager>(root);
        AddSingleton<GridManager>(root);
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

        // 100×100 plane (Unity Plane is 10 units; scale by 10 to get 100)
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
        // Storehouse occupies the center cells (3×3 around origin)
        GridManager.Instance.OccupyCells(3, 3, new Vector2Int(-1, -1));
    }

    // ── Villagers ─────────────────────────────────────────────────────────────

    int _villagerIndex;

    GameObject CreateVillager(Vector3 pos, string villageName)
    {
        var villagers = GameObject.Find("Villagers") ?? new GameObject("Villagers");

        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = villageName;
        go.transform.SetParent(villagers.transform);
        go.transform.position   = pos;
        go.transform.localScale = new Vector3(1f, 0.9f, 1f); // radius 0.5, height ~1.8

        go.GetComponent<Renderer>().material = _whiteMat;

        var agent = go.AddComponent<NavMeshAgent>();
        agent.radius          = 0.4f;
        agent.height          = 1.8f;
        agent.speed           = 3.5f;
        agent.angularSpeed    = 120f;
        agent.acceleration    = 8f;
        agent.stoppingDistance = 0.3f;

        go.AddComponent<VillagerAgent>();
        go.AddComponent<VillagerNeeds>();
        go.AddComponent<VillagerAnimator>();

        return go;
    }

    // ── Camera ────────────────────────────────────────────────────────────────

    // Camera already exists in SampleScene; just add the controller.
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
    }

    void SetupStartingDeck()
    {
        EnsureCardAssets();

        var startingCards = new List<CardData>();
        for (int i = 0; i < 10; i++) startingCards.Add(woodCard);
        for (int i = 0; i < 6;  i++) startingCards.Add(stoneCard);
        DeckManager.Instance.SetStartingDeck(startingCards);
    }

    void SetupMarket()
    {
        EnsureBuildingAssets();

        var market = MarketManager.Instance;
        market.AddEntry(new MarketEntry { building = woodcutterDef, unlocked = true });
        market.AddEntry(new MarketEntry { building = cottageDef,    unlocked = true });
    }

    // ── Asset creation (runtime fallback) ─────────────────────────────────────

    void EnsureBuildingAssets()
    {
        if (woodcutterDef == null)
        {
            woodcutterDef = ScriptableObject.CreateInstance<BuildingDefinition>();
            woodcutterDef.buildingName        = "Woodcutter's Hut";
            woodcutterDef.description         = "Puts a villager to work felling timber.";
            woodcutterDef.widthCells          = 2;
            woodcutterDef.depthCells          = 2;
            woodcutterDef.materialCost        = new List<ResourceCost>
                { new(ResourceType.Wood, 2), new(ResourceType.Stone, 1) };
            woodcutterDef.constructionTimeBase = 20f;
            woodcutterDef.type                = BuildingType.Job;
            woodcutterDef.jobSlots            = 1;
            woodcutterDef.associatedCard      = woodCard; // adds a Wood card to the deck on completion
        }

        if (cottageDef == null)
        {
            cottageDef = ScriptableObject.CreateInstance<BuildingDefinition>();
            cottageDef.buildingName        = "Cottage";
            cottageDef.description         = "Housing for two villagers.";
            cottageDef.widthCells          = 2;
            cottageDef.depthCells          = 2;
            cottageDef.materialCost        = new List<ResourceCost>
                { new(ResourceType.Wood, 3), new(ResourceType.Stone, 1) };
            cottageDef.constructionTimeBase = 25f;
            cottageDef.type                = BuildingType.Housing;
            cottageDef.housingCapacity     = 2;
            cottageDef.associatedCard      = stoneCard; // adds a Stone card to the deck on completion
        }
    }

    void EnsureCardAssets()
    {
        if (woodCard == null)
        {
            var effect = ScriptableObject.CreateInstance<AddResourceEffect>();
            effect.resourceType = ResourceType.Wood;
            effect.amount       = 1;

            woodCard             = ScriptableObject.CreateInstance<CardData>();
            woodCard.cardName    = "Wood";
            woodCard.description = "A piece of timber. A villager carries it to the storehouse.";
            woodCard.type        = CardType.Resource;
            woodCard.effect      = effect;
        }

        if (stoneCard == null)
        {
            var effect = ScriptableObject.CreateInstance<AddResourceEffect>();
            effect.resourceType = ResourceType.Stone;
            effect.amount       = 1;

            stoneCard             = ScriptableObject.CreateInstance<CardData>();
            stoneCard.cardName    = "Stone";
            stoneCard.description = "A block of stone. A villager carries it to the storehouse.";
            stoneCard.type        = CardType.Resource;
            stoneCard.effect      = effect;
        }
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    void CreateUI()
    {
        // Ensure EventSystem exists
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var canvasGo = new GameObject("UI");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>();

        CreateInventoryHUD(canvasGo);
        CreateDebugPanel(canvasGo);
        CreateTaskDebugPanel(canvasGo);
        CreateHandUI(canvasGo);
        CreateMarketUI(canvasGo);
    }

    void CreateInventoryHUD(GameObject canvas)
    {
        var go = MakePanel("InventoryHUD", canvas,
            new Vector2(1f, 1f), new Vector2(1f, 1f),   // anchor top-right
            new Vector2(-10f, -10f), new Vector2(-160f, -10f),
            new Vector2(150f, 80f));
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        MakeText(go, "");
        go.AddComponent<InventoryHUD>();
        go.AddComponent<DraggablePanel>();
    }

    void CreateDebugPanel(GameObject canvas)
    {
        var go = MakePanel("DebugPanel", canvas,
            new Vector2(0f, 1f), new Vector2(0f, 1f),   // anchor top-left
            new Vector2(10f, -10f), new Vector2(10f, -10f),
            new Vector2(210f, 160f));
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        MakeText(go, "");
        go.AddComponent<DebugPanel>();
        go.AddComponent<DraggablePanel>();
    }

    // Task debug panel — anchored top-left below the main debug panel; toggle with T.
    void CreateTaskDebugPanel(GameObject canvas)
    {
        var go = MakePanel("TaskDebugPanel", canvas,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -180f), new Vector2(10f, -180f),
            new Vector2(320f, 300f));
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);
        MakeText(go, "");
        go.AddComponent<TaskDebugPanel>();
        go.AddComponent<DraggablePanel>();
    }

    void CreateHandUI(GameObject canvas)
    {
        var go = MakePanel("HandUI", canvas,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), // anchor bottom-center
            new Vector2(0f, 10f), new Vector2(0f, 10f),
            new Vector2(900f, 160f));
        go.AddComponent<HandUI>();
    }

    void CreateMarketUI(GameObject canvas)
    {
        var go = new GameObject("MarketUI");
        go.transform.SetParent(canvas.transform, false);
        go.AddComponent<MarketUI>();

        // Panel (hidden by default)
        var panel = MakePanel("Panel", go,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero,
            new Vector2(500f, 400f));
        panel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Scroll list
        var list = new GameObject("List", typeof(RectTransform));
        list.transform.SetParent(panel.transform, false);
        var listRt = list.GetComponent<RectTransform>();
        listRt.anchorMin = Vector2.zero;
        listRt.anchorMax = Vector2.one;
        listRt.offsetMin = new Vector2(10, 10);
        listRt.offsetMax = new Vector2(-10, -10);

        var vlg = list.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childControlHeight  = false;
        vlg.childForceExpandHeight = false;
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
        rt.anchorMin       = anchorMin;
        rt.anchorMax       = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta       = sizeDelta;
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
        tmp.text      = text;
        tmp.fontSize  = 14f;
        tmp.color     = Color.white;
        return tmp;
    }

    void CreateMaterials() =>
        _whiteMat = ResourceVisuals.CreateUnlit(Color.white);
}

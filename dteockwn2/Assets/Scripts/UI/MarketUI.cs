using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Market panel — always visible, draggable.
/// Shows all unlocked buildings with cost and a Buy button.
/// </summary>
public class MarketUI : MonoBehaviour
{
    Transform _listRoot;

    readonly List<GameObject> _rows = new();

    void Start()
    {
        _listRoot = transform.Find("List");
        Rebuild();
    }

    void OnEnable()
    {
        GameEvents.OnResourceAdded    += OnInventoryChange;
        GameEvents.OnResourceConsumed += OnInventoryChange;
        GameEvents.OnBuildingUnlocked += OnBuildingUnlocked;
    }

    void OnDisable()
    {
        GameEvents.OnResourceAdded    -= OnInventoryChange;
        GameEvents.OnResourceConsumed -= OnInventoryChange;
        GameEvents.OnBuildingUnlocked -= OnBuildingUnlocked;
    }

    void OnBuildingUnlocked(BuildingDefinition _) { Rebuild(); }

    void OnInventoryChange(ResourceType _, int __) { Rebuild(); }

    void Rebuild()
    {
        foreach (var r in _rows) Destroy(r);
        _rows.Clear();

        if (MarketManager.Instance == null || _listRoot == null) return;

        foreach (var entry in MarketManager.Instance.Entries)
        {
            if (!entry.unlocked) continue;
            _rows.Add(CreateRow(entry.building));
        }
    }

    GameObject CreateRow(BuildingDefinition def)
    {
        var row = new GameObject(def.buildingName, typeof(RectTransform));
        row.transform.SetParent(_listRoot, false);

        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing             = 8;
        layout.childControlHeight  = true;
        layout.childControlWidth   = false;
        layout.childForceExpandWidth = false;
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 60f);

        // Info text
        var infoGo = new GameObject("Info", typeof(RectTransform));
        infoGo.transform.SetParent(row.transform, false);
        infoGo.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 60f);
        var info = infoGo.AddComponent<TextMeshProUGUI>();
        info.text     = BuildCostString(def);
        info.fontSize = 13f;

        // Buy button
        var btnGo = new GameObject("BuyBtn", typeof(RectTransform));
        btnGo.transform.SetParent(row.transform, false);
        btnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 50f);
        btnGo.AddComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f);
        var btn      = btnGo.AddComponent<Button>();
        var captured = def;
        btn.onClick.AddListener(() => MarketManager.Instance.TryPurchase(captured));

        var btnLabel = new GameObject("Label", typeof(RectTransform));
        btnLabel.transform.SetParent(btnGo.transform, false);
        var brt = btnLabel.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        var bTmp = btnLabel.AddComponent<TextMeshProUGUI>();
        bTmp.text      = "Build";
        bTmp.alignment = TextAlignmentOptions.Center;
        bTmp.fontSize  = 13f;

        return row;
    }

    static string BuildCostString(BuildingDefinition def)
    {
        var sb = new StringBuilder();
        sb.Append($"<b>{def.buildingName}</b>\n");
        sb.Append($"<size=80%>{def.description}</size>\n");
        bool canAfford = GameInventory.Instance?.CanAfford(def.materialCost) ?? false;
        sb.Append(canAfford ? "<color=green>" : "<color=red>");
        foreach (var c in def.materialCost)
            sb.Append($"{c.type}: {c.amount}  ");
        sb.Append("</color>");
        sb.Append($"  <color=#FFD700>\u26d3 {def.hammerCost}</color>");
        return sb.ToString();
    }
}

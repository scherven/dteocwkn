using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Canvas-based panel for assigning villagers to completed job buildings.
/// Always visible; drag to reposition via DraggablePanel.
///
/// Workflow:
///   1. Click a villager name in "Unassigned" to select them (highlighted in yellow).
///   2. Click "Assign here" under any building with a free slot to assign the selected villager.
///   3. Click [X] next to an assigned villager to unassign them.
///   4. Clicking an already-selected villager deselects them.
/// </summary>
public class JobAssignmentPanel : MonoBehaviour
{
    Transform     _contentRoot;
    VillagerAgent _selected;

    readonly List<GameObject> _rows = new();

    public void Init(Transform contentRoot)
    {
        _contentRoot = contentRoot;
    }

    void OnEnable()
    {
        GameEvents.OnBuildingCompleted    += OnBuildingCompleted;
        GameEvents.OnVillagerCountChanged += OnVillagerCountChanged;
        Rebuild();
    }

    void OnDisable()
    {
        GameEvents.OnBuildingCompleted    -= OnBuildingCompleted;
        GameEvents.OnVillagerCountChanged -= OnVillagerCountChanged;
    }

    void OnBuildingCompleted(BuildingDefinition _) { Rebuild(); }
    void OnVillagerCountChanged(int _) { Rebuild(); }

    void Rebuild()
    {
        foreach (var r in _rows) Destroy(r);
        _rows.Clear();

        if (_contentRoot == null) return;

        var bm = BuildingManager.Instance;
        var vm = VillagerManager.Instance;
        if (bm == null || vm == null) return;

        // ── Buildings section ────────────────────────────────────────────────
        _rows.Add(MakeLabel("── Buildings with job slots ──", bold: true));

        bool anyJobBuilding = false;
        foreach (var (def, pos, _) in bm.AllBuildings)
        {
            if (def.jobSlots <= 0) continue;
            anyJobBuilding = true;

            List<VillagerAgent> occupants = vm.GetOccupantsFor(def, pos);
            bool unlimited = def.jobSlots < 0;
            int  freeSlots = unlimited ? 1 : def.jobSlots - occupants.Count;
            string slotText = unlimited ? $"{occupants.Count}/∞" : $"{occupants.Count}/{def.jobSlots}";

            _rows.Add(MakeLabel($"{def.buildingName}  ({slotText})"));

            foreach (var occ in occupants)
            {
                var captured = occ;
                _rows.Add(MakeButtonRow(
                    $"  · {occ.name}",
                    "X", new Color(0.6f, 0.2f, 0.2f),
                    () => { vm.UnassignJob(captured); if (_selected == captured) _selected = null; Rebuild(); }));
            }

            if (freeSlots > 0 && _selected != null && vm.GetJobDef(_selected) == null)
            {
                var capDef = def;
                var capPos = pos;
                var capSel = _selected;
                _rows.Add(MakeButton(
                    $"  + Assign {_selected.name} here",
                    new Color(0.2f, 0.6f, 0.2f),
                    () => { vm.AssignJob(capSel, capDef, capPos); _selected = null; Rebuild(); }));
            }
            else if (freeSlots > 0)
            {
                _rows.Add(MakeLabel("  (select a villager below to assign)", grey: true));
            }

            _rows.Add(MakeSpacer(4f));
        }

        if (!anyJobBuilding)
            _rows.Add(MakeLabel("  No job buildings completed yet.", grey: true));

        _rows.Add(MakeSpacer(6f));

        // ── Unassigned villagers ─────────────────────────────────────────────
        _rows.Add(MakeLabel("── Unassigned Villagers ──", bold: true));

        bool anyUnassigned = false;
        foreach (var v in vm.AllVillagers)
        {
            if (vm.GetJobDef(v) != null) continue;
            anyUnassigned = true;

            bool isSelected = _selected == v;
            var captured = v;
            _rows.Add(MakeButton(
                isSelected ? $"» {v.name}  (selected)" : v.name,
                isSelected ? new Color(0.6f, 0.5f, 0f) : new Color(0.25f, 0.25f, 0.25f),
                () => { _selected = (_selected == captured) ? null : captured; Rebuild(); }));
        }

        if (!anyUnassigned)
            _rows.Add(MakeLabel("  All villagers have jobs.", grey: true));

        _rows.Add(MakeSpacer(6f));

        // ── Assigned villagers ───────────────────────────────────────────────
        _rows.Add(MakeLabel("── Assigned Villagers ──", bold: true));

        bool anyAssigned = false;
        foreach (var v in vm.AllVillagers)
        {
            var jobDef = vm.GetJobDef(v);
            if (jobDef == null) continue;
            anyAssigned = true;

            var captured = v;
            _rows.Add(MakeButtonRow(
                $"  {v.name}  →  {jobDef.buildingName}",
                "X", new Color(0.6f, 0.2f, 0.2f),
                () => { vm.UnassignJob(captured); Rebuild(); }));
        }

        if (!anyAssigned)
            _rows.Add(MakeLabel("  No villagers assigned.", grey: true));
    }

    // ── UI row builders ───────────────────────────────────────────────────────

    GameObject MakeLabel(string text, bool bold = false, bool grey = false)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(_contentRoot, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 22f;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text     = bold ? $"<b>{text}</b>" : text;
        tmp.fontSize = 12f;
        tmp.color    = grey ? new Color(0.6f, 0.6f, 0.6f) : Color.white;

        return go;
    }

    GameObject MakeSpacer(float height)
    {
        var go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(_contentRoot, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight       = height;
        return go;
    }

    GameObject MakeButton(string label, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn", typeof(RectTransform));
        go.transform.SetParent(_contentRoot, false);
        go.AddComponent<LayoutElement>().preferredHeight = 28f;

        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(4f, 0f);
        rt.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 12f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        return go;
    }

    GameObject MakeButtonRow(string leftText, string btnLabel, Color btnColor, UnityEngine.Events.UnityAction onClick)
    {
        var row = new GameObject("Row", typeof(RectTransform));
        row.transform.SetParent(_contentRoot, false);

        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing              = 4;
        layout.childControlHeight   = true;
        layout.childControlWidth    = false;
        layout.childForceExpandWidth = false;
        row.AddComponent<LayoutElement>().preferredHeight = 26f;

        // Left label
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(row.transform, false);
        var labelLe = labelGo.AddComponent<LayoutElement>();
        labelLe.flexibleWidth = 1f;
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text     = leftText;
        tmp.fontSize = 12f;

        // Right button
        var btnGo = new GameObject("Btn", typeof(RectTransform));
        btnGo.transform.SetParent(row.transform, false);
        btnGo.AddComponent<LayoutElement>().preferredWidth = 28f;

        var img = btnGo.AddComponent<Image>();
        img.color = btnColor;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var btnTextGo = new GameObject("Label", typeof(RectTransform));
        btnTextGo.transform.SetParent(btnGo.transform, false);
        var brt = btnTextGo.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        var bTmp = btnTextGo.AddComponent<TextMeshProUGUI>();
        bTmp.text      = btnLabel;
        bTmp.fontSize  = 11f;
        bTmp.alignment = TextAlignmentOptions.Center;

        return row;
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debug popup shown when a Villager card is played.
/// Player picks which construction project to apply 1 Hammer to.
/// </summary>
public class HammerTargetPopup : MonoBehaviour
{
    CardPlayContext _ctx;

    public static void Show(CardPlayContext ctx)
    {
        var canvas = GameObject.Find("UI")?.GetComponent<Canvas>()
                     ?? Object.FindObjectOfType<Canvas>();
        if (canvas == null) { ApplyToFirst(ctx); return; }

        var go = new GameObject("HammerTargetPopup", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(320f, 40f); // grows with rows
        rt.anchoredPosition = new Vector2(180f, 0f); // offset from center so it doesn't cover hand

        go.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        var popup = go.AddComponent<HammerTargetPopup>();
        popup._ctx = ctx;
        popup.Build(go);
    }

    void Build(GameObject root)
    {
        var queue = ConstructionQueue.Instance?.Queue;
        bool hasProjects = queue != null && queue.Count > 0;

        var rows = new System.Collections.Generic.List<string>();

        // Title row
        AddLabel(root, "Apply hammer to:", 0);

        float rowH  = 44f;
        float titleH = 28f;
        int   count = hasProjects ? queue.Count : 1; // 1 for "no projects" row
        float totalH = titleH + count * rowH + 8f;

        root.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, totalH);

        if (!hasProjects)
        {
            AddLabel(root, "No active projects — hammer wasted.", titleH + 4f);
            AddCloseButton(root, "OK", totalH - rowH / 2f - 4f, () => Destroy(gameObject));
            return;
        }

        for (int i = 0; i < queue.Count; i++)
        {
            var site    = queue[i];
            if (site == null || site.IsComplete) continue;
            float yOff  = titleH + i * rowH + 4f;
            int   idx   = i; // capture for closure
            AddProjectRow(root, site, yOff, () =>
            {
                site.ApplyHammers(1);
                _ctx.hammerManager?.AddHammer(1);
                Destroy(gameObject);
            });
        }
    }

    static void ApplyToFirst(CardPlayContext ctx)
    {
        var queue = ConstructionQueue.Instance?.Queue;
        if (queue != null && queue.Count > 0)
        {
            queue[0].ApplyHammers(1);
            ctx.hammerManager?.AddHammer(1);
        }
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    void AddLabel(GameObject parent, string text, float yFromTop)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(0f, 24f);
        rt.anchoredPosition = new Vector2(0f, -(yFromTop + 12f));

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = 12f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
    }

    void AddProjectRow(GameObject parent, BuildSite site, float yFromTop,
                       UnityEngine.Events.UnityAction onClick)
    {
        var row = new GameObject("Row", typeof(RectTransform));
        row.transform.SetParent(parent.transform, false);
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.sizeDelta = new Vector2(-8f, 38f);
        rowRt.anchoredPosition = new Vector2(0f, -(yFromTop + 19f));

        // Label
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(row.transform, false);
        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0f);
        lrt.anchorMax = new Vector2(0.75f, 1f);
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = $"{site.Definition.buildingName}  [{site.HammersApplied}/{site.HammerCost} \u26d3]";
        tmp.fontSize  = 12f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color     = Color.white;

        // Button
        var btnGo = new GameObject("Btn", typeof(RectTransform));
        btnGo.transform.SetParent(row.transform, false);
        var brt = btnGo.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.76f, 0f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.8f, 0.6f, 0.1f, 0.9f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var bLabelGo = new GameObject("Label", typeof(RectTransform));
        bLabelGo.transform.SetParent(btnGo.transform, false);
        var blrt = bLabelGo.GetComponent<RectTransform>();
        blrt.anchorMin = Vector2.zero;
        blrt.anchorMax = Vector2.one;
        blrt.offsetMin = blrt.offsetMax = Vector2.zero;

        var bTmp = bLabelGo.AddComponent<TextMeshProUGUI>();
        bTmp.text      = "Apply";
        bTmp.fontSize  = 12f;
        bTmp.alignment = TextAlignmentOptions.Center;
        bTmp.color     = Color.white;
    }

    void AddCloseButton(GameObject parent, string label, float yCentre,
                        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("CloseBtn", typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(100f, 34f);
        rt.anchoredPosition = new Vector2(0f, -yCentre);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.35f, 0.35f, 0.35f, 0.9f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var lGo = new GameObject("Label", typeof(RectTransform));
        lGo.transform.SetParent(go.transform, false);
        var lrt = lGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var tmp = lGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 12f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
    }
}

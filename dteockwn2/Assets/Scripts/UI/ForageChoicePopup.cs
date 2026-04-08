using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modal popup that asks the player to choose Wood or Stone when playing the Forage card.
/// </summary>
public class ForageChoicePopup : MonoBehaviour
{
    Action<ResourceType> _callback;

    public static void Show(Action<ResourceType> callback)
    {
        // Find the main UI canvas
        var canvas = GameObject.Find("UI")?.GetComponent<Canvas>();
        if (canvas == null)
        {
            // Fallback: use any canvas
            canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        }
        if (canvas == null) { callback(ResourceType.Wood); return; }

        var go = new GameObject("ForageChoicePopup", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(280f, 130f);
        rt.anchoredPosition = Vector2.zero;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

        var popup = go.AddComponent<ForageChoicePopup>();
        popup._callback = callback;

        // Title
        MakeLabel(go, "What do you find?", new Vector2(0, 35f), 15f);

        // Wood button
        MakeButton(go, "Wood", new Vector2(-70f, -20f), new Color(0.55f, 0.35f, 0.15f),
            () => popup.Choose(ResourceType.Wood));

        // Stone button
        MakeButton(go, "Stone", new Vector2(70f, -20f), new Color(0.40f, 0.45f, 0.55f),
            () => popup.Choose(ResourceType.Stone));
    }

    void Choose(ResourceType type)
    {
        _callback?.Invoke(type);
        Destroy(gameObject);
    }

    static void MakeLabel(GameObject parent, string text, Vector2 pos, float size)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(260f, 30f);
        rt.anchoredPosition = pos;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
    }

    static void MakeButton(GameObject parent, string label, Vector2 pos, Color color,
                           UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Btn", typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(110f, 44f);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var txtGo = new GameObject("Label", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 14f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
    }
}

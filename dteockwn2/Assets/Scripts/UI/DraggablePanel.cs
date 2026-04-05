using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Add to any RectTransform panel to make it draggable and resizable.
/// Drag the panel body to reposition; drag the resize handle (bottom-right corner) to resize.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    const float MinWidth  = 120f;
    const float MinHeight = 60f;
    const float HandleSize = 18f;

    RectTransform _rt;
    Canvas        _canvas;

    void Awake()
    {
        _rt     = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        CreateResizeHandle();
    }

    // ── Drag to move ───────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData _) { }

    public void OnDrag(PointerEventData e)
    {
        if (_canvas == null) return;
        _rt.anchoredPosition += e.delta / _canvas.scaleFactor;
    }

    // ── Resize handle ──────────────────────────────────────────────────────────

    void CreateResizeHandle()
    {
        var handleGo = new GameObject("ResizeHandle", typeof(RectTransform));
        handleGo.transform.SetParent(transform, false);

        var rt = handleGo.GetComponent<RectTransform>();
        rt.anchorMin       = Vector2.zero;
        rt.anchorMax       = Vector2.zero;
        rt.pivot           = Vector2.zero;
        rt.sizeDelta       = new Vector2(HandleSize, HandleSize);
        rt.anchoredPosition = Vector2.zero; // bottom-left of parent

        // Visible triangle hint
        var img = handleGo.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(1, 1, 1, 0.25f);

        // Drag component
        var resizer = handleGo.AddComponent<ResizeHandle>();
        resizer.Target = _rt;
    }
}

/// <summary>Inner component that handles dragging the resize corner.</summary>
public class ResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public RectTransform Target;

    Canvas _canvas;

    void Awake() => _canvas = GetComponentInParent<Canvas>();

    public void OnBeginDrag(PointerEventData _) { }

    public void OnDrag(PointerEventData e)
    {
        if (Target == null || _canvas == null) return;

        Vector2 delta = e.delta / _canvas.scaleFactor;
        // Increase width, decrease height (dragging right-down grows the panel)
        Target.sizeDelta = new Vector2(
            Mathf.Max(120f, Target.sizeDelta.x + delta.x),
            Mathf.Max( 60f, Target.sizeDelta.y - delta.y));
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Sprite tile;
    private bool highlighted = false;
    private float highlightOpacity = 0.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (highlighted && highlightOpacity < 1f)
        {
            highlightOpacity += Time.deltaTime * 4;
        }
        else if (!highlighted && highlightOpacity > 0f)
        {
            highlightOpacity -= Time.deltaTime * 4;
        }

        highlightOpacity = Mathf.Clamp01(highlightOpacity);
        transform.Find("Highlight").GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, highlightOpacity);
    }

    public void Highlight(bool highlight)
    {
        highlighted = highlight;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Highlight(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Highlight(false);
    }
}

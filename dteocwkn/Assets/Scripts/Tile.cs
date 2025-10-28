using UnityEngine;

public class Tile : MonoBehaviour
{
    private bool highlighted = false;
    private float highlightOpacity = 0.0f;
    private Material tileMaterial;
    private Color baseColor;
    private Color highlightColor = new Color(1f, 1f, 0.5f); // Yellowish highlight

    void Start()
    {
        // Get the material from the MeshRenderer
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // Create a new material instance so each tile has its own
            tileMaterial = new Material(renderer.material);
            renderer.material = tileMaterial;
            baseColor = tileMaterial.color;
        }
    }

    void Update()
    {
        if (tileMaterial == null) return;

        if (highlighted && highlightOpacity < 1f)
        {
            highlightOpacity += Time.deltaTime * 4;
        }
        else if (!highlighted && highlightOpacity > 0f)
        {
            highlightOpacity -= Time.deltaTime * 4;
        }

        highlightOpacity = Mathf.Clamp01(highlightOpacity);
        
        // Blend between base color and highlight color
        tileMaterial.color = Color.Lerp(baseColor, highlightColor, highlightOpacity);
    }

    public void Highlight(bool highlight)
    {
        highlighted = highlight;
    }
}
using UnityEngine;

/// <summary>
/// Green semi-transparent 2×2 quad placed on cleared terrain cells.
/// </summary>
public static class PreparedPlotVisual
{
    public static void Create(Vector3 worldCenter)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "PreparedPlot";
        go.transform.position  = worldCenter;
        go.transform.rotation  = Quaternion.Euler(90, 0, 0);
        go.transform.localScale = new Vector3(2f, 2f, 1f);
        go.GetComponent<Renderer>().material =
            ResourceVisuals.CreateUnlit(new Color(0.25f, 0.75f, 0.25f, 0.35f));
        Object.Destroy(go.GetComponent<Collider>());
    }
}

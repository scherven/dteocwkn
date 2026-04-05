using UnityEngine;

/// <summary>Factory for greybox resource GameObjects. Materials are shared across instances.</summary>
public static class ResourceVisuals
{
    static Material _woodMat;
    static Material _stoneMat;

    public static GameObject Spawn(ResourceType type, Vector3 pos)
    {
        GameObject go;
        switch (type)
        {
            case ResourceType.Wood:
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.transform.localScale = new Vector3(0.6f, 0.15f, 0.6f); // flat disk
                go.GetComponent<Renderer>().material = WoodMat;
                break;
            case ResourceType.Stone:
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.localScale = Vector3.one * 0.4f;
                go.GetComponent<Renderer>().material = StoneMat;
                break;
            default:
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.localScale = Vector3.one * 0.3f;
                break;
        }

        // Resource visuals should not interfere with gameplay raycasts
        if (go.TryGetComponent<Collider>(out var col)) Object.Destroy(col);

        go.transform.position = pos;
        go.name = $"Resource_{type}";
        return go;
    }

    public static Material CreateUnlit(Color color)
    {
        var mat = new Material(Shader.Find("Unlit/Color")) { color = color };
        return mat;
    }

    static Material WoodMat  => _woodMat  ??= CreateUnlit(HexColor("8B5E3C"));
    static Material StoneMat => _stoneMat ??= CreateUnlit(HexColor("888888"));

    public static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}

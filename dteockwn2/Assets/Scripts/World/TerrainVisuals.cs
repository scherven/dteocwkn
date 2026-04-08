using UnityEngine;

/// <summary>Factory for greybox terrain GameObjects. Follows the same pattern as ResourceVisuals.</summary>
public static class TerrainVisuals
{
    static Material _trunkMat;
    static Material _canopyMat;
    static Material _rockMat;

    static Material TrunkMat  => _trunkMat  ??= ResourceVisuals.CreateUnlit(ResourceVisuals.HexColor("5C3A1E"));
    static Material CanopyMat => _canopyMat ??= ResourceVisuals.CreateUnlit(ResourceVisuals.HexColor("3A7A3A"));
    static Material RockMat   => _rockMat   ??= ResourceVisuals.CreateUnlit(ResourceVisuals.HexColor("888888"));

    public static GameObject SpawnTree(Vector3 pos)
    {
        var root = new GameObject("TerrainTree");
        root.transform.position = pos;

        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(root.transform);
        trunk.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        trunk.transform.localScale    = new Vector3(0.15f, 0.4f, 0.15f);
        trunk.GetComponent<Renderer>().material = TrunkMat;
        DestroyCollider(trunk);

        var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "Canopy";
        canopy.transform.SetParent(root.transform);
        canopy.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        canopy.transform.localScale    = Vector3.one * 0.5f;
        canopy.GetComponent<Renderer>().material = CanopyMat;
        DestroyCollider(canopy);

        return root;
    }

    public static GameObject SpawnRock(Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "TerrainRock";
        go.transform.position   = pos + new Vector3(0f, 0.15f, 0f);
        go.transform.localScale = new Vector3(0.5f, 0.3f, 0.45f);
        go.GetComponent<Renderer>().material = RockMat;
        DestroyCollider(go);
        return go;
    }

    static void DestroyCollider(GameObject go)
    {
        if (go.TryGetComponent<Collider>(out var col)) Object.Destroy(col);
    }
}

using System.Collections.Generic;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public static TerrainManager Instance { get; private set; }

    readonly Dictionary<Vector2Int, TerrainType> _terrain = new();
    readonly Dictionary<Vector2Int, GameObject>  _visuals = new();

    [SerializeField] int   centerClearRadius = 9;
    [SerializeField] float noiseScale        = 0.08f;
    [SerializeField] float treeDensity       = 0.38f;
    [SerializeField] float stoneDensity      = 0.28f;
    [SerializeField] float visualSpawnChance = 0.55f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Generate(int seed)
    {
        var visualRoot = new GameObject("TerrainVisuals");

        for (int x = -50; x <= 50; x++)
        {
            for (int z = -50; z <= 50; z++)
            {
                var cell = new Vector2Int(x, z);

                if (Mathf.Sqrt(x * x + z * z) <= centerClearRadius)
                {
                    _terrain[cell] = TerrainType.Field;
                    continue;
                }

                float treeNoise  = Mathf.PerlinNoise((x + seed)       * noiseScale, (z + seed)       * noiseScale);
                float stoneNoise = Mathf.PerlinNoise((x + seed + 500f) * noiseScale, (z + seed + 500f) * noiseScale);

                TerrainType type = TerrainType.Field;
                if (treeNoise  > 1f - treeDensity)                               type = TerrainType.Tree;
                if (stoneNoise > 1f - stoneDensity && type == TerrainType.Field) type = TerrainType.Stone;

                _terrain[cell] = type;

                if (type != TerrainType.Field && Random.value < visualSpawnChance)
                    SpawnVisual(cell, type, visualRoot);
            }
        }
    }

    public TerrainType GetTerrainType(Vector2Int cell) =>
        _terrain.TryGetValue(cell, out var t) ? t : TerrainType.Field;

    public void ClearCell(Vector2Int cell)
    {
        _terrain[cell] = TerrainType.Field;
        if (_visuals.TryGetValue(cell, out var go))
        {
            Destroy(go);
            _visuals.Remove(cell);
        }
    }

    void SpawnVisual(Vector2Int cell, TerrainType type, GameObject parent)
    {
        var worldPos = new Vector3(cell.x, 0f, cell.y);
        var go = type == TerrainType.Tree
            ? TerrainVisuals.SpawnTree(worldPos)
            : TerrainVisuals.SpawnRock(worldPos);

        go.transform.SetParent(parent.transform);
        _visuals[cell] = go;
    }
}

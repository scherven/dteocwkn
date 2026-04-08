using UnityEngine;

/// <summary>
/// Spring/Summer: play the Farm card to plant a seed (one seed per farm).
/// Autumn: play the Farm card to harvest 5 Food, which villagers deliver to the granary.
/// Does nothing in Winter or if the wrong action for the season.
/// The building position is stamped in via BindToBuilding when the farm completes.
/// </summary>
[CreateAssetMenu(menuName = "Cardstone/Effects/Farm", fileName = "FarmEffect")]
public class FarmEffect : CardEffect
{
    [HideInInspector] public Vector3 buildingPosition;
    [HideInInspector] public bool    hasBuildingPosition;

    // Runtime-only: the seed visual placed on this farm.
    GameObject _seedObject;

    public override CardEffect BindToBuilding(Vector3 buildingPos)
    {
        var inst = CreateInstance<FarmEffect>();
        inst.buildingPosition    = buildingPos;
        inst.hasBuildingPosition = true;
        return inst;
    }

    public override Vector3? GetBuildingHint() =>
        hasBuildingPosition ? buildingPosition : (Vector3?)null;

    public override void Resolve(CardPlayContext ctx)
    {
        var season = SeasonManager.Instance?.CurrentSeason ?? Season.Spring;

        if (season == Season.Spring || season == Season.Summer)
        {
            PlantSeed();
        }
        else if (season == Season.Autumn)
        {
            Harvest(ctx);
        }
        // Winter: do nothing.
    }

    // ── Planting ──────────────────────────────────────────────────────────────

    void PlantSeed()
    {
        if (_seedObject != null) return; // already seeded this season

        var center = hasBuildingPosition ? buildingPosition : Vector3.zero;
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "SeedVisual";
        go.transform.position   = center + new Vector3(0f, 0.6f, 0f);
        go.transform.localScale = Vector3.one * 0.35f;
        go.GetComponent<Renderer>().material =
            ResourceVisuals.CreateUnlit(ResourceVisuals.HexColor("6DBF3E"));
        var col = go.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        _seedObject = go;
        BuildingManager.Instance?.SetFarmSeeded(buildingPosition, true, _seedObject);
    }

    // ── Harvesting ────────────────────────────────────────────────────────────

    void Harvest(CardPlayContext ctx)
    {
        if (_seedObject == null && !(BuildingManager.Instance?.IsFarmSeeded(buildingPosition) ?? false))
            return; // no seed to harvest

        if (_seedObject != null)
        {
            Object.Destroy(_seedObject);
            _seedObject = null;
        }
        BuildingManager.Instance?.SetFarmSeeded(buildingPosition, false);

        for (int i = 0; i < 5; i++)
            SpawnFood(ctx);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void SpawnFood(CardPlayContext ctx)
    {
        Vector3 spawnPos    = NearBuilding();
        GameObject food     = ResourceVisuals.Spawn(ResourceType.Food, spawnPos);
        Vector3 granaryPos  = BuildingManager.Instance?.GranaryPosition
                              ?? BuildingManager.Instance?.StorehousePosition
                              ?? Vector3.zero;

        var deliverTask = new VillagerTask
        {
            Type           = TaskType.DeliverResource,
            TargetPosition = granaryPos,
            TargetObject   = food,
            OnComplete     = () => ctx.inventory.Add(ResourceType.Food, 1)
        };

        var pickupTask = new VillagerTask
        {
            Type           = TaskType.PickUpResource,
            TargetPosition = spawnPos,
            TargetObject   = food,
            FollowUp       = deliverTask
        };

        ctx.taskDispatcher.EnqueueTask(pickupTask);
    }

    Vector3 NearBuilding()
    {
        Vector2 offset = Random.insideUnitCircle * 3f;
        return (hasBuildingPosition ? buildingPosition : Vector3.zero)
               + new Vector3(offset.x, 0f, offset.y);
    }
}

using UnityEngine;

/// <summary>
/// Playing the Farm card harvests 3 Food in Summer; does nothing the rest of the year.
/// The building position is stamped in via BindToBuilding when the farm completes.
/// </summary>
[CreateAssetMenu(menuName = "Cardstone/Effects/Farm", fileName = "FarmEffect")]
public class FarmEffect : CardEffect
{
    [HideInInspector] public Vector3 buildingPosition;
    [HideInInspector] public bool    hasBuildingPosition;

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
        if (SeasonManager.Instance?.CurrentSeason != Season.Summer) return;
        for (int i = 0; i < 3; i++)
            SpawnFood(ctx);
    }

    void SpawnFood(CardPlayContext ctx)
    {
        Vector3 spawnPos = NearBuilding();
        GameObject food  = ResourceVisuals.Spawn(ResourceType.Food, spawnPos);

        var deliverTask = new VillagerTask
        {
            Type           = TaskType.DeliverResource,
            TargetPosition = BuildingManager.Instance.StorehousePosition,
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

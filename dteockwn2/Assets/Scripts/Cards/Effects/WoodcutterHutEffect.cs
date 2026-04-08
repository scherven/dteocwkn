using UnityEngine;

/// <summary>
/// Playing the Woodcutter's Hut card spawns 3 Logs near the hut.
/// Idle villagers pick them up and deliver to the storehouse.
/// </summary>
[CreateAssetMenu(menuName = "Cardstone/Effects/Woodcutter Hut", fileName = "WoodcutterHutEffect")]
public class WoodcutterHutEffect : CardEffect
{
    [HideInInspector] public Vector3 buildingPosition;
    [HideInInspector] public bool    hasBuildingPosition;

    public override CardEffect BindToBuilding(Vector3 buildingPos)
    {
        var inst = CreateInstance<WoodcutterHutEffect>();
        inst.buildingPosition    = buildingPos;
        inst.hasBuildingPosition = true;
        return inst;
    }

    public override Vector3? GetBuildingHint() =>
        hasBuildingPosition ? buildingPosition : (Vector3?)null;

    public override void Resolve(CardPlayContext ctx)
    {
        for (int i = 0; i < 3; i++)
            SpawnLog(ctx);
    }

    void SpawnLog(CardPlayContext ctx)
    {
        Vector3 spawnPos = NearBuilding();
        GameObject log   = ResourceVisuals.Spawn(ResourceType.Wood, spawnPos);

        var deliverTask = new VillagerTask
        {
            Type           = TaskType.DeliverResource,
            TargetPosition = BuildingManager.Instance.StorehousePosition,
            TargetObject   = log,
            OnComplete     = () => ctx.inventory.Add(ResourceType.Wood, 1)
        };

        var pickupTask = new VillagerTask
        {
            Type           = TaskType.PickUpResource,
            TargetPosition = spawnPos,
            TargetObject   = log,
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

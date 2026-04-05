using UnityEngine;

[CreateAssetMenu(menuName = "Cardstone/Effects/Add Resource", fileName = "AddResourceEffect")]
public class AddResourceEffect : CardEffect
{
    public ResourceType resourceType;
    public int amount = 1;

    public override void Resolve(CardPlayContext ctx)
    {
        for (int i = 0; i < amount; i++)
            SpawnResourceDelivery(ctx);
    }

    void SpawnResourceDelivery(CardPlayContext ctx)
    {
        Vector3 spawnPos = RandomMapEdge();
        GameObject resourceObj = ResourceVisuals.Spawn(resourceType, spawnPos);

        var deliverTask = new VillagerTask
        {
            Type = TaskType.DeliverResource,
            TargetPosition = BuildingManager.Instance.StorehousePosition,
            TargetObject = resourceObj,
            OnComplete = () =>
            {
                ctx.inventory.Add(resourceType, 1);
            }
        };

        var pickupTask = new VillagerTask
        {
            Type = TaskType.PickUpResource,
            TargetPosition = spawnPos,
            TargetObject = resourceObj,
            FollowUp = deliverTask
        };

        ctx.taskDispatcher.EnqueueTask(pickupTask);
    }

    static Vector3 RandomMapEdge()
    {
        float h = 48f;
        return Random.Range(0, 4) switch
        {
            0 => new Vector3(Random.Range(-h, h), 0, h),
            1 => new Vector3(Random.Range(-h, h), 0, -h),
            2 => new Vector3(h, 0, Random.Range(-h, h)),
            _ => new Vector3(-h, 0, Random.Range(-h, h))
        };
    }
}

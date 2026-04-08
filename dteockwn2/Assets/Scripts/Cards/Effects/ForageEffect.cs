using UnityEngine;

/// <summary>
/// Forage — player picks Wood or Stone; a villager fetches it from the wilderness.
/// </summary>
[CreateAssetMenu(menuName = "Cardstone/Effects/Forage", fileName = "ForageEffect")]
public class ForageEffect : CardEffect
{
    public override void Resolve(CardPlayContext ctx)
    {
        ForageChoicePopup.Show(chosen => SpawnResourceDelivery(ctx, chosen));
    }

    static void SpawnResourceDelivery(CardPlayContext ctx, ResourceType resourceType)
    {
        Vector3 spawnPos    = RandomMapEdge();
        GameObject resource = ResourceVisuals.Spawn(resourceType, spawnPos);

        var deliverTask = new VillagerTask
        {
            Type           = TaskType.DeliverResource,
            TargetPosition = BuildingManager.Instance.StorehousePosition,
            TargetObject   = resource,
            OnComplete     = () => ctx.inventory.Add(resourceType, 1)
        };

        var pickupTask = new VillagerTask
        {
            Type           = TaskType.PickUpResource,
            TargetPosition = spawnPos,
            TargetObject   = resource,
            FollowUp       = deliverTask
        };

        ctx.taskDispatcher.EnqueueTask(pickupTask);
    }

    static Vector3 RandomMapEdge()
    {
        float h = 48f;
        return Random.Range(0, 4) switch
        {
            0 => new Vector3(Random.Range(-h, h), 0,  h),
            1 => new Vector3(Random.Range(-h, h), 0, -h),
            2 => new Vector3( h, 0, Random.Range(-h, h)),
            _ => new Vector3(-h, 0, Random.Range(-h, h))
        };
    }
}

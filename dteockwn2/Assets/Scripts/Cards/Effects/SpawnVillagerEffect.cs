using UnityEngine;

/// <summary>
/// Sends two idle villagers to the cottage that produced this card.
/// When both arrive, a new villager spawns there.
///
/// cottagePosition is baked in at the time the building completes —
/// each cottage card is a unique runtime instance so the position is fixed.
/// </summary>
[CreateAssetMenu(menuName = "Cardstone/Effects/Spawn Villager", fileName = "SpawnVillagerEffect")]
public class SpawnVillagerEffect : CardEffect
{
    /// <summary>Stamped in via BindToBuilding when the building completes.</summary>
    public Vector3 cottagePosition;

    public override CardEffect BindToBuilding(Vector3 buildingPos)
    {
        var instance = CreateInstance<SpawnVillagerEffect>();
        instance.cottagePosition = buildingPos;
        return instance;
    }

    public override void Resolve(CardPlayContext ctx)
    {
        // Local counter captured per-play so replaying the card never
        // corrupts an in-progress pair from a previous play.
        int arrived = 0;

        void OnArrive()
        {
            arrived++;
            if (arrived >= 2)
                VillagerManager.Instance.SpawnVillager(cottagePosition);
        }

        for (int i = 0; i < 2; i++)
        {
            ctx.taskDispatcher.EnqueueTask(new VillagerTask
            {
                Type           = TaskType.WalkTo,
                TargetPosition = cottagePosition,
                OnComplete     = OnArrive
            });
        }
    }
}

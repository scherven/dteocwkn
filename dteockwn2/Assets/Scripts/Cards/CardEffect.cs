using UnityEngine;

public abstract class CardEffect : ScriptableObject
{
    public abstract void Resolve(CardPlayContext context);

    /// <summary>
    /// Returns a runtime instance of this effect bound to a specific building position.
    /// Stateless effects return themselves; position-dependent effects (e.g.
    /// SpawnVillagerEffect) override to create a fresh instance with the position set.
    /// </summary>
    public virtual CardEffect BindToBuilding(Vector3 buildingPos) => this;
}

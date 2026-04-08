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

    /// <summary>
    /// Returns the world position of the building this effect is associated with, or null.
    /// Used by HandUI to highlight the related building on card hover.
    /// </summary>
    public virtual Vector3? GetBuildingHint() => null;
}

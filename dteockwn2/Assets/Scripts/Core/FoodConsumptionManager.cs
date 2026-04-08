/// <summary>
/// Each morning, consumes 0.2 food per villager from the inventory.
/// Fractional amounts accumulate and are consumed once they reach a whole unit.
/// </summary>
public class FoodConsumptionManager : UnityEngine.MonoBehaviour
{
    public static FoodConsumptionManager Instance { get; private set; }

    public const float FoodPerVillagerPerDay = 0.2f;

    float _fractional; // tracks sub-integer food owed

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()  => GameEvents.OnTurnPhaseChanged += OnPhaseChanged;
    void OnDisable() => GameEvents.OnTurnPhaseChanged -= OnPhaseChanged;

    void OnPhaseChanged(TurnPhase phase)
    {
        if (phase != TurnPhase.Morning) return;

        int villagers  = VillagerManager.Instance?.TotalVillagerCount ?? 0;
        _fractional   += villagers * FoodPerVillagerPerDay;

        int toConsume  = (int)_fractional;
        _fractional   -= toConsume;

        if (toConsume > 0)
            GameInventory.Instance?.Consume(ResourceType.Food, toConsume);
    }

    /// <summary>How much food will be consumed next morning, including any pending fraction.</summary>
    public float DailyDrain =>
        (VillagerManager.Instance?.TotalVillagerCount ?? 0) * FoodPerVillagerPerDay;
}

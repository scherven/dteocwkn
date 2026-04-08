using System.Collections.Generic;
using UnityEngine;

public class VillagerManager : MonoBehaviour
{
    public static VillagerManager Instance { get; private set; }

    readonly List<VillagerAgent> _villagers = new();

    // Per-villager job: def + building world position to identify the specific instance.
    readonly Dictionary<VillagerAgent, (BuildingDefinition def, Vector3 pos)> _jobs = new();

    // Shared stateless effects — created once in Awake, reused by all villager cards.
    GiveHammerEffect   _hammerEffect;
    GiveHammerEffect   _doubleHammerEffect;
    GiveResourceEffect _woodJobEffect;

    public int TotalVillagerCount => _villagers.Count;
    public IReadOnlyList<VillagerAgent> AllVillagers => _villagers;

    // Stub: always returns 1 until VillagerNeeds is wired up.
    public float AverageHappiness => 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _hammerEffect        = ScriptableObject.CreateInstance<GiveHammerEffect>();
        _hammerEffect.amount = 1;

        _doubleHammerEffect        = ScriptableObject.CreateInstance<GiveHammerEffect>();
        _doubleHammerEffect.amount = 2;

        _woodJobEffect              = ScriptableObject.CreateInstance<GiveResourceEffect>();
        _woodJobEffect.resourceType = ResourceType.Wood;
        _woodJobEffect.amount       = 1;
    }

    void OnEnable()  => GameEvents.OnHandDiscarded += OnHandDiscarded;
    void OnDisable() => GameEvents.OnHandDiscarded -= OnHandDiscarded;

    // ── Villager registration ─────────────────────────────────────────────────

    public void RegisterVillager(VillagerAgent v)
    {
        if (_villagers.Contains(v)) return;
        _villagers.Add(v);
        GameEvents.RaiseVillagerCountChanged(TotalVillagerCount);
    }

    public void UnregisterVillager(VillagerAgent v)
    {
        _villagers.Remove(v);
        _jobs.Remove(v);
        GameEvents.RaiseVillagerCountChanged(TotalVillagerCount);
    }

    public VillagerAgent GetIdleVillager()
    {
        foreach (var v in _villagers)
            if (v.IsIdle) return v;
        return null;
    }

    // ── Villager spawning ──────────────────────────────────────────────────────

    int _spawnCount;

    /// <summary>
    /// Creates a new villager capsule on the NavMesh at the given position,
    /// and injects a personal Villager card into the deck.
    /// </summary>
    public VillagerAgent SpawnVillager(Vector3 pos)
    {
        var parent = GameObject.Find("Villagers") ?? new GameObject("Villagers");

        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = $"Villager_{++_spawnCount:D2}";
        go.transform.SetParent(parent.transform);
        go.transform.position   = pos;
        go.transform.localScale = new Vector3(1f, 0.9f, 1f);

        go.GetComponent<Renderer>().material =
            ResourceVisuals.CreateUnlit(Color.white);

        var agent = go.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.radius           = 0.4f;
        agent.height           = 1.8f;
        agent.speed            = 3.5f;
        agent.angularSpeed     = 120f;
        agent.acceleration     = 8f;
        agent.stoppingDistance = 0.3f;

        go.AddComponent<VillagerNeeds>();
        go.AddComponent<VillagerAnimator>();
        var villager = go.AddComponent<VillagerAgent>(); // Start() registers with us

        // Give this villager a personal card and add it to the active deck.
        var card         = ScriptableObject.CreateInstance<CardData>();
        card.cardName    = "Villager";
        card.description = "A villager puts in a day's work. +1 Hammer.";
        card.type        = CardType.Villager;
        card.effect      = _hammerEffect;
        villager.OwnedCard = card;

        DeckManager.Instance?.AddCardToDeck(card);

        return villager;
    }

    // ── Job assignment ─────────────────────────────────────────────────────────

    /// <summary>
    /// Assigns a villager to a specific building job.
    /// Mutates their card immediately unless the card is currently in hand,
    /// in which case the change is deferred until the next hand discard.
    /// </summary>
    public void AssignJob(VillagerAgent v, BuildingDefinition def, Vector3 buildingPos)
    {
        _jobs[v] = (def, buildingPos);

        if (v.OwnedCard == null) return;

        bool inHand = DeckManager.Instance?.IsCardInHand(v.OwnedCard) ?? false;
        if (inHand)
        {
            v.HasPendingJobChange = true;
            v.PendingJobDef       = def;
        }
        else
        {
            ApplyJobToCard(v.OwnedCard, def);
        }
    }

    /// <summary>
    /// Removes a villager's job assignment.
    /// Reverts their card to plain Villager, deferred if the card is in hand.
    /// </summary>
    public void UnassignJob(VillagerAgent v)
    {
        _jobs.Remove(v);

        if (v.OwnedCard == null) return;

        bool inHand = DeckManager.Instance?.IsCardInHand(v.OwnedCard) ?? false;
        if (inHand)
        {
            v.HasPendingJobChange = true;
            v.PendingJobDef       = null; // null → revert to Villager
        }
        else
        {
            ApplyJobToCard(v.OwnedCard, null);
        }
    }

    /// <summary>Returns the building definition this villager is assigned to, or null.</summary>
    public BuildingDefinition GetJobDef(VillagerAgent v) =>
        _jobs.TryGetValue(v, out var j) ? j.def : null;

    /// <summary>Returns all villagers currently assigned to the given building instance.</summary>
    public List<VillagerAgent> GetOccupantsFor(BuildingDefinition def, Vector3 pos)
    {
        var result = new List<VillagerAgent>();
        foreach (var kvp in _jobs)
            if (kvp.Value.def == def && Vector3.Distance(kvp.Value.pos, pos) < 0.5f)
                result.Add(kvp.Key);
        return result;
    }

    // ── Deferred card mutation ─────────────────────────────────────────────────

    /// <summary>
    /// Called when the player's hand is discarded.
    /// Applies any pending job changes to cards that were in hand.
    /// </summary>
    void OnHandDiscarded()
    {
        foreach (var v in _villagers)
        {
            if (!v.HasPendingJobChange) continue;
            ApplyJobToCard(v.OwnedCard, v.PendingJobDef);
            v.HasPendingJobChange = false;
            v.PendingJobDef       = null;
        }
    }

    // ── Card mutation ──────────────────────────────────────────────────────────

    /// <summary>
    /// Rewrites <paramref name="card"/> to reflect the given job.
    /// Pass <c>null</c> for <paramref name="jobDef"/> to revert to a plain Villager card.
    /// </summary>
    void ApplyJobToCard(CardData card, BuildingDefinition jobDef)
    {
        if (jobDef == null)
        {
            card.cardName    = "Villager";
            card.description = "A villager puts in a day's work. +1 Hammer.";
            card.effect      = _hammerEffect;
        }
        else if (jobDef.type == BuildingType.Guildhall)
        {
            card.cardName    = "Guildhall";
            card.description = "Works at the Guildhall. +2 Hammers.";
            card.effect      = _doubleHammerEffect;
        }
        else
        {
            card.cardName    = jobDef.buildingName;
            card.description = $"Works at the {jobDef.buildingName}. +1 Wood.";
            card.effect      = _woodJobEffect;
        }
        // card.type stays CardType.Villager — keeps the blue card colour in the hand UI.
    }
}

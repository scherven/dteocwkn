using System.Collections.Generic;
using UnityEngine;

public class VillagerManager : MonoBehaviour
{
    public static VillagerManager Instance { get; private set; }

    readonly List<VillagerAgent> _villagers = new();

    // Per-villager explicit job assignment: def + building world position.
    // Villagers NOT in this dict implicitly work at the fallback workplace.
    readonly Dictionary<VillagerAgent, (BuildingDefinition def, Vector3 pos)> _jobs = new();

    // The "catch-all" workplace (Builder's Hut initially, replaced by Guildhall).
    // Any villager not explicitly assigned elsewhere is implicitly assigned here.
    BuildingDefinition _fallbackDef;
    Vector3            _fallbackPos;

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

    void OnEnable()
    {
        GameEvents.OnHandDiscarded      += OnHandDiscarded;
        GameEvents.OnBuildingCompleted  += OnBuildingCompleted;
    }

    void OnDisable()
    {
        GameEvents.OnHandDiscarded      -= OnHandDiscarded;
        GameEvents.OnBuildingCompleted  -= OnBuildingCompleted;
    }

    // ── Fallback workplace ────────────────────────────────────────────────────

    /// <summary>
    /// Sets the catch-all workplace for all unassigned villagers.
    /// Mutates cards for every villager not currently in an explicit job.
    /// If the old fallback was a Builder's Hut, removes it from BuildingManager.
    /// </summary>
    public void SetFallbackWorkplace(BuildingDefinition def, Vector3 pos)
    {
        var oldDef = _fallbackDef;
        var oldPos = _fallbackPos;

        _fallbackDef = def;
        _fallbackPos = pos;

        // Update colour and mutate cards for all implicit workers (those not explicitly assigned).
        foreach (var v in _villagers)
        {
            if (_jobs.ContainsKey(v)) continue;

            ApplyJobColor(v, def);

            if (v.OwnedCard == null) continue;
            bool inHand = DeckManager.Instance?.IsCardInHand(v.OwnedCard) ?? false;
            if (inHand) { v.HasPendingJobChange = true; v.PendingJobDef = def; }
            else ApplyJobToCard(v.OwnedCard, def);
        }

        // Remove the old Builder's Hut from BuildingManager once Guildhall replaces it.
        if (oldDef != null && oldDef.type == BuildingType.BuildersHut)
            BuildingManager.Instance?.RemoveBuilding(oldDef, oldPos);
    }

    // When a Guildhall completes construction, it becomes the new fallback workplace.
    void OnBuildingCompleted(BuildingDefinition def)
    {
        if (def.type != BuildingType.Guildhall) return;

        // Find the completed Guildhall's position from BuildingManager.
        foreach (var (d, p, _) in BuildingManager.Instance.AllBuildings)
        {
            if (d == def)
            {
                SetFallbackWorkplace(def, p);
                return;
            }
        }
    }

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
    /// New villagers automatically work at the current fallback workplace.
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

        // Card and colour reflect the current fallback workplace.
        var card   = ScriptableObject.CreateInstance<CardData>();
        card.type  = CardType.Villager;
        ApplyJobToCard(card, _fallbackDef);
        villager.OwnedCard = card;
        ApplyJobColor(villager, _fallbackDef);

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

        ApplyJobColor(v, def);

        if (v.OwnedCard != null)
        {
            bool inHand = DeckManager.Instance?.IsCardInHand(v.OwnedCard) ?? false;
            if (inHand) { v.HasPendingJobChange = true; v.PendingJobDef = def; }
            else ApplyJobToCard(v.OwnedCard, def);
        }

        // If the villager is idle and assigned to a specific building, walk them there now.
        if (v.IsIdle && def.type != BuildingType.BuildersHut && def.type != BuildingType.Guildhall)
            TaskDispatcher.Instance?.AssignTask(
                new VillagerTask { Type = TaskType.WalkTo, TargetPosition = buildingPos }, v);
    }

    /// <summary>
    /// Removes a villager's explicit job assignment.
    /// They automatically return to the fallback workplace (Builder's Hut or Guildhall).
    /// </summary>
    public void UnassignJob(VillagerAgent v)
    {
        _jobs.Remove(v);

        ApplyJobColor(v, _fallbackDef);

        if (v.OwnedCard == null) return;

        // Revert to the fallback workplace card, not a plain unassigned Villager.
        bool inHand = DeckManager.Instance?.IsCardInHand(v.OwnedCard) ?? false;
        if (inHand)
        {
            v.HasPendingJobChange = true;
            v.PendingJobDef       = _fallbackDef;
        }
        else
        {
            ApplyJobToCard(v.OwnedCard, _fallbackDef);
        }
    }

    /// <summary>
    /// Returns the building this villager works at.
    /// For villagers without an explicit assignment, returns the fallback workplace.
    /// </summary>
    public BuildingDefinition GetJobDef(VillagerAgent v) =>
        _jobs.TryGetValue(v, out var j) ? j.def : _fallbackDef;

    /// <summary>Returns the world position of the building this villager is explicitly assigned to.</summary>
    public Vector3 GetJobPosition(VillagerAgent v) =>
        _jobs.TryGetValue(v, out var j) ? j.pos : _fallbackPos;

    /// <summary>
    /// Returns all villagers currently at the given building instance.
    /// For the fallback workplace, also includes all villagers without explicit assignments.
    /// </summary>
    public List<VillagerAgent> GetOccupantsFor(BuildingDefinition def, Vector3 pos)
    {
        var result = new List<VillagerAgent>();
        foreach (var kvp in _jobs)
            if (kvp.Value.def == def && Vector3.Distance(kvp.Value.pos, pos) < 0.5f)
                result.Add(kvp.Key);

        // Implicit workers at the fallback building.
        if (_fallbackDef == def && Vector3.Distance(_fallbackPos, pos) < 0.5f)
            foreach (var v in _villagers)
                if (!_jobs.ContainsKey(v))
                    result.Add(v);

        return result;
    }

    // ── Deferred card mutation ─────────────────────────────────────────────────

    void OnHandDiscarded()
    {
        foreach (var v in _villagers)
        {
            if (!v.HasPendingJobChange) continue;
            ApplyJobToCard(v.OwnedCard, v.PendingJobDef);
            ApplyJobColor(v, v.PendingJobDef);
            v.HasPendingJobChange = false;
            v.PendingJobDef       = null;
        }
    }

    // ── Job colour ────────────────────────────────────────────────────────────

    /// <summary>Updates the villager's body colour to match their current job.</summary>
    void ApplyJobColor(VillagerAgent v, BuildingDefinition def)
        => v.SetBodyColor(JobColor(def));

    static Color JobColor(BuildingDefinition def)
    {
        if (def == null || def.type == BuildingType.BuildersHut)
            return Color.white;

        return def.type switch
        {
            BuildingType.Guildhall => ResourceVisuals.HexColor("FFD700"), // gold
            BuildingType.Job       => ResourceVisuals.HexColor("C8954A"), // warm tan — woodcutter/stonecutter
            BuildingType.Farm      => ResourceVisuals.HexColor("5CB85C"), // green
            BuildingType.Housing   => ResourceVisuals.HexColor("5BC0DE"), // sky blue
            BuildingType.Granary   => ResourceVisuals.HexColor("E87722"), // orange
            _                      => ResourceVisuals.HexColor("CCCCCC"), // light grey fallback
        };
    }

    // ── Card mutation ──────────────────────────────────────────────────────────

    /// <summary>
    /// Rewrites <paramref name="card"/> to reflect the given job.
    /// Null or BuildersHut → plain Villager card (+1 Hammer).
    /// Guildhall → +2 Hammers.
    /// Other jobs → building-specific card (+1 Wood).
    /// </summary>
    void ApplyJobToCard(CardData card, BuildingDefinition jobDef)
    {
        if (jobDef == null || jobDef.type == BuildingType.BuildersHut)
        {
            card.cardName    = "Villager";
            card.description = "A villager puts in a day's work. +1 Hammer.";
            card.effect      = _hammerEffect;
        }
        else if (jobDef.type == BuildingType.Guildhall)
        {
            card.cardName    = "Guildhall Worker";
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

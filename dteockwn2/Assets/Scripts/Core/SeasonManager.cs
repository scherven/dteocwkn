using UnityEngine;

/// <summary>
/// Tracks the current season and day-of-season.
/// Each season lasts <see cref="DaysPerSeason"/> turns.
/// Cycle: Spring → Summer → Autumn → Winter → Spring …
/// Listens to Morning phase events so it advances exactly once per game day.
/// </summary>
public class SeasonManager : MonoBehaviour
{
    public static SeasonManager Instance { get; private set; }

    public const int DaysPerSeason = 4; // change here to tune season length

    public Season CurrentSeason  { get; private set; } = Season.Spring;
    public int    DayOfSeason    { get; private set; } = 0; // 0 before first Morning; 1-4 in play

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

        DayOfSeason++;

        if (DayOfSeason > DaysPerSeason)
        {
            DayOfSeason   = 1;
            CurrentSeason = (Season)(((int)CurrentSeason + 1) % 4);
            GameEvents.RaiseSeasonChanged(CurrentSeason);
        }
    }

    /// <summary>e.g. "Summer (Day 2 / 4)"</summary>
    public string DisplayString() =>
        $"{CurrentSeason}  (Day {DayOfSeason} / {DaysPerSeason})";
}

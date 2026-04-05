using UnityEngine;

// Stub — no implementations yet.
// Future: data-driven building unlock rules (milestone reached, building count, etc.)
public abstract class UnlockCondition : ScriptableObject
{
    public abstract bool IsMet();
}

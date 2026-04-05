using UnityEngine;

// Stub — all values exist but nothing reads or writes them yet.
// Future: influence TaskDispatcher priority and DeckManager draw rate.
public class VillagerNeeds : MonoBehaviour
{
    [Range(0f, 1f)] public float hunger    = 1f;
    [Range(0f, 1f)] public float rest      = 1f;
    [Range(0f, 1f)] public float happiness = 1f;
}

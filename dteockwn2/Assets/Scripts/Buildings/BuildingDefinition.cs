using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cardstone/Building Definition", fileName = "NewBuilding")]
public class BuildingDefinition : ScriptableObject
{
    public string buildingName;
    [TextArea] public string description;
    public int widthCells = 2;
    public int depthCells = 2;
    public List<ResourceCost> materialCost = new();

    [Tooltip("Total seconds for 1 villager working alone to complete construction.")]
    public float constructionTimeBase = 20f;

    public BuildingType type;
    public int housingCapacity;  // Housing only
    public int jobSlots;         // Job only

    public GameObject greyboxPrefab; // optional; Bootstrap creates a box if null

    [Tooltip("Card injected into the deck when this building completes. May be null.")]
    public CardData associatedCard;
}

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

    [Tooltip("Total Hammers required to complete construction across one or more days.")]
    public int hammerCost = 4;

    public BuildingType type;
    public int housingCapacity;  // Housing only
    public int jobSlots;         // Job only

    public GameObject greyboxPrefab; // optional; Bootstrap creates a box if null

    [Tooltip("If not Field, ALL cells in this building's footprint must be that terrain type. Also bypasses the Clear Land preparation requirement.")]
    public TerrainType requiredTerrain = TerrainType.Field;

    [Tooltip("Card injected into the deck when this building completes. May be null.")]
    public CardData associatedCard;
}

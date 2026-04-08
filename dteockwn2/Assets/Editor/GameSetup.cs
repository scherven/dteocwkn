// Assets/Editor/GameSetup.cs — only compiled in the Unity Editor
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu item: Cardstone > Create Starter Assets
/// Creates persistent BuildingDefinition and CardData .asset files under Assets/Data/,
/// then wires them into the Bootstrap component if one is found in the active scene.
/// </summary>
public static class GameSetup
{
    const string DataPath = "Assets/Data";

    [MenuItem("Cardstone/Create Starter Assets")]
    static void CreateStarterAssets()
    {
        EnsureFolder(DataPath + "/Buildings");
        EnsureFolder(DataPath + "/Cards");
        EnsureFolder(DataPath + "/Effects");

        // --- Effects ---
        var hammerEffect = GetOrCreate<GiveHammerEffect>(DataPath + "/Effects/Effect_Hammer.asset");

        var forageEffect = GetOrCreate<ForageEffect>(DataPath + "/Effects/Effect_Forage.asset");

        var clearEffect  = GetOrCreate<ClearLandEffect>(DataPath + "/Effects/Effect_ClearLand.asset");

        var hutEffect    = GetOrCreate<WoodcutterHutEffect>(DataPath + "/Effects/Effect_WoodcutterHut.asset");

        var cottageEffect = GetOrCreate<CottageEffect>(DataPath + "/Effects/Effect_Cottage.asset");

        var stonecutterEffect = GetOrCreate<StonecutterEffect>(DataPath + "/Effects/Effect_Stonecutter.asset");

        var farmEffect = GetOrCreate<FarmEffect>(DataPath + "/Effects/Effect_Farm.asset");

        // --- Cards ---
        var villagerCard = GetOrCreate<CardData>(DataPath + "/Cards/Card_Villager.asset");
        villagerCard.cardName    = "Villager";
        villagerCard.description = "A villager puts in a day's work. +1 Hammer.";
        villagerCard.type        = CardType.Villager;
        villagerCard.effect      = hammerEffect;

        var forageCard = GetOrCreate<CardData>(DataPath + "/Cards/Card_Forage.asset");
        forageCard.cardName    = "Forage";
        forageCard.description = "Gather from the wilderness. Choose: Wood or Stone.";
        forageCard.type        = CardType.Resource;
        forageCard.effect      = forageEffect;

        var clearCard = GetOrCreate<CardData>(DataPath + "/Cards/Card_ClearLand.asset");
        clearCard.cardName    = "Clear Land";
        clearCard.description = "Prepare a plot for construction.";
        clearCard.type        = CardType.Event;
        clearCard.effect      = clearEffect;

        var hutCard = GetOrCreate<CardData>(DataPath + "/Cards/Card_WoodcutterHut.asset");
        hutCard.cardName    = "Woodcutter's Hut";
        hutCard.description = "Produce 3 Logs into the Town Buffer.";
        hutCard.type        = CardType.Building;
        hutCard.effect      = hutEffect;

        var cottageCard = GetOrCreate<CardData>(DataPath + "/Cards/Card_Cottage.asset");
        cottageCard.cardName    = "Cottage";
        cottageCard.description = "Housing +2. Gain 1 villager next morning.";
        cottageCard.type        = CardType.Building;
        cottageCard.effect      = cottageEffect;

        // --- Buildings ---
        var woodcutter = GetOrCreate<BuildingDefinition>(DataPath + "/Buildings/Building_Woodcutter.asset");
        woodcutter.buildingName  = "Woodcutter's Hut";
        woodcutter.description   = "Puts a villager to work felling timber.";
        woodcutter.widthCells    = 2;
        woodcutter.depthCells    = 2;
        woodcutter.materialCost  = new List<ResourceCost>
            { new(ResourceType.Wood, 2), new(ResourceType.Stone, 1) };
        woodcutter.hammerCost    = 4;
        woodcutter.type          = BuildingType.Job;
        woodcutter.jobSlots      = 1;
        woodcutter.associatedCard = hutCard;

        var cottage = GetOrCreate<BuildingDefinition>(DataPath + "/Buildings/Building_Cottage.asset");
        cottage.buildingName     = "Cottage";
        cottage.description      = "Housing for two villagers.";
        cottage.widthCells       = 2;
        cottage.depthCells       = 2;
        cottage.materialCost     = new List<ResourceCost>
            { new(ResourceType.Wood, 3), new(ResourceType.Stone, 1) };
        cottage.hammerCost       = 2;
        cottage.type             = BuildingType.Housing;
        cottage.housingCapacity  = 2;
        cottage.associatedCard   = cottageCard;

        var stonecutterCard = GetOrCreate<CardData>(DataPath + "/Cards/Card_Stonecutter.asset");
        stonecutterCard.cardName    = "Stonecutter";
        stonecutterCard.description = "Cut 3 Stone from the quarry.";
        stonecutterCard.type        = CardType.Building;
        stonecutterCard.effect      = stonecutterEffect;

        var stonecutter = GetOrCreate<BuildingDefinition>(DataPath + "/Buildings/Building_Stonecutter.asset");
        stonecutter.buildingName    = "Stonecutter";
        stonecutter.description     = "Must be built on stone terrain. Cuts stone for the town.";
        stonecutter.widthCells      = 2;
        stonecutter.depthCells      = 2;
        stonecutter.materialCost    = new List<ResourceCost> { new(ResourceType.Wood, 4) };
        stonecutter.hammerCost      = 4;
        stonecutter.type            = BuildingType.Job;
        stonecutter.jobSlots        = 1;
        stonecutter.requiredTerrain = TerrainType.Stone;
        stonecutter.associatedCard  = stonecutterCard;

        var farmCard = GetOrCreate<CardData>(DataPath + "/Cards/Card_Farm.asset");
        farmCard.cardName    = "Farm";
        farmCard.description = "Harvest 3 Food. (Summer only)";
        farmCard.type        = CardType.Building;
        farmCard.effect      = farmEffect;

        var farm = GetOrCreate<BuildingDefinition>(DataPath + "/Buildings/Building_Farm.asset");
        farm.buildingName   = "Farm";
        farm.description    = "Grows food in Summer. Play the Farm card to harvest.";
        farm.widthCells     = 2;
        farm.depthCells     = 2;
        farm.materialCost   = new List<ResourceCost> { new(ResourceType.Wood, 2) };
        farm.hammerCost     = 3;
        farm.type           = BuildingType.Farm;
        farm.associatedCard = farmCard;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Wire into Bootstrap if present in the active scene
        var bootstrap = Object.FindObjectOfType<GameBootstrap>();
        if (bootstrap != null)
        {
            var so = new SerializedObject(bootstrap);
            so.FindProperty("woodcutterDef").objectReferenceValue = woodcutter;
            so.FindProperty("cottageDef").objectReferenceValue    = cottage;
            so.FindProperty("farmDef").objectReferenceValue       = farm;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(bootstrap);
            Debug.Log("[GameSetup] Wired assets into GameBootstrap.");
        }

        Debug.Log("[GameSetup] Starter assets created at " + DataPath);
        Selection.activeObject = woodcutter;
    }

    static T GetOrCreate<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parts  = path.Split('/');
            var parent = string.Join("/", parts[..^1]);
            var folder = parts[^1];
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}

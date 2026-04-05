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
        var woodEffect  = GetOrCreate<AddResourceEffect>(DataPath + "/Effects/Effect_Wood.asset");
        woodEffect.resourceType = ResourceType.Wood;
        woodEffect.amount       = 1;

        var stoneEffect = GetOrCreate<AddResourceEffect>(DataPath + "/Effects/Effect_Stone.asset");
        stoneEffect.resourceType = ResourceType.Stone;
        stoneEffect.amount       = 1;

        // --- Cards ---
        var woodCard  = GetOrCreate<CardData>(DataPath + "/Cards/Card_Wood.asset");
        woodCard.cardName    = "Wood";
        woodCard.description = "A piece of timber. A villager carries it to the storehouse.";
        woodCard.type        = CardType.Resource;
        woodCard.effect      = woodEffect;

        var stoneCard = GetOrCreate<CardData>(DataPath + "/Cards/Card_Stone.asset");
        stoneCard.cardName    = "Stone";
        stoneCard.description = "A block of stone. A villager carries it to the storehouse.";
        stoneCard.type        = CardType.Resource;
        stoneCard.effect      = stoneEffect;

        // --- Buildings ---
        var woodcutter = GetOrCreate<BuildingDefinition>(DataPath + "/Buildings/Building_Woodcutter.asset");
        woodcutter.buildingName        = "Woodcutter's Hut";
        woodcutter.description         = "Puts a villager to work felling timber.";
        woodcutter.widthCells          = 2;
        woodcutter.depthCells          = 2;
        woodcutter.materialCost        = new List<ResourceCost>
            { new(ResourceType.Wood, 2), new(ResourceType.Stone, 1) };
        woodcutter.constructionTimeBase = 20f;
        woodcutter.type                = BuildingType.Job;
        woodcutter.jobSlots            = 1;

        var cottage = GetOrCreate<BuildingDefinition>(DataPath + "/Buildings/Building_Cottage.asset");
        cottage.buildingName        = "Cottage";
        cottage.description         = "Housing for two villagers.";
        cottage.widthCells          = 2;
        cottage.depthCells          = 2;
        cottage.materialCost        = new List<ResourceCost>
            { new(ResourceType.Wood, 3), new(ResourceType.Stone, 1) };
        cottage.constructionTimeBase = 25f;
        cottage.type                = BuildingType.Housing;
        cottage.housingCapacity     = 2;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Wire into Bootstrap if present in the active scene
        var bootstrap = Object.FindObjectOfType<GameBootstrap>();
        if (bootstrap != null)
        {
            var so = new SerializedObject(bootstrap);
            so.FindProperty("woodcutterDef").objectReferenceValue = woodcutter;
            so.FindProperty("cottageDef").objectReferenceValue    = cottage;
            so.FindProperty("woodCard").objectReferenceValue      = woodCard;
            so.FindProperty("stoneCard").objectReferenceValue     = stoneCard;
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
            var parts   = path.Split('/');
            var parent  = string.Join("/", parts[..^1]);
            var folder  = parts[^1];
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}

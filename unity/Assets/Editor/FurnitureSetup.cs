using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit setup keeps the artist's assets reusable and scene references serialized.
public static class FurnitureSetup
{
    const string Root = "Assets/Art/Furniture/";

    [MenuItem("Orbita/Prepare cabin furniture")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before preparing furniture assets.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save all open scenes before preparing furniture assets.");

        AssetDatabase.Refresh();
        var bed = CharacterSetup.BuildPrefab("CabinBed", 1.55f, Root);
        var bookshelf = CharacterSetup.BuildPrefab("WoodBookshelf", 2.35f, Root);
        var sofa = CharacterSetup.BuildPrefab("TerracottaSofa", 1.10f, Root);
        var monstera = CharacterSetup.BuildPrefab("Monstera", 1.55f, Root);
        var snakePlant = CharacterSetup.BuildPrefab("SnakePlant", 1.20f, Root);
        var pothos = CharacterSetup.BuildPrefab("Pothos", .72f, Root);

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Ship.unity", OpenSceneMode.Single);
        var ship = UnityEngine.Object.FindFirstObjectByType<CourierShip>();
        if (ship == null) throw new InvalidOperationException("Ship scene has no CourierShip.");
        var furnishings = ship.GetComponent<ShipFurnishings>();
        if (furnishings == null) furnishings = Undo.AddComponent<ShipFurnishings>(ship.gameObject);
        var settings = new SerializedObject(furnishings);
        settings.FindProperty("cabinBedPrefab").objectReferenceValue = bed;
        settings.FindProperty("bookshelfPrefab").objectReferenceValue = bookshelf;
        settings.FindProperty("sofaPrefab").objectReferenceValue = sofa;
        settings.FindProperty("monsteraPrefab").objectReferenceValue = monstera;
        settings.FindProperty("snakePlantPrefab").objectReferenceValue = snakePlant;
        settings.FindProperty("pothosPrefab").objectReferenceValue = pothos;
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Orbita: cabin furniture prefabs assigned to Ship scene.");
    }
}

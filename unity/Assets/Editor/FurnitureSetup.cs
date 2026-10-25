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
        var bed = SizedPrefab("CabinBed", ShipLayout.BedSize);
        var bookshelf = SizedPrefab("WoodBookshelf", ShipLayout.BookshelfSize);
        var sofa = SizedPrefab("TerracottaSofa", ShipLayout.SofaSize);
        var monstera = CharacterSetup.BuildPrefab("Monstera", ShipLayout.MonsteraHeight, Root);
        var snakePlant = CharacterSetup.BuildPrefab("SnakePlant", ShipLayout.SnakePlantHeight, Root);
        var pothos = CharacterSetup.BuildPrefab("Pothos", ShipLayout.PothosHeight, Root);

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

    // Furniture is measured in all three axes: its height alone does not define a usable bed.
    static GameObject SizedPrefab(string name, Vector3 dimensions)
    {
        var prefab = CharacterSetup.BuildPrefab(name, dimensions.y, Root);
        string path = AssetDatabase.GetAssetPath(prefab);
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var visual = root.transform.GetChild(0);
            var renderers = visual.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var ratio = new Vector3(dimensions.x/bounds.size.x, dimensions.y/bounds.size.y, dimensions.z/bounds.size.z);
            // A separate identity transform scales Unity's axes even if the FBX root is rotated.
            var metreScale = new GameObject("Metre scale").transform;
            metreScale.SetParent(root.transform, false);
            visual.SetParent(metreScale, false);
            metreScale.localScale = ratio;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }
}

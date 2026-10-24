using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit, repeatable import step. Keeps generated GUIDs stable on subsequent runs.
[InitializeOnLoad]
public static class CharacterSetup
{
    const string Root = "Assets/Art/Characters/";

    static CharacterSetup()
    {
        EditorApplication.delayCall += PrepareFirstImport;
    }

    static void PrepareFirstImport()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || HasDirtyScenes()) return;
        if (!File.Exists(Root + "CourierGirl/CourierGirl.fbx") || !File.Exists(Root + "SpaceOwl/SpaceOwl.fbx")) return;
        if (File.Exists(Root + "CourierGirl/CourierGirl.prefab") && File.Exists(Root + "SpaceOwl/SpaceOwl.prefab")) return;
        Prepare();
    }

    [Serializable] sealed class Palette { public MaterialEntry[] materials; }
    [Serializable] sealed class MaterialEntry
    {
        public string name;
        public float[] color;
        public float roughness;
        public float metallic;
    }

    [MenuItem("Orbita/Prepare character prefabs")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before preparing character assets.");
        if (HasDirtyScenes())
            throw new InvalidOperationException("Save scene changes before preparing character assets.");

        AssetDatabase.Refresh();
        var courier = BuildPrefab("CourierGirl", 1.78f);
        var owl = BuildPrefab("SpaceOwl", .48f);
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Ship.unity", OpenSceneMode.Single);
        var ship = UnityEngine.Object.FindFirstObjectByType<CourierShip>();
        if (ship == null) throw new InvalidOperationException("Ship scene has no CourierShip.");
        var characters = ship.GetComponent<CourierCharacters>();
        if (characters == null) characters = Undo.AddComponent<CourierCharacters>(ship.gameObject);
        var settings = new SerializedObject(characters);
        settings.FindProperty("courierPrefab").objectReferenceValue = courier;
        settings.FindProperty("owlPrefab").objectReferenceValue = owl;
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Orbita: courier and owl prefabs assigned to Ship scene.");
    }

    static bool HasDirtyScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) return true;
        return false;
    }

    internal static GameObject BuildPrefab(string name, float height, string assetRoot = Root)
    {
        string folder = assetRoot + name;
        string modelPath = folder + "/" + name + ".fbx";
        var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("Missing model: " + modelPath);
        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.addCollider = false;
        importer.isReadable = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;

        string materialFolder = folder + "/Materials";
        if (!AssetDatabase.IsValidFolder(materialFolder))
            AssetDatabase.CreateFolder(folder, "Materials");
        var palette = JsonUtility.FromJson<Palette>(File.ReadAllText(folder + "/" + name + ".json"));
        var shader = Shader.Find("Standard");
        if (shader == null) throw new InvalidOperationException("Built-in Standard shader is unavailable.");
        foreach (var entry in palette.materials)
        {
            string materialPath = materialFolder + "/" + entry.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            // Blender's shader colours are linear; Unity's saved material colour is sRGB.
            material.color = new Color(entry.color[0], entry.color[1], entry.color[2], entry.color[3]).gamma;
            material.SetFloat("_Glossiness", 1 - entry.roughness);
            material.SetFloat("_Metallic", entry.metallic);
            EditorUtility.SetDirty(material);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.name), material);
        }
        importer.SaveAndReimport();

        var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var root = new GameObject(name);
        try
        {
            var visual = UnityEngine.Object.Instantiate(source, root.transform, false);
            visual.name = "Visual";
            var renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("Model has no renderers: " + name);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.y <= 0) throw new InvalidOperationException("Invalid height: " + name);
            float scale = height / bounds.size.y;
            visual.transform.localScale *= scale;
            // Feet at y=0; the centre lies on the movement pivot.
            visual.transform.localPosition = -new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;
            string path = folder + "/" + name + ".prefab";
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}

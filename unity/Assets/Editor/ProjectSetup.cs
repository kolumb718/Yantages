using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ProjectSetup
{
    const string ScenePath = "Assets/Scenes/Ship.unity";
    static ProjectSetup() { EditorApplication.delayCall += Prepare; }
    static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || System.IO.File.Exists(ScenePath)) return;
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Courier ship").AddComponent<CourierShip>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.companyName = "Yantages";
        PlayerSettings.productName = "Орбита — корабль курьера";
        AssetDatabase.SaveAssets();
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ProjectSetup
{
    const string ScenePath = "Assets/Scenes/Ship.unity";
    static ProjectSetup() { EditorApplication.delayCall += Prepare; }
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || System.IO.File.Exists(ScenePath)) return;
        if (SceneManager.GetActiveScene().isDirty) return;
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Courier ship").AddComponent<CourierShip>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.companyName = "Yantages";
        PlayerSettings.productName = "Орбита — корабль курьера";
        AssetDatabase.SaveAssets();
    }

    // Explicit entrypoint for project setup and connecting to a prestarted local server.
    [MenuItem("Orbita/Connect local MCP")]
    public static async void ConnectMcp()
    {
        Prepare();
        EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
        EditorPrefs.SetString("MCPForUnity.HttpTransportScope", "local");
        EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080");
        EditorPrefs.SetBool("MCPForUnity.TelemetryDisabled", true);
        MCPForUnity.Editor.Services.EditorConfigurationCache.Instance.SetUseHttpTransport(true);
        MCPForUnity.Editor.Services.EditorConfigurationCache.Instance.SetHttpTransportScope("local");
        MCPForUnity.Editor.Services.EditorConfigurationCache.Instance.SetHttpBaseUrl("http://127.0.0.1:8080");
        bool connected = await MCPForUnity.Editor.Services.MCPServiceLocator.Bridge.StartAsync();
        Debug.Log("Orbita MCP connection: " + connected);
    }
}

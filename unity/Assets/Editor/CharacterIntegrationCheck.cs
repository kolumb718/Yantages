using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Batch-only smoke check: real Play Mode, movement, both decks and rendered evidence.
[InitializeOnLoad]
public static class CharacterIntegrationCheck
{
    const string StageKey = "Orbita.CharacterCheck.Stage";
    const string ErrorKey = "Orbita.CharacterCheck.Error";
    static int ticks;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    static CharacterIntegrationCheck()
    {
        if (SessionState.GetInt(StageKey, 0) > 0) Attach();
    }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run this check in a separate batch editor.");
        CharacterSetup.Prepare();
        FurnitureSetup.Prepare();
        SessionState.SetString(ErrorKey, "");
        SessionState.SetString("Orbita.CharacterCheck.Deadline", DateTime.UtcNow.AddMinutes(3).Ticks.ToString());
        SessionState.SetInt(StageKey, 1);
        Attach();
        EditorApplication.isPlaying = true;
    }

    static void Attach()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= RecordError;
        Application.logMessageReceived += RecordError;
    }

    static void RecordError(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            SessionState.SetString(ErrorKey, message + "\n" + stack);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static Transform Find(string name)
    {
        var obj = GameObject.Find(name);
        Require(obj != null, "Missing runtime object: " + name);
        return obj.transform;
    }

    static void ValidateCharacters()
    {
        Require(UnityEngine.Object.FindObjectsByType<CourierCharacters>(FindObjectsSortMode.None).Length == 1,
            "Duplicate character presenters after Play Mode restart.");
        foreach (var name in new[] { "Courier girl", "Space owl companion" })
        {
            var actor = Find(name);
            var renderers = actor.GetComponentsInChildren<Renderer>();
            Require(renderers.Length > 0, name + " has no renderer.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                foreach (var material in renderer.sharedMaterials)
                    Require(material != null && material.shader != null && material.shader.isSupported,
                        name + " has a missing or unsupported material.");
            }
            float height = name == "Courier girl" ? 1.78f : .48f;
            Require(Mathf.Abs(bounds.size.y - height) < .02f, name + " has incorrect height or up axis.");
            Require(bounds.size.x < height && bounds.size.z < height, name + " has incorrect proportions.");
        }
    }

    static void Invoke(CourierShip ship, string method, params object[] args)
    {
        typeof(CourierShip).GetMethod(method, Private).Invoke(ship, args);
    }

    static void ValidateFurnishings()
    {
        Require(UnityEngine.Object.FindObjectsByType<ShipFurnishings>(FindObjectsSortMode.None).Length == 1,
            "Duplicate furnishing builders.");
        string[] names = { "Cabin bed", "Wood bookshelf", "Terracotta sofa", "Monstera downstairs",
            "Snake plant bridge", "Monstera upstairs", "Pothos reading nook" };
        float[] heights = { 1.55f, 2.35f, 1.10f, 1.55f, 1.20f, 1.55f, .72f };
        for (int i = 0; i < names.Length; i++)
        {
            var prop = Find(names[i]);
            var renderer = prop.GetComponentInChildren<Renderer>();
            Require(renderer != null, "Missing furniture geometry: " + names[i]);
            Require(Mathf.Abs(renderer.bounds.size.y - heights[i]) < .02f, "Wrong furniture height: " + names[i]);
            Require(Mathf.Abs(renderer.bounds.min.y - prop.position.y) < .02f, "Furniture floats or sinks: " + names[i]);
            foreach (var material in renderer.sharedMaterials)
                Require(material != null && material.shader != null && material.shader.isSupported,
                    "Missing furniture material: " + names[i]);
        }
    }

    static void Capture(string name, bool close)
    {
        var camera = Camera.main;
        var position = camera.transform.position;
        var rotation = camera.transform.rotation;
        float fov = camera.fieldOfView;
        float aspect = camera.aspect;
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var target = new RenderTexture(1600, 1000, 24);
        var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        try
        {
            if (close)
            {
                var courier = Find("Courier");
                camera.transform.position = courier.position + new Vector3(.30f, 1.9f, 4.4f);
                camera.transform.LookAt(courier.position + new Vector3(.25f, .9f, 0));
                camera.fieldOfView = 37;
            }
            if (name == "furniture-bedroom")
            {
                camera.transform.position = new Vector3(-7.3f, 7.0f, 1.1f);
                camera.transform.LookAt(new Vector3(-8.3f, 5.1f, -2.8f));
                camera.fieldOfView = 60;
            }
            else if (name == "furniture-lounge")
            {
                camera.transform.position = new Vector3(-9.2f, 2.0f, .2f);
                camera.transform.LookAt(new Vector3(-10.2f, .65f, -1.8f));
                camera.fieldOfView = 67;
            }
            camera.targetTexture = target;
            camera.aspect = 1600f / 1000;
            if (!close && !name.StartsWith("furniture-"))
                UnityEngine.Object.FindFirstObjectByType<ShipCameraFraming>().Reframe();
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Captures");
            File.WriteAllBytes("Captures/" + name + ".png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.fieldOfView = fov;
            camera.aspect = aspect;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }

    static void Tick()
    {
        try
        {
            string error = SessionState.GetString(ErrorKey, "");
            Require(string.IsNullOrEmpty(error), error);
            long deadline = long.Parse(SessionState.GetString("Orbita.CharacterCheck.Deadline", "0"));
            Require(DateTime.UtcNow.Ticks < deadline, "Timed out waiting for Play Mode.");
            int stage = SessionState.GetInt(StageKey, 0);
            if (stage == 4)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                SessionState.SetInt(StageKey, 5);
                ticks = 0;
                EditorApplication.isPlaying = true;
                return;
            }
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || ++ticks < 20) return;
            var ship = UnityEngine.Object.FindFirstObjectByType<CourierShip>();
            var courier = Find("Courier");
            var owl = Find("Space owl companion");
            if (stage == 1)
            {
                ValidateCharacters();
                ValidateFurnishings();
                Capture("characters-game", false);
                Capture("characters-close", true);
                Capture("furniture-bedroom", false);
                Capture("furniture-lounge", false);
                var start = courier.position;
                Invoke(ship, "MoveCourier", Vector3.right, 1f);
                Require(Mathf.Abs(courier.position.x - start.x - 1) < .001f, "Courier movement failed.");
                courier.position = start;
                Invoke(ship, "MoveCourier", new Vector3(1, 0, 1), 1f);
                Require(Mathf.Abs(Vector3.Distance(courier.position, start) - 1) < .001f, "Diagonal speed changed.");
                Invoke(ship, "MoveCourier", Vector3.right, 100f);
                Require(Mathf.Abs(courier.position.x - 13.2f) < .001f, "Deck movement bounds failed.");
                courier.position = new Vector3(.3f, 0, 1.8f);
                Invoke(ship, "Interact");
                Require(Mathf.Abs(courier.position.y - 4.1f) < .001f, "Lift up failed.");
                SessionState.SetInt(StageKey, 2);
                ticks = 0;
            }
            else if (stage == 2)
            {
                Require(Mathf.Abs(owl.position.y - courier.position.y - 1.05f) < .1f, "Owl did not follow to upper deck.");
                Capture("characters-upper-deck", false);
                Invoke(ship, "Interact");
                Require(Mathf.Abs(courier.position.y) < .001f, "Lift down failed.");
                SessionState.SetInt(StageKey, 3);
                ticks = 0;
            }
            else if (stage == 3)
            {
                Require(Mathf.Abs(owl.position.y - courier.position.y - 1.05f) < .1f, "Owl did not return to lower deck.");
                courier.position = new Vector3(.4f, 0, -.6f);
                Invoke(ship, "Interact");
                Require((bool)typeof(CourierShip).GetField("terminal", Private).GetValue(ship), "Terminal interaction regressed.");
                SessionState.SetInt(StageKey, 4);
                EditorApplication.isPlaying = false;
            }
            else if (stage == 5)
            {
                ValidateCharacters();
                ValidateFurnishings();
                SessionState.SetInt(StageKey, 0);
                Directory.CreateDirectory("Captures");
                File.WriteAllText("Captures/character-check.txt", "PASS: character and furniture materials, scale, furniture floor placement, movement, diagonal speed, bounds, lift both ways, owl deck following, terminal, Play Mode restart.\n");
                Debug.Log("ORBITA_CHARACTER_CHECK_PASS");
                EditorApplication.Exit(0);
            }
        }
        catch (Exception exception)
        {
            SessionState.SetInt(StageKey, 0);
            Directory.CreateDirectory("Captures");
            File.WriteAllText("Captures/character-check.txt", "FAIL: " + exception);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
}

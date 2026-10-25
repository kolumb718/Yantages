using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

// Batch-only check of the systems around the courier: camera window, input bindings,
// furniture footprints, animated lift doors and build-safe shaders. Writes Captures/ship-systems-*.
[InitializeOnLoad]
public static class ShipSystemsCheck
{
    const string StageKey = "Orbita.ShipSystems.Stage";
    const string Report = "Captures/ship-systems-check.txt";
    static double waitUntil;

    static ShipSystemsCheck()
    {
        if (SessionState.GetInt(StageKey, 0) > 0) EditorApplication.update += Tick;
    }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run this check in a separate batch editor.");
        Directory.CreateDirectory("Captures");
        File.WriteAllText(Report, "");
        EditorSceneManager.OpenScene("Assets/Scenes/Ship.unity");
        SessionState.SetInt(StageKey, 1);
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    static void Log(string line)
    {
        File.AppendAllText(Report, line + "\n");
        Debug.Log("SHIP_SYSTEMS " + line);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static void Finish(int code)
    {
        SessionState.SetInt(StageKey, 0);
        EditorApplication.update -= Tick;
        Log(code == 0 ? "ORBITA_SHIP_SYSTEMS_PASS" : "ORBITA_SHIP_SYSTEMS_FAIL");
        EditorApplication.Exit(code);
    }

    static Vector2 OnScreen(Camera camera, Transform courier)
        => (Vector2)camera.WorldToViewportPoint(courier.position + Vector3.up * 1.1f) * 2 - Vector2.one;

    static void Capture(string name)
    {
        var camera = Camera.main;
        var target = new RenderTexture(1600, 1000, 24);
        var previous = camera.targetTexture;
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
        image.Apply();
        File.WriteAllBytes("Captures/" + name + ".png", image.EncodeToPNG());
        RenderTexture.active = null;
        camera.targetTexture = previous;
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(target);
    }

    static void Wait(double seconds, int next)
    {
        waitUntil = EditorApplication.timeSinceStartup + seconds;
        SessionState.SetInt(StageKey, next);
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < waitUntil) return;
        int stage = SessionState.GetInt(StageKey, 0);
        try
        {
            var ship = UnityEngine.Object.FindFirstObjectByType<CourierShip>();
            if (ship == null || !ship.Ready) return;
            var framing = UnityEngine.Object.FindFirstObjectByType<ShipCameraFraming>();
            var camera = Camera.main;
            var courier = ship.Motor.Courier;
            if (stage == 1) { Wait(3, 2); return; }
            if (stage == 2)
            {
                CheckBuildSafety();
                CheckInput(ship);
                var screen = OnScreen(camera, courier);
                Log("follow start: camera " + camera.transform.position + " courier screen " + screen);
                Require(Mathf.Abs(screen.x) <= .32f && Mathf.Abs(screen.y) <= .26f, "Courier starts outside the camera window.");
                Capture("ship-systems-follow");
                SessionState.SetString("Orbita.ShipSystems.Camera", JsonUtility.ToJson(camera.transform.position));
                ship.Motor.Move(Vector3.right, .3f);
                Wait(1, 3);
            }
            else if (stage == 3)
            {
                var start = JsonUtility.FromJson<Vector3>(SessionState.GetString("Orbita.ShipSystems.Camera", ""));
                float drift = Vector3.Distance(start, camera.transform.position);
                Log("dead zone drift " + drift.ToString("F4"));
                Require(drift < .01f, "Camera moved while the courier stayed inside the window.");
                ship.Motor.Move(Vector3.back, 1.2f);
                ship.Motor.Move(Vector3.right, 12);
                Wait(2, 4);
            }
            else if (stage == 4)
            {
                var start = JsonUtility.FromJson<Vector3>(SessionState.GetString("Orbita.ShipSystems.Camera", ""));
                var screen = OnScreen(camera, courier);
                float pan = Vector3.Dot(camera.transform.position - start, camera.transform.right);
                Log("walk to bridge: courier x " + courier.position.x.ToString("F2") + " pan " + pan.ToString("F2") + " screen " + screen);
                Require(Mathf.Abs(pan) > 1, "Camera did not pan when the courier pushed the window edge.");
                Require(Mathf.Abs(screen.x) <= .33f && Mathf.Abs(screen.x) > .2f, "Courier is not held at the window edge.");
                Capture("ship-systems-bridge");
                CheckObstacles(ship);
                framing.Follow = false;
                Wait(2.5, 5);
            }
            else if (stage == 5)
            {
                var overview = framing.OverviewPosition;
                float error = Vector3.Distance(camera.transform.position, overview);
                Log("overview error " + error.ToString("F4"));
                Require(error < .05f, "Overview toggle does not return to the whole-ship frame.");
                Capture("ship-systems-overview");
                framing.Follow = true;
                courier.position = new Vector3(-1.4f, 0, 1.8f);
                ship.Motor.Move(Vector3.right, 1.5f);
                ship.Interact();
                Require(ship.Lift.IsRiding, "Courier could not board the lift.");
                Wait(.2, 6);
            }
            else if (stage == 6)
            {
                var leaf = GameObject.Find("Lift Glass door assembly 0 0").transform;
                float z = Mathf.Abs(leaf.localPosition.z);
                Log("door leaf while departing " + z.ToString("F3"));
                Require(z > .33f && z < .87f, "Lift doors snap instead of sliding.");
                Wait(8, 7);
            }
            else if (stage == 7)
            {
                if (ship.Lift.IsMoving) return;
                Require(ship.Deck == ShipLayout.CabinDeck, "Lift did not reach the cabin.");
                var screen = OnScreen(camera, courier);
                Log("cabin arrival screen " + screen);
                Require(Mathf.Abs(screen.x) < .6f && Mathf.Abs(screen.y) < .6f, "Camera lost the courier after the lift ride.");
                Capture("ship-systems-cabin");
                Finish(0);
            }
        }
        catch (Exception error)
        {
            Log("ERROR " + error.Message);
            Finish(1);
        }
    }

    static void CheckBuildSafety()
    {
        foreach (var name in new[] { "Shaders/WindowCity", "Shaders/Hologram", "Shaders/OrbitaUnlit" })
            Require(Resources.Load<Shader>(name) != null, "Shader is not in Resources: " + name);
        var standard = Resources.Load<Material>("Shaders/StandardReference");
        Require(standard != null && standard.shader.name == "Standard", "Standard shader is not pinned for builds.");
        foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            foreach (var material in renderer.sharedMaterials)
                Require(material == null || (material.shader.name != "Unlit/Color" && material.shader.name != "Unlit/Texture"),
                    "Built-in unlit shader is still used by " + renderer.name);
        Log("build safety: custom shaders in Resources, Standard pinned, no built-in unlit materials");
    }

    static void CheckInput(CourierShip ship)
    {
        var input = ship.Controls;
        Require(input != null, "Courier input is not created.");
        Require(input.Label(CourierAction.Interact) == "E", "Keyboard interact label: " + input.Label(CourierAction.Interact));
        // A batch editor never has focus; let the virtual pad through for the duration of the test.
        var settings = InputSystem.settings;
        var background = settings.backgroundBehavior;
        var editorBehaviour = settings.editorInputBehaviorInPlayMode;
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var gamepad = InputSystem.AddDevice<Gamepad>();
        try
        {
            InputSystem.QueueStateEvent(gamepad, new UnityEngine.InputSystem.LowLevel.GamepadState { leftStick = new Vector2(1, 0) });
            InputSystem.Update();
            Require(input.Move.x > .9f, "Gamepad left stick does not drive movement.");
            Require(input.UsingGamepad, "Prompts did not switch to the gamepad.");
            Log("input: keyboard E, gamepad stick " + input.Move + ", gamepad interact label " + input.Label(CourierAction.Interact));
        }
        finally
        {
            InputSystem.RemoveDevice(gamepad);
            settings.backgroundBehavior = background;
            settings.editorInputBehaviorInPlayMode = editorBehaviour;
        }
    }

    static void CheckObstacles(CourierShip ship)
    {
        var courier = ship.Motor.Courier;
        courier.position = new Vector3(-5, 0, .6f);
        ship.Motor.Move(Vector3.left, 6);
        Log("walk into coffee table stops at x " + courier.position.x.ToString("F2"));
        Require(courier.position.x > -7.6f, "Courier walked through the lounge coffee table.");
        courier.position = new Vector3(11.6f, 0, 4.2f);
        ship.Motor.Move(Vector3.back, 3);
        Log("walk into wardrobe stops at z " + courier.position.z.ToString("F2"));
        Require(courier.position.z > 2.4f, "Courier walked through the equipment wardrobe.");
        courier.position = new Vector3(-4, 0, -.6f);
        ship.Motor.Move(Vector3.right, 100);
        Require(Mathf.Abs(courier.position.x - 13.2f) < .001f, "Furniture blocks the main deck route.");
        courier.position = ShipLayout.CourierStart;
    }
}

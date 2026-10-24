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
        Directory.CreateDirectory("Captures");
        File.WriteAllText("Captures/lift-animation-times.csv", "");
        SessionState.SetInt("Orbita.CharacterCheck.LiftFrame", -1);
        SessionState.SetBool("Orbita.CharacterCheck.LiftMiddle", false);
        SessionState.SetBool("Orbita.CharacterCheck.LiftDownMiddle", false);
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

    static void ValidateInteriorDetails()
    {
        var window = Find("Panoramic city window");
        var renderers = window.GetComponentsInChildren<Renderer>();
        Require(renderers.Length > 0, "Window dressing has no visible geometry.");
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        Require(bounds.min.x >= 2.6f && bounds.max.x <= 13.4f && bounds.min.y >= 1.1f && bounds.max.y <= 7.9f,
            "City geometry escapes the panoramic window.");
        foreach (var name in new[] { "Ship name lettering", "Cabin deck sign", "Bridge sign" })
        {
            var label = Find(name).GetComponent<TextMesh>();
            Require(label != null && label.font != null && label.font.HasCharacter('Я'), "Wall lettering has no Cyrillic font.");
            var size = label.GetComponent<Renderer>().bounds.size;
            Require(size.x < 10 && size.y < 1.3f, "Wall lettering is oversized: " + name);
        }
        var planet = Find("Holographic planet");
        var atmosphere = Find("Hologram atmosphere").GetComponent<Renderer>();
        Require(atmosphere.sharedMaterial.shader.isSupported, "Hologram shader is unsupported.");
        Require(planet.position.y > 1 && planet.position.y < 2, "Hologram is misplaced.");
        var platform = Find("Navigation hologram platform");
        Require(platform.position.x > 5 && platform.position.z < 0, "Hologram must be next to the bridge window.");
        Require(Vector3.Distance(platform.position, new Vector3(.3f, .08f, 1.8f)) > 3,
            "Hologram still overlaps the lift.");
        Require(Find("Hanging wall cable").GetComponent<LineRenderer>().positionCount > 2, "Hanging wire has no curved geometry.");
        var wardrobe = Find("Equipment wardrobe");
        var equipment = wardrobe.GetComponentsInChildren<Renderer>();
        Require(equipment.Length > 0, "Wardrobe has no geometry.");
        var wardrobeBounds = equipment[0].bounds;
        foreach (var renderer in equipment)
        {
            wardrobeBounds.Encapsulate(renderer.bounds);
            foreach (var material in renderer.sharedMaterials)
                Require(material != null && material.shader != null && material.shader.isSupported,
                    "Wardrobe equipment has an unsupported material.");
        }
        Require(wardrobeBounds.min.y > -.05f && wardrobeBounds.max.y < 3.2f && wardrobeBounds.max.x < 13.4f
            && wardrobeBounds.max.z < 3.2f, "Wardrobe does not fit inside the ship.");
        foreach (var name in new[] { "Stored EVA suit", "Wardrobe helmet visor", "Wardrobe medkit",
            "Wardrobe oxygen tank", "Wardrobe tool case" }) Find(name);
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
        bool animationFrame = name.StartsWith("lift-frame-");
        int width = animationFrame ? 800 : 1600, height = animationFrame ? 800 : 1000;
        var target = new RenderTexture(width, height, 24);
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
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
            else if (name == "details-window")
            {
                camera.transform.position = new Vector3(10.0f, 5.2f, 5.5f);
                camera.transform.LookAt(new Vector3(8, 4.45f, -4.45f));
                camera.fieldOfView = 44;
            }
            else if (name == "details-hologram")
            {
                camera.transform.position = new Vector3(5.5f, 2.8f, 3.7f);
                camera.transform.LookAt(new Vector3(7.8f, 1.05f, -.8f));
                camera.fieldOfView = 36;
            }
            else if (name == "details-wardrobe")
            {
                camera.transform.position = new Vector3(13.7f, 2.5f, 7.2f);
                camera.transform.LookAt(new Vector3(11.6f, 1.35f, 2.0f));
                camera.fieldOfView = 40;
            }
            else if (name.StartsWith("lift-") && name != "lift-overview")
            {
                camera.transform.position = new Vector3(5.6f, 5.5f, 8.0f);
                camera.transform.LookAt(new Vector3(.3f, 3.1f, 1.8f));
                camera.fieldOfView = 48;
            }
            camera.targetTexture = target;
            camera.aspect = (float)width / height;
            if (!close && !name.StartsWith("furniture-") && !name.StartsWith("details-")
                && (!name.StartsWith("lift-") || name == "lift-overview"))
                UnityEngine.Object.FindFirstObjectByType<ShipCameraFraming>().Reframe();
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Captures");
            File.WriteAllBytes("Captures/" + name + ".png", image.EncodeToPNG());
            if (animationFrame)
                File.AppendAllText("Captures/lift-animation-times.csv", name+","+Time.time.ToString("R", System.Globalization.CultureInfo.InvariantCulture)+"\n");
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
            var lift = UnityEngine.Object.FindFirstObjectByType<ShipLift>();
            if (stage == 1)
            {
                ValidateCharacters();
                ValidateFurnishings();
                ValidateInteriorDetails();
                Capture("characters-game", false);
                Capture("characters-close", true);
                Capture("furniture-bedroom", false);
                Capture("furniture-lounge", false);
                Capture("details-window", false);
                Capture("details-hologram", false);
                Capture("details-wardrobe", false);
                SessionState.SetFloat("Orbita.CharacterCheck.PlanetYaw", Find("Holographic planet").localEulerAngles.y);
                var start = courier.position;
                Invoke(ship, "MoveCourier", Vector3.right, 1f);
                Require(Mathf.Abs(courier.position.x - start.x - 1) < .001f, "Courier movement failed.");
                courier.position = start;
                Invoke(ship, "MoveCourier", new Vector3(1, 0, 1), 1f);
                Require(Mathf.Abs(Vector3.Distance(courier.position, start) - 1) < .001f, "Diagonal speed changed.");
                courier.position = new Vector3(-4, 0, -.6f);
                Invoke(ship, "MoveCourier", Vector3.right, 100f);
                Require(Mathf.Abs(courier.position.x - 13.2f) < .001f, "Deck movement bounds failed.");
                // Glass blocks entry from the front; the left landing door admits the player.
                courier.position = new Vector3(.3f, 0, 3.8f);
                Invoke(ship, "MoveCourier", Vector3.back, 2f);
                Require(courier.position.z >= 3.1f, "Courier walked through the front glass.");
                courier.position = new Vector3(-1.4f, 0, 1.8f);
                Invoke(ship, "MoveCourier", Vector3.right, 1.5f);
                Require(courier.position.x > -.1f, "Open left landing door is blocked.");
                foreach (var renderer in ship.GetComponentsInChildren<Renderer>())
                    Require(!renderer.bounds.Contains(new Vector3(.3f, 3.9f, 1.8f)), "Upper deck blocks the lift shaft opening.");
                Capture("lift-bottom", false);
                Invoke(ship, "Interact");
                Require(lift.IsRiding && lift.IsMoving && Mathf.Abs(courier.position.y) < .001f, "Lift teleported instead of starting a ride.");
                SessionState.SetFloat("Orbita.CharacterCheck.LiftStart", Time.time);
                Capture("lift-frame-000", false);
                SessionState.SetInt("Orbita.CharacterCheck.LiftFrame", 0);
                SessionState.SetInt(StageKey, 2);
                ticks = 0;
            }
            else if (stage == 2)
            {
                if (lift.IsMoving)
                {
                    CheckRiding(ship, courier, owl, lift);
                    int bin = Mathf.FloorToInt(lift.Progress*20);
                    if (bin > SessionState.GetInt("Orbita.CharacterCheck.LiftFrame", -1))
                    {
                        Capture("lift-frame-"+bin.ToString("D3"), false);
                        SessionState.SetInt("Orbita.CharacterCheck.LiftFrame", bin);
                    }
                    if (lift.Progress > .4f && lift.Progress < .7f && !SessionState.GetBool("Orbita.CharacterCheck.LiftMiddle", false))
                    {
                        Require(courier.position.y > .5f && courier.position.y < 3.8f, "No intermediate lift height.");
                        Capture("lift-middle", false);
                        Capture("lift-overview", false);
                        SessionState.SetBool("Orbita.CharacterCheck.LiftMiddle", true);
                    }
                    return;
                }
                Require(Time.time-SessionState.GetFloat("Orbita.CharacterCheck.LiftStart", 0) >= ShipLift.TravelSeconds,
                    "Lift arrived too fast.");
                Require(SessionState.GetBool("Orbita.CharacterCheck.LiftMiddle", false), "Lift skipped intermediate frames.");
                Require(lift.CurrentDeck == 1 && Mathf.Abs(courier.position.y-4.1f) < .001f, "Lift up failed.");
                Require(courier.parent != lift.Platform, "Courier was not released on arrival.");
                Capture("lift-frame-021", false);
                Capture("lift-top", false);
                Require(Mathf.Abs(Mathf.DeltaAngle(SessionState.GetFloat("Orbita.CharacterCheck.PlanetYaw", 0),
                    Find("Holographic planet").localEulerAngles.y)) > .01f, "Hologram planet does not rotate.");
                Require(Mathf.Abs(owl.position.y - courier.position.y - 1.05f) < .1f, "Owl did not follow to upper deck.");
                Capture("characters-upper-deck", false);
                Invoke(ship, "Interact");
                Require(lift.IsRiding && Mathf.Abs(courier.position.y-4.1f) < .001f, "Lift down teleported or failed to start.");
                SessionState.SetInt(StageKey, 3);
                ticks = 0;
            }
            else if (stage == 3)
            {
                if (lift.IsMoving)
                {
                    CheckRiding(ship, courier, owl, lift);
                    if (lift.Progress > .4f && lift.Progress < .7f && !SessionState.GetBool("Orbita.CharacterCheck.LiftDownMiddle", false))
                    {
                        Capture("lift-descending", false);
                        SessionState.SetBool("Orbita.CharacterCheck.LiftDownMiddle", true);
                    }
                    return;
                }
                Require(lift.CurrentDeck == 0 && Mathf.Abs(courier.position.y) < .001f, "Lift down failed.");
                Require(Mathf.Abs(owl.position.y - courier.position.y - 1.05f) < .1f, "Owl did not return to lower deck.");
                courier.position = new Vector3(.4f, 0, -.6f);
                Invoke(ship, "Interact");
                Require((bool)typeof(CourierShip).GetField("terminal", Private).GetValue(ship), "Terminal interaction regressed.");
                typeof(CourierShip).GetField("terminal", Private).SetValue(ship, false);
                // Simulate a passenger waiting upstairs: an absent platform must be callable.
                courier.position = new Vector3(-1.4f, 4.1f, 1.8f);
                typeof(CourierShip).GetField("deck", Private).SetValue(ship, 1);
                Invoke(ship, "MoveCourier", Vector3.right, 2f);
                Require(courier.position.x < -1.10f, "Closed landing door admits the passenger into an empty shaft.");
                courier.position = new Vector3(-1.4f, 4.1f, 1.8f);
                Invoke(ship, "Interact");
                Require(lift.IsMoving && !lift.IsRiding, "Empty lift call failed.");
                SessionState.SetInt(StageKey, 6);
                ticks = 0;
            }
            else if (stage == 6)
            {
                Require(Mathf.Abs(courier.position.y-4.1f) < .001f, "Empty lift moved the waiting passenger.");
                if (lift.IsMoving) return;
                Require(lift.CurrentDeck == 1, "Called lift did not arrive upstairs.");
                Invoke(ship, "MoveCourier", Vector3.right, 1.5f);
                Invoke(ship, "Interact");
                Require(lift.IsRiding, "Passenger cannot board after calling the lift.");
                SessionState.SetInt(StageKey, 7);
                ticks = 0;
            }
            else if (stage == 7)
            {
                if (lift.IsMoving) return;
                Require(lift.CurrentDeck == 0 && Mathf.Abs(courier.position.y) < .001f, "Return ride after empty call failed.");
                SessionState.SetInt(StageKey, 4);
                EditorApplication.isPlaying = false;
            }
            else if (stage == 5)
            {
                ValidateCharacters();
                ValidateFurnishings();
                ValidateInteriorDetails();
                SessionState.SetInt(StageKey, 0);
                Directory.CreateDirectory("Captures");
                File.WriteAllText("Captures/character-check.txt", "PASS: scene and materials, glass lift boundaries and open doorway, clear shaft opening, six-second ascent/descent, intermediate passenger/platform height, movement lock, owl cabin travel, repeat-input guard, arrival release, empty lift call and boarding, terminal, Play Mode restart.\n");
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

    static void CheckRiding(CourierShip ship, Transform courier, Transform owl, ShipLift lift)
    {
        Require(lift.IsRiding && courier.parent == lift.Platform, "Passenger is not attached to the moving platform.");
        Require(Mathf.Abs(courier.position.y-lift.Platform.position.y) < .002f, "Passenger drifted off the platform.");
        Require(Mathf.Abs(owl.position.y-courier.position.y-1.05f) < .10f, "Owl left the moving cabin.");
        var position = courier.position;
        Invoke(ship, "MoveCourier", Vector3.right, 1f);
        Require(Vector3.Distance(position, courier.position) < .001f, "Movement was not locked during travel.");
        Require(!lift.TryUse(lift.CurrentDeck), "Repeated interaction restarted a moving lift.");
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Batch-only smoke check: real Play Mode, movement, both decks and rendered evidence.
[InitializeOnLoad]
public static class CharacterIntegrationCheck
{
    const string StageKey = "Orbita.CharacterCheck.Stage";
    const string ErrorKey = "Orbita.CharacterCheck.Error";
    static int ticks;

    static CharacterIntegrationCheck()
    {
        if (SessionState.GetInt(StageKey, 0) > 0) Attach();
    }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run this check in a separate batch editor.");
        CharacterSetup.Prepare();
        AnimatedCharacterSetup.Prepare();
        FurnitureSetup.Prepare();
        Directory.CreateDirectory("Captures");
        File.WriteAllText("Captures/lift-animation-times.csv", "");
        File.WriteAllText("Captures/hangar-animation-times.csv", "");
        SessionState.SetInt("Orbita.CharacterCheck.HangarFrame", -1);
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

    static Transform FindInactive(string name)
    {
        foreach (var item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (item.name == name) return item;
        throw new Exception("Missing runtime object: " + name);
    }

    static void ValidateCharacters()
    {
        Require(UnityEngine.Object.FindObjectsByType<CourierCharacters>(FindObjectsSortMode.None).Length == 1,
            "Duplicate character presenters after Play Mode restart.");
        foreach (var name in new[] { "Kristi", "Space owl companion" })
        {
            var actor = Find(name);
            var renderers = actor.GetComponentsInChildren<Renderer>();
            Require(renderers.Length > 0, name + " has no renderer.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                foreach (var material in renderer.sharedMaterials)
                {
                    Require(material != null && material.shader != null && material.shader.isSupported
                        , name + " / " + renderer.name + " has a missing or unsupported material.");
                    // Imported skinned characters retain their texture maps. The optional
                    // courier backpack is palette-coloured primitive geometry, like scenery.
                    if (renderer is SkinnedMeshRenderer)
                        Require(material.mainTexture != null, name + " / " + renderer.name + " lost its imported texture.");
                }
            }
            var animator = actor.GetComponentInChildren<Animator>();
            Require(animator != null && animator.runtimeAnimatorController != null && animator.isInitialized,
                name + " has no running animator.");
            if (name == "Kristi")
            {
                Require(animator.isHuman && animator.avatar.isValid, "Kristi has no valid humanoid avatar.");
                // Idle pose: arms down, so the animated bounds are narrower than the height.
                Require(Mathf.Abs(bounds.size.y - AnimatedCharacterSetup.KristiHeight) < .08f, "Kristi has incorrect height or up axis.");
                Require(bounds.size.x < bounds.size.y && bounds.size.z < bounds.size.y, "Kristi has incorrect proportions.");
            }
            else Require(bounds.size.y > .3f && bounds.size.y < 1.6f, "Owl has an implausible size.");
        }
    }

    static void Move(CourierShip ship, Vector3 direction, float distance) => ship.Motor.Move(direction, distance);

    static void ValidateFurnishings()
    {
        Require(UnityEngine.Object.FindObjectsByType<ShipFurnishings>(FindObjectsSortMode.None).Length == 1,
            "Duplicate furnishing builders.");
        string[] names = { "Cabin bed", "Wood bookshelf", "Terracotta sofa", "Monstera downstairs",
            "Snake plant bridge", "Monstera upstairs", "Pothos reading nook" };
        float[] heights = { ShipLayout.BedSize.y, ShipLayout.BookshelfSize.y, ShipLayout.SofaSize.y,
            ShipLayout.MonsteraHeight, ShipLayout.SnakePlantHeight, ShipLayout.MonsteraHeight, ShipLayout.PothosHeight };
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
        Require(bounds.min.x >= 2.0f && bounds.max.x <= 14f && bounds.min.y >= -.9f && bounds.max.y <= 5.6f,
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
        var wardrobe = FindInactive("Equipment wardrobe");
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
        Require(Vector3.Distance(wardrobe.localPosition, ShipLayout.EvaWardrobePosition)<.001f
            && wardrobe.parent.name == "Underground hangar", "EVA wardrobe is not attached to the hangar.");
        Require(wardrobeBounds.min.y > ShipLayout.HangarHeight-.05f
            && wardrobeBounds.max.y < ShipLayout.HangarHeight+3.2f,
            "Wardrobe does not stand on the hangar floor.");
        foreach (var name in new[] { "Stored EVA suit", "Wardrobe helmet visor", "Wardrobe medkit",
            "Wardrobe oxygen tank", "Wardrobe tool case" }) FindInactive(name);
    }

    static void ValidateWindowParallax()
    {
        var view = UnityEngine.Object.FindFirstObjectByType<ShipWindowView>();
        var camera = Camera.main;
        var start = camera.transform.position;
        var near = Find("Foreground docks");
        var far = Find("Distant skyline");
        var frame = Find("Panoramic window frame");
        view.UpdateView(camera);
        var nearStart = near.localPosition;
        var farStart = far.localPosition;
        var frameStart = frame.position;
        camera.transform.position += new Vector3(2, 1, 0);
        view.UpdateView(camera);
        Require(near.localPosition.x > nearStart.x && near.localPosition.y > nearStart.y,
            "Window layers do not respond to horizontal and vertical camera movement.");
        Require((far.localPosition - farStart).magnitude > (near.localPosition - nearStart).magnitude * 3,
            "Window parallax has no depth separation.");
        Require(frame.position == frameStart, "Window frame moves with scenery.");
        camera.transform.position = start;
        view.UpdateView(camera);
        Require(Vector3.Distance(near.localPosition, nearStart) < .0001f, "Window parallax accumulates drift.");
        foreach (var renderer in near.GetComponentsInChildren<Renderer>())
            foreach (var material in renderer.sharedMaterials)
                Require(material.shader.name == "Orbita/WindowCity" && material.shader.isSupported,
                    "Window aperture clipping shader is unavailable.");
        for (int i = 0; i < 21; i++) Capture("parallax-" + i.ToString("D3"), false);
        view.UpdateView(camera);
        File.WriteAllText("Captures/parallax-check.txt",
            "PASS: horizontal/vertical camera response, distant scenery compensates eye movement more than foreground, fixed frame, return without drift, supported aperture shader. 21 actual Unity frames.\n");
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
        bool hangarFrame = name.StartsWith("hangar-frame-");
        bool animationFrame = name.StartsWith("lift-frame-") || hangarFrame;
        bool parallax = name.StartsWith("parallax-");
        int width = animationFrame || parallax ? 800 : 1600, height = animationFrame ? 800 : parallax ? 500 : 1000;
        var target = new RenderTexture(width, height, 24);
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            if (parallax)
            {
                float phase = int.Parse(name.Substring(9)) * Mathf.PI * 2 / 20;
                camera.transform.position = new Vector3(8 + Mathf.Sin(phase) * 1.3f, 4.5f + Mathf.Sin(phase) * .45f, 7);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.fieldOfView = 38;
            }
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
            else if (name == "hangar-craft")
            {
                camera.transform.position = new Vector3(-16, -1.6f, 11);
                camera.transform.LookAt(new Vector3(-6, -5.1f, -2.65f));
                camera.fieldOfView = 48;
            }
            else if (name == "details-window")
            {
                camera.transform.position = new Vector3(10.0f, 3.2f, 5.5f);
                camera.transform.LookAt(new Vector3(8, 2.5f, -4.45f));
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
                camera.transform.position = ShipLayout.EvaWardrobePosition+new Vector3(3,2,5);
                camera.transform.LookAt(ShipLayout.EvaWardrobePosition+Vector3.up*1.2f);
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
            if (name != "hangar-craft" && !parallax && !close && !name.StartsWith("furniture-") && !name.StartsWith("details-")
                && (!name.StartsWith("lift-") || name == "lift-overview"))
                UnityEngine.Object.FindFirstObjectByType<ShipCameraFraming>().Reframe();
            UnityEngine.Object.FindFirstObjectByType<ShipWindowView>().UpdateView(camera);
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Captures");
            File.WriteAllBytes("Captures/" + name + ".png", image.EncodeToPNG());
            if (animationFrame)
                File.AppendAllText("Captures/"+(hangarFrame ? "hangar" : "lift")+"-animation-times.csv", name+","+Time.time.ToString("R", System.Globalization.CultureInfo.InvariantCulture)+"\n");
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.fieldOfView = fov;
            camera.aspect = aspect;
            UnityEngine.Object.FindFirstObjectByType<ShipWindowView>().UpdateView(camera);
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
                ValidateWindowParallax();
                Capture("details-hologram", false);
                SessionState.SetFloat("Orbita.CharacterCheck.PlanetYaw", Find("Holographic planet").localEulerAngles.y);
                var start = courier.position;
                Move(ship, Vector3.right, 1f);
                Require(Mathf.Abs(courier.position.x - start.x - 1) < .001f, "Courier movement failed.");
                courier.position = start;
                Move(ship, new Vector3(1, 0, 1), 1f);
                Require(Mathf.Abs(Vector3.Distance(courier.position, start) - 1) < .001f, "Diagonal speed changed.");
                courier.position = new Vector3(-4, 0, -.6f);
                Move(ship, Vector3.right, 100f);
                Require(Mathf.Abs(courier.position.x - 13.2f) < .001f, "Deck movement bounds failed.");
                // Glass blocks entry from the front; the left landing door admits the player.
                courier.position = new Vector3(.3f, 0, 3.8f);
                Move(ship, Vector3.back, 2f);
                Require(courier.position.z >= 3.1f, "Courier walked through the front glass.");
                courier.position = new Vector3(-1.4f, 0, 1.8f);
                Move(ship, Vector3.right, 1.5f);
                Require(courier.position.x > -.1f, "Open left landing door is blocked.");
                foreach (var renderer in ship.GetComponentsInChildren<Renderer>())
                    Require(!renderer.bounds.Contains(new Vector3(.3f, 3.9f, 1.8f)), "Upper deck blocks the lift shaft opening.");
                Capture("lift-bottom", false);
                ship.Interact();
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
                ship.Interact();
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
                ship.Interact();
                Require(ship.Terminal.IsOpen, "Terminal interaction regressed.");
                ship.Terminal.Close();
                // Simulate a passenger waiting upstairs: an absent platform must be callable.
                courier.position = new Vector3(-1.4f, 4.1f, 1.8f);
                ship.Motor.Deck = 1;
                Move(ship, Vector3.right, 2f);
                Require(courier.position.x < -1.10f, "Closed landing door admits the passenger into an empty shaft.");
                courier.position = new Vector3(-1.4f, 4.1f, 1.8f);
                ship.Interact();
                Require(lift.IsMoving && !lift.IsRiding, "Empty lift call failed.");
                SessionState.SetInt(StageKey, 6);
                ticks = 0;
            }
            else if (stage == 6)
            {
                Require(Mathf.Abs(courier.position.y-4.1f) < .001f, "Empty lift moved the waiting passenger.");
                if (lift.IsMoving) return;
                Require(lift.CurrentDeck == 1, "Called lift did not arrive upstairs.");
                Move(ship, Vector3.right, 1.5f);
                ship.Interact();
                Require(lift.IsRiding, "Passenger cannot board after calling the lift.");
                SessionState.SetInt(StageKey, 7);
                ticks = 0;
            }
            else if (stage == 7)
            {
                if (lift.IsMoving) return;
                Require(lift.CurrentDeck == 0 && Mathf.Abs(courier.position.y) < .001f, "Return ride after empty call failed.");
                var framing = UnityEngine.Object.FindFirstObjectByType<ShipCameraFraming>();
                framing.UpdateDeckView();
                var hangar = UnityEngine.Object.FindFirstObjectByType<ShipHangar>();
                Require(!hangar.Interior.gameObject.activeSelf, "Hangar is visible before descending.");
                foreach (var renderer in ship.GetComponentsInChildren<Renderer>())
                    Require(!renderer.bounds.Contains(new Vector3(.3f, -.25f, 1.8f)), "Living deck blocks the underground shaft.");
                SessionState.SetFloat("Orbita.CharacterCheck.HomeCameraY", Camera.main.transform.position.y);
                ship.Descend();
                Require(lift.IsRiding && lift.DestinationDeck == -1 && Mathf.Abs(courier.position.y) < .001f,
                    "R does not start a continuous descent to the hangar.");
                Capture("hangar-frame-000", false);
                SessionState.SetInt("Orbita.CharacterCheck.HangarFrame", 0);
                SessionState.SetInt(StageKey, 8);
                ticks = 0;
            }
            else if (stage == 8)
            {
                var framing = UnityEngine.Object.FindFirstObjectByType<ShipCameraFraming>();
                framing.UpdateDeckView();
                var hangar = UnityEngine.Object.FindFirstObjectByType<ShipHangar>();
                if (lift.IsMoving)
                {
                    CheckRiding(ship, courier, owl, lift);
                    if (courier.position.y < -.3f)
                        Require(hangar.Interior.gameObject.activeSelf, "Hangar does not reveal during descent.");
                    int bin = Mathf.FloorToInt(lift.Progress*20);
                    if (bin > SessionState.GetInt("Orbita.CharacterCheck.HangarFrame", -1))
                    {
                        Capture("hangar-frame-"+bin.ToString("D3"), false);
                        SessionState.SetInt("Orbita.CharacterCheck.HangarFrame", bin);
                    }
                    return;
                }
                Require(lift.CurrentDeck == -1 && Mathf.Abs(courier.position.y-ShipLayout.HangarHeight) < .001f,
                    "Hangar arrival height is incorrect.");
                Require(courier.parent != lift.Platform, "Hangar passenger was not released.");
                Capture("details-wardrobe", false);
                Require(Camera.main.transform.position.y < SessionState.GetFloat("Orbita.CharacterCheck.HomeCameraY", 0)-3,
                    "Camera did not follow to the underground room.");
                Require(hangar.Interior.gameObject.activeSelf && !FindInactive("Panoramic city window").gameObject.activeInHierarchy,
                    "Underground cutaway does not hide the upper home.");
                var hull = Find("Craft sculpted green hull").GetComponent<Renderer>();
                Require(hull.bounds.size.x > 13 && hull.bounds.max.y < -.5f, "Hangar craft has incorrect size or height.");
                foreach (var renderer in hangar.Interior.GetComponentsInChildren<Renderer>())
                    foreach (var material in renderer.sharedMaterials)
                        Require(material != null && material.shader.isSupported, "Hangar has an unsupported material.");
                Require(!ship.AtTerminal,
                    "Terminal can be used from the underground floor.");
                Capture("hangar-frame-021", false);
                Capture("hangar-overview", false);
                Capture("hangar-craft", false);
                Move(ship, Vector3.left, 2);
                Require(courier.position.x < -1.1f, "Cannot exit the lift into the hangar.");
                courier.position = new Vector3(-2, ShipLayout.HangarHeight, 3.6f);
                Move(ship, Vector3.right, 100);
                Require(Mathf.Abs(courier.position.x-17.2f) < .001f, "Hangar right bounds failed.");
                Move(ship, Vector3.left, 100);
                Require(Mathf.Abs(courier.position.x+17.2f) < .001f, "Hangar left bounds failed.");
                Move(ship, Vector3.forward, 100);
                Require(Mathf.Abs(courier.position.z-5.4f) < .001f, "Hangar front bounds failed.");
                courier.position = new Vector3(-1.4f, ShipLayout.HangarHeight, 1.8f);
                Move(ship, Vector3.right, 1.5f);
                ship.Interact();
                Require(lift.IsRiding && lift.DestinationDeck == 0, "E does not return from the hangar.");
                SessionState.SetInt(StageKey, 9);
                ticks = 0;
            }
            else if (stage == 9)
            {
                if (lift.IsMoving) { CheckRiding(ship, courier, owl, lift); return; }
                Require(lift.CurrentDeck == 0 && Mathf.Abs(courier.position.y) < .001f, "Hangar return failed.");
                UnityEngine.Object.FindFirstObjectByType<ShipCameraFraming>().UpdateDeckView();
                Require(FindInactive("Panoramic city window").gameObject.activeInHierarchy, "Home did not reappear on return.");
                Require(!UnityEngine.Object.FindFirstObjectByType<ShipHangar>().Interior.gameObject.activeSelf,
                    "Hangar remains visible after returning home.");
                courier.position = new Vector3(-1.4f, ShipLayout.HangarHeight, 1.8f);
                ship.Motor.Deck = -1;
                Move(ship, Vector3.right, 2);
                Require(courier.position.x < -1.1f, "Absent hangar platform leaves an open shaft.");
                ship.Interact();
                Require(lift.IsMoving && !lift.IsRiding, "Empty lift cannot be called from the hangar.");
                SessionState.SetInt(StageKey, 10);
                ticks = 0;
            }
            else if (stage == 10)
            {
                Require(Mathf.Abs(courier.position.y-ShipLayout.HangarHeight) < .001f, "Empty call moved hangar passenger.");
                if (lift.IsMoving) return;
                Require(lift.CurrentDeck == -1, "Empty lift failed to reach hangar.");
                Move(ship, Vector3.right, 1.5f);
                ship.Interact();
                Require(lift.IsRiding && lift.DestinationDeck == 0, "Cannot board a called hangar platform.");
                SessionState.SetInt(StageKey, 11);
                ticks = 0;
            }
            else if (stage == 11)
            {
                if (lift.IsMoving) return;
                Require(lift.CurrentDeck == 0, "Final hangar return failed.");
                File.WriteAllText("Captures/hangar-check.txt", "PASS: hidden initial hangar, unobstructed ground-floor shaft, continuous six-second descent with passenger and owl, reveal during descent, camera transition, craft scale/materials, exit and walkable bounds, terminal excluded, return/home restoration, closed absent landing, empty hangar call, boarding and return.\n");
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
        // The owl may be roaming when the ride starts; it must be aboard once the platform is under way.
        if (lift.Progress > .1f)
            Require(Mathf.Abs(owl.position.y-courier.position.y-1.05f) < .10f, "Owl left the moving cabin.");
        var position = courier.position;
        Move(ship, Vector3.right, 1f);
        Require(Vector3.Distance(position, courier.position) < .001f, "Movement was not locked during travel.");
        Require(!lift.TryUse(lift.CurrentDeck), "Repeated interaction restarted a moving lift.");
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LowLevel;

// Batch-only check of the systems around the courier: camera window, input bindings,
// furniture footprints, animated lift doors and build-safe shaders. Writes Captures/ship-systems-*.
[InitializeOnLoad]
public static class ShipSystemsCheck
{
    const string StageKey = "Orbita.ShipSystems.Stage";
    const string Report = "Captures/ship-systems-check.txt";
    static double waitUntil;
    static Keyboard keyboard;

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
        if (keyboard != null) { InputSystem.RemoveDevice(keyboard); keyboard = null; }
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

    // A close-up from a temporary pose; the game camera is restored afterwards.
    static void CaptureFrom(string name, Vector3 position, Vector3 target)
    {
        var camera = Camera.main.transform;
        var pose = (camera.position, camera.rotation);
        camera.position = position;
        camera.LookAt(target);
        Capture(name);
        camera.SetPositionAndRotation(pose.position, pose.rotation);
    }

    static CourierShip walkingShip;
    static float walkingSpeed;
    static bool walkerInstalled;

    // Steps the courier every game frame from the player loop, the way input would; editor ticks
    // are not game frames, so stepping from Tick would under-report the speed to the animator.
    static void Walk(CourierShip ship, float speed)
    {
        if (!walkerInstalled && ship != null)
        {
            walkerInstalled = true;
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            for (int i = 0; i < loop.subSystemList.Length; i++)
                if (loop.subSystemList[i].type == typeof(UnityEngine.PlayerLoop.Update))
                {
                    var systems = new List<PlayerLoopSystem>(loop.subSystemList[i].subSystemList)
                    {
                        new PlayerLoopSystem { type = typeof(ShipSystemsCheck), updateDelegate = WalkFrame },
                    };
                    loop.subSystemList[i].subSystemList = systems.ToArray();
                }
            PlayerLoop.SetPlayerLoop(loop);
        }
        walkingShip = ship;
        walkingSpeed = speed;
    }

    static void WalkFrame()
    {
        if (walkingShip != null && walkingShip.Ready && walkingSpeed > 0)
            walkingShip.Motor.Move(Vector3.right, walkingSpeed * Time.deltaTime);
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
                CheckScaleAndOrbit(ship, framing);
                CheckLivingDeckDetails(ship);
                var screen = OnScreen(camera, courier);
                Log("follow start: camera " + camera.transform.position + " courier screen " + screen);
                Require(Mathf.Abs(screen.x) <= .32f && Mathf.Abs(screen.y) <= .26f, "Courier starts outside the camera window.");
                Capture("ship-systems-follow");
                SessionState.SetString("Orbita.ShipSystems.Camera", JsonUtility.ToJson(camera.transform.position));
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard = InputSystem.AddDevice<Keyboard>();
                SessionState.SetString("Orbita.Audit.Start",JsonUtility.ToJson(courier.position));
                SessionState.SetString("Orbita.Audit.Right",JsonUtility.ToJson(Vector3.ProjectOnPlane(camera.transform.right,Vector3.up).normalized));
                InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.D));
                Wait(1, 3);
            }
            else if (stage == 3)
            {
                var start = JsonUtility.FromJson<Vector3>(SessionState.GetString("Orbita.ShipSystems.Camera", ""));
                float drift = Vector3.Distance(start, camera.transform.position);
                var walked = courier.position-JsonUtility.FromJson<Vector3>(SessionState.GetString("Orbita.Audit.Start",""));
                var right = JsonUtility.FromJson<Vector3>(SessionState.GetString("Orbita.Audit.Right",""));
                Require(Vector3.Dot(walked,right) > .2f,"Actual keyboard D does not move to camera-right.");
                InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
                Log("dead zone drift " + drift.ToString("F4"));
                Require(drift > .1f, "Third-person camera does not follow a small movement.");
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
                Require(Mathf.Abs(screen.x) < .08f, "Third-person camera lost the courier at the centre.");
                Capture("ship-systems-bridge");
                CheckObstacles(ship);
                InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.C));
                Wait(2.5, 5);
            }
            else if (stage == 5)
            {
                InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
                Require(!framing.Follow,"Keyboard C did not select overview mode.");
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
                InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.R));
                Wait(.3,14);
            }
            else if (stage == 14)
            {
                InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
                Require(ship.Lift.IsRiding && ship.Lift.DestinationDeck == ShipLayout.HangarDeck,
                    "R from the cabin does not go directly to the hangar.");
                Wait(8,12);
            }
            else if (stage == 12)
            {
                if (ship.Lift.IsMoving) return;
                Require(ship.Deck == ShipLayout.HangarDeck, "Cabin-to-hangar ride did not arrive.");
                framing.UpdateDeckView();
                Capture("audit-third-person-hangar");
                ship.Interact();
                Require(ship.Lift.IsRiding && ship.Lift.DestinationDeck == ShipLayout.LivingDeck,
                    "Cannot return home after the direct descent.");
                Wait(8,13);
            }
            else if (stage == 13)
            {
                if (ship.Lift.IsMoving) return;
                // Back on the living deck for the locomotion close-ups.
                courier.position = new Vector3(-9, 0, 2.2f);
                ship.Motor.Deck = ShipLayout.LivingDeck;
                SessionState.SetFloat("Orbita.ShipSystems.Clock", 0);
                SessionState.SetInt(StageKey, 8);
            }
            else if (stage == 8 || stage == 9)
            {
                bool running = stage == 9;
                float clock = SessionState.GetFloat("Orbita.ShipSystems.Clock", 0) + Time.deltaTime;
                SessionState.SetFloat("Orbita.ShipSystems.Clock", clock);
                Walk(ship, running ? CourierMotor.SprintSpeed : CourierMotor.WalkSpeed);
                var animator = courier.GetComponentInChildren<Animator>();
                int shot = SessionState.GetInt("Orbita.ShipSystems.Shot", 0);
                // Frames spread over one cycle after the blend has settled.
                if (clock > .8f + shot * .17f && shot < 6)
                {
                    float blend = animator.GetFloat("Speed");
                    Log((running ? "run" : "walk") + " frame " + shot + ": speed param " + blend.ToString("F2")
                        + ", playback " + animator.speed.ToString("F2"));
                    CaptureFrom((running ? "kristi-run-" : "kristi-walk-") + shot, courier.position + new Vector3(.4f, 1.2f, 3.6f),
                        courier.position + Vector3.up * .9f);
                    SessionState.SetInt("Orbita.ShipSystems.Shot", shot + 1);
                }
                if (shot < 6) return;
                Require(animator.GetFloat("Speed") > (running ? 3.6f : 2), "Locomotion speed does not reach the animator.");
                Walk(null, 0);
                SessionState.SetInt("Orbita.ShipSystems.Shot", 0);
                SessionState.SetFloat("Orbita.ShipSystems.Clock", 0);
                if (!running) { courier.position = new Vector3(-12, 0, 3.6f); SessionState.SetInt(StageKey, 9); return; }
                var owl = UnityEngine.Object.FindFirstObjectByType<OwlCompanion>();
                owl.SendToNest();
                Log("owl sent to nest: " + owl.Activity);
                Wait(1.5, 10);
            }
            else if (stage == 10)
            {
                var owl = UnityEngine.Object.FindFirstObjectByType<OwlCompanion>();
                if (!SessionState.GetBool("Orbita.ShipSystems.OwlFlight", false))
                {
                    CaptureFrom("owl-flight", owl.transform.position + new Vector3(1.6f, .5f, 3.2f), owl.transform.position);
                    SessionState.SetBool("Orbita.ShipSystems.OwlFlight", true);
                }
                if (!owl.Resting)
                {
                    Require(EditorApplication.timeSinceStartup - waitUntil < 40, "Owl never reached the aviary: " + owl.Activity);
                    return;
                }
                var aviary = UnityEngine.Object.FindFirstObjectByType<OwlAviary>();
                Wait(2, 11);
            }
            else if (stage == 11)
            {
                var aviary = UnityEngine.Object.FindFirstObjectByType<OwlAviary>();
                var owl = UnityEngine.Object.FindFirstObjectByType<OwlCompanion>();
                Log("owl resting at " + owl.transform.position + " perch " + aviary.Perch);
                Require(owl.Resting && Vector3.Distance(owl.transform.position, aviary.Perch) < .02f, "Owl rests off the perch.");
                CaptureFrom("owl-aviary", aviary.Perch + new Vector3(.6f, .6f, 3.4f), aviary.Perch + Vector3.down * .3f);
                CaptureFrom("owl-aviary-wide", aviary.Perch + new Vector3(2.5f, 1.4f, 6.5f), aviary.Perch + Vector3.down * .4f);
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

    static Bounds BoundsOf(string name)
    {
        var obj = GameObject.Find(name);
        Require(obj != null,"Missing scale reference: "+name);
        var renderers = obj.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    static void CheckScaleAndOrbit(CourierShip ship, ShipCameraFraming camera)
    {
        foreach (var pair in new[] {
            ("Terracotta sofa",ShipLayout.SofaSize),
            ("Cabin bed",new Vector3(ShipLayout.BedSize.z,ShipLayout.BedSize.y,ShipLayout.BedSize.x)),
            ("Wood bookshelf",ShipLayout.BookshelfSize) })
        {
            var bounds = BoundsOf(pair.Item1);
            Log(pair.Item1+" measured metres "+bounds.size);
            Require(Vector3.Distance(bounds.size,pair.Item2)<.035f,"Incorrect furniture proportions: "+pair.Item1);
        }
        var window = BoundsOf("Panoramic window frame");
        Require(Mathf.Abs(window.min.y)<.015f && Mathf.Abs(window.max.y-5)<.015f,"Window is not five metres high from the floor.");
        Require(GameObject.Find("Navigation console")==null && GameObject.Find("Navigation display")==null,
            "The rectangle with three white displays is still present.");
        Require(Mathf.Abs(BoundsOf("Stored EVA suit").size.y-ShipLayout.EvaSuitHeight)<.03f,"EVA suit has incorrect human scale.");
        Require(Mathf.Abs(BoundsOf("Order terminal").max.y-ShipLayout.TerminalHeight)<.03f,"Terminal is not human-sized.");
        var view = Camera.main;
        camera.UpdateDeckView();
        var target = ship.Motor.Courier.position+Vector3.up*ShipLayout.ThirdPersonFocusHeight;
        float range = Vector3.Distance(view.transform.position,target);
        Require(range>.3f && range<6.1f,"Camera is not in third person.");
        camera.ApplyLook(new Vector2(90,0),0); camera.UpdateDeckView();
        var right = Vector3.ProjectOnPlane(view.transform.right,Vector3.up).normalized;
        var forward = Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;
        Require(Vector3.Dot(ship.MovementDirection(Vector2.right),right)>.99f
            && Vector3.Dot(ship.MovementDirection(Vector2.up),forward)>.99f,"Orbit mirrors camera-relative movement.");
        Capture("audit-third-person-orbit");
        camera.ApplyLook(new Vector2(-90,0),0); camera.UpdateDeckView();
        Capture("audit-third-person-living");
        var saved = ship.Motor.Courier.position;
        ship.Motor.Courier.position = new Vector3(-12,0,-3.6f);
        camera.ApplyLook(new Vector2(180,0),0); camera.UpdateDeckView();
        Require(view.transform.position.z>-4.85f,"Third-person camera passed through the rear wall.");
        Require(Vector3.Distance(view.transform.position,ship.Motor.Courier.position+Vector3.up*ShipLayout.ThirdPersonFocusHeight)<3,
            "Camera did not shorten its boom at the wall.");
        camera.ApplyLook(new Vector2(-180,0),0);
        ship.Motor.Courier.position = saved; camera.UpdateDeckView();
        Log("scale and orbit: five-metre floor-level window, furniture XYZ, terminal, EVA suit, orbit controls, wall clearance PASS");
    }

    static void CheckLivingDeckDetails(CourierShip ship)
    {
        foreach (string name in new[] { "Galley refrigerator", "Galley breakfast stool", "Galley compact coffee maker",
            "Galley sink basin", "Galley fresh citrus", "Galley pantry jar", "Lounge reading lampshade",
            "Lounge coffee mug", "Lounge star atlas pages", "Lounge sofa cushion", "Sofa folded throw",
            "Kristi travel memory board", "Living parcel rack", "Stored courier parcel", "Living storage bench" })
            Require(GameObject.Find(name)!=null,"Living-deck detail missing: "+name);
        Require(Mathf.Abs(BoundsOf("Galley refrigerator").size.y-ShipLayout.GalleyFridgeSize.y)<.03f,
            "Refrigerator is not human-sized.");
        var courier = ship.Motor.Courier;
        var saved = courier.position;
        try
        {
            // Follow actual walkable corridors; each segment uses the same motor as Update.
            // Keyboard input itself is covered immediately afterwards by the existing D check.
            courier.position = ShipLayout.CourierStart;
            WalkTo(ship,new Vector3(-4,0,-.6f));
            WalkTo(ship,ShipLayout.TerminalPosition);
            Require(ship.AtTerminal,"Dressing blocks access to terminal.");
            WalkTo(ship,new Vector3(-1.4f,0,-.6f));
            WalkTo(ship,new Vector3(-1.4f,0,1.8f));
            WalkTo(ship,new Vector3(.1f,0,1.8f));
            Require(ship.AtLift,"Dressing blocks lift boarding.");
            courier.position = ShipLayout.CourierStart;
            WalkTo(ship,new Vector3(-7.8f,0,.6f));
            WalkTo(ship,new Vector3(-7.8f,0,-.6f));
            WalkTo(ship,new Vector3(-10,0,-.6f));
            courier.position = ShipLayout.CourierStart;
            WalkTo(ship,new Vector3(-1.4f,0,-.6f));
            WalkTo(ship,new Vector3(6,0,-.6f));
            BlockedFurniture(ship,"Galley refrigerator",ShipLayout.GalleyFridgePosition);
            BlockedFurniture(ship,"Galley breakfast stool",ShipLayout.GalleyStoolPosition);
            BlockedFurniture(ship,"Living parcel rack",ShipLayout.LivingParcelShelfPosition);
            BlockedFurniture(ship,"Living storage bench",ShipLayout.LivingStorageBenchPosition);
            BlockedFurniture(ship,"Lounge lamp side table",ShipLayout.LoungeSideTablePosition);
            CaptureFrom("living-deck-lounge",new Vector3(-6.4f,2.45f,5),new Vector3(-10,1.05f,-1));
            CaptureFrom("living-deck-galley",new Vector3(-.9f,1.95f,-.2f),new Vector3(-3.3f,1.12f,-4.1f));
            CaptureFrom("living-deck-parcels",new Vector3(-9.2f,1.8f,-.6f),new Vector3(-12.1f,1.0f,-3.4f));
            CaptureFrom("living-deck-storage",new Vector3(-7,1.7f,3.8f),new Vector3(-11.5f,.65f,3.1f));
            Log("living deck: household objects, human-sized fridge, obstacles, sofa/terminal/lift/bridge routes PASS (motor path; keyboard checked separately)");
        }
        finally { courier.position=saved; }
    }

    static void WalkTo(CourierShip ship,Vector3 destination)
    {
        var courier=ship.Motor.Courier;
        var start=courier.position;
        var delta=destination-start;
        int steps=Mathf.CeilToInt(delta.magnitude/.05f);
        for(int i=0;i<steps;i++) ship.Motor.Move(delta.normalized,delta.magnitude/steps);
        Require(Vector3.Distance(courier.position,destination)<.06f,
            "Dressing blocks corridor from "+start+" to "+destination+" at "+courier.position);
    }

    static void BlockedFurniture(CourierShip ship,string name,Vector3 position)
    {
        var bounds=BoundsOf(name);
        ship.Motor.Courier.position=new Vector3(position.x,0,bounds.max.z+.7f);
        ship.Motor.Move(Vector3.back,2);
        Require(ship.Motor.Courier.position.z>=bounds.max.z+CourierMotor.Radius-.05f,
            "Courier can walk through "+name);
    }

    static void CheckObstacles(CourierShip ship)
    {
        var courier = ship.Motor.Courier;
        courier.position = new Vector3(-5, 0, .6f);
        ship.Motor.Move(Vector3.left, 6);
        Log("walk into coffee table stops at x " + courier.position.x.ToString("F2"));
        var tableBounds = GameObject.Find("Lounge coffee table").GetComponentInChildren<Renderer>().bounds;
        Require(courier.position.x >= tableBounds.max.x+CourierMotor.Radius-.03f,
            "Courier walked through the lounge coffee table.");
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

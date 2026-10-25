using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Runs the real input path through CourierShip.Update, including preview, cancellation,
// saving, movement lock and return to walking. Does not modify the player's saved outfit.
[InitializeOnLoad]
public static class WardrobeIntegrationCheck
{
    const string State = "Orbita.WardrobeCheck.Stage", Preference = "Orbita.Kristi.Outfit";
    static Keyboard keyboard;
    static double until;
    static bool hadPreference;
    static int savedPreference;
    static Vector3 stationary;
    static Material skin;
    static RuntimeAnimatorController controller;

    static WardrobeIntegrationCheck()
    {
        if (SessionState.GetInt(State,0)>0) EditorApplication.update += Tick;
    }
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use a separate batch editor.");
        Directory.CreateDirectory("Captures");
        File.WriteAllText("Captures/wardrobe-check.txt", "");
        EditorSceneManager.OpenScene("Assets/Scenes/Ship.unity");
        SessionState.SetInt(State,1);
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Log(string message)
    {
        Debug.Log(message);
        File.AppendAllText("Captures/wardrobe-check.txt",message+"\n");
    }
    static void Next(int stage, double delay=.25)
    {
        SessionState.SetInt(State,stage);
        until = EditorApplication.timeSinceStartup + delay;
    }
    static void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
    static void Capture(string name, Vector3 position, Vector3 target)
    {
        var camera = Camera.main;
        var previousPosition = camera.transform.position;
        var previousRotation = camera.transform.rotation;
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var texture = new RenderTexture(1400,1000,24);
        var image = new Texture2D(1400,1000,TextureFormat.RGB24,false);
        try
        {
            camera.transform.position = position;
            camera.transform.LookAt(target);
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0,0,1400,1000),0,0);
            image.Apply();
            File.WriteAllBytes("Captures/"+name+".png",image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            camera.transform.SetPositionAndRotation(previousPosition,previousRotation);
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup<until) return;
        var ship = UnityEngine.Object.FindFirstObjectByType<CourierShip>();
        if (ship==null || !ship.Ready) return;
        var outfits = ship.Outfits;
        var courier = ship.Motor.Courier;
        try
        {
            switch (SessionState.GetInt(State,0))
            {
                case 1:
                    hadPreference = PlayerPrefs.HasKey(Preference);
                    savedPreference = PlayerPrefs.GetInt(Preference,0);
                    InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                    InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    keyboard = InputSystem.AddDevice<Keyboard>();
                    skin = SkinRenderer(courier).sharedMaterial;
                    controller = courier.GetComponentInChildren<Animator>().runtimeAnimatorController;
                    Require(outfits.ClothingParts>=3,"Missing skinned clothing parts.");
                    Require(!ship.AtWardrobe,"Cabin wardrobe usable from downstairs.");
                    ship.Motor.Deck = ShipLayout.CabinDeck;
                    courier.position = new Vector3(-1.1f,ShipLayout.DeckHeight,-2.65f);
                    ship.Motor.Move(Vector3.left,2.2f);
                    Require(ship.AtWardrobe,"Wardrobe approach is blocked or not in reach.");
                    stationary = courier.position;
                    Keys(Key.E); Next(2); break;
                case 2:
                    Require(outfits.IsOpen,"Actual keyboard E did not open wardrobe.");
                    outfits.Preview(0); outfits.Confirm(); outfits.Open();
                    Keys(); Next(3); break;
                case 3:
                    Keys(Key.D,Key.W); Next(4,.6); break;
                case 4:
                    Require(outfits.Selected==1 && outfits.BagVisible,"Keyboard selection did not preview courier outfit/backpack.");
                    Require(Vector3.Distance(courier.position,stationary)<.001f,"Walking is not blocked while changing clothes.");
                    Require(!outfits.Preview(99),"Invalid outfit accepted.");
                    Capture("wardrobe-courier",courier.position+new Vector3(1,1.3f,3.2f),courier.position+Vector3.up*.95f);
                    Capture("wardrobe-backpack",courier.position+new Vector3(2.4f,1.3f,-.3f),courier.position+Vector3.up*1.1f);
                    Keys(Key.Escape); Next(5); break;
                case 5:
                    Require(!outfits.IsOpen && outfits.Equipped==0 && !outfits.BagVisible,"Cancel did not restore original outfit.");
                    Keys(); Next(6); break;
                case 6: Keys(Key.E); Next(7); break;
                case 7:
                    Require(outfits.IsOpen,"Wardrobe cannot reopen.");
                    Keys(Key.LeftArrow); Next(8); break;
                case 8:
                    Require(outfits.Selected==2,"Left arrow does not cycle to home outfit.");
                    Capture("wardrobe-home",courier.position+new Vector3(1,1.3f,3.2f),courier.position+Vector3.up*.95f);
                    Capture("wardrobe-cabin",ShipLayout.ClothingWardrobePosition+new Vector3(2.5f,1.65f,4),ShipLayout.ClothingWardrobePosition+Vector3.up*1.05f);
                    ScreenCapture.CaptureScreenshot("Captures/wardrobe-ui.png");
                    Keys(); Next(9,.5); break;
                case 9:
                    Keys(Key.E); Next(12); break;
                case 12:
                    Require(!outfits.IsOpen && outfits.Equipped==2 && PlayerPrefs.GetInt(Preference,-1)==2,"Confirm did not equip and save home outfit.");
                    Require(courier.GetComponentInChildren<Animator>().runtimeAnimatorController==controller,"Dressing replaced animator.");
                    Require(SkinRenderer(courier).sharedMaterial==skin,"Skin material changed.");
                    Keys(); Next(10); break;
                case 10:
                    Keys(Key.S); Next(11,.4); break;
                case 11:
                    Require(Vector3.Distance(courier.position,stationary)>.15f,"Closing wardrobe did not restore movement.");
                    Keys();
                    Log("WARDROBE_CHECK_PASS: cabin approach; real E/D/W/Escape/LeftArrow; preview; cancel; confirm; persistence; movement lock/release; unchanged animator and skin.");
                    Finish(0); break;
            }
        }
        catch(Exception error) { Log("WARDROBE_CHECK_FAIL: "+error); Finish(1); }
    }
    static void Finish(int code)
    {
        if (keyboard!=null) InputSystem.RemoveDevice(keyboard);
        if (hadPreference) PlayerPrefs.SetInt(Preference,savedPreference); else PlayerPrefs.DeleteKey(Preference);
        PlayerPrefs.Save();
        SessionState.SetInt(State,0);
        EditorApplication.update -= Tick;
        EditorApplication.Exit(code);
    }
    static SkinnedMeshRenderer SkinRenderer(Transform courier)
        => Array.Find(courier.GetComponentsInChildren<SkinnedMeshRenderer>(), renderer => renderer.name == "face");
}

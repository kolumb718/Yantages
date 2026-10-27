using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Real Play Mode and real CourierInput actions. UI captures use the actual overlay camera.
[InitializeOnLoad]
public static class GalaxyIntegrationCheck
{
    const string State="Orbita.GalaxyCheck.Stage";
    static Keyboard keyboard;
    static Mouse mouse;
    static double until,deadline;
    static Vector3 standing;
    static GalaxyIntegrationCheck(){if(SessionState.GetInt(State,0)>0)EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use a separate batch editor.");
        Directory.CreateDirectory("Captures");File.WriteAllText("Captures/galaxy-check.txt","");
        EditorSceneManager.OpenScene("Assets/Scenes/Ship.unity");
        SessionState.SetInt(State,1);EditorApplication.update+=Tick;
        deadline=EditorApplication.timeSinceStartup+160;
        EditorApplication.isPlaying=true;
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Log(string message){Debug.Log(message);File.AppendAllText("Captures/galaxy-check.txt",message+"\n");}
    static void Next(int state,double delay=.3){SessionState.SetInt(State,state);until=EditorApplication.timeSinceStartup+delay;}
    static void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
    static void Pointer(Vector2 at,bool down)
    {
        var state=new MouseState{position=at};
        if(down)state=state.WithButton(MouseButton.Left);
        InputSystem.QueueStateEvent(mouse,state);
    }
    static void Capture(GalaxyMapView ui,string name)
    {
        ui.Refresh();
        var camera=ui.ViewCamera;
        var target=new RenderTexture(1600,1000,24);
        var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        var previous=camera.targetTexture;var active=RenderTexture.active;
        float aspect=camera.aspect,size=camera.orthographicSize;
        try
        {
            camera.targetTexture=target;camera.aspect=1.6f;camera.orthographicSize=5.2f;camera.Render();
            RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();
            File.WriteAllBytes("Captures/"+name+".png",image.EncodeToPNG());
            Require(ui.LayoutIssues().Length==0,string.Join("\n",ui.LayoutIssues()));
        }
        finally
        {
            camera.targetTexture=previous;camera.aspect=aspect;camera.orthographicSize=size;
            RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
        }
    }
    static void Tick()
    {
        int stage=SessionState.GetInt(State,0);
        if(EditorApplication.timeSinceStartup<until)return;
        if(stage==20 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {Next(21,2);EditorApplication.isPlaying=true;return;}
        if(!EditorApplication.isPlaying)return;
        var ship=UnityEngine.Object.FindFirstObjectByType<CourierShip>();
        if(ship==null || !ship.Ready)return;
        try
        {
            if(deadline>0)Require(EditorApplication.timeSinceStartup<deadline,"Galaxy check timed out.");
            var atlas=ship.Galaxy;var ui=ship.GalaxyView;var courier=ship.Motor.Courier;
            switch(stage)
            {
                case 1:
                    Require(atlas!=null && atlas.Catalog.Count==6,"Galaxy worlds did not load.");
                    Require(!atlas.IsOpen && !ui.Visible,"Galaxy opens at startup without interaction.");
                    Require(DeliveryOrder.NeptuneSoup.Reward==240 && DeliveryOrder.NeptuneSoup.Client.Contains("Мира")
                        && DeliveryOrder.NeptuneSoup.Cargo.Contains("лунными грибами"),"First order canon changed.");
                    Require(!ship.AtGalaxy,"Hologram usable from the starting point.");
                    InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                    InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
                    courier.position=new Vector3(ShipLayout.HologramCentre.x,0,.9f);
                    ship.Motor.Deck=ShipLayout.LivingDeck; standing=courier.position;
                    Require(ship.AtGalaxy && !GalaxyAtlas.InReach(courier.position,ShipLayout.CabinDeck),"Hologram reach/deck gate invalid.");
                    Keys(Key.E);Next(2);break;
                case 2:
                    Require(atlas.IsOpen && ui.Visible && !atlas.ShowingPlanet,"Keyboard E did not open the galaxy.");
                    Capture(ui,"galaxy-overview");
                    Keys(Key.W);Next(3,.6);break;
                case 3:
                    Require(Vector3.Distance(courier.position,standing)<.001f,"W walks while atlas is open.");
                    Keys();
                    int lumena=atlas.Catalog.IndexOf("lumena");
                    Pointer(ui.ButtonScreenCentre(1+lumena),true);Next(4);break;
                case 4:
                    Require(atlas.ShowingPlanet && atlas.Planet.id=="lumena","Real pointer click did not open Lumena.");
                    Pointer(Vector2.zero,false);Keys(Key.RightArrow);Next(5);break;
                case 5:
                    Require(atlas.Tab==1,"Arrow navigation did not open History.");
                    Capture(ui,"galaxy-lumena-history");Keys();
                    for(int i=0;i<atlas.Catalog.Count;i++)
                    {
                        Require(atlas.OpenPlanet(i),"Planet cannot be opened.");
                        for(int tab=0;tab<6;tab++)
                        {
                            Require(atlas.SelectTab(tab) && atlas.Planet.Section(tab).Length>100,"Missing lore section.");
                            Capture(ui,"galaxy-layout-check");
                        }
                        atlas.SelectTab(0);Capture(ui,"galaxy-world-"+atlas.Planet.id);
                    }
                    Require(!atlas.OpenPlanet(99) && !atlas.SelectTab(-1),"Invalid navigation index accepted.");
                    atlas.OpenPlanet(atlas.Catalog.IndexOf("lumena"));atlas.SelectTab(5);
                    Capture(ui,"galaxy-food-order");Keys(Key.E);Next(6);break;
                case 6:
                    Require(ship.Terminal.Accepted && ship.Terminal.Order.PlanetId=="lumena","Map acceptance did not reach the terminal.");
                    Require(!atlas.AcceptOrder(),"Order accepted twice.");
                    atlas.OpenPlanet(atlas.Catalog.IndexOf("neptune"));atlas.SelectTab(5);
                    Require(!atlas.AcceptOrder() && ship.Terminal.Order.PlanetId=="lumena","Browsing/accepting overwrites active delivery.");
                    Capture(ui,"galaxy-active-order");Keys(Key.Escape);Next(7);break;
                case 7:
                    Require(atlas.IsOpen && !atlas.ShowingPlanet,"Escape did not return to the galaxy.");
                    Keys();Next(8);break;
                case 8:Keys(Key.Escape);Next(9);break;
                case 9:
                    Require(!atlas.IsOpen && !ui.Visible,"Second Escape did not close the atlas camera.");
                    Keys();
                    var screen=Camera.main.WorldToScreenPoint(ShipLayout.HologramCentre);
                    Require(screen.z>0,"Hologram is behind the camera.");
                    Pointer(screen,true);Next(10);break;
                case 10:
                    Require(atlas.IsOpen,"Clicking the actual hologram did not open the atlas.");
                    Pointer(Vector2.zero,false);Keys(Key.Escape);Next(11);break;
                case 11:
                    Require(!atlas.IsOpen,"Reopened map did not close.");Keys();Next(12);break;
                case 12: Keys(Key.S);Next(13,.4);break;
                case 13:
                    Require(Vector3.Distance(courier.position,standing)>.15f,"Atlas close did not release walking.");
                    Keys();courier.position=ShipLayout.TerminalPosition;Next(14);break;
                case 14:Keys(Key.E);Next(15);break;
                case 15:
                    Require(ship.Terminal.IsOpen && ship.Terminal.Order.PlanetId=="lumena","Terminal lost atlas order.");
                    Log("Galaxy: E and world click; real UI click; arrow tabs; all six worlds/six sections rendered; canonical Neptune order; one active food delivery; terminal synchronization; Escape/back; walking lock/release PASS.");
                    InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);keyboard=null;mouse=null;
                    SessionState.SetInt(State,20);EditorApplication.isPlaying=false;break;
                case 21:
                    Require(!ship.Galaxy.IsOpen && !ship.GalaxyView.Visible,"Atlas overlay survived Play Mode restart.");
                    Require(UnityEngine.Object.FindObjectsByType<GalaxyMapView>(FindObjectsSortMode.None).Length==1,"Duplicate atlas after restart.");
                    Log("GALAXY_CHECK_PASS: Play Mode restart clean.");Finish(0);break;
            }
        }
        catch(Exception error){Log("GALAXY_CHECK_FAIL: "+error);Finish(1);}
    }
    static void Finish(int code)
    {
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
        if(mouse!=null)InputSystem.RemoveDevice(mouse);
        SessionState.SetInt(State,0);EditorApplication.update-=Tick;EditorApplication.Exit(code);
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Pixel evidence for depth-tested lettering and the flat, composited window at close angles.
[InitializeOnLoad]
public static class PresentationRegressionCheck
{
    const string State="Orbita.PresentationCheck";
    static int ticks;
    static PresentationRegressionCheck(){if(SessionState.GetBool(State,false))EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use a batch project copy.");
        EditorSceneManager.OpenScene("Assets/Scenes/Ship.unity");
        Directory.CreateDirectory("Captures");File.WriteAllText("Captures/presentation-check.txt","");
        SessionState.SetBool(State,true);EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Log(string message){Debug.Log(message);File.AppendAllText("Captures/presentation-check.txt",message+"\n");}
    static Texture2D Render(Camera camera,int width,int height,string name=null)
    {
        var target=new RenderTexture(width,height,24);var previous=camera.targetTexture;var active=RenderTexture.active;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            if(name!=null)File.WriteAllBytes("Captures/"+name+".png",image.EncodeToPNG());
            return image;
        }
        finally{camera.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(target);}
    }
    static int Bright(Texture2D image)
    {
        int count=0;foreach(var pixel in image.GetPixels32())if(pixel.r>180 && pixel.g>180 && pixel.b>180)count++;
        UnityEngine.Object.DestroyImmediate(image);return count;
    }
    static void CheckText()
    {
        var kit=new SceneKit();var group=new GameObject("World text depth probe");group.transform.position=new Vector3(400,400,0);
        var camera=new GameObject("Lettering probe camera").AddComponent<Camera>();
        camera.transform.position=group.transform.position+Vector3.forward*3;
        camera.transform.LookAt(group.transform.position);camera.orthographic=true;camera.orthographicSize=.8f;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var label=kit.Caption("Depth test lettering","ЖИЛОЙ ОТСЕК",Vector3.zero,.24f,Color.white,group.transform);
        var wall=kit.Box("Opaque lettering occluder",new Vector3(0,0,.3f),new Vector3(3,1.5f,.12f),kit.Unlit("Probe wall",new Color(.1f,.1f,.1f)),group.transform);
        try
        {
            wall.SetActive(false);
            int visible=Bright(Render(camera,480,300,"lettering-visible"));
            Require(visible>100,"Front lettering is not readable with the world-text shader.");
            wall.SetActive(true);
            int hidden=Bright(Render(camera,480,300,"lettering-occluded"));
            Require(hidden<visible*.02f,"Lettering still shows through an opaque wall.");
            wall.SetActive(false);camera.transform.position=group.transform.position-Vector3.forward*3;camera.transform.LookAt(group.transform.position);
            int back=Bright(Render(camera,480,300,"lettering-backface"));
            Require(back<visible*.02f,"Mirrored backface lettering remains visible.");
            Log("Lettering pixel check: front="+visible+", behind wall="+hidden+", reverse="+back+" PASS");
        }
        finally{UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(group);kit.Release();}
    }
    static void CheckWindow(CourierShip ship)
    {
        var window=UnityEngine.Object.FindFirstObjectByType<ShipWindowView>();var camera=Camera.main;
        Require(window.Display!=null && window.Display.sharedMaterial.mainTexture is RenderTexture,"Window is not a composited surface.");
        Require(window.Display.bounds.size.z<.0001f,"Window display has physical layer depth.");
        Require((camera.cullingMask&(1<<ShipLayout.WindowSceneryLayer))==0,"Player camera still renders source city geometry.");
        foreach(string name in new[]{"Distant skyline","Midcity skyline","Foreground docks","Stepped skyline landmark","City antennas and traffic lights"})
            Require(GameObject.Find(name).layer==ShipLayout.WindowSceneryLayer,"Exposed city source layer: "+name);
        var saved=(camera.transform.position,camera.transform.rotation,camera.fieldOfView);
        var position=ship.Motor.Courier.position;int deck=ship.Motor.Deck;
        try
        {
            camera.transform.position=new Vector3(8,1.8f,-3.5f);camera.transform.LookAt(new Vector3(8,1.8f,ShipLayout.WindowSurfaceZ));camera.fieldOfView=58;
            window.UpdateView(camera);UnityEngine.Object.DestroyImmediate(Render(camera,1600,1000,"window-close-flat"));
            camera.transform.position=new Vector3(11,1.8f,-3.6f);camera.transform.LookAt(new Vector3(8.8f,1.9f,ShipLayout.WindowSurfaceZ));
            window.UpdateView(camera);UnityEngine.Object.DestroyImmediate(Render(camera,1600,1000,"window-close-oblique"));
            ship.Motor.Deck=ShipLayout.LivingDeck;ship.Motor.Courier.position=new Vector3(8,0,-3.4f);
            ship.Motor.Move(Vector3.back,2);
            Require(ship.Motor.Courier.position.z>=ShipLayout.WindowBarrier.yMax+CourierMotor.Radius-.01f,
                "Courier can walk through the window plane.");
            Log("Window: single opaque surface, source geometry hidden from player, near/oblique captures, physical approach limit PASS");
        }
        finally{camera.transform.SetPositionAndRotation(saved.Item1,saved.Item2);camera.fieldOfView=saved.Item3;ship.Motor.Courier.position=position;ship.Motor.Deck=deck;window.UpdateView(camera);}
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying || ++ticks<30)return;
        var ship=UnityEngine.Object.FindFirstObjectByType<CourierShip>();if(ship==null || !ship.Ready)return;
        EditorApplication.update-=Tick;SessionState.SetBool(State,false);
        try{CheckText();CheckWindow(ship);Log("PRESENTATION_REGRESSION_PASS");EditorApplication.Exit(0);}
        catch(Exception e){Log("PRESENTATION_REGRESSION_FAIL: "+e);EditorApplication.Exit(1);}
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.LowLevel;

// Measures the real animated bones while CourierShip receives walking input each game frame.
[InitializeOnLoad]
public static class WalkingPresentationCheck
{
    const string State="Orbita.WalkingPresentation";
    static Keyboard keyboard;
    static int frame=-1,samples,contactSamples,shot;
    static float started,maxLateral,plantTravel,plantTime,minZ=999,maxZ=-999,minY=999,maxY=-999;
    static float minLean=999,maxLean=-999,minSupportFlex=999;
    static Vector3 previousLeft,previousRight,start;
    static bool leftPlanted,rightPlanted;
    static bool sampling;
    static bool Inject(ref PlayerLoopSystem parent)
    {
        if(parent.subSystemList==null)return false;
        for(int i=0;i<parent.subSystemList.Length;i++)
        {
            var child=parent.subSystemList[i];
            if(child.type==typeof(UnityEngine.PlayerLoop.PreLateUpdate.ScriptRunBehaviourLateUpdate))
            {
                var list=new System.Collections.Generic.List<PlayerLoopSystem>(parent.subSystemList);
                list.Insert(i+1,new PlayerLoopSystem{type=typeof(WalkingPresentationCheck),updateDelegate=SampleFinalPose});
                parent.subSystemList=list.ToArray();return true;
            }
            if(Inject(ref child)){parent.subSystemList[i]=child;return true;}
        }
        return false;
    }
    static WalkingPresentationCheck(){if(SessionState.GetInt(State,0)>0)EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use a batch project copy.");
        AnimatedCharacterSetup.Prepare();
        Directory.CreateDirectory("Captures");File.WriteAllText("Captures/walking-presentation.txt","");
        SessionState.SetInt(State,1);EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Log(string text){Debug.Log(text);File.AppendAllText("Captures/walking-presentation.txt",text+"\n");}
    static void Capture(Camera camera,string name,Vector3 position,Vector3 target)
    {
        var pose=(camera.transform.position,camera.transform.rotation,camera.fieldOfView);
        var render=new RenderTexture(800,1000,24);var texture=new Texture2D(800,1000,TextureFormat.RGB24,false);
        var previous=camera.targetTexture;var active=RenderTexture.active;
        try
        {
            camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=36;
            camera.targetTexture=render;camera.Render();RenderTexture.active=render;
            texture.ReadPixels(new Rect(0,0,800,1000),0,0);texture.Apply();
            File.WriteAllBytes("Captures/"+name+".png",texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=previous;RenderTexture.active=active;
            camera.transform.SetPositionAndRotation(pose.Item1,pose.Item2);camera.fieldOfView=pose.Item3;
            UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(render);
        }
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying || frame==Time.frameCount)return;
        frame=Time.frameCount;
        var ship=UnityEngine.Object.FindFirstObjectByType<CourierShip>();if(ship==null || !ship.Ready)return;
        try
        {
            var courier=ship.Motor.Courier;
            if(SessionState.GetInt(State,0)==1)
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard=InputSystem.AddDevice<Keyboard>();
                // Exercise a normal game timestep, rather than hiding contact errors at batch frame rates.
                Time.captureDeltaTime=1f/60;
                var loop=PlayerLoop.GetCurrentPlayerLoop();Require(Inject(ref loop),"No post-LateUpdate sampling point.");PlayerLoop.SetPlayerLoop(loop);
                courier.position=new Vector3(-6,0,2.4f);start=courier.position;
                // The probe teleports to a clear corridor; settle the follow camera before W,
                // otherwise its old position turns this straight-walk test into a curved start.
                UnityEngine.Object.FindFirstObjectByType<ShipCameraFraming>().UpdateDeckView();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
                started=Time.time;SessionState.SetInt(State,2);return;
            }
            float elapsed=Time.time-started;
            var characters=ship.GetComponent<CourierCharacters>();
            Require(characters.Legs!=null,"No anatomical leg placement.");
            if(elapsed<2.5f)return;
            Require(samples>15 && contactSamples>8,"Insufficient actual walking frames/contacts.");
            Require(Vector3.Distance(courier.position,start)>2,"Real keyboard walking did not move courier.");
            Require(maxLateral<.065f,"Feet still swing laterally: "+maxLateral);
            Require(maxZ-minZ>.3f && maxY-minY>.06f,"Legs do not stride forward and lift.");
            Log("Torso lean range (forward positive): "+minLean.ToString("F2")+" .. "+maxLean.ToString("F2")+" degrees.");
            Require(minLean>=0 && maxLean<7,"Walking torso leans backwards or bends too far forward.");
            Require(minSupportFlex<18,"Support leg remains crouched throughout the stride.");
            Log("Minimum support knee flexion: "+minSupportFlex.ToString("F2")+" degrees.");
            Require(plantTime>0 && plantTravel/plantTime<.18f,"Stance feet still skate: "+plantTravel/Mathf.Max(.001f,plantTime)+" m/s");
            Log("WALKING_PRESENTATION_PASS: "+samples+" frames, lateral error "+maxLateral.ToString("F4")
                +" m, fore/aft span "+(maxZ-minZ).ToString("F3")+" m, swing lift "+(maxY-minY).ToString("F3")
                +" m, stance drift "+(plantTravel/plantTime).ToString("F3")+" m/s.");
            Finish(0);
        }
        catch(Exception error){Log("WALKING_PRESENTATION_FAIL: "+error);Finish(1);}
    }
    static void SampleFinalPose()
    {
        if(sampling || SessionState.GetInt(State,0)!=2)return;
        var ship=UnityEngine.Object.FindFirstObjectByType<CourierShip>();if(ship==null || !ship.Ready)return;
        var courier=ship.Motor.Courier;var characters=ship.GetComponent<CourierCharacters>();
        float elapsed=Time.time-started;
        try
        {
            sampling=true;
            if(elapsed>.55f && characters.Legs.Weight>.95f)
            {
                var animator=courier.GetComponentInChildren<Animator>();
                var hips=animator.GetBoneTransform(HumanBodyBones.Hips);
                var neck=animator.GetBoneTransform(HumanBodyBones.Neck);
                var torso=courier.InverseTransformVector(neck.position-hips.position);
                float lean=Mathf.Atan2(torso.z,torso.y)*Mathf.Rad2Deg;
                minLean=Mathf.Min(minLean,lean);maxLean=Mathf.Max(maxLean,lean);
                var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                var right=animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
                if(characters.Legs.LeftPlanted)
                {
                    var hip=animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
                    var knee=animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position;
                    minSupportFlex=Mathf.Min(minSupportFlex,180-Vector3.Angle(hip-knee,left-knee));
                }
                var localLeft=courier.InverseTransformPoint(left);var localRight=courier.InverseTransformPoint(right);
                maxLateral=Mathf.Max(maxLateral,Mathf.Abs(localLeft.x+.105f),Mathf.Abs(localRight.x-.105f));
                minZ=Mathf.Min(minZ,localLeft.z);maxZ=Mathf.Max(maxZ,localLeft.z);
                minY=Mathf.Min(minY,localLeft.y);maxY=Mathf.Max(maxY,localLeft.y);
                if(samples>0 && leftPlanted && characters.Legs.LeftPlanted){plantTravel+=Vector3.Distance(previousLeft,left);plantTime+=Time.deltaTime;contactSamples++;}
                if(samples>0 && rightPlanted && characters.Legs.RightPlanted){plantTravel+=Vector3.Distance(previousRight,right);plantTime+=Time.deltaTime;contactSamples++;}
                previousLeft=left;previousRight=right;leftPlanted=characters.Legs.LeftPlanted;rightPlanted=characters.Legs.RightPlanted;samples++;
                if(shot<6 && elapsed>.65f+shot*.14f)
                {
                    var target=courier.position+Vector3.up*.95f;
                    Capture(Camera.main,"walking-front-"+shot.ToString("D2"),courier.position+courier.forward*3.4f+Vector3.up*1.05f,target);
                    Capture(Camera.main,"walking-side-"+shot.ToString("D2"),courier.position+courier.right*3.4f+Vector3.up*1.05f,target);
                    shot++;
                }
            }
        }
        catch(Exception error){Log("WALKING_PRESENTATION_FAIL: "+error);Finish(1);}
        finally{sampling=false;}
    }
    static void Finish(int code)
    {
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
        Time.captureDeltaTime=0;
        SessionState.SetInt(State,0);EditorApplication.update-=Tick;EditorApplication.Exit(code);
    }
}

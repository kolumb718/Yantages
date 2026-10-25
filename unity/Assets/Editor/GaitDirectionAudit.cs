using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
// Samples the actual humanoid controller and its muscle curves independently.
// A planted foot must travel towards local -Z during forward locomotion.
public static class GaitDirectionAudit
{
    const string Root = "Assets/Art/Characters/KristiWorker/";
    const string Stage = "Orbita.GaitAudit.Stage";
    static int frames;
    static GaitDirectionAudit()
    {
        if(SessionState.GetBool(Stage,false)) EditorApplication.update += Tick;
    }
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run in a separate batch editor.");
        AnimatedCharacterSetup.Prepare();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Stage,true);
        EditorApplication.update += Tick;
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying || ++frames < 20) return;
        EditorApplication.update -= Tick;
        SessionState.SetBool(Stage,false);
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root+"KristiWorker.prefab");
            if(prefab == null) throw new Exception("No KristiWorker prefab");
            var actor=UnityEngine.Object.Instantiate(prefab);
            var animator=actor.GetComponentInChildren<Animator>();
            animator.applyRootMotion=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Update(0);
            Directory.CreateDirectory("Captures");
            File.WriteAllText("Captures/gait-direction.txt", "");
            foreach(var stem in new[]{"Walk","Run"})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"KristiWorker_"+stem+".anim");
                SampleController(animator,clip,stem);
                SampleMuscles(animator,clip,stem);
            }
            UnityEngine.Object.DestroyImmediate(actor);
            Debug.Log("GAIT_DIRECTION_AUDIT_PASS");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void SampleController(Animator animator,AnimationClip clip,string label)
    {
        animator.enabled=true;
        animator.SetFloat("Speed",label=="Walk"?CourierCharacters.WalkCycleSpeed:CourierCharacters.RunCycleSpeed);
        Sample(animator,clip,"CONTROLLER_"+label, t=>
        {
            animator.Play("Locomotion",0,t/clip.length);
            animator.Update(.0001f);
        });
    }
    static void SampleMuscles(Animator animator,AnimationClip clip,string label)
    {
        animator.enabled=false;
        var handler=new HumanPoseHandler(animator.avatar,animator.transform);
        var pose=new HumanPose();handler.GetHumanPose(ref pose);
        var baseline=pose.muscles.ToArray();
        var curves=AnimationUtility.GetCurveBindings(clip)
            .Where(b=>b.type==typeof(Animator))
            .ToDictionary(b=>b.propertyName,b=>AnimationUtility.GetEditorCurve(clip,b));
        Sample(animator,clip,"MUSCLES_"+label,t=>
        {
            pose.muscles=baseline.ToArray();
            for(int i=0;i<HumanTrait.MuscleCount;i++)
                if(curves.TryGetValue(HumanTrait.MuscleName[i],out var curve))pose.muscles[i]=curve.Evaluate(t);
            handler.SetHumanPose(ref pose);
        });
        handler.Dispose();animator.enabled=true;
    }
    static void Sample(Animator animator,AnimationClip clip,string label,Action<float> evaluate)
    {
        if(clip==null)throw new Exception("Missing gait clip");
        const int count=128;
        var left=new Vector3[count];var right=new Vector3[count];
        var ankle=animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var toe=animator.GetBoneTransform(HumanBodyBones.LeftToes);
        for(int i=0;i<count;i++)
        {
            evaluate(clip.length*i/count);
            left[i]=animator.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position);
            right[i]=animator.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightFoot).position);
        }
        string report=label+" humanMotion="+clip.humanMotion+" duration="+clip.length.ToString("F3")+" forwardToeZ="+animator.transform.InverseTransformVector(toe.position-ankle.position).z.ToString("F4")+" "+Measure(left,clip.length)+" right:"+Measure(right,clip.length);
        File.AppendAllText("Captures/gait-direction.txt",report+"\n");Debug.Log("GAIT_DIRECTION "+report);
        if(report.Contains("UNRESOLVED") || report.Contains("BACKWARD"))
            throw new Exception("Gait must produce a measurable forward stance in both feet: "+label);
    }
    static string Measure(Vector3[] p,float duration)
    {
        var sorted=p.Select(v=>v.y).OrderBy(v=>v).ToArray();
        float threshold=sorted[(int)(p.Length*.35f)];
        float dz=0;int n=0;
        for(int i=0;i<p.Length;i++)
        {
            int j=(i+1)%p.Length;
            if(p[i].y<=threshold&&p[j].y<=threshold){dz+=(p[j].z-p[i].z)*p.Length/duration;n++;}
        }
        float speed=n>0?dz/n:0;
        return "footHeight="+sorted[0].ToString("F4")+".."+sorted[sorted.Length-1].ToString("F4")+" stanceDz="+speed.ToString("F4")+" stanceSamples="+n+" direction="+(speed<-.001f?"FORWARD":speed>.001f?"BACKWARD":"UNRESOLVED");
    }
}

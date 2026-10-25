using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Builds looping walk and run cycles from humanoid muscle curves. The idle clip supplies the
// resting posture (hands, fingers, head), so the cycle fits whichever humanoid model is imported.
public static class GaitClips
{
    const int Samples = 32;

    sealed class Gait
    {
        public float Length, Hip, Knee, KneeStance, Foot, Arm, Elbow, Lean, Twist, Bob;
    }

    static readonly Gait Walk = new Gait
    {
        Length = 1.0f, Hip = .42f, Knee = .55f, KneeStance = .06f, Foot = .22f,
        Arm = .32f, Elbow = .12f, Lean = .02f, Twist = .05f, Bob = .012f,
    };

    static readonly Gait Run = new Gait
    {
        Length = .66f, Hip = .62f, Knee = .95f, KneeStance = .18f, Foot = .32f,
        Arm = .45f, Elbow = .55f, Lean = .10f, Twist = .08f, Bob = .028f,
    };

    public static AnimationClip Build(AnimationClip idle, string path, bool running)
    {
        var gait = running ? Run : Walk;
        var baseline = new Dictionary<string, float>();
        foreach (var binding in AnimationUtility.GetCurveBindings(idle))
        {
            var curve = AnimationUtility.GetEditorCurve(idle, binding);
            if (curve == null || curve.length == 0) continue;
            float sum = 0;
            for (int i = 0; i < Samples; i++) sum += curve.Evaluate(curve[curve.length - 1].time * i / Samples);
            baseline[binding.propertyName] = sum / Samples;
        }
        if (!baseline.ContainsKey("Left Upper Leg Front-Back"))
            throw new InvalidOperationException("Idle clip has no humanoid muscle curves.");

        var clip = new AnimationClip { frameRate = 30, name = System.IO.Path.GetFileNameWithoutExtension(path) };
        var curves = new Dictionary<string, Func<float, float>>();
        // Hold the idle posture for everything the gait does not drive.
        foreach (var pair in baseline)
        {
            float value = pair.Value;
            curves[pair.Key] = _ => value;
        }
        float Base(string muscle) => baseline.TryGetValue(muscle, out var value) ? value : 0;

        foreach (var side in new[] { "Left", "Right" })
        {
            // Phase 0: this leg passes under the body moving forward; the other leg is half a cycle later.
            float offset = side == "Left" ? 0 : .5f;
            float Wave(float t) => (t / gait.Length + offset) * Mathf.PI * 2;
            string leg = side + " Upper Leg Front-Back", knee = side + " Lower Leg Stretch", foot = side + " Foot Up-Down";
            string arm = side + " Arm Front-Back", forearm = side + " Forearm Stretch";
            float hip0 = Base(leg), knee0 = Base(knee), foot0 = Base(foot), arm0 = Base(arm), forearm0 = Base(forearm);
            curves[leg] = t => hip0 + gait.Hip * Mathf.Sin(Wave(t));
            // The knee folds while the leg swings forward (cos > 0) and softens on landing.
            curves[knee] = t =>
            {
                float swing = Mathf.Pow(Mathf.Max(0, Mathf.Cos(Wave(t) - .35f)), 1.4f);
                return knee0 - gait.Knee * swing - gait.KneeStance;
            };
            curves[foot] = t => foot0 + gait.Foot * Mathf.Sin(Wave(t) - .6f);
            // Each arm swings against the leg on its own side.
            curves[arm] = t => arm0 - gait.Arm * Mathf.Sin(Wave(t));
            curves[forearm] = t => forearm0 - gait.Elbow;
        }
        float Stride(float t) => t / gait.Length * Mathf.PI * 2;
        float spine0 = Base("Spine Front-Back"), twist0 = Base("Spine Twist Left-Right"), chest0 = Base("Chest Twist Left-Right");
        float rootY = Base("RootT.y");
        curves["Spine Front-Back"] = t => spine0 + gait.Lean;
        curves["Spine Twist Left-Right"] = t => twist0 + gait.Twist * Mathf.Sin(Stride(t));
        curves["Chest Twist Left-Right"] = t => chest0 - gait.Twist * Mathf.Sin(Stride(t));
        // The body is highest as the legs pass each other, twice per cycle.
        if (baseline.ContainsKey("RootT.y")) curves["RootT.y"] = t => rootY + gait.Bob * Mathf.Cos(Stride(t) * 2);

        foreach (var pair in curves)
        {
            var keys = new Keyframe[Samples + 1];
            for (int i = 0; i <= Samples; i++)
            {
                float time = gait.Length * i / Samples;
                keys[i] = new Keyframe(time, pair.Value(time));
            }
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++) curve.SmoothTangents(i, 0);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), pair.Key), curve);
        }
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.loopBlend = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }
}

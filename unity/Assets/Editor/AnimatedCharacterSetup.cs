using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Imports the rigged characters supplied by the user: Kristi in the worker outfit (humanoid,
// idle clip plus generated walk/run) and the Watcher owl (generic rig, flight loop).
// Builds materials, animator controllers and prefabs, then assigns them to the Ship scene.
public static class AnimatedCharacterSetup
{
    const string Root = "Assets/Art/Characters/";
    const string Kristi = Root + "KristiWorker/";
    const string Owl = Root + "WatcherOwl/";
    public const float KristiHeight = 1.72f;
    public const float OwlHeight = .72f;

    [MenuItem("Orbita/Prepare animated characters")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before preparing characters.");
        AssetDatabase.Refresh();
        var courier = BuildKristi();
        var owl = BuildOwl();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Ship.unity", OpenSceneMode.Single);
        var characters = UnityEngine.Object.FindFirstObjectByType<CourierCharacters>();
        if (characters == null) throw new InvalidOperationException("Ship scene has no CourierCharacters.");
        var settings = new SerializedObject(characters);
        settings.FindProperty("courierPrefab").objectReferenceValue = courier;
        settings.FindProperty("owlPrefab").objectReferenceValue = owl;
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Orbita: animated Kristi and Watcher owl assigned to Ship scene.");
    }

    // ---------- Kristi ----------

    static GameObject BuildKristi()
    {
        string model = Kristi + "KristiWorker.fbx";
        var clothes = TexturedMaterial(Kristi, "KristiWorker_Clothes", "Textures/KristiWorker_Clothes.png", .12f);
        var skin = TexturedMaterial(Kristi, "KristiWorker_Skin", "Textures/KristiWorker_Skin.png", .28f);
        var hair = TexturedMaterial(Kristi, "KristiWorker_Hair", "Textures/KristiWorker_Hair.png", .34f);

        var importer = Importer(model);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
        // Mixamo-style bones carry a "girlBone_" prefix; map them explicitly instead of trusting heuristics.
        var description = importer.humanDescription;
        description.human = HumanMap("girlBone_");
        importer.humanDescription = description;
        foreach (var material in AssetDatabase.LoadAllAssetsAtPath(model).OfType<Material>())
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material.name),
                material.name.StartsWith("set4") ? clothes : material.name.StartsWith("09") ? hair : skin);
        var clips = importer.defaultClipAnimations;
        var idleSource = clips.FirstOrDefault(c => c.takeName.ToLowerInvariant().Contains("idle"))
            ?? throw new InvalidOperationException("Kristi model has no idle take.");
        idleSource.name = "Idle";
        idleSource.loopTime = true;
        idleSource.loopPose = true;
        idleSource.lockRootRotation = idleSource.lockRootHeightY = idleSource.lockRootPositionXZ = true;
        idleSource.keepOriginalOrientation = idleSource.keepOriginalPositionY = idleSource.keepOriginalPositionXZ = true;
        importer.clipAnimations = new[] { idleSource };
        importer.SaveAndReimport();

        var assets = AssetDatabase.LoadAllAssetsAtPath(model);
        var avatar = assets.OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
            throw new InvalidOperationException("Kristi humanoid avatar is invalid.");
        var idle = assets.OfType<AnimationClip>().First(c => c.name == "Idle");
        var walk = GaitClips.Build(idle, Kristi + "KristiWorker_Walk.anim", false);
        var run = GaitClips.Build(idle, Kristi + "KristiWorker_Run.anim", true);

        var controller = Controller(Kristi + "KristiWorker.controller", "Speed", out var machine);
        var locomotion = new BlendTree { name = "Locomotion", blendParameter = "Speed", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(locomotion, controller);
        locomotion.AddChild(idle, 0);
        locomotion.AddChild(walk, CourierCharacters.WalkCycleSpeed);
        locomotion.AddChild(run, CourierCharacters.RunCycleSpeed);
        machine.AddState("Locomotion").motion = locomotion;

        return BuildPrefab(Kristi + "KristiWorker.prefab", model, KristiHeight, avatar, controller, null);
    }

    static HumanBone[] HumanMap(string prefix)
    {
        var map = new Dictionary<string, string>
        {
            { "Hips", "Hips" }, { "Spine", "Spine" }, { "Chest", "Spine1" }, { "UpperChest", "Spine2" },
            { "Neck", "Neck" }, { "Head", "Head" },
        };
        foreach (var side in new[] { "Left", "Right" })
        {
            map[side + "Shoulder"] = side + "Shoulder";
            map[side + "UpperArm"] = side + "Arm";
            map[side + "LowerArm"] = side + "ForeArm";
            map[side + "Hand"] = side + "Hand";
            map[side + "UpperLeg"] = side + "UpLeg";
            map[side + "LowerLeg"] = side + "Leg";
            map[side + "Foot"] = side + "Foot";
            map[side + "Toes"] = side + "ToeBase";
            foreach (var finger in new[] { "Thumb", "Index", "Middle", "Ring", "Little" })
            {
                string source = finger == "Little" ? "Pinky" : finger;
                string[] joints = { "Proximal", "Intermediate", "Distal" };
                for (int i = 0; i < 3; i++)
                    map[side + " " + finger + " " + joints[i]] = side + "Hand" + source + (i + 1);
            }
        }
        return map.Select(pair => new HumanBone
        {
            humanName = pair.Key,
            boneName = prefix + pair.Value,
            limit = new HumanLimit { useDefaultValues = true },
        }).ToArray();
    }

    // ---------- Watcher owl ----------

    static GameObject BuildOwl()
    {
        string model = Owl + "WatcherOwl.fbx";
        var body = TexturedMaterial(Owl, "WatcherOwl_Body", "Textures/WatcherOwl_Body.png", .25f, cutout: true);
        var deco = TexturedMaterial(Owl, "WatcherOwl_Deco", "Textures/WatcherOwl_Deco.png", .2f, fade: true);

        var importer = Importer(model);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
        foreach (var material in AssetDatabase.LoadAllAssetsAtPath(model).OfType<Material>())
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material.name),
                material.name.StartsWith("TT_checker") ? deco : body);
        var clips = importer.defaultClipAnimations;
        if (clips.Length == 0) throw new InvalidOperationException("Owl model has no animation.");
        clips[0].name = "Fly";
        clips[0].loopTime = true;
        importer.clipAnimations = new[] { clips[0] };
        importer.SaveAndReimport();

        var assets = AssetDatabase.LoadAllAssetsAtPath(model);
        var fly = assets.OfType<AnimationClip>().First(c => c.name == "Fly");
        var controller = Controller(Owl + "WatcherOwl.controller", null, out var machine);
        machine.AddState("Fly").motion = fly;
        // Duplicate shells and the inverted-hull outline only render correctly in the source tool.
        bool Hidden(string name) => name.Contains(".001") || name.Contains("outline");
        return BuildPrefab(Owl + "WatcherOwl.prefab", model, OwlHeight, assets.OfType<Avatar>().FirstOrDefault(),
            controller, Hidden);
    }

    // ---------- shared ----------

    static ModelImporter Importer(string path)
        => AssetImporter.GetAtPath(path) as ModelImporter ?? throw new InvalidOperationException("Missing model: " + path);

    static Material TexturedMaterial(string folder, string name, string texture, float smoothness,
        bool cutout = false, bool fade = false)
    {
        var textureImporter = AssetImporter.GetAtPath(folder + texture) as TextureImporter
            ?? throw new InvalidOperationException("Missing texture: " + folder + texture);
        textureImporter.sRGBTexture = true;
        textureImporter.alphaIsTransparency = cutout || fade;
        textureImporter.maxTextureSize = 2048;
        textureImporter.SaveAndReimport();
        if (!AssetDatabase.IsValidFolder(folder + "Materials")) AssetDatabase.CreateFolder(folder.TrimEnd('/'), "Materials");
        string path = folder + "Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = Shader.Find("Standard");
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + texture);
        material.color = Color.white;
        material.SetFloat("_Glossiness", smoothness);
        material.SetFloat("_Metallic", 0);
        SetBlend(material, cutout ? 1 : fade ? 2 : 0);
        EditorUtility.SetDirty(material);
        return material;
    }

    // Standard shader modes: 0 opaque, 1 cutout, 2 fade.
    static void SetBlend(Material material, int mode)
    {
        material.SetFloat("_Mode", mode);
        material.SetFloat("_Cutoff", .5f);
        material.SetInt("_SrcBlend", (int)(mode == 2 ? BlendMode.SrcAlpha : BlendMode.One));
        material.SetInt("_DstBlend", (int)(mode == 2 ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
        material.SetInt("_ZWrite", mode == 2 ? 0 : 1);
        Keyword(material, "_ALPHATEST_ON", mode == 1);
        Keyword(material, "_ALPHABLEND_ON", mode == 2);
        Keyword(material, "_ALPHAPREMULTIPLY_ON", false);
        material.SetOverrideTag("RenderType", mode == 1 ? "TransparentCutout" : mode == 2 ? "Transparent" : "");
        material.renderQueue = mode == 1 ? (int)RenderQueue.AlphaTest : mode == 2 ? (int)RenderQueue.Transparent : -1;
    }

    static void Keyword(Material material, string keyword, bool enabled)
    {
        if (enabled) material.EnableKeyword(keyword);
        else material.DisableKeyword(keyword);
    }

    static AnimatorController Controller(string path, string floatParameter, out AnimatorStateMachine machine)
    {
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        if (floatParameter != null) controller.AddParameter(floatParameter, AnimatorControllerParameterType.Float);
        machine = controller.layers[0].stateMachine;
        return controller;
    }

    static GameObject BuildPrefab(string path, string model, float height, Avatar avatar,
        RuntimeAnimatorController controller, Func<string, bool> hidden)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(model);
        var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
        try
        {
            var visual = UnityEngine.Object.Instantiate(source, root.transform, false);
            visual.name = "Visual";
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                if (hidden != null && hidden(renderer.name)) UnityEngine.Object.DestroyImmediate(renderer.gameObject);
            var animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("Model has no renderers: " + model);
            foreach (var skinned in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                // Bounds follow the animated pose, not the bind pose (correct culling and measurements).
                skinned.updateWhenOffscreen = true;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float scale = height / bounds.size.y;
            visual.transform.localScale *= scale;
            // Feet (or talons) at y=0; the body centre lies on the movement pivot.
            visual.transform.localPosition = -new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}

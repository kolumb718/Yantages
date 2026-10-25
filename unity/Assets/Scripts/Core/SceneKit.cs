using System.Collections.Generic;
using UnityEngine;

// Shared factory for runtime-built scenery: owned materials and meshes, collider-free
// primitives and wall lettering. Each builder keeps one kit and releases it on destroy.
public sealed class SceneKit
{
    readonly List<Object> owned = new List<Object>();
    readonly Dictionary<Color, Material> flat = new Dictionary<Color, Material>();
    Font font;

    public Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    public T Own<T>(T asset) where T : Object
    {
        owned.Add(asset);
        return asset;
    }

    public Material Surface(string name, Color color, float smoothness = .22f, float metallic = 0)
    {
        var material = Own(new Material(ShipShaders.Standard) { name = name, color = color });
        material.SetFloat("_Glossiness", smoothness);
        material.SetFloat("_Metallic", metallic);
        return material;
    }

    public Material Emissive(string name, Color color, float emission, float smoothness = .22f, float metallic = 0)
    {
        var material = Surface(name, color, smoothness, metallic);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * emission);
        return material;
    }

    public Material Unlit(string name, Color color) => Own(new Material(ShipShaders.Unlit) { name = name, color = color });

    public Material Custom(string name, Shader shader) => Own(new Material(shader) { name = name });

    // Many structural boxes share a colour: one material each keeps draw calls batchable.
    public Material Flat(string hex, float emission = 0)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var color);
        var key = new Color(color.r, color.g, color.b, emission);
        if (flat.TryGetValue(key, out var material)) return material;
        material = emission > 0 ? Emissive("Hull #" + hex + " glow", color, emission) : Surface("Hull #" + hex, color);
        flat.Add(key, material);
        return material;
    }

    public GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale,
        Material material, Transform parent)
    {
        var item = GameObject.CreatePrimitive(type);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        // Walking uses explicit bounds and obstacles, not physics.
        Object.Destroy(item.GetComponent<Collider>());
        return item;
    }

    public GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        => Shape(name, PrimitiveType.Cube, position, scale, material, parent);

    public GameObject Tube(string name, Vector3 a, Vector3 b, float radius, Material material, Transform parent)
    {
        var delta = b - a;
        var tube = Shape(name, PrimitiveType.Cylinder, (a + b) * .5f,
            new Vector3(radius * 2, delta.magnitude * .5f, radius * 2), material, parent);
        tube.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        return tube;
    }

    // Lettering faces the cutaway camera; size is the character height in metres.
    public TextMesh Caption(string name, string text, Vector3 position, float size, Color color, Transform parent)
    {
        var item = new GameObject(name);
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localRotation = Quaternion.Euler(0, 180, 0);
        var label = item.AddComponent<TextMesh>();
        label.font = Font;
        label.fontSize = 64;
        // Font size controls atlas detail; compensate so the requested size stays in metres.
        label.characterSize = size * 10 / label.fontSize;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = color;
        label.text = text;
        item.GetComponent<MeshRenderer>().sharedMaterial = Font.material;
        return label;
    }

    public Light PointLight(string name, Vector3 position, Color color, float intensity, float range,
        Transform parent, LightShadows shadows = LightShadows.None)
    {
        var light = new GameObject(name).AddComponent<Light>();
        light.transform.SetParent(parent, false);
        light.transform.localPosition = position;
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = shadows;
        return light;
    }

    public void Release()
    {
        foreach (var asset in owned) if (asset != null) Object.Destroy(asset);
        owned.Clear();
        flat.Clear();
    }
}

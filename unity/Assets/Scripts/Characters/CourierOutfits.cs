using System.Collections.Generic;
using UnityEngine;

// Reuses the skinned clothing, UVs and animator. Only clothing materials and the courier bag
// change: skin, hair, the imported prefab and shared project materials remain untouched.
public sealed class CourierOutfits : MonoBehaviour
{
    const string Preference = "Orbita.Kristi.Outfit";
    public static readonly string[] Names = { "Рабочий комплект", "Курьер · терракота", "Дом · шалфей" };
    public static readonly string[] Descriptions = {
        "Песочная рубашка, шорты и рабочие ботинки.",
        "Терракотовая рубашка, графитовые шорты и рюкзак для доставки.",
        "Шалфейная рубашка, тёплые коричневые шорты и мягкие светлые тона."
    };
    public static readonly Color[] Swatches = { new Color(.78f,.67f,.48f), new Color(.88f,.39f,.21f), new Color(.48f,.72f,.57f) };
    readonly SceneKit kit = new SceneKit();
    readonly List<(Renderer renderer, Material[][] choices)> clothing = new List<(Renderer, Material[][])>();
    Transform bag;
    bool axisHeld;
    int original;
    public int Selected { get; private set; }
    public int Equipped { get; private set; }
    public bool IsOpen { get; private set; }
    public int ClothingParts => clothing.Count;
    public bool BagVisible => bag != null && bag.gameObject.activeSelf;

    public void Initialize(Transform courier)
    {
        // Cache material variants once. A renderer may contain several material slots.
        var cache = new Dictionary<(Material, int, int), Material>();
        foreach (var renderer in courier.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (!renderer.name.StartsWith("clothingSet_04_")) continue;
            int region = renderer.name.Contains("body") ? 0 : renderer.name.Contains("pants") ? 1 : 2;
            var variants = new Material[Names.Length][];
            variants[0] = renderer.sharedMaterials;
            for (int outfit = 1; outfit < Names.Length; outfit++)
            {
                variants[outfit] = new Material[variants[0].Length];
                for (int slot = 0; slot < variants[0].Length; slot++)
                {
                    var source = variants[0][slot];
                    if (source == null) continue;
                    var key = (source, outfit, region);
                    if (!cache.TryGetValue(key, out var material))
                    {
                        material = kit.Own(new Material(source) { name = Names[outfit] + " / " + region });
                        material.color = Tint(outfit, region);
                        cache.Add(key, material);
                    }
                    variants[outfit][slot] = material;
                }
            }
            clothing.Add((renderer, variants));
        }
        if (clothing.Count == 0) throw new MissingReferenceException("Kristi has no supported skinned clothing.");
        var animator = courier.GetComponentInChildren<Animator>();
        var chest = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Chest) : null;
        if (chest != null)
        {
            bag = new GameObject("Courier delivery backpack").transform;
            bag.SetPositionAndRotation(courier.TransformPoint(ShipLayout.CourierBagPosition), courier.rotation);
            bag.SetParent(chest, true);
            var scale = chest.lossyScale;
            bag.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
            var fabric = kit.Surface("Courier bag terracotta", new Color(.58f,.22f,.12f));
            var trim = kit.Surface("Courier bag sand", new Color(.88f,.72f,.48f));
            var dark = kit.Surface("Courier bag straps", new Color(.21f,.27f,.25f));
            kit.Box("Delivery backpack", Vector3.zero, ShipLayout.CourierBagSize, fabric, bag);
            kit.Box("Backpack flap", new Vector3(0,.10f,-.075f), new Vector3(.34f,.13f,.045f), trim, bag);
            kit.Box("Backpack parcel badge", new Vector3(0,-.045f,-.073f), new Vector3(.11f,.075f,.018f), trim, bag);
            foreach (float side in new[] { -1f, 1f })
                kit.Box("Backpack shoulder strap", new Vector3(side*.115f,.035f,.075f), new Vector3(.045f,.42f,.035f), dark, bag);
        }
        Equipped = Mathf.Clamp(PlayerPrefs.GetInt(Preference, 0), 0, Names.Length - 1);
        Selected = Equipped;
        Apply(Equipped);
    }

    static Color Tint(int outfit, int region) => outfit == 1
        ? (region == 0 ? new Color(1,.52f,.30f) : region == 1 ? new Color(.43f,.56f,.58f) : new Color(.48f,.35f,.27f))
        : (region == 0 ? new Color(.65f,.96f,.76f) : region == 1 ? new Color(.82f,.64f,.48f) : new Color(.88f,.83f,.72f));

    void Apply(int index)
    {
        foreach (var part in clothing) part.renderer.sharedMaterials = part.choices[index];
        if (bag != null) bag.gameObject.SetActive(index == 1);
    }

    public void Open()
    {
        if (IsOpen) return;
        original = Equipped;
        Selected = Equipped;
        axisHeld = false;
        IsOpen = true;
    }

    public bool Preview(int index)
    {
        if (!IsOpen || index < 0 || index >= Names.Length) return false;
        Selected = index;
        Apply(index);
        return true;
    }

    // A stick/key press advances once; release before advancing again.
    public void Navigate(float axis)
    {
        if (Mathf.Abs(axis) < .4f) { axisHeld = false; return; }
        if (!IsOpen || axisHeld) return;
        axisHeld = true;
        Preview((Selected + (axis > 0 ? 1 : Names.Length - 1)) % Names.Length);
    }

    public void Confirm()
    {
        if (!IsOpen) return;
        Equipped = Selected;
        PlayerPrefs.SetInt(Preference, Equipped);
        PlayerPrefs.Save();
        IsOpen = false;
    }

    public void Close()
    {
        if (!IsOpen) return;
        Selected = Equipped = original;
        Apply(original);
        IsOpen = false;
    }

    void OnDestroy() => kit.Release();
}

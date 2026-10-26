using UnityEngine;

// An open equipment locker: static, original low-poly dressing with reusable materials.
public sealed class ShipWardrobe : MonoBehaviour
{
    readonly SceneKit kit = new SceneKit();
    Transform root;
    Material frame, timber, recess, cream, rust, rubber, brass, visor, cyan, red;
    bool built;
    public Transform Root => root;

    public void Build(Transform ship)
    {
        if (built) return;
        built = true;
        root = new GameObject("Equipment wardrobe").transform;
        root.SetParent(ship, false);
        root.localPosition = ShipLayout.EvaWardrobePosition;
        frame = Material("Wardrobe graphite", new Color(.20f, .25f, .28f));
        timber = Material("Wardrobe honey trim", new Color(.48f, .30f, .16f));
        recess = Material("Wardrobe dark interior", new Color(.10f, .135f, .15f));
        cream = Material("EVA oatmeal fabric", new Color(.83f, .79f, .67f));
        rust = Material("EVA terracotta reinforcement", new Color(.60f, .25f, .12f));
        rubber = Material("EVA rubber joints", new Color(.15f, .18f, .19f));
        brass = Material("EVA brass fittings", new Color(.65f, .44f, .19f), .45f, .45f);
        visor = Material("EVA dark visor", new Color(.025f, .095f, .14f), .75f, .5f);
        cyan = Material("Wardrobe mint indicators", new Color(.32f, .89f, .79f), .25f, 0, true);
        red = Material("Wardrobe medical red", new Color(.68f, .14f, .09f));
        BuildCabinet();
        BuildSuit();
        BuildLoot();
        // The locker remains large enough for a 1.8 m wearer plus helmet and boots.
        root.localScale = Vector3.one * (ShipLayout.WardrobeHeight / 2.65f);
        var suit = root.Find("Stored EVA suit");
        var renderers = suit.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        suit.localScale *= ShipLayout.EvaSuitHeight / bounds.size.y;
    }

    Material Material(string name, Color color, float gloss=.22f, float metallic=0, bool glow=false)
        => glow ? kit.Emissive(name, color, .35f, gloss, metallic) : kit.Surface(name, color, gloss, metallic);

    GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale,
        Material material, Transform parent=null) => kit.Shape(name, type, position, scale, material, parent != null ? parent : root);

    void Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent=null)
    {
        Shape(name, PrimitiveType.Cube, position, scale, material, parent);
    }

    void Ellipsoid(string name, Vector3 position, Vector3 size, Material material, Transform parent)
    {
        Shape(name, PrimitiveType.Sphere, position, size, material, parent);
    }

    void Tube(string name, Vector3 a, Vector3 b, float radius, Material material, Transform parent=null)
        => kit.Tube(name, a, b, radius, material, parent != null ? parent : root);

    void Caption(string name, string text, Vector3 position, float size)
        => kit.Caption(name, text, position, size, new Color(.92f, .81f, .61f), root);

    void BuildCabinet()
    {
        Box("Wardrobe rear panel", new Vector3(0, 1.34f, -.35f), new Vector3(2.50f, 2.60f, .10f), recess);
        foreach (float x in new[] { -1.20f, 1.20f })
        {
            Box("Wardrobe side upright", new Vector3(x, 1.34f, 0), new Vector3(.10f, 2.60f, .80f), frame);
            Box("Wardrobe warm edge", new Vector3(x, 1.34f, .405f), new Vector3(.055f, 2.60f, .035f), timber);
        }
        Box("Wardrobe header", new Vector3(0, 2.55f, .04f), new Vector3(2.50f, .20f, .84f), frame);
        Box("Wardrobe base plinth", new Vector3(0, .08f, .03f), new Vector3(2.50f, .16f, .84f), frame);
        Box("Wardrobe compartment divider", new Vector3(.26f, 1.37f, 0), new Vector3(.075f, 2.23f, .73f), timber);
        for (int i=0; i<2; i++)
        {
            float x = -.65f+i*1.23f;
            Box("Wardrobe lower drawer", new Vector3(x, .24f, .38f), new Vector3(1.13f, .25f, .10f), timber);
            Tube("Drawer brass handle", new Vector3(x-.14f, .27f, .45f), new Vector3(x+.14f, .27f, .45f), .018f, brass);
        }
        foreach (float y in new[] { .43f, 1.02f, 1.66f, 2.32f })
        {
            Box("Wardrobe gear shelf", new Vector3(.73f, y, .02f), new Vector3(.89f, .065f, .72f), timber);
            Box("Wardrobe shelf lip", new Vector3(.73f, y+.035f, .38f), new Vector3(.89f, .045f, .04f), frame);
        }
        Tube("Suit hanging rail", new Vector3(-1.10f, 2.43f, .04f), new Vector3(.16f, 2.43f, .04f), .024f, brass);
        Caption("Wardrobe main label", "СКАФАНДР / EVA-07", new Vector3(-.43f, 2.56f, .47f), .135f);
        Caption("Wardrobe gear label", "СНАРЯЖЕНИЕ", new Vector3(.75f, 2.56f, .47f), .10f);
        Box("Locker mint status light", new Vector3(-1.20f, 2.32f, .43f), new Vector3(.06f, .05f, .025f), cyan);
        Box("Suit bay warm top strip", new Vector3(-.47f, 2.43f, .32f), new Vector3(1.16f, .025f, .035f), brass);
    }

    void BuildSuit()
    {
        var suit = new GameObject("Stored EVA suit").transform;
        suit.SetParent(root, false);
        suit.localPosition = new Vector3(-.46f, 0, .04f);
        // Large soft forms are joined by dark flexible seals and separate guards.
        Ellipsoid("EVA torso", new Vector3(0, 1.58f, .04f), new Vector3(.64f, .72f, .40f), cream, suit);
        Ellipsoid("EVA hip section", new Vector3(0, 1.22f, .035f), new Vector3(.55f, .35f, .37f), cream, suit);
        Box("EVA waist belt", new Vector3(0, 1.31f, .06f), new Vector3(.57f, .075f, .38f), rubber, suit);
        Box("EVA belt buckle", new Vector3(0, 1.31f, .265f), new Vector3(.13f, .09f, .035f), brass, suit);
        foreach (float side in new[] { -1f, 1f })
        {
            Ellipsoid("EVA padded shoulder", new Vector3(side*.32f, 1.81f, .04f), new Vector3(.26f, .29f, .34f), rust, suit);
            Tube("EVA upper sleeve", new Vector3(side*.34f, 1.77f, .05f), new Vector3(side*.43f, 1.48f, .10f), .105f, cream, suit);
            Ellipsoid("EVA elbow seal", new Vector3(side*.43f, 1.47f, .10f), new Vector3(.20f, .15f, .21f), rubber, suit);
            Tube("EVA lower sleeve", new Vector3(side*.43f, 1.44f, .10f), new Vector3(side*.44f, 1.24f, .16f), .095f, cream, suit);
            Tube("EVA wrist cuff", new Vector3(side*.44f, 1.26f, .16f), new Vector3(side*.44f, 1.20f, .18f), .105f, rust, suit);
            Ellipsoid("EVA hanging glove", new Vector3(side*.44f, 1.11f, .18f), new Vector3(.18f, .22f, .21f), rubber, suit);
            Ellipsoid("EVA thumb", new Vector3(side*.36f, 1.14f, .22f), new Vector3(.08f, .13f, .10f), rubber, suit);
            Tube("EVA trouser leg", new Vector3(side*.17f, 1.17f, .04f), new Vector3(side*.17f, .65f, .05f), .13f, cream, suit);
            Box("EVA knee guard", new Vector3(side*.17f, .87f, .175f), new Vector3(.20f, .18f, .065f), rust, suit);
            Tube("EVA ankle seal", new Vector3(side*.17f, .65f, .05f), new Vector3(side*.17f, .57f, .05f), .135f, rubber, suit);
            Ellipsoid("EVA boot", new Vector3(side*.17f, .48f, .12f), new Vector3(.27f, .24f, .43f), rust, suit);
            Box("EVA boot sole", new Vector3(side*.17f, .375f, .12f), new Vector3(.28f, .055f, .43f), rubber, suit);
        }
        Tube("EVA helmet neck ring", new Vector3(0, 1.88f, .04f), new Vector3(0, 1.94f, .04f), .22f, brass, suit);
        Ellipsoid("EVA helmet shell", new Vector3(0, 2.13f, .045f), new Vector3(.55f, .53f, .51f), cream, suit);
        Ellipsoid("Wardrobe helmet visor", new Vector3(0, 2.12f, .245f), new Vector3(.43f, .30f, .21f), visor, suit);
        Ellipsoid("Visor reflected light", new Vector3(-.105f, 2.18f, .335f), new Vector3(.09f, .10f, .012f), cyan, suit);
        foreach (float side in new[] { -1f, 1f })
            Ellipsoid("Helmet side hinge", new Vector3(side*.255f, 2.11f, .08f), new Vector3(.08f, .13f, .16f), brass, suit);
        Box("Suit chest control backing", new Vector3(0, 1.65f, .256f), new Vector3(.33f, .30f, .065f), rust, suit);
        Box("Suit chest control display", new Vector3(0, 1.70f, .295f), new Vector3(.235f, .09f, .018f), visor, suit);
        for (int i=0; i<3; i++)
            Box("Suit control indicator", new Vector3(-.08f+i*.08f, 1.58f, .30f), new Vector3(.04f, .035f, .02f), i==2 ? brass : cyan, suit);
        var hose = new GameObject("Suit breathing hose").AddComponent<LineRenderer>();
        hose.transform.SetParent(suit, false);
        hose.useWorldSpace = false;
        hose.sharedMaterial = rubber;
        hose.widthMultiplier = .035f;
        hose.positionCount = 17;
        hose.numCapVertices = 3;
        hose.numCornerVertices = 3;
        for (int i=0; i<17; i++)
        {
            float t=i/16f;
            hose.SetPosition(i, new Vector3(.15f+.20f*Mathf.Sin(t*Mathf.PI), 1.62f-.30f*t, .27f));
        }
    }

    void BuildLoot()
    {
        // Shelved equipment is legible from the main camera and close inspection.
        Shape("Wardrobe oxygen tank", PrimitiveType.Capsule, new Vector3(.61f, 1.99f, .04f), new Vector3(.21f, .27f, .21f), cream);
        Box("Oxygen tank orange band", new Vector3(.61f, 1.95f, .04f), new Vector3(.22f, .065f, .22f), rust);
        Tube("Oxygen tank valve", new Vector3(.61f, 2.24f, .04f), new Vector3(.61f, 2.31f, .04f), .035f, brass);
        Shape("Oxygen spare filter", PrimitiveType.Cylinder, new Vector3(.95f, 1.84f, .10f), new Vector3(.18f, .14f, .18f), frame);
        Box("Filter mint indicator", new Vector3(.95f, 1.85f, .20f), new Vector3(.08f, .035f, .025f), cyan);
        Box("Wardrobe medkit", new Vector3(.54f, 1.255f, .10f), new Vector3(.34f, .40f, .32f), cream);
        Box("Medkit cross vertical", new Vector3(.54f, 1.255f, .27f), new Vector3(.06f, .22f, .018f), red);
        Box("Medkit cross horizontal", new Vector3(.54f, 1.255f, .28f), new Vector3(.20f, .06f, .018f), red);
        for (int i=0; i<2; i++)
        {
            Box("Spare battery pack", new Vector3(.91f+i*.12f, 1.215f, .08f), new Vector3(.09f, .32f, .22f), frame);
            Box("Battery charge indicator", new Vector3(.91f+i*.12f, 1.255f, .20f), new Vector3(.045f, .09f, .015f), cyan);
            Box("Battery brass terminal", new Vector3(.91f+i*.12f, 1.395f, .08f), new Vector3(.045f, .025f, .07f), brass);
        }
        Box("Wardrobe tool case", new Vector3(.73f, .65f, .08f), new Vector3(.67f, .37f, .43f), rust);
        Box("Tool case lid", new Vector3(.73f, .85f, .08f), new Vector3(.70f, .045f, .45f), rubber);
        Tube("Tool case handle", new Vector3(.61f, .92f, .08f), new Vector3(.85f, .92f, .08f), .026f, brass);
        foreach (float x in new[] { .51f, .95f })
            Box("Tool case clasp", new Vector3(x, .74f, .31f), new Vector3(.06f, .10f, .025f), brass);
        // A sealed parcel sits on the floor beside the locker, clear of its shelves.
        Box("Wardrobe sealed parcel", new Vector3(-1.49f, .15f, .20f), new Vector3(.45f, .30f, .40f), timber);
        Box("Parcel tie", new Vector3(-1.49f, .31f, .20f), new Vector3(.06f, .018f, .41f), cream);
    }

    void OnDestroy() => kit.Release();
}

using UnityEngine;

// Clothes storage in Kristi's cabin. Selection and dressing are handled separately;
// the two hanging sets make the wardrobe readable before the interaction is opened.
public sealed class CabinWardrobe : MonoBehaviour
{
    readonly SceneKit kit = new SceneKit();
    Material wood, trim, interior, linen, terracotta, sage, dark, brass, glow;
    public Transform Root { get; private set; }

    public void Build(Transform ship)
    {
        if (Root != null) return;
        Root = new GameObject("Cabin clothing wardrobe").transform;
        Root.SetParent(ship, false);
        Root.localPosition = ShipLayout.ClothingWardrobePosition;
        wood = kit.Surface("Cabin wardrobe warm walnut", new Color(.34f, .21f, .14f));
        trim = kit.Surface("Cabin wardrobe honey edges", new Color(.64f, .43f, .25f));
        interior = kit.Surface("Cabin wardrobe cocoa backing", new Color(.20f, .145f, .12f));
        linen = kit.Surface("Cabin wardrobe linen", new Color(.84f, .77f, .62f));
        terracotta = kit.Surface("Hanging courier terracotta", new Color(.72f, .31f, .19f));
        sage = kit.Surface("Hanging cabin sage", new Color(.43f, .59f, .43f));
        dark = kit.Surface("Cabin wardrobe boots", new Color(.20f, .17f, .14f));
        brass = kit.Surface("Cabin wardrobe brass", new Color(.72f, .52f, .25f), .45f, .5f);
        glow = kit.Emissive("Cabin wardrobe warm strip", new Color(1f, .72f, .38f), 1.3f);
        BuildCase();
        BuildClothes(-.29f, terracotta, "Courier");
        BuildClothes(.015f, sage, "Cabin");
        BuildShelves();
        kit.Caption("Cabin wardrobe name", "ГАРДЕРОБ / КРИСТИ",
            Position(0, .947f, .515f), .105f, new Color(.97f, .86f, .65f), Root);
        kit.PointLight("Cabin wardrobe warm lamp", Position(-.14f, .87f, .25f),
            new Color(1f, .73f, .43f), .9f, 3f, Root);
    }

    // Model positions are fractions of the shared metre dimensions; the root stays unscaled,
    // so text and lights retain their intended physical size.
    static Vector3 Position(float x, float y, float z)
        => Vector3.Scale(new Vector3(x, y, z), ShipLayout.ClothingWardrobeSize);

    GameObject Box(string name, Vector3 position, Vector3 size, Material material)
        => kit.Box(name, Vector3.Scale(position, ShipLayout.ClothingWardrobeSize),
            Vector3.Scale(size, ShipLayout.ClothingWardrobeSize), material, Root);

    void Box(string name, float x, float y, float z, float w, float h, float d, Material material)
        => Box(name, new Vector3(x, y, z), new Vector3(w, h, d), material);

    void Tube(string name, Vector3 a, Vector3 b, float radius, Material material)
        => kit.Tube(name, Vector3.Scale(a, ShipLayout.ClothingWardrobeSize),
            Vector3.Scale(b, ShipLayout.ClothingWardrobeSize), radius, material, Root);

    void BuildCase()
    {
        Box("Clothes cabinet back", 0, .5f, -.46f, 1, 1, .08f, interior);
        foreach (float x in new[] { -.475f, .475f })
            Box("Clothes cabinet side", x, .5f, 0, .05f, 1, 1, wood);
        foreach (float y in new[] { .025f, .975f })
            Box("Clothes cabinet cap", 0, y, 0, 1, .05f, 1, trim);
        Box("Clothes cabinet plinth", 0, .065f, .02f, .91f, .08f, .90f, wood);
        Box("Clothes cabinet divider", .20f, .51f, -.02f, .035f, .84f, .88f, trim);
        Box("Clothes cabinet name fascia", 0, .947f, .475f, .94f, .072f, .055f, wood);
        Box("Clothes cabinet warm light strip", -.14f, .904f, .32f, .59f, .011f, .065f, glow);
        Tube("Clothes cabinet hanging rail", new Vector3(-.44f, .845f, -.02f),
            new Vector3(.17f, .845f, -.02f), .014f, brass);
        Box("Clothes cabinet bottom drawer", -.14f, .15f, .36f, .59f, .10f, .19f, wood);
        Tube("Clothes cabinet drawer handle", new Vector3(-.24f, .15f, .475f),
            new Vector3(-.04f, .15f, .475f), .012f, brass);
    }

    void BuildClothes(float x, Material fabric, string name)
    {
        // A triangular hanger and softened shirt volume, with short sleeves and trousers.
        Tube(name + " hanger neck", new Vector3(x, .845f, -.02f), new Vector3(x, .79f, -.02f), .009f, brass);
        Tube(name + " hanger left", new Vector3(x, .79f, -.02f), new Vector3(x-.115f, .735f, -.02f), .009f, brass);
        Tube(name + " hanger right", new Vector3(x, .79f, -.02f), new Vector3(x+.115f, .735f, -.02f), .009f, brass);
        kit.Shape(name + " hanging shirt", PrimitiveType.Sphere, Position(x, .635f, .045f),
            Position(.225f, .245f, .25f), fabric, Root);
        foreach (float side in new[] { -1f, 1f })
        {
            var sleeve = kit.Shape(name + " shirt sleeve", PrimitiveType.Sphere,
                Position(x + side*.117f, .702f, .045f), Position(.12f, .11f, .23f), fabric, Root);
            sleeve.transform.localRotation = Quaternion.Euler(0, 0, side*34f);
            Box(name + " hanging trouser leg", x+side*.052f, .395f, .015f, .085f, .27f, .21f, fabric);
        }
        Box(name + " shirt collar", x, .757f, .11f, .065f, .018f, .045f, linen);
        Box(name + " trouser waistband", x, .525f, .02f, .195f, .022f, .23f, dark);
        Box(name + " clothing pocket", x-.057f, .657f, .17f, .055f, .055f, .018f, linen);
        Box(name + " clothing seam", x, .641f, .171f, .008f, .18f, .015f, trim);
    }

    void BuildShelves()
    {
        foreach (float y in new[] { .29f, .50f, .71f, .88f })
            Box("Clothes cabinet right shelf", .337f, y, -.015f, .24f, .025f, .83f, trim);
        for (int i=0; i<3; i++)
        {
            Box("Folded cabin linen", .337f, .74f+i*.038f, .01f, .195f, .033f, .63f,
                i==1 ? sage : linen);
            Box("Folded courier clothes", .337f, .53f+i*.035f, .01f, .19f, .03f, .61f,
                i==1 ? linen : terracotta);
        }
        Box("Wardrobe woven storage basket", .337f, .385f, .015f, .19f, .145f, .62f, linen);
        Box("Storage basket inset handle", .337f, .404f, .335f, .07f, .025f, .018f, dark);
        foreach (float x in new[] { .28f, .39f })
        {
            kit.Shape("Stored cabin boot", PrimitiveType.Sphere, Position(x, .17f, .03f),
                Position(.085f, .11f, .54f), dark, Root);
            Box("Stored boot ankle", x, .225f, -.065f, .075f, .12f, .22f, dark);
        }
    }

    void OnDestroy() => kit.Release();
}

using UnityEngine;

// Domestic dressing for the living-deck galley. The counter already owns its footprint;
// only the added fridge and stool need to be registered with CourierMotor by the builder.
public sealed class LivingGalleyDetails : MonoBehaviour
{
    readonly SceneKit kit = new SceneKit();
    Transform root, counter;
    Material walnut, cream, sage, terracotta, brass, dark, steel, linen, paper, coffee, fruit, glow;
    public Transform[] Obstacles { get; private set; } = new Transform[0];

    public void Build(Transform ship)
    {
        if (root != null) return;
        root = Group("Living galley details", ship, Vector3.zero);
        counter = Group("Galley domestic items", root, ShipLayout.GalleyDetailPosition);
        walnut = kit.Surface("Galley walnut", new Color(.36f,.23f,.15f));
        cream = kit.Surface("Galley warm ceramic", new Color(.88f,.81f,.65f), .38f);
        sage = kit.Surface("Galley sage enamel", new Color(.39f,.57f,.48f), .36f);
        terracotta = kit.Surface("Galley terracotta", new Color(.70f,.29f,.17f));
        brass = kit.Surface("Galley brass fittings", new Color(.69f,.49f,.24f), .45f,.65f);
        dark = kit.Surface("Galley graphite", new Color(.12f,.16f,.16f));
        steel = kit.Surface("Galley brushed steel", new Color(.46f,.53f,.51f), .55f,.7f);
        linen = kit.Surface("Galley striped linen", new Color(.78f,.68f,.51f));
        paper = kit.Surface("Galley paper note", new Color(.97f,.86f,.60f));
        coffee = kit.Surface("Galley coffee surface", new Color(.16f,.075f,.035f), .5f);
        fruit = kit.Surface("Galley citrus", new Color(.96f,.55f,.11f), .25f);
        glow = kit.Emissive("Galley coffee status", new Color(.65f,.91f,.68f), .6f);
        var fridge = BuildFridge();
        var stool = BuildStool();
        BuildCountertop();
        BuildWallShelf();
        Obstacles = new[] { fridge, stool };
    }

    static Transform Group(string name, Transform parent, Vector3 position)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent, false);
        group.localPosition = position;
        return group;
    }

    GameObject Box(string name, Vector3 position, Vector3 size, Material material, Transform parent)
        => kit.Box(name, position, size, material, parent);

    GameObject Sphere(string name, Vector3 position, Vector3 size, Material material, Transform parent)
        => kit.Shape(name, PrimitiveType.Sphere, position, size, material, parent);

    GameObject Cylinder(string name, Vector3 position, Vector3 size, Material material, Transform parent)
        => kit.Shape(name, PrimitiveType.Cylinder, position, new Vector3(size.x,size.y*.5f,size.z), material, parent);

    Transform BuildFridge()
    {
        var fridge = Group("Galley refrigerator", root, ShipLayout.GalleyFridgePosition);
        Vector3 size = ShipLayout.GalleyFridgeSize;
        Box("Refrigerator enamel case", Vector3.up*(size.y*.5f),size,sage,fridge);
        Box("Refrigerator freezer door", new Vector3(0,size.y*.82f,size.z*.51f),
            new Vector3(size.x*.92f,size.y*.27f,.035f),cream,fridge);
        Box("Refrigerator main door", new Vector3(0,size.y*.36f,size.z*.51f),
            new Vector3(size.x*.92f,size.y*.60f,.035f),cream,fridge);
        kit.Tube("Freezer brass handle",new Vector3(-size.x*.33f,size.y*.79f,size.z*.57f),
            new Vector3(-size.x*.33f,size.y*.88f,size.z*.57f),.017f,brass,fridge);
        kit.Tube("Fridge brass handle",new Vector3(-size.x*.33f,size.y*.46f,size.z*.57f),
            new Vector3(-size.x*.33f,size.y*.64f,size.z*.57f),.017f,brass,fridge);
        Box("Fridge shopping note",new Vector3(.075f,size.y*.49f,size.z*.545f),new Vector3(.19f,.23f,.008f),paper,fridge);
        Sphere("Fridge terracotta magnet",new Vector3(.04f,size.y*.565f,size.z*.56f),Vector3.one*.045f,terracotta,fridge);
        Sphere("Fridge mint magnet",new Vector3(.23f,size.y*.86f,size.z*.56f),Vector3.one*.055f,sage,fridge);
        for (int i=0;i<3;i++)
            Box("Shopping note line",new Vector3(.075f,size.y*.52f-i*.04f,size.z*.553f),new Vector3(.12f,.009f,.003f),walnut,fridge);
        Box("Fridge lower vent",new Vector3(0,.07f,size.z*.515f),new Vector3(size.x*.70f,.06f,.025f),dark,fridge);
        return fridge;
    }

    Transform BuildStool()
    {
        var stool = Group("Galley breakfast stool",root,ShipLayout.GalleyStoolPosition);
        var size = ShipLayout.GalleyStoolSize;
        Cylinder("Breakfast stool round seat",new Vector3(0,size.y-.035f,0),new Vector3(size.x,.07f,size.z),walnut,stool);
        foreach (float x in new[] { -.31f,.31f })
            foreach (float z in new[] { -.31f,.31f })
                kit.Tube("Breakfast stool tapered leg",new Vector3(x*size.x,.035f,z*size.z),
                    new Vector3(x*size.x*.8f,size.y-.07f,z*size.z*.8f),.023f,brass,stool);
        foreach (float side in new[] { -1f,1f })
            kit.Tube("Breakfast stool footrest",new Vector3(-size.x*.3f,.22f,side*size.z*.3f),
                new Vector3(size.x*.3f,.22f,side*size.z*.3f),.012f,brass,stool);
        return stool;
    }

    void BuildCountertop()
    {
        float y = ShipLayout.GalleyWorktopHeight;
        // Left: coffee machine. Centre: sink. Right: food preparation.
        Box("Galley compact coffee maker",new Vector3(-.83f,y+.17f,-.045f),new Vector3(.30f,.34f,.29f),sage,counter);
        Box("Coffee maker drip recess",new Vector3(-.83f,y+.13f,.107f),new Vector3(.21f,.19f,.035f),dark,counter);
        Box("Coffee maker drip tray",new Vector3(-.83f,y+.025f,.13f),new Vector3(.24f,.025f,.16f),steel,counter);
        kit.Tube("Coffee maker spout",new Vector3(-.83f,y+.21f,.11f),new Vector3(-.83f,y+.18f,.15f),.014f,brass,counter);
        Box("Coffee maker mint button",new Vector3(-.74f,y+.30f,.11f),new Vector3(.035f,.035f,.015f),glow,counter);
        Mug("Morning coffee mug",new Vector3(-.83f,y+.045f,.14f),cream,counter);
        Box("Galley sink rim",new Vector3(-.12f,y+.012f,.005f),new Vector3(.48f,.024f,.33f),steel,counter);
        Box("Galley sink basin",new Vector3(-.12f,y+.026f,.005f),new Vector3(.39f,.009f,.25f),dark,counter);
        Cylinder("Galley sink drain",new Vector3(-.12f,y+.033f,.005f),new Vector3(.065f,.005f,.065f),steel,counter);
        kit.Tube("Galley faucet riser",new Vector3(-.12f,y+.025f,-.18f),new Vector3(-.12f,y+.27f,-.18f),.014f,brass,counter);
        kit.Tube("Galley faucet bend",new Vector3(-.12f,y+.27f,-.18f),new Vector3(-.12f,y+.27f,-.015f),.014f,brass,counter);
        kit.Tube("Galley faucet outlet",new Vector3(-.12f,y+.27f,-.015f),new Vector3(-.12f,y+.22f,-.015f),.014f,brass,counter);
        Box("Galley wooden chopping board",new Vector3(.68f,y+.018f,.02f),new Vector3(.48f,.035f,.34f),walnut,counter);
        Cylinder("Galley fruit plate",new Vector3(.68f,y+.045f,.02f),new Vector3(.32f,.025f,.28f),cream,counter);
        foreach (var offset in new[] { new Vector3(-.065f,.07f,0),new Vector3(.055f,.07f,-.025f),new Vector3(0,.155f,0) })
            Sphere("Galley fresh citrus",new Vector3(.68f,y+.045f,.02f)+offset,Vector3.one*.12f,fruit,counter);
        Box("Galley folded linen towel",new Vector3(.45f,.70f,.39f),new Vector3(.24f,.37f,.025f),linen,counter);
        for (int i=0;i<3;i++)
            Box("Galley towel woven stripe",new Vector3(.45f,.60f+i*.035f,.406f),new Vector3(.24f,.012f,.008f),terracotta,counter);
        kit.Tube("Galley towel rail",new Vector3(.28f,.87f,.405f),new Vector3(.62f,.87f,.405f),.012f,brass,counter);
        foreach (float x in new[] { -.78f,0,.78f })
            Box("Galley cabinet drawer handle",new Vector3(x,.72f,.35f),new Vector3(.23f,.025f,.035f),brass,counter);
    }

    void Mug(string name, Vector3 bottom, Material material, Transform parent)
    {
        Cylinder(name,bottom+Vector3.up*.05f,new Vector3(.09f,.10f,.09f),material,parent);
        Cylinder(name+" dark coffee",bottom+Vector3.up*.102f,new Vector3(.071f,.004f,.071f),coffee,parent);
        for (int i=0;i<6;i++)
        {
            float a = -.5f*Mathf.PI+i*Mathf.PI/6f, b = a+Mathf.PI/6f;
            kit.Tube(name+" handle",bottom+new Vector3(.043f+Mathf.Cos(a)*.038f,.05f+Mathf.Sin(a)*.035f,0),
                bottom+new Vector3(.043f+Mathf.Cos(b)*.038f,.05f+Mathf.Sin(b)*.035f,0),.008f,material,parent);
        }
    }

    void BuildWallShelf()
    {
        Box("Galley tiled backsplash",new Vector3(0,1.37f,-.52f),new Vector3(2.32f,.65f,.035f),sage,counter);
        for (int i=0;i<6;i++)
            Box("Galley backsplash grout",new Vector3(-.97f+i*.39f,1.37f,-.498f),new Vector3(.008f,.65f,.006f),linen,counter);
        Box("Galley wooden wall shelf",new Vector3(0,1.89f,-.48f),new Vector3(2.34f,.06f,.36f),walnut,counter);
        foreach (float x in new[] { -.91f,.91f })
            Box("Galley shelf brass bracket",new Vector3(x,1.77f,-.52f),new Vector3(.035f,.23f,.17f),brass,counter);
        for (int i=0;i<3;i++)
        {
            float x=-.83f+i*.25f;
            Cylinder("Galley pantry jar",new Vector3(x,2.045f,-.49f),new Vector3(.16f,.25f,.16f),i==1?sage:cream,counter);
            Cylinder("Galley pantry cork lid",new Vector3(x,2.18f,-.49f),new Vector3(.17f,.035f,.17f),walnut,counter);
            Box("Pantry jar label",new Vector3(x,2.045f,-.405f),new Vector3(.095f,.065f,.01f),paper,counter);
        }
        Mug("Shelf terracotta cup",new Vector3(.35f,1.925f,-.45f),terracotta,counter);
        Mug("Shelf cream cup",new Vector3(.61f,1.925f,-.45f),cream,counter);
        Box("Shelf recipe booklet",new Vector3(.94f,1.96f,-.47f),new Vector3(.22f,.07f,.24f),terracotta,counter);
    }

    void OnDestroy() => kit.Release();
}

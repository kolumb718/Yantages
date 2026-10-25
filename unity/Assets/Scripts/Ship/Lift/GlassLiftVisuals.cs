using UnityEngine;
using UnityEngine.Rendering;

// Presentation only: ShipLift owns travel, passengers and movement constraints.
public sealed class GlassLiftVisuals : MonoBehaviour
{
    public Transform Platform { get; private set; }

    readonly SceneKit kit = new SceneKit();
    readonly Transform[,] doorLeaves = new Transform[3, 2];
    readonly Renderer[] levelIndicators = new Renderer[3];
    Transform shaft, section;
    public Transform Underground { get; private set; }
    public Transform Upper { get; private set; }
    Material graphite, rail, deckMaterial, glass, mint, amber, dark;
    bool built;

    public void Build(Transform ship, Vector3 centre)
    {
        if (built) return;
        built = true;
        shaft = new GameObject("Glass lift shaft").transform;
        shaft.SetParent(ship, false);
        shaft.localPosition = centre;
        graphite = Surface("Lift graphite frame", new Color(.19f, .24f, .27f), .30f, .15f);
        rail = Surface("Lift brushed metal", new Color(.56f, .61f, .60f), .52f, .45f);
        deckMaterial = Surface("Lift light metal deck", new Color(.56f, .60f, .58f), .30f, .15f);
        dark = Surface("Lift dark recess", new Color(.075f, .11f, .13f), .20f, 0);
        mint = LightMaterial("Lift cyan guidance", new Color(.38f, .88f, .86f));
        amber = LightMaterial("Lift travel amber", new Color(.96f, .65f, .28f));
        glass = kit.Surface("Lift Glass transparent blue", new Color(.47f, .79f, .85f, .055f), .62f, 0);
        glass.SetFloat("_Mode", 2);
        glass.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        glass.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        glass.SetInt("_ZWrite", 0);
        glass.DisableKeyword("_ALPHATEST_ON");
        glass.EnableKeyword("_ALPHABLEND_ON");
        glass.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        glass.SetOverrideTag("RenderType", "Transparent");
        glass.renderQueue = (int)RenderQueue.Transparent;

        Upper = new GameObject("Residential lift shaft").transform;
        Upper.SetParent(shaft, false);
        section = Upper;
        BuildFrame();
        BuildGlass();
        BuildPlatform();
        BuildLanding(0, 0);
        BuildLanding(1, ShipLayout.DeckHeight);
        Caption("Lift shaft header", "ЛИФТ / 00-02", new Vector3(0, 6.52f, 1.055f), .14f);
        Underground = new GameObject("Underground lift shaft").transform;
        Underground.SetParent(shaft, false);
        section = Underground;
        BuildUnderground();
        BuildLanding(-1, ShipLayout.HangarHeight);
        section = null;
        Underground.gameObject.SetActive(false);
    }

    Material Surface(string name, Color color, float smoothness, float metallic)
        => kit.Surface(name, color, smoothness, metallic);

    Material LightMaterial(string name, Color color) => kit.Emissive(name, color, .30f, .30f);

    Transform Section => section != null ? section : shaft;

    GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 size,
        Material material, Transform parent = null)
    {
        var item = kit.Shape(name, type, position, size, material, parent != null ? parent : Section);
        if (material == glass)
        {
            var renderer = item.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        return item;
    }

    GameObject Box(string name, Vector3 position, Vector3 size, Material material, Transform parent = null)
        => Shape(name, PrimitiveType.Cube, position, size, material, parent);

    void Caption(string name, string text, Vector3 position, float size)
        => kit.Caption(name, text, position, size, new Color(.75f, .89f, .87f), Section);

    void BuildFrame()
    {
        foreach (float x in new[] { -1.115f, 1.115f })
            foreach (float z in new[] { -1.015f, 1.015f })
            {
                Box("Lift corner upright", new Vector3(x, 3.325f, z), new Vector3(.065f, 6.65f, .065f), graphite);
                Box("Lift corner metal reveal", new Vector3(x, 3.325f, z+.033f), new Vector3(.018f, 6.55f, .008f), rail);
            }
        foreach (float y in new[] { .035f, 4.015f, 6.58f })
        {
            Box("Lift front crossbeam", new Vector3(0, y, 1.015f), new Vector3(2.20f, .075f, .065f), graphite);
            Box("Lift rear crossbeam", new Vector3(0, y, -1.015f), new Vector3(2.20f, .075f, .065f), graphite);
            Box("Lift right crossbeam", new Vector3(1.115f, y, 0), new Vector3(.065f, .075f, 2.03f), graphite);
        }
        foreach (float x in new[] { -.96f, .96f })
        {
            Box("Lift rear guide rail", new Vector3(x, 3.28f, -.94f), new Vector3(.045f, 6.46f, .055f), rail);
            Box("Lift guide dark track", new Vector3(x, 3.28f, -.905f), new Vector3(.017f, 6.42f, .012f), dark);
        }
        // Sparse height marks make the travel legible without cluttering the glass.
        for (int i = 1; i <= 12; i++)
            Box("Lift guide height mark", new Vector3(1.075f, .45f*i, .94f), new Vector3(.026f, .018f, .09f), i%3 == 0 ? mint : rail);
    }

    void BuildGlass()
    {
        Box("Lift Glass front panel", new Vector3(0, 3.30f, 1.002f), new Vector3(2.14f, 6.42f, .018f), glass);
        Box("Lift Glass rear panel", new Vector3(0, 3.30f, -1.002f), new Vector3(2.14f, 6.42f, .018f), glass);
        Box("Lift Glass right panel", new Vector3(1.102f, 3.30f, 0), new Vector3(.018f, 6.42f, 1.96f), glass);
        // The left face has a door on each landing rather than an uninterrupted pane.
        foreach (float z in new[] { -.88f, .88f })
            Box("Lift Glass left side strip", new Vector3(-1.102f, 3.30f, z), new Vector3(.018f, 6.42f, .25f), glass);
        Box("Lift Glass between landings", new Vector3(-1.102f, 3.22f, 0), new Vector3(.018f, 1.68f, 1.49f), glass);
        Box("Lift Glass upper header", new Vector3(-1.102f, 6.49f, 0), new Vector3(.018f, .17f, 1.49f), glass);
        foreach (float z in new[] { -.975f, .975f })
        {
            Box("Lift Glass front edge highlight", new Vector3(1.068f, 3.29f, z), new Vector3(.012f, 6.34f, .012f), mint);
        }
    }

    void BuildPlatform()
    {
        Platform = new GameObject("Moving lift platform").transform;
        Platform.SetParent(shaft, false);
        Platform.localPosition = Vector3.zero;
        // Surface y=0 is the passenger's standing level; all structural thickness is below it.
        Box("Moving platform underframe", new Vector3(0, -.09f, 0), new Vector3(2.16f, .14f, 1.96f), graphite, Platform);
        Box("Moving platform floor", new Vector3(0, -.012f, 0), new Vector3(2.09f, .024f, 1.89f), deckMaterial, Platform);
        foreach (float x in new[] { -1.015f, 1.015f })
            Box("Platform cyan side edge", new Vector3(x, -.032f, 0), new Vector3(.026f, .028f, 1.86f), mint, Platform);
        foreach (float z in new[] { -.915f, .915f })
            Box("Platform cyan cross edge", new Vector3(0, -.032f, z), new Vector3(2.02f, .028f, .026f), mint, Platform);
        for (int i = 0; i < 6; i++)
            Box("Platform grip strip", new Vector3(-.72f+i*.29f, .0015f, 0), new Vector3(.018f, .003f, 1.55f), graphite, Platform);
        foreach (float x in new[] { -.92f, .92f })
        {
            Box("Moving rear handrail post", new Vector3(x, .43f, -.87f), new Vector3(.025f, .86f, .025f), rail, Platform);
            var roller = Shape("Platform guide roller", PrimitiveType.Cylinder, new Vector3(x, -.08f, -.90f),
                new Vector3(.12f, .035f, .12f), dark, Platform);
            roller.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }
        Box("Moving rear handrail", new Vector3(0, .86f, -.87f), new Vector3(1.86f, .035f, .035f), rail, Platform);
    }

    void BuildLanding(int level, float y)
    {
        Box("Landing left sill", new Vector3(-1.105f, y-.025f, 0), new Vector3(.075f, .05f, 1.50f), rail);
        Box("Landing left lintel", new Vector3(-1.105f, y+2.29f, 0), new Vector3(.075f, .075f, 1.50f), graphite);
        foreach (float z in new[] { -.73f, .73f })
            Box("Landing door jamb", new Vector3(-1.105f, y+1.14f, z), new Vector3(.075f, 2.28f, .055f), graphite);
        for (int side = 0; side < 2; side++)
        {
            var leaf = new GameObject("Lift Glass door assembly "+level+" "+side).transform;
            leaf.SetParent(Section, false);
            leaf.localPosition = new Vector3(-1.095f, y+1.13f, side == 0 ? -.325f : .325f);
            doorLeaves[level+1, side] = leaf;
            Box("Lift Glass door leaf", Vector3.zero, new Vector3(.018f, 2.18f, .65f), glass, leaf);
            Box("Glass door moving edge", new Vector3(-.015f, 0, side == 0 ? .313f : -.313f),
                new Vector3(.022f, 2.18f, .022f), rail, leaf);
            Box("Glass door handle", new Vector3(-.03f, -.10f, side == 0 ? .24f : -.24f),
                new Vector3(.025f, .26f, .028f), mint, leaf);
        }
        var indicator = Box("Lift landing indicator "+level, new Vector3(-.96f, y+2.29f, .96f),
            new Vector3(.065f, .065f, .025f), mint);
        levelIndicators[level+1] = indicator.GetComponent<Renderer>();
        Caption("Lift landing number "+level, level < 0 ? "00" : level == 0 ? "01" : "02", new Vector3(-.77f, y+2.29f, 1.055f), .14f);
    }

    void BuildUnderground()
    {
        float bottom = ShipLayout.HangarHeight;
        float height = -bottom;
        foreach (float x in new[] { -1.115f, 1.115f })
            foreach (float z in new[] { -1.015f, 1.015f })
                Box("Underground lift upright", new Vector3(x, bottom/2, z), new Vector3(.065f, height, .065f), graphite);
        foreach (float x in new[] { -.96f, .96f })
            Box("Underground guide rail", new Vector3(x, bottom/2, -.94f), new Vector3(.045f, height, .055f), rail);
        foreach (float z in new[] { -1.002f, 1.002f })
            Box("Underground Glass panel", new Vector3(0, bottom/2, z), new Vector3(2.14f, height, .018f), glass);
        Box("Underground Glass right", new Vector3(1.102f, bottom/2, 0), new Vector3(.018f, height, 1.96f), glass);
        foreach (float z in new[] { -.88f, .88f })
            Box("Underground Glass side strip", new Vector3(-1.102f, bottom/2, z), new Vector3(.018f, height, .25f), glass);
        Box("Underground Glass above door", new Vector3(-1.102f, (bottom+2.4f)/2, 0), new Vector3(.018f, height-2.4f, 1.49f), glass);
        foreach (float z in new[] { -1.015f, 1.015f })
            Box("Hangar landing crossbeam", new Vector3(0, bottom+.035f, z), new Vector3(2.2f, .075f, .065f), graphite);
        for (int i = 1; i < 17; i++)
            Box("Underground guide height mark", new Vector3(1.075f, -.48f*i, .94f), new Vector3(.026f, .018f, .09f), i%3 == 0 ? mint : rail);
    }

    // 0 = closed, 1 = open. Telescoping leaves slide into the narrow glass side pockets.
    public void SetDoor(int level, float opening)
    {
        for (int side = 0; side < 2; side++)
        {
            var leaf = doorLeaves[level+1, side];
            if (leaf == null) continue;
            float sign = side == 0 ? -1 : 1;
            leaf.localPosition = new Vector3(-1.095f, ShipLayout.FloorHeight(level)+1.13f, sign*Mathf.Lerp(.325f, .875f, opening));
            leaf.localScale = new Vector3(1, 1, Mathf.Lerp(1, .44f, opening));
        }
    }

    public void SetIndicators(int deck, bool moving)
    {
        for (int level = ShipLayout.HangarDeck; level <= ShipLayout.CabinDeck; level++)
            if (levelIndicators[level+1] != null)
                levelIndicators[level+1].sharedMaterial = moving ? amber : deck == level ? mint : dark;
    }

    void OnDestroy() => kit.Release();
}

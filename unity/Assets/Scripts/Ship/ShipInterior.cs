using UnityEngine;
using UnityEngine.Rendering;

// Shell of the cutaway ship: floors, walls, gallery, built-in furniture blocks and cabin light.
// Imported furniture, lettering, the wardrobe and the window view are built by their own components.
public sealed class ShipInterior : MonoBehaviour
{
    readonly SceneKit kit = new SceneKit();
    Transform shell;
    Material terminalScreen;
    Color terminalGlow;
    float highlight;

    // Footprints the courier walks around (imported props are registered by CourierShip).
    public Transform[] Obstacles { get; private set; }

    public void Build(Transform ship)
    {
        shell = new GameObject("Ship shell").transform;
        shell.SetParent(ship, false);
        BuildLight();
        BuildStructure();
        Obstacles = new[]
        {
            Furniture("Lounge coffee table", "B98E77", new Vector3(-9, ShipLayout.CoffeeTableTop-ShipLayout.CoffeeTableSize.y/2, .6f),
                ShipLayout.CoffeeTableSize, ShipLayout.CoffeeTableTop-ShipLayout.CoffeeTableSize.y),
            Block("Galley counter", new Vector3(-3, ShipLayout.GalleySize.y/2, -4.1f), ShipLayout.GalleySize, "526467",
                ("Galley worktop", new Vector3(-3, ShipLayout.GalleyWorktopHeight-.03f, -4.1f),
                    new Vector3(ShipLayout.GalleySize.x+.08f, .06f, ShipLayout.GalleySize.z+.06f), "C6AA90")),
            BuildTerminal(),
            Block("Reading armchair", new Vector3(-4.6f, ShipLayout.DeckHeight+.28f, -2), new Vector3(.85f, .5f, .80f), "B37A88",
                ("Reading armchair back", new Vector3(-4.6f, ShipLayout.DeckHeight+.62f, -2.32f), new Vector3(.85f, .56f, .16f), "B37A88")),
        };
        Box("Lounge rug", new Vector3(-9, .06f, 1), new Vector3(3.2f, .025f, 2.4f), "775B78");
    }

    void BuildLight()
    {
        // Trilight keeps the cutaway readable: cool ceiling, warm walls, deep floor shadows.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.42f, .38f, .55f);
        RenderSettings.ambientEquatorColor = new Color(.40f, .32f, .42f);
        RenderSettings.ambientGroundColor = new Color(.20f, .16f, .22f);
        var warm = new Color(1, .77f, .60f);
        kit.PointLight("Lounge lamp", new Vector3(-10, 2, 1), warm, 3, 9, shell, LightShadows.Soft).shadowStrength = .55f;
        kit.PointLight("Cabin lamp", new Vector3(-8, 6, -1), warm, 3, 9, shell, LightShadows.Soft).shadowStrength = .55f;
        kit.PointLight("Bridge violet light", new Vector3(7, 4, -2), new Color(.73f, .51f, .91f), 4, 12, shell);
        kit.PointLight("Galley task light", new Vector3(-3, 3, -2.6f), warm, 1.2f, 5, shell);
    }

    void BuildStructure()
    {
        // The floor opening continues the lift shaft down into the hangar.
        Box("Living deck slab", new Vector3(-7.425f, -.25f, 0), new Vector3(13.15f, .5f, 10), "283040");
        Box("Living deck slab", new Vector3(7.725f, -.25f, 0), new Vector3(12.55f, .5f, 10), "283040");
        Box("Living deck slab", new Vector3(.3f, -.25f, -2.125f), new Vector3(2.3f, .5f, 5.75f), "283040");
        Box("Living deck slab", new Vector3(.3f, -.25f, 3.925f), new Vector3(2.3f, .5f, 2.15f), "283040");
        Box("Rear hull wall", new Vector3(0, 4, -5), new Vector3(28, 8.4f, .25f), "353347");
        Box("Side hull wall", new Vector3(-14, 4, 0), new Vector3(.25f, 8, 10), "353347");
        for (int x = -13; x < 14; x++)
        {
            if (x >= -1 && x <= 1)
            {
                Box("Floor plank", new Vector3(x, .025f, -2.075f), new Vector3(.94f, .05f, 5.65f), "48414C");
                Box("Floor plank", new Vector3(x, .025f, 3.875f), new Vector3(.94f, .05f, 2.05f), "48414C");
            }
            else Box("Floor plank", new Vector3(x, .025f, 0), new Vector3(.94f, .05f, 9.8f), "48414C");
        }
        // Four slabs leave a real opening around the glass lift shaft.
        Box("Gallery slab", new Vector3(-7.325f, 3.9f, -.8f), new Vector3(12.95f, .3f, 7.8f), "63505E");
        Box("Gallery slab", new Vector3(.3f, 3.9f, -1.975f), new Vector3(2.3f, .3f, 5.45f), "63505E");
        Box("Gallery slab", new Vector3(1.625f, 3.9f, -.8f), new Vector3(.35f, .3f, 7.8f), "63505E");
        Box("Gallery slab", new Vector3(.3f, 3.9f, 2.975f), new Vector3(2.3f, .3f, .25f), "63505E");
        for (int x = -13; x < 2; x += 2)
            Box("Gallery railing post", new Vector3(x, 4.65f, 3), new Vector3(.06f, 1.2f, .06f), "BC9690");
        Box("Gallery handrail", new Vector3(-6, 5.25f, 3), new Vector3(15.6f, .06f, .06f), "D1A58D");
        Box("Gallery edge light", new Vector3(-6, 4, 3.1f), new Vector3(15.6f, .05f, .05f), "ED8EC6", 1.5f);
        for (int i = 0; i < 17; i++)
            Box("Gallery string light", new Vector3(-13 + i * .9f, 7.4f - Mathf.Sin(i / 16f * Mathf.PI) * .6f, 1.6f),
                new Vector3(.15f, .2f, .15f), "FFD2A0", 1.5f);
    }

    Transform BuildTerminal()
    {
        var terminal = Block("Order terminal", new Vector3(.4f, ShipLayout.TerminalBodySize.y/2, -1.9f), ShipLayout.TerminalBodySize, "283E42",
            ("Order terminal housing", new Vector3(.4f, ShipLayout.TerminalScreenHeight, -1.9f), new Vector3(.65f, .50f, .14f), "526467"));
        ColorUtility.TryParseHtmlString("#8CD5A9", out terminalGlow);
        terminalScreen = kit.Emissive("Order terminal screen", terminalGlow, 1.5f);
        kit.Box("Order terminal screen", new Vector3(.4f, ShipLayout.TerminalScreenHeight, -1.815f), new Vector3(.55f, .39f, .026f), terminalScreen, shell);
        return terminal;
    }

    // A table top on four slim legs instead of a slab floating above the rug.
    Transform Furniture(string name, string hex, Vector3 top, Vector3 size, float legHeight)
    {
        var root = new GameObject(name).transform;
        root.SetParent(shell, false);
        kit.Box(name + " top", top, size, kit.Flat(hex), root);
        foreach (float x in new[] { -1, 1 })
            foreach (float z in new[] { -1, 1 })
                kit.Box(name + " leg", new Vector3(top.x + x * (size.x * .5f - .12f), legHeight * .5f, top.z + z * (size.z * .5f - .12f)),
                    new Vector3(.07f, legHeight, .07f), kit.Flat("6B4F45"), root);
        return root;
    }

    Transform Block(string name, Vector3 position, Vector3 size, string hex,
        params (string name, Vector3 position, Vector3 size, string hex)[] parts)
    {
        var root = new GameObject(name).transform;
        root.SetParent(shell, false);
        kit.Box(name + " body", position, size, kit.Flat(hex), root);
        foreach (var part in parts) kit.Box(part.name, part.position, part.size, kit.Flat(part.hex), root);
        return root;
    }

    void Box(string name, Vector3 position, Vector3 size, string hex, float emission = 0)
        => kit.Box(name, position, size, kit.Flat(hex, emission), shell);

    // The terminal screen brightens while the courier can use it.
    public void SetTerminalHighlight(bool active)
    {
        float target = active ? 1 : 0;
        if (Mathf.Approximately(highlight, target)) return;
        highlight = Mathf.MoveTowards(highlight, target, Time.deltaTime * 4);
        terminalScreen.SetColor("_EmissionColor", terminalGlow * Mathf.Lerp(1.5f, 2.6f, highlight));
    }

    void OnDestroy() => kit.Release();
}

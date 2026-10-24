using UnityEngine;

// Local dressing of the ship: lettering, service wiring and the navigation projector.
public sealed class ShipInteriorDetails : MonoBehaviour
{
    Transform globe;
    Transform dressing;
    Material metal, warmMetal, cable, amber, cyan, mint;
    Font font;

    public void Build(Transform ship)
    {
        dressing = new GameObject("Ship interior details").transform;
        dressing.SetParent(ship, false);
        metal = Surface("Detail graphite", new Color(.14f, .18f, .22f));
        warmMetal = Surface("Detail brass", new Color(.54f, .37f, .19f));
        cable = Surface("Cable rubber", new Color(.07f, .085f, .09f));
        amber = Glow("Amber markings", new Color(.96f, .64f, .27f));
        cyan = Glow("Hologram cyan", new Color(.22f, .77f, .94f));
        mint = Glow("Hologram land", new Color(.39f, .97f, .71f));
        BuildWallDetails();
        BuildHologram();
    }

    static Material Surface(string name, Color color)
    {
        var material = new Material(Shader.Find("Standard")) { name = name, color = color };
        material.SetFloat("_Glossiness", .22f);
        return material;
    }

    static Material Glow(string name, Color color)
    {
        return new Material(Shader.Find("Unlit/Color")) { name = name, color = color };
    }

    static GameObject Primitive(string name, PrimitiveType type, Vector3 position,
        Vector3 scale, Material material, Transform parent)
    {
        var item = GameObject.CreatePrimitive(type);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(item.GetComponent<Collider>());
        return item;
    }

    void Box(string name, Vector3 position, Vector3 scale, Material material)
    {
        Primitive(name, PrimitiveType.Cube, position, scale, material, dressing);
    }

    void Label(string name, string text, Vector3 position, float size, Color color,
        Transform parent = null, bool plaque = true)
    {
        var item = new GameObject(name);
        item.transform.SetParent(parent != null ? parent : dressing, false);
        item.transform.localPosition = position;
        item.transform.localRotation = Quaternion.Euler(0, 180, 0);
        var lettering = item.AddComponent<TextMesh>();
        lettering.font = font;
        lettering.fontSize = 64;
        // Font size controls atlas detail; compensate so the requested size stays in metres.
        lettering.characterSize = size * 10f / lettering.fontSize;
        lettering.anchor = TextAnchor.MiddleCenter;
        lettering.alignment = TextAlignment.Center;
        lettering.color = color;
        lettering.text = text;
        item.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        if (plaque)
        {
            float width = text.Split('\n')[0].Length * size * .48f + .25f;
            Box(name+" backing", position+new Vector3(0, 0, -.025f), new Vector3(width, size*1.7f, .035f), metal);
        }
    }

    LineRenderer Line(string name, Vector3[] points, Material material, float width,
        Transform parent, bool loop = false)
    {
        var line = new GameObject(name).AddComponent<LineRenderer>();
        line.transform.SetParent(parent, false);
        line.useWorldSpace = false;
        line.sharedMaterial = material;
        line.widthMultiplier = width;
        line.positionCount = points.Length;
        line.SetPositions(points);
        line.loop = loop;
        line.numCornerVertices = 3;
        line.numCapVertices = 3;
        return line;
    }

    void Wire(string name, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        var points = new Vector3[33];
        for (int i = 0; i < points.Length; i++)
        {
            float t = i / 32f, u = 1-t;
            points[i] = u*u*u*a + 3*u*u*t*b + 3*u*t*t*c + t*t*t*d;
        }
        Line(name, points, cable, .045f, dressing);
        Box(name+" socket", a, Vector3.one*.15f, warmMetal);
        Box(name+" connector", d, new Vector3(.10f, .19f, .10f), amber);
    }

    void BuildWallDetails()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Label("Ship name lettering", "КРОШКА  /  ORBITA 07", new Vector3(-8.5f, 7.65f, -4.82f), .40f, new Color(.91f, .76f, .53f), plaque:false);
        Label("Cabin deck sign", "02  /  КАЮТА", new Vector3(-10.4f, 6.45f, -4.82f), .27f, new Color(.92f, .82f, .65f));
        Label("Lounge deck sign", "01  /  ЖИЛОЙ ОТСЕК", new Vector3(-9.0f, 2.8f, -4.82f), .25f, new Color(.89f, .74f, .56f));
        Label("Galley sign", "КУХНЯ  //  07", new Vector3(-3.0f, 2.9f, -4.82f), .21f, new Color(.89f, .74f, .56f));
        Label("Bridge sign", "МОСТИК  /  НАВИГАЦИЯ", new Vector3(8, 7.92f, -4.82f), .24f, new Color(.88f, .76f, .59f), plaque:false);
        Label("Cargo handling stencil", "НЕ ПЕРЕВОРАЧИВАТЬ", new Vector3(.6f, 3.1f, -4.82f), .17f, new Color(.88f, .64f, .32f));
        Label("Service stencil", "ПИТАНИЕ / 24V", new Vector3(1.15f, 5.7f, -4.82f), .17f, new Color(.82f, .68f, .44f));

        Box("Upper service panel", new Vector3(1.8f, 7.5f, -4.64f), new Vector3(.65f, .57f, .18f), metal);
        for (int i = 0; i < 3; i++)
            Box("Service status lamp", new Vector3(1.57f+i*.17f, 7.53f, -4.535f), new Vector3(.07f, .06f, .015f), i == 2 ? amber : mint);
        Wire("Hanging wall cable", new Vector3(1.45f, 7.24f, -4.55f), new Vector3(.65f, 5.95f, -4.25f),
            new Vector3(2.6f, 5.72f, -4.28f), new Vector3(2.45f, 7.4f, -4.55f));
        Wire("Loose galley cable", new Vector3(-1.2f, 3.55f, -4.55f), new Vector3(-1.65f, 3.1f, -4.38f),
            new Vector3(-.85f, 2.65f, -4.32f), new Vector3(-1.35f, 2.35f, -4.4f));
        Wire("Cable beneath gallery", new Vector3(-12.5f, 3.69f, 2.96f), new Vector3(-11, 3.0f, 2.93f),
            new Vector3(-8.9f, 3.05f, 2.93f), new Vector3(-7.8f, 3.69f, 2.96f));
        for (int i = 0; i < 4; i++)
            Box("Wall access vent", new Vector3(-12.4f+i*.14f, 2.98f, -4.80f), new Vector3(.07f, .36f, .035f), metal);
    }

    static Vector3 SpherePoint(float longitude, float latitude, float radius)
    {
        float lon = longitude*Mathf.Deg2Rad, lat = latitude*Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(lon)*Mathf.Cos(lat), Mathf.Sin(lat), Mathf.Cos(lon)*Mathf.Cos(lat))*radius;
    }

    void Circle(string name, float radius, float y, Material material, Transform parent, float width=.012f)
    {
        var points = new Vector3[72];
        for (int i = 0; i < points.Length; i++)
        {
            float a = i*Mathf.PI*2/points.Length;
            points[i] = new Vector3(Mathf.Cos(a)*radius, y, Mathf.Sin(a)*radius);
        }
        Line(name, points, material, width, parent, true);
    }

    void Continent(string name, Vector2[] outline)
    {
        var points = new Vector3[outline.Length];
        for (int i = 0; i < points.Length; i++) points[i] = SpherePoint(outline[i].x, outline[i].y, .615f);
        Line(name, points, mint, .023f, globe, true);
    }

    void BuildHologram()
    {
        var platform = new GameObject("Navigation hologram platform").transform;
        platform.SetParent(dressing, false);
        platform.localPosition = new Vector3(7.8f, .08f, -.8f);
        Primitive("Projector base", PrimitiveType.Cylinder, new Vector3(0, .02f, 0), new Vector3(2.12f, .10f, 2.12f), metal, platform);
        Primitive("Projector bronze rim", PrimitiveType.Cylinder, new Vector3(0, .12f, 0), new Vector3(1.87f, .035f, 1.87f), warmMetal, platform);
        Primitive("Projector dark lens", PrimitiveType.Cylinder, new Vector3(0, .16f, 0), new Vector3(1.62f, .025f, 1.62f), metal, platform);
        Circle("Projector cyan rim", .91f, .165f, cyan, platform, .026f);
        Circle("Projector inner ring", .73f, .19f, cyan, platform, .014f);
        for (int i = 0; i < 12; i++)
        {
            float a = i*Mathf.PI/6;
            var marker = Primitive("Projector rim marker", PrimitiveType.Cube,
                new Vector3(Mathf.Cos(a)*.99f, .145f, Mathf.Sin(a)*.99f), new Vector3(.055f, .018f, .13f), i%3 == 0 ? amber : cyan, platform);
            marker.transform.localRotation = Quaternion.Euler(0, -a*Mathf.Rad2Deg, 0);
        }
        globe = new GameObject("Holographic planet").transform;
        globe.SetParent(platform, false);
        globe.localPosition = new Vector3(0, 1.25f, 0);
        var skin = new Material(Shader.Find("Orbita/Hologram")) { name = "Holographic atmosphere" };
        skin.SetColor("_Tint", new Color(.18f, .73f, 1, .25f));
        Primitive("Hologram atmosphere", PrimitiveType.Sphere, Vector3.zero, Vector3.one*1.2f, skin, globe);
        for (int latitude = -60; latitude <= 60; latitude += 20)
        {
            var points = new Vector3[72];
            for (int i = 0; i < points.Length; i++) points[i] = SpherePoint(i*5, latitude, .607f);
            Line("Planet latitude", points, cyan, .006f, globe, true);
        }
        for (int longitude = 0; longitude < 180; longitude += 20)
        {
            var points = new Vector3[72];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i*Mathf.PI/36;
                points[i] = new Vector3(Mathf.Sin(longitude*Mathf.Deg2Rad)*Mathf.Cos(angle), Mathf.Sin(angle),
                    Mathf.Cos(longitude*Mathf.Deg2Rad)*Mathf.Cos(angle))*.607f;
            }
            Line("Planet meridian", points, cyan, .006f, globe, true);
        }
        Continent("Hologram continent A", new[] { new Vector2(-35, 50), new Vector2(-15, 58), new Vector2(5, 48), new Vector2(21, 37), new Vector2(13, 20), new Vector2(30, 5), new Vector2(15, -25), new Vector2(1, -37), new Vector2(-9, -10), new Vector2(-23, 6), new Vector2(-19, 28) });
        Continent("Hologram continent B", new[] { new Vector2(50, 63), new Vector2(91, 53), new Vector2(124, 28), new Vector2(101, 7), new Vector2(73, 21), new Vector2(59, 7), new Vector2(35, 33) });
        Continent("Hologram continent C", new[] { new Vector2(-118, 51), new Vector2(-79, 57), new Vector2(-54, 25), new Vector2(-80, 4), new Vector2(-48, -8), new Vector2(-67, -47), new Vector2(-88, -20), new Vector2(-101, 11) });
        var orbit = new GameObject("Planet orbital ring").transform;
        orbit.SetParent(platform, false);
        orbit.localPosition = new Vector3(0, 1.25f, 0);
        orbit.localRotation = Quaternion.Euler(18, 0, 12);
        Circle("Orbit cyan ring", .86f, 0, cyan, orbit, .014f);
        for (int i = 0; i < 3; i++)
        {
            float a = i*Mathf.PI*2/3;
            Line("Projection ray", new[] { new Vector3(Mathf.Cos(a)*.60f, .20f, Mathf.Sin(a)*.60f),
                new Vector3(Mathf.Cos(a)*.25f, .77f, Mathf.Sin(a)*.25f) }, cyan, .008f, platform);
        }
        Label("Projector planet label", "ЗЕМЛЯ / EARTH", new Vector3(0, .47f, 1.02f), .12f, new Color(.53f, .91f, 1), platform, false);
        var light = new GameObject("Hologram projector light").AddComponent<Light>();
        light.transform.SetParent(platform, false);
        light.transform.localPosition = new Vector3(0, .6f, 0);
        light.type = LightType.Point;
        light.color = new Color(.22f, .73f, 1);
        light.range = 3;
        light.intensity = .45f;
    }

    void Update()
    {
        if (globe != null) globe.Rotate(Vector3.up, Time.deltaTime*9, Space.Self);
    }
}

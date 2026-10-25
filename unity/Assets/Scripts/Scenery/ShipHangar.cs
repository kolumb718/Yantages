using System.Collections.Generic;
using UnityEngine;

// Original cutaway hangar and a warm low-poly courier craft. Presentation only.
public sealed class ShipHangar : MonoBehaviour
{
    const float FloorHeight = ShipLayout.HangarHeight;
    public Transform Interior { get; private set; }
    public Transform Craft { get; private set; }

    readonly SceneKit kit = new SceneKit();
    Material concrete, wall, steel, dark, cream, amber, mint, green, belly, red, glass, brass;

    public void Build(Transform ship)
    {
        if (Interior != null) return;
        Interior = new GameObject("Underground hangar").transform;
        Interior.SetParent(ship, false);
        concrete = kit.Surface("Hangar warm concrete", new Color(.29f, .27f, .25f), .18f);
        wall = kit.Surface("Hangar plum walls", new Color(.22f, .245f, .28f), .18f);
        steel = kit.Surface("Hangar brushed steel", new Color(.34f, .39f, .41f), .3f, .4f);
        dark = kit.Surface("Hangar dark recess", new Color(.075f, .10f, .12f), .18f);
        cream = kit.Surface("Hangar ivory markings", new Color(.85f, .78f, .61f), .18f);
        amber = kit.Emissive("Hangar amber guidance", new Color(1f, .62f, .25f), .65f, .25f);
        mint = kit.Emissive("Hangar mint guidance", new Color(.25f, .79f, .71f), .65f, .3f);
        green = kit.Surface("Courier craft jade enamel", new Color(.19f, .64f, .40f), .42f, .15f);
        belly = kit.Surface("Courier craft deep jade", new Color(.075f, .34f, .25f), .35f, .15f);
        red = kit.Surface("Courier craft vermilion belt", new Color(.69f, .16f, .105f), .4f, .1f);
        glass = kit.Surface("Courier craft petrol glazing", new Color(.045f, .15f, .19f), .85f, .4f);
        brass = kit.Surface("Courier craft aged brass", new Color(.63f, .53f, .33f), .4f, .5f);
        BuildRoom();
        BuildCraft();
    }

    GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 size, Material material,
        Transform parent = null) => kit.Shape(name, type, position, size, material, parent != null ? parent : Interior);

    GameObject Box(string name, Vector3 position, Vector3 size, Material material, Transform parent = null)
        => Shape(name, PrimitiveType.Cube, position, size, material, parent);

    void Tube(string name, Vector3 a, Vector3 b, float radius, Material material, Transform parent = null)
        => kit.Tube(name, a, b, radius, material, parent != null ? parent : Interior);

    void Caption(string name, string text, Vector3 position, float size)
        => kit.Caption(name, text, position, size, new Color(.95f, .82f, .61f), Interior);

    void BuildRoom()
    {
        // Front and ceiling are intentionally open so the lower level reads as a stage.
        Box("Hangar foundation", new Vector3(0, FloorHeight - .30f, -1), new Vector3(36, .44f, 14), dark);
        for (int x = 0; x < 12; x++)
            for (int z = 0; z < 7; z++)
                Box("Hangar floor tile", new Vector3(-16.5f + x * 3, FloorHeight - .035f, -7 + z * 2),
                    new Vector3(2.96f, .07f, 1.96f), concrete);
        Box("Hangar rear wall", new Vector3(0, -4.35f, -8.10f), new Vector3(36, 7.7f, .20f), wall);
        Box("Hangar left wall", new Vector3(-18.1f, -4.35f, -1), new Vector3(.2f, 7.7f, 14), wall);
        // A low side wall preserves the player's sightline from the usual +X camera.
        Box("Hangar right service parapet", new Vector3(18.1f, -7.55f, -1), new Vector3(.2f, 1.3f, 14), wall);
        Box("Hangar blast door recess", new Vector3(-6, -4.45f, -7.93f), new Vector3(20, 6.8f, .16f), dark);
        for (int i = 0; i < 10; i++)
        {
            Box("Hangar blast door segment", new Vector3(-15 + i * 2, -4.5f, -7.79f),
                new Vector3(1.92f, 6.45f, .13f), steel);
            Box("Hangar door lower reinforcement", new Vector3(-15 + i * 2, -6.85f, -7.67f),
                new Vector3(1.55f, .13f, .10f), wall);
        }
        foreach (float x in new[] { -17f, 5f, 16.8f })
        {
            Box("Hangar structural column", new Vector3(x, -4.35f, -7.55f), new Vector3(.6f, 7.7f, .65f), dark);
            Box("Hangar column warm reveal", new Vector3(x, -3.85f, -7.19f), new Vector3(.13f, 4.6f, .035f), amber);
            for (int i = 0; i < 5; i++)
            {
                var stripe = Box("Hangar column hazard stripe", new Vector3(x, -7.6f + i * .24f, -7.19f),
                    new Vector3(.57f, .10f, .035f), i % 2 == 0 ? amber : dark);
                stripe.transform.localRotation = Quaternion.Euler(0, 0, -18);
            }
        }
        Box("Hangar overhead rear beam", new Vector3(0, -.85f, -7.5f), new Vector3(36, .4f, .7f), dark);
        for (int i = 0; i < 7; i++)
        {
            float x = -15 + i * 5;
            Box("Hangar amber wall luminaire", new Vector3(x, -1.65f, -7.1f), new Vector3(1.2f, .16f, .16f), amber);
            kit.PointLight("Hangar warm pool light", new Vector3(x, -2.5f, -3.5f), new Color(1, .76f, .52f), 1.4f, 11, Interior);
        }
        // The entire foreground is a walkable apron. The lift landing remains clear.
        for (int i = 0; i < 12; i++)
        {
            Box("Hangar pedestrian lane dash", new Vector3(-16 + i * 2.9f, FloorHeight + .012f, 4.9f),
                new Vector3(1.45f, .018f, .085f), cream);
            Box("Hangar apron guide light", new Vector3(-16 + i * 2.9f, FloorHeight + .025f, 5.7f),
                new Vector3(.22f, .035f, .12f), mint);
        }
        foreach (float z in new[] { -5.9f, .65f })
            Box("Hangar landing zone stripe", new Vector3(-6, FloorHeight + .012f, z), new Vector3(17.6f, .018f, .12f), cream);
        foreach (float x in new[] { -14.8f, 2.8f })
            Box("Hangar landing zone end", new Vector3(x, FloorHeight + .012f, -2.62f), new Vector3(.12f, .018f, 6.65f), amber);
        Caption("Hangar level lettering", "АНГАР / 00", new Vector3(10.5f, -2.0f, -7.92f), .60f);
        Caption("Hangar dock lettering", "КРОШКА / ДОК 07", new Vector3(-6, -1.5f, -7.64f), .36f);
        Caption("Hangar safety lettering", "ТОПЛИВО  •  ТЕХОТСЕК", new Vector3(10.8f, -3.15f, -7.91f), .22f);
        Caption("Hangar lift lettering", "ЛИФТ / В ЖИЛОЙ ОТСЕК", new Vector3(10.5f, -4.15f, -7.91f), .22f);

        Box("Hangar fuel station", new Vector3(11.5f, -7.15f, -5.8f), new Vector3(2.3f, 2.1f, 1.2f), steel);
        Box("Hangar fuel station inset", new Vector3(11.5f, -6.8f, -5.17f), new Vector3(1.7f, .8f, .05f), dark);
        Box("Hangar fuel station display", new Vector3(11.1f, -6.7f, -5.13f), new Vector3(.6f, .3f, .035f), mint);
        for (int i = 0; i < 3; i++)
        {
            float x = 8.1f + i * .65f;
            Shape("Hangar fuel canister", PrimitiveType.Cylinder, new Vector3(x, -7.3f, -6.1f),
                new Vector3(.48f, .9f, .48f), i == 1 ? amber : steel);
        }
        for (int i = 0; i < 4; i++)
        {
            var p = new Vector3(14.2f + (i % 2) * 1.35f, -7.65f + (i / 2) * 1.12f, -4.9f);
            Box("Hangar cargo crate", p, new Vector3(1.15f, 1.08f, 1.1f), i % 2 == 0 ? wall : concrete);
            Box("Hangar crate strap", p + Vector3.forward * .56f, new Vector3(.14f, 1.08f, .025f), brass);
        }
        var hosePoints = new[] { new Vector3(12.5f, -6.8f, -5.15f), new Vector3(13, -7.3f, -4.6f),
            new Vector3(12.7f, -8.13f, -3.9f), new Vector3(11.7f, -8.13f, -3.6f), new Vector3(11, -8.1f, -4.3f) };
        for (int i = 1; i < hosePoints.Length; i++)
            Tube("Hangar refuelling hose", hosePoints[i - 1], hosePoints[i], .055f, dark);
    }

    void BuildCraft()
    {
        Craft = new GameObject("Jade courier spacecraft").transform;
        Craft.SetParent(Interior, false);
        Craft.localPosition = new Vector3(-6, FloorHeight, -2.65f);
        BuildHull();

        // Windshield and dark oval windows sit on the near side of the enamel shell.
        Panel("Craft forward windshield", new[] { new Vector3(-6.8f, 3.48f, .85f), new Vector3(-5.75f, 4.05f, 1.17f),
            new Vector3(-4.6f, 4.05f, 1.58f), new Vector3(-4.5f, 3.65f, 1.79f) }, .065f, glass);
        Tube("Craft windshield warm divider", new Vector3(-5.55f, 3.55f, 1.51f),
            new Vector3(-5.45f, 4.06f, 1.29f), .045f, brass, Craft);
        for (int i = 0; i < 5; i++)
        {
            float x = -3.1f + i * 1.55f;
            var rim = Shape("Craft round porthole rim", PrimitiveType.Cylinder, new Vector3(x, 3.83f, 1.72f),
                new Vector3(.48f, .035f, .48f), brass, Craft);
            rim.transform.localRotation = Quaternion.Euler(73, 0, 0);
            var pane = Shape("Craft round porthole", PrimitiveType.Cylinder, new Vector3(x, 3.845f, 1.76f),
                new Vector3(.35f, .045f, .35f), glass, Craft);
            pane.transform.localRotation = Quaternion.Euler(73, 0, 0);
        }
        // Thick fins have their own silhouette and both sides, rather than flat single-sided planes.
        Panel("Craft upper tail fin", new[] { new Vector3(1.7f, 4.02f, 0), new Vector3(3.8f, 5.98f, 0),
            new Vector3(7.15f, 5.63f, 0), new Vector3(5.2f, 3.7f, 0) }, .20f, green);
        Tube("Craft tail red tip", new Vector3(6.78f, 5.63f, 0), new Vector3(7.15f, 5.63f, 0), .10f, red, Craft);
        for (int side = -1; side <= 1; side += 2)
        {
            Panel("Craft swept side fin", new[] { new Vector3(2.25f, 2.6f, side * 1.7f),
                new Vector3(3.45f, 1.4f, side * 3.25f), new Vector3(7.25f, 1.14f, side * 3.3f),
                new Vector3(5.2f, 2.45f, side * 1.2f) }, .19f, belly);
            Shape("Craft red wingtip", PrimitiveType.Sphere, new Vector3(7.1f, 1.16f, side * 3.27f),
                new Vector3(.35f, .22f, .22f), red, Craft);
        }
        var engine = Shape("Craft stern engine collar", PrimitiveType.Cylinder, new Vector3(6.5f, 2.95f, 0),
            new Vector3(1.75f, .45f, 1.75f), brass, Craft);
        engine.transform.localRotation = Quaternion.Euler(0, 0, 90);
        var nozzle = Shape("Craft recessed engine nozzle", PrimitiveType.Cylinder, new Vector3(6.98f, 2.95f, 0),
            new Vector3(1.38f, .10f, 1.38f), dark, Craft);
        nozzle.transform.localRotation = Quaternion.Euler(0, 0, 90);
        var core = Shape("Craft idle engine core", PrimitiveType.Cylinder, new Vector3(7.09f, 2.95f, 0),
            new Vector3(.61f, .025f, .61f), mint, Craft);
        core.transform.localRotation = Quaternion.Euler(0, 0, 90);
        for (int side = -1; side <= 1; side += 2)
        {
            foreach (float x in new[] { -3.9f, 3.8f })
            {
                Tube("Craft landing strut", new Vector3(x, 1.75f, side * 1.25f),
                    new Vector3(x + .35f, .37f, side * 1.85f), .115f, steel, Craft);
                Tube("Craft landing shock absorber", new Vector3(x + .07f, 1.46f, side * 1.4f),
                    new Vector3(x + .22f, .9f, side * 1.65f), .19f, brass, Craft);
                Shape("Craft landing pad", PrimitiveType.Cylinder, new Vector3(x + .35f, .12f, side * 1.85f),
                    new Vector3(.95f, .12f, .74f), brass, Craft);
            }
        }
        // A small boarding ramp remains behind the clear foreground pedestrian lane.
        var ramp = Box("Craft boarding ramp", new Vector3(-4.9f, .63f, 2.52f), new Vector3(1.1f, .13f, 1.45f), steel, Craft);
        ramp.transform.localRotation = Quaternion.Euler(-31, 0, 0);
        for (int i = 0; i < 5; i++)
            Box("Craft boarding ramp tread", new Vector3(-4.9f, .25f + i * .19f, 3.12f - i * .27f),
                new Vector3(.95f, .06f, .09f), cream, Craft);
        Box("Craft service hatch", new Vector3(3.65f, 3.62f, 1.71f), new Vector3(1.10f, .53f, .08f), belly, Craft);
        for (int i = 0; i < 5; i++)
            Box("Craft cooling slit", new Vector3(3.23f + i * .20f, 3.62f, 1.765f), new Vector3(.055f, .33f, .025f), dark, Craft);
        var dome = Shape("Craft navigation dome", PrimitiveType.Sphere, new Vector3(-.7f, 4.65f, 0),
            new Vector3(.9f, .30f, .8f), brass, Craft);
        Tube("Craft navigation aerial", new Vector3(-.7f, 4.75f, 0), new Vector3(-.7f, 5.2f, 0), .025f, steel, Craft);
        Shape("Craft navigation beacon", PrimitiveType.Sphere, new Vector3(-.7f, 5.2f, 0), new Vector3(.10f, .10f, .10f), amber, Craft);
    }

    void BuildHull()
    {
        // Sculpted longitudinal rings: narrow beak, full cabin, tapered engine stern.
        float[] x = { -7.6f, -7.1f, -6.2f, -4.8f, -2.5f, 0, 2.5f, 4.7f, 5.8f, 6.45f };
        float[] radiusY = { .08f, .42f, .77f, 1.15f, 1.43f, 1.50f, 1.38f, 1.12f, .82f, .65f };
        float[] radiusZ = { .10f, .65f, 1.13f, 1.60f, 1.95f, 2.05f, 1.90f, 1.54f, 1.04f, .74f };
        const int sides = 24;
        var vertices = new List<Vector3>();
        var triangles = new[] { new List<int>(), new List<int>(), new List<int>() };
        for (int ring = 0; ring < x.Length; ring++)
            for (int j = 0; j < sides; j++)
            {
                float angle = j * Mathf.PI * 2 / sides;
                vertices.Add(new Vector3(x[ring], 3.1f + Mathf.Sin(angle) * radiusY[ring], Mathf.Cos(angle) * radiusZ[ring]));
            }
        for (int ring = 0; ring < x.Length - 1; ring++)
            for (int j = 0; j < sides; j++)
            {
                float height = Mathf.Sin((j + .5f) * Mathf.PI * 2 / sides);
                int slot = Mathf.Abs(height) < .18f ? 2 : height > 0 ? 0 : 1;
                int a = ring * sides + j, b = ring * sides + (j + 1) % sides;
                int c = (ring + 1) * sides + j, d = (ring + 1) * sides + (j + 1) % sides;
                triangles[slot].AddRange(new[] { a, c, b, b, c, d });
            }
        vertices.Add(new Vector3(x[0], 3.1f, 0));
        vertices.Add(new Vector3(x[x.Length - 1], 3.1f, 0));
        for (int j = 0; j < sides; j++)
        {
            triangles[0].AddRange(new[] { vertices.Count - 2, j, (j + 1) % sides });
            int end = (x.Length - 1) * sides;
            triangles[1].AddRange(new[] { vertices.Count - 1, end + (j + 1) % sides, end + j });
        }
        var mesh = new Mesh { name = "Original sculpted courier hull mesh" };
        mesh.SetVertices(vertices);
        mesh.subMeshCount = 3;
        for (int i = 0; i < 3; i++) mesh.SetTriangles(triangles[i], i);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        MeshObject("Craft sculpted green hull", mesh, new[] { green, belly, red });
    }

    // A four-corner polygon extruded through Z, with two visible sides and an edge band.
    void Panel(string name, Vector3[] points, float thickness, Material material)
    {
        var vertices = new Vector3[8];
        for (int i = 0; i < 4; i++)
        {
            vertices[i] = points[i] + Vector3.forward * thickness * .5f;
            vertices[i + 4] = points[i] - Vector3.forward * thickness * .5f;
        }
        var indices = new List<int> { 0, 1, 2, 0, 2, 3, 6, 5, 4, 7, 6, 4 };
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;
            indices.AddRange(new[] { i, i + 4, next, next, i + 4, next + 4 });
        }
        if (Vector3.Cross(points[1] - points[0], points[2] - points[0]).z < 0)
            for (int i = 0; i < indices.Count; i += 3)
            {
                int previous = indices[i + 1];
                indices[i + 1] = indices[i + 2];
                indices[i + 2] = previous;
            }
        var mesh = new Mesh { name = name + " mesh", vertices = vertices, triangles = indices.ToArray() };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        MeshObject(name, mesh, new[] { material });
    }

    void MeshObject(string name, Mesh mesh, Material[] palette)
    {
        var item = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        item.transform.SetParent(Craft, false);
        item.GetComponent<MeshFilter>().sharedMesh = mesh;
        item.GetComponent<MeshRenderer>().sharedMaterials = palette;
        kit.Own(mesh);
    }

    void OnDestroy() => kit.Release();
}

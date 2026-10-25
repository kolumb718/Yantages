using UnityEngine;

// A tall brass aviary on the living deck where the owl rests and eats. Presentation plus the
// points OwlCompanion flies to: the doorway, the perch and the feeding bowl.
public sealed class OwlAviary : MonoBehaviour
{
    public static readonly Vector3 Centre = new Vector3(3.25f, 0, -3.6f);
    const float Radius = 1.05f, WallHeight = 2.15f, Bars = 22;
    // The open door faces the camera (+Z); its bars are swung aside.
    const float DoorHalfAngle = 19;

    readonly SceneKit kit = new SceneKit();
    Transform root;

    public Transform Root => root;
    public Vector3 Doorway => root.TransformPoint(new Vector3(0, 1.45f, Radius + .75f));
    public Vector3 Perch => root.TransformPoint(new Vector3(0, 1.32f, -.05f));
    public Vector3 Bowl => root.TransformPoint(new Vector3(.32f, 1.30f, .18f));

    public void Build(Transform ship)
    {
        root = new GameObject("Owl aviary").transform;
        root.SetParent(ship, false);
        root.localPosition = Centre;
        var brass = kit.Surface("Aviary brass", new Color(.72f, .53f, .27f), .55f, .65f);
        var timber = kit.Surface("Aviary perch wood", new Color(.42f, .27f, .15f), .2f);
        var base_ = kit.Surface("Aviary plinth", new Color(.20f, .23f, .27f), .3f, .2f);
        var sand = kit.Surface("Aviary floor straw", new Color(.78f, .64f, .40f), .05f);
        var ceramic = kit.Surface("Aviary ceramic bowl", new Color(.88f, .84f, .76f), .6f);
        var seeds = kit.Surface("Aviary seed mix", new Color(.62f, .38f, .18f), .15f);
        var water = kit.Surface("Aviary water", new Color(.35f, .70f, .82f), .9f);
        var glow = kit.Emissive("Aviary warm lamp", new Color(1, .78f, .52f), 1.6f);

        kit.Shape("Aviary plinth", PrimitiveType.Cylinder, new Vector3(0, .09f, 0), new Vector3(Radius * 2 + .2f, .09f, Radius * 2 + .2f), base_, root);
        kit.Shape("Aviary straw floor", PrimitiveType.Cylinder, new Vector3(0, .19f, 0), new Vector3(Radius * 2 - .06f, .015f, Radius * 2 - .06f), sand, root);
        foreach (float y in new[] { .2f, 1.1f, WallHeight })
            Ring("Aviary hoop", y, Radius, .028f, brass);
        for (int i = 0; i < Bars; i++)
        {
            float angle = i * 360f / Bars;
            // Bars facing the camera at the door are hinged open instead.
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 90)) < DoorHalfAngle) continue;
            var top = Around(angle, Radius, WallHeight);
            kit.Tube("Aviary bar", Around(angle, Radius, .2f), top, .014f, brass, root);
            // Dome ribs curve from the wall to the finial.
            var previous = top;
            for (int k = 1; k <= 4; k++)
            {
                float lift = k / 4f;
                var point = Around(angle, Radius * Mathf.Cos(lift * Mathf.PI * .5f), WallHeight + .75f * Mathf.Sin(lift * Mathf.PI * .5f));
                kit.Tube("Aviary dome rib", previous, point, .012f, brass, root);
                previous = point;
            }
        }
        BuildDoor(brass);
        kit.Shape("Aviary finial", PrimitiveType.Sphere, new Vector3(0, WallHeight + .8f, 0), Vector3.one * .12f, brass, root);
        kit.Tube("Aviary hanging ring", new Vector3(0, WallHeight + .86f, 0), new Vector3(0, WallHeight + 1.05f, 0), .02f, brass, root);

        // A branch perch across the middle and a feeding shelf beside it.
        kit.Tube("Aviary perch branch", new Vector3(-.8f, 1.25f, -.05f), new Vector3(.8f, 1.29f, -.05f), .045f, timber, root);
        kit.Tube("Aviary perch twig", new Vector3(.35f, 1.27f, -.05f), new Vector3(.55f, 1.45f, -.22f), .02f, timber, root);
        kit.Box("Aviary feeding shelf", new Vector3(.32f, 1.22f, .18f), new Vector3(.36f, .03f, .26f), timber, root);
        kit.Shape("Aviary seed bowl", PrimitiveType.Cylinder, new Vector3(.32f, 1.265f, .18f), new Vector3(.2f, .035f, .2f), ceramic, root);
        kit.Shape("Aviary seed heap", PrimitiveType.Sphere, new Vector3(.32f, 1.29f, .18f), new Vector3(.15f, .05f, .15f), seeds, root);
        kit.Shape("Aviary water dish", PrimitiveType.Cylinder, new Vector3(-.45f, .23f, .35f), new Vector3(.3f, .03f, .3f), ceramic, root);
        kit.Shape("Aviary water", PrimitiveType.Cylinder, new Vector3(-.45f, .25f, .35f), new Vector3(.25f, .008f, .25f), water, root);
        kit.Shape("Aviary nest lamp", PrimitiveType.Sphere, new Vector3(0, WallHeight + .55f, 0), Vector3.one * .14f, glow, root);
        kit.PointLight("Aviary nest light", new Vector3(0, WallHeight + .4f, 0), new Color(1, .76f, .52f), .8f, 3.2f, root);
        kit.Caption("Aviary plate", "ГНЕЗДО  /  СОВА", new Vector3(0, .12f, Radius + .11f), .085f, new Color(.95f, .82f, .61f), root);
    }

    void BuildDoor(Material brass)
    {
        // The door leaf hangs from a hinge on the left of the opening, swung outwards.
        float start = 90 + DoorHalfAngle;
        var hinge = new GameObject("Aviary open door").transform;
        hinge.SetParent(root, false);
        hinge.localPosition = Around(start, Radius, 0);
        hinge.localRotation = Quaternion.Euler(0, -70, 0);
        float width = 2 * Radius * Mathf.Sin(DoorHalfAngle * Mathf.Deg2Rad);
        for (int i = 0; i <= 4; i++)
        {
            float x = -width * i / 4;
            kit.Tube("Aviary door bar", new Vector3(x, .25f, 0), new Vector3(x, 1.85f, 0), .013f, brass, hinge);
        }
        foreach (float y in new[] { .25f, 1.05f, 1.85f })
            kit.Tube("Aviary door rail", new Vector3(0, y, 0), new Vector3(-width, y, 0), .016f, brass, hinge);
    }

    void Ring(string name, float y, float radius, float thickness, Material material)
    {
        const int segments = 28;
        for (int i = 0; i < segments; i++)
            kit.Tube(name, Around(i * 360f / segments, radius, y), Around((i + 1) * 360f / segments, radius, y), thickness, material, root);
    }

    // Angle measured from +X towards +Z, so 90° faces the camera.
    static Vector3 Around(float degrees, float radius, float y)
    {
        float a = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
    }

    void OnDestroy() => kit.Release();
}

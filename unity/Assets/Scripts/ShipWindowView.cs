using System.Collections.Generic;
using UnityEngine;

// Original procedural city diorama: self-lit scenery stays inside the window frame.
public sealed class ShipWindowView : MonoBehaviour
{
    bool built;
    readonly List<Transform> layers = new List<Transform>();
    readonly float[] depthWeights = { .16f, .075f, .025f, .075f, .04f };
    Vector3 referenceCamera;
    bool hasReference;

    // Parallax is zero when the camera stands at this world position (the whole-ship overview).
    public void SetReference(Vector3 cameraPosition)
    {
        if (!built) return;
        referenceCamera = windowRoot.InverseTransformPoint(cameraPosition);
        hasReference = true;
    }

    // Absolute camera offsets in ship space avoid drift on a return trip.
    // ShipCameraFraming calls this after placing the camera, so layers never lag a frame.
    public void UpdateView(Camera camera)
    {
        if (!built || camera == null) return;
        Vector3 position = windowRoot.InverseTransformPoint(camera.transform.position);
        if (!hasReference) { referenceCamera = position; hasReference = true; }
        Vector3 delta = position - referenceCamera;
        // Soft limits stay responsive even when inspecting the window from close up.
        delta = new Vector3(delta.x / Mathf.Sqrt(1 + delta.x * delta.x / 25),
            delta.y / Mathf.Sqrt(1 + delta.y * delta.y / 6.25f), 0);
        // Far scenery follows the eye more, so it travels less on screen than nearby docks.
        for (int i = 0; i < layers.Count; i++) layers[i].localPosition = delta * depthWeights[i];
        foreach (var material in materials)
            if (material.shader.name == "Orbita/WindowCity")
                material.SetMatrix("_WorldToWindow", windowRoot.worldToLocalMatrix);
    }

    readonly List<Material> materials = new List<Material>();
    Transform windowRoot;
    Texture2D skyTexture;
    Material skyMaterial;

    public void Build(Transform ship)
    {
        if (built) return;
        built = true;
        var root = new GameObject("Panoramic city window").transform;
        windowRoot = root;
        root.SetParent(ship, false);

        var far = Flat("Distant plum skyline", "675372");
        var middle = Flat("Midcity blue plum", "393B56");
        var front = Flat("Foreground dusk buildings", "252F41");
        var roof = Flat("Roof edges", "8A7487");
        var amber = Flat("Amber city windows", "D9AB78");
        var mint = Flat("Mint city windows", "81AEA6");
        var rose = Flat("Rose city beacons", "C58DA4");
        var darkWindow = Flat("Unlit city windows", "41465D");
        BuildSky(root);

        var distant = new Geometry(materials.Count);
        var city = new Geometry(materials.Count);
        var docks = new Geometry(materials.Count);
        var random = new System.Random(401);
        for (int i = 0; i < 28; i++)
        {
            float x = 3.18f + i * .345f;
            float height = .8f + (float)random.NextDouble() * 2.35f;
            float width = .22f + (float)random.NextDouble() * .17f;
            Building(distant, x, 1.53f, height, width, -4.65f, far, roof,
                amber, mint, darkWindow, random, false);
        }
        for (int i = 0; i < 19; i++)
        {
            float x = 3.25f + i * .50f;
            float height = .9f + (float)random.NextDouble() * 2.45f;
            if (x > 7.3f && x < 8.8f) height *= .65f;
            float width = .30f + (float)random.NextDouble() * .19f;
            Building(city, x, 1.53f, height, width, -4.48f, middle, roof,
                amber, mint, darkWindow, random, true);
        }
        for (int i = 0; i < 12; i++)
        {
            float x = 3.35f + i * .80f;
            float height = .42f + (float)random.NextDouble() * .67f;
            Building(docks, x, 1.53f, height, .57f, -4.30f, front, roof,
                amber, mint, darkWindow, random, false);
        }

        // A stepped landmark and its illuminated setbacks evoke a layered Asian skyline.
        var landmark = new Geometry(materials.Count);
        landmark.Box(new Vector3(8.18f, 2.45f, -4.40f), new Vector3(.76f, 1.82f, .065f), middle);
        for (int tier = 0; tier < 7; tier++)
        {
            float y = 3.42f + tier * .38f;
            float width = .69f - tier * .045f;
            landmark.Box(new Vector3(8.18f, y, -4.40f), new Vector3(width * .83f, .34f, .065f), middle);
            landmark.Box(new Vector3(8.18f, y - .17f, -4.355f), new Vector3(width, .045f, .05f), roof);
            landmark.Quad(8.18f - width * .27f, y - .10f, width * .14f, .15f, -4.358f, amber);
            landmark.Quad(8.18f + width * .13f, y - .10f, width * .14f, .15f, -4.358f, mint);
        }
        landmark.Box(new Vector3(8.18f, 6.12f, -4.40f), new Vector3(.24f, .46f, .06f), middle);
        landmark.Box(new Vector3(8.18f, 6.63f, -4.37f), new Vector3(.035f, .61f, .035f), rose);
        landmark.Box(new Vector3(8.18f, 6.95f, -4.35f), new Vector3(.055f, .055f, .03f), amber);

        // Elevated walkways, antenna masts and transport streaks give the city scale.
        var details = new Geometry(materials.Count);
        details.Box(new Vector3(5.43f, 2.82f, -4.36f), new Vector3(1.23f, .055f, .04f), roof);
        details.Box(new Vector3(10.31f, 3.10f, -4.36f), new Vector3(1.10f, .055f, .04f), roof);
        for (int i = 0; i < 11; i++)
        {
            float x = 3.6f + i * .82f;
            float y = 4.8f + Mathf.Sin(i * 2.4f) * .85f;
            details.Quad(x, y, .065f, .025f, -4.27f, i % 2 == 0 ? amber : mint);
            details.Quad(x - .13f, y + .004f, .11f, .012f, -4.275f, rose);
        }
        int frameMaterial = Flat("Window frame", "303A46");
        // Geometry grows the material slots only for this final mesh.
        var frame = new Geometry(materials.Count);
        frame.Box(new Vector3(3.06f, 4.5f, -4.205f), new Vector3(.12f, 6, .10f), frameMaterial);
        frame.Box(new Vector3(12.94f, 4.5f, -4.205f), new Vector3(.12f, 6, .10f), frameMaterial);
        frame.Box(new Vector3(8, 1.56f, -4.205f), new Vector3(9.88f, .12f, .10f), frameMaterial);
        frame.Box(new Vector3(8, 7.44f, -4.205f), new Vector3(9.88f, .12f, .10f), frameMaterial);
        frame.Box(new Vector3(6.45f, 4.5f, -4.205f), new Vector3(.055f, 5.8f, .075f), frameMaterial);
        frame.Box(new Vector3(10.20f, 4.5f, -4.205f), new Vector3(.055f, 5.8f, .075f), frameMaterial);

        layers.Add(distant.Create("Distant skyline", root, materials));
        layers.Add(city.Create("Midcity skyline", root, materials));
        layers.Add(docks.Create("Foreground docks", root, materials));
        layers.Add(landmark.Create("Stepped skyline landmark", root, materials));
        layers.Add(details.Create("City antennas and traffic lights", root, materials));
        frame.Create("Panoramic window frame", root, materials);
    }

    int Flat(string name, string hex)
    {
        Color color;
        ColorUtility.TryParseHtmlString("#" + hex, out color);
        var shader = Shader.Find(name == "Window frame" ? "Unlit/Color" : "Orbita/WindowCity");
        if (shader == null) throw new MissingReferenceException("Window material shader is unavailable: " + name);
        var material = new Material(shader) { name = name, color = color };
        materials.Add(material);
        return materials.Count - 1;
    }

    void BuildSky(Transform root)
    {
        const int width = 512, height = 256;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        skyTexture = texture;
        texture.name = "Original warm dusk gradient";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color[width * height];
        Color horizon = new Color(.70f, .43f, .43f);
        Color middle = new Color(.43f, .31f, .46f);
        Color zenith = new Color(.20f, .21f, .34f);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float t = y / (height - 1f);
                Color color = t < .45f ? Color.Lerp(horizon, middle, t / .45f)
                    : Color.Lerp(middle, zenith, (t - .45f) / .55f);
                float cloud = Mathf.PerlinNoise(x * .012f, y * .045f) * .025f;
                float haze = .035f * Mathf.Sin(t * 57 + x * .002f) * Mathf.Pow(1 - t, 2);
                pixels[y * width + x] = color + new Color(cloud + haze, cloud * .6f + haze, cloud * .8f + haze);
            }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        var shader = Shader.Find("Unlit/Texture");
        if (shader == null) throw new MissingReferenceException("Built-in Unlit/Texture shader is unavailable.");
        var material = new Material(shader) { name = "Muted dusk sky", mainTexture = texture };
        skyMaterial = material;
        var mesh = new Mesh { name = "Window sky quad" };
        mesh.vertices = new[] { new Vector3(3.08f, 1.58f, -4.775f), new Vector3(3.08f, 7.42f, -4.775f),
            new Vector3(12.92f, 7.42f, -4.775f), new Vector3(12.92f, 1.58f, -4.775f) };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        var sky = new GameObject("Muted sunset sky", typeof(MeshFilter), typeof(MeshRenderer));
        sky.transform.SetParent(root, false);
        sky.GetComponent<MeshFilter>().sharedMesh = mesh;
        sky.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    void OnDestroy()
    {
        if (windowRoot != null)
            foreach (var filter in windowRoot.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh != null) Destroy(filter.sharedMesh);
        foreach (var material in materials) if (material != null) Destroy(material);
        if (skyMaterial != null) Destroy(skyMaterial);
        if (skyTexture != null) Destroy(skyTexture);
    }

    static void Building(Geometry geometry, float x, float bottom, float height, float width, float z,
        int body, int roof, int amber, int mint, int darkWindow, System.Random random, bool antennas)
    {
        // Hidden overscan keeps the base of a vertically shifted skyline below the sill.
        bottom -= .45f;
        height += .45f;
        geometry.Box(new Vector3(x, bottom + height * .5f, z), new Vector3(width, height, .065f), body);
        geometry.Box(new Vector3(x, bottom + height, z + .008f), new Vector3(width * 1.05f, .045f, .08f), roof);
        if (random.Next(3) == 0)
            geometry.Box(new Vector3(x + width * .13f, bottom + height + .10f, z), new Vector3(width * .43f, .17f, .05f), body);
        if (antennas && random.Next(3) == 0)
            geometry.Box(new Vector3(x - width * .21f, bottom + height + .21f, z), new Vector3(.018f, .40f, .018f), roof);
        int columns = width > .37f ? 3 : 2;
        for (float y = bottom + .16f; y < bottom + height - .10f; y += .19f)
            for (int column = 0; column < columns; column++)
            {
                int pick = random.Next(7);
                int material = pick < 3 ? amber : pick == 3 ? mint : darkWindow;
                float windowWidth = width / (columns * 2.5f);
                float xx = x - width * .37f + column * width * .72f / (columns - 1);
                geometry.Quad(xx, y, windowWidth, .055f, z + .035f, material);
            }
    }

    // Shared material submeshes keep hundreds of windows from becoming GameObjects.
    sealed class Geometry
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int>[] triangles;
        public Geometry(int slots)
        {
            triangles = new List<int>[slots];
            for (int i = 0; i < slots; i++) triangles[i] = new List<int>();
        }

        public void Quad(float x, float y, float width, float height, float z, int material)
        {
            Face(new Vector3(x, y, z), new Vector3(x, y + height, z),
                new Vector3(x + width, y + height, z), new Vector3(x + width, y, z), material);
        }

        void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int material)
        {
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            triangles[material].AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
        }

        public void Box(Vector3 position, Vector3 size, int material)
        {
            Vector3 p = position - size * .5f, q = position + size * .5f;
            Face(new Vector3(p.x, p.y, q.z), new Vector3(p.x, q.y, q.z), new Vector3(q.x, q.y, q.z), new Vector3(q.x, p.y, q.z), material);
            Face(new Vector3(q.x, p.y, p.z), new Vector3(q.x, q.y, p.z), new Vector3(p.x, q.y, p.z), new Vector3(p.x, p.y, p.z), material);
            Face(new Vector3(p.x, p.y, p.z), new Vector3(p.x, q.y, p.z), new Vector3(p.x, q.y, q.z), new Vector3(p.x, p.y, q.z), material);
            Face(new Vector3(q.x, p.y, q.z), new Vector3(q.x, q.y, q.z), new Vector3(q.x, q.y, p.z), new Vector3(q.x, p.y, p.z), material);
            Face(new Vector3(p.x, q.y, q.z), new Vector3(p.x, q.y, p.z), new Vector3(q.x, q.y, p.z), new Vector3(q.x, q.y, q.z), material);
            Face(new Vector3(p.x, p.y, p.z), new Vector3(p.x, p.y, q.z), new Vector3(q.x, p.y, q.z), new Vector3(q.x, p.y, p.z), material);
        }

        public Transform Create(string name, Transform root, List<Material> materials)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.subMeshCount = triangles.Length;
            for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(root, false);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            // Earlier layers were built before the frame material was appended.
            var slots = new Material[triangles.Length];
            for (int i = 0; i < slots.Length; i++) slots[i] = materials[i];
            obj.GetComponent<MeshRenderer>().sharedMaterials = slots;
            return obj.transform;
        }
    }
}

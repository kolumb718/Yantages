using UnityEngine;

// Fit the ship's visible geometry to the viewport, including after a window resize.
public sealed class ShipCameraFraming : MonoBehaviour
{
    Camera view;
    Vector3[] corners;
    Vector3 centre;
    float lastAspect;
    Vector3 homeView, hangarView;
    Vector3[] hangarCorners;
    Vector3 hangarCentre;
    Transform passenger, hangar;
    GlassLiftVisuals lift;
    GameObject[] homeGroups;
    float lastBlend = -1;

    public void ConfigureHangar(Transform interior, Transform courier, GameObject[] home, GlassLiftVisuals visuals)
    {
        hangar = interior;
        passenger = courier;
        homeGroups = home;
        lift = visuals;
        var bounds = new Bounds(interior.TransformPoint(new Vector3(0, -4.4f, -1)), Vector3.zero);
        foreach (var renderer in interior.GetComponentsInChildren<Renderer>(true)) bounds.Encapsulate(renderer.bounds);
        hangarCentre = bounds.center;
        hangarCorners = new Vector3[8];
        int i = 0;
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                    hangarCorners[i++] = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x*2-1, y*2-1, z*2-1));
        Reframe();
    }

    public void Initialize(Camera camera, Transform ship)
    {
        view = camera;
        var renderers = ship.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        corners = new Vector3[renderers.Length * 8];
        int index = 0;
        foreach (var renderer in renderers)
        {
            var box = renderer.bounds;
            bounds.Encapsulate(box);
            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                    for (int z = 0; z < 2; z++)
                        corners[index++] = box.center + Vector3.Scale(box.extents,
                            new Vector3(x * 2 - 1, y * 2 - 1, z * 2 - 1));
        }
        centre = bounds.center;
        Reframe();
    }

    public void Reframe()
    {
        if (view == null || corners == null) return;
        homeView = Fit(corners, centre);
        if (hangarCorners != null) hangarView = Fit(hangarCorners, hangarCentre);
        lastAspect = view.aspect;
        lastBlend = -1;
        UpdateDeckView();
    }

    Vector3 Fit(Vector3[] points, Vector3 centrePoint)
    {
        float tangent = Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f);
        float aspect = Mathf.Max(.1f, view.aspect);
        var inverse = Quaternion.Inverse(view.transform.rotation);
        var target = centrePoint;
        for (int iteration = 0; iteration < 6; iteration++)
        {
            float distance = 0;
            foreach (var corner in points)
            {
                var point = inverse * (corner - target);
                // A small margin leaves room around the ship without wasting the screen.
                distance = Mathf.Max(distance, Mathf.Abs(point.x) * 1.06f / (tangent * aspect) - point.z,
                    Mathf.Abs(point.y) * 1.06f / tangent - point.z);
            }
            view.transform.position = target - view.transform.forward * distance;
            var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in points)
            {
                Vector2 point = view.WorldToViewportPoint(corner);
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }
            // Perspective makes the projected centre differ from the world bounds centre.
            var offset = (minimum + maximum) * .5f - Vector2.one * .5f;
            target += (view.transform.right * offset.x * aspect + view.transform.up * offset.y)
                * (2 * distance * tangent);
        }
        return view.transform.position;
    }

    public void UpdateDeckView()
    {
        float blend = passenger == null ? 0 : Mathf.SmoothStep(0, 1,
            Mathf.Clamp01(-passenger.position.y / -ShipLift.HangarHeight));
        if (Mathf.Abs(blend-lastBlend) > .00001f)
        {
            view.transform.position = Vector3.Lerp(homeView, hangarView, blend);
            lastBlend = blend;
        }
        if (hangar == null) return;
        bool underground = passenger.position.y < -.2f;
        hangar.gameObject.SetActive(underground);
        lift.Underground.gameObject.SetActive(underground);
        lift.Upper.gameObject.SetActive(passenger.position.y > -1.1f);
        // Cutaway presentation removes the upper floors once the passenger is below them.
        foreach (var group in homeGroups) group.SetActive(passenger.position.y > -1.1f);
    }

    void LateUpdate()
    {
        if (view != null && Mathf.Abs(view.aspect - lastAspect) > .001f) Reframe();
        else if (view != null) UpdateDeckView();
    }
}

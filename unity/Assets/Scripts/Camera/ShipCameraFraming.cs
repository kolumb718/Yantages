using UnityEngine;

// Fit the ship's visible geometry to the viewport, including after a window resize.
// Follow mode zooms into that frame and pans with a camera window: the view moves only
// when the courier pushes against the edges of an invisible box around the screen centre.
public sealed class ShipCameraFraming : MonoBehaviour
{
    // Zoomed camera distance as a fraction of the overview distance to the ship centre.
    const float FollowDistance = .58f;
    // Half-size of the camera window in normalised screen units (1 = screen edge).
    static readonly Vector2 Window = new Vector2(.30f, .24f);
    const float FocusHeight = 1.1f;
    const float PanSmoothing = .16f;
    const float ZoomSmoothing = .45f;

    Camera view;
    Vector3[] corners;
    Vector3 centre;
    float lastAspect;
    Vector3 homeView, hangarView;
    float homeDepth, hangarDepth;
    Vector3[] hangarCorners;
    Vector3 hangarCentre;
    Transform passenger, hangar;
    GlassLiftVisuals lift;
    GameObject[] homeGroups;
    ShipWindowView windowView;
    float zoom, zoomVelocity;
    Vector2 pan, windowPan, panVelocity;

    // C toggles between the camera window and the whole-ship overview.
    public bool Follow { get; set; } = true;

    // Whole-ship frame for the passenger's current height, with no follow zoom or pan.
    public Vector3 OverviewPosition => Vector3.Lerp(homeView, hangarView, HangarBlend());

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
        windowView = GetComponent<ShipWindowView>();
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
        homeDepth = Vector3.Dot(centre - homeView, view.transform.forward);
        if (hangarCorners != null)
        {
            hangarView = Fit(hangarCorners, hangarCentre);
            hangarDepth = Vector3.Dot(hangarCentre - hangarView, view.transform.forward);
        }
        else { hangarView = homeView; hangarDepth = homeDepth; }
        lastAspect = view.aspect;
        // Window parallax is measured from the whole-ship overview, not the follow position.
        if (windowView != null) windowView.SetReference(homeView);
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

    float HangarBlend() => passenger == null ? 0 : Mathf.SmoothStep(0, 1,
        Mathf.Clamp01(-passenger.position.y / -ShipLayout.HangarHeight));

    // Overview camera for the current floor plus how far the follow zoom may travel inwards.
    Vector3 BaseView(out float approach, out Vector2 limit)
    {
        float blend = HangarBlend();
        float depth = Mathf.Lerp(homeDepth, hangarDepth, blend);
        approach = depth * (1 - Mathf.Lerp(1, FollowDistance, zoom));
        // Panning never reveals more than the overview frame at the ship's depth.
        float tangent = Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f);
        limit = new Vector2(tangent * view.aspect, tangent) * approach;
        return Vector3.Lerp(homeView, hangarView, blend);
    }

    static float Push(float value, float edge) => value > edge ? value - edge : value < -edge ? value + edge : 0;

    void StepFollow(float deltaTime)
    {
        zoom = Mathf.SmoothDamp(zoom, Follow && passenger != null ? 1 : 0, ref zoomVelocity,
            ZoomSmoothing, Mathf.Infinity, deltaTime);
        var origin = BaseView(out float approach, out var limit);
        if (passenger != null)
        {
            var camera = view.transform;
            var position = origin + camera.forward * approach + camera.right * windowPan.x + camera.up * windowPan.y;
            var local = Quaternion.Inverse(camera.rotation) * (passenger.position + Vector3.up * FocusHeight - position);
            // Measured at the courier's own depth, so the push is exact for a sideways pan.
            float tangent = Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f) * Mathf.Max(.1f, local.z);
            windowPan.x += Push(local.x, Window.x * tangent * view.aspect);
            windowPan.y += Push(local.y, Window.y * tangent);
        }
        windowPan = Vector2.Max(-limit, Vector2.Min(limit, windowPan));
        pan = Vector2.SmoothDamp(pan, windowPan, ref panVelocity, PanSmoothing, Mathf.Infinity, deltaTime);
        pan = Vector2.Max(-limit, Vector2.Min(limit, pan));
    }

    public void UpdateDeckView()
    {
        if (view == null) return;
        var origin = BaseView(out float approach, out _);
        var camera = view.transform;
        camera.position = origin + camera.forward * approach + camera.right * pan.x + camera.up * pan.y;
        if (hangar == null || passenger == null) return;
        bool underground = passenger.position.y < -.2f;
        bool home = passenger.position.y > -1.1f;
        if (hangar.gameObject.activeSelf != underground)
        {
            hangar.gameObject.SetActive(underground);
            lift.Underground.gameObject.SetActive(underground);
        }
        if (lift.Upper.gameObject.activeSelf == home) return;
        lift.Upper.gameObject.SetActive(home);
        // Cutaway presentation removes the upper floors once the passenger is below them.
        foreach (var group in homeGroups) group.SetActive(home);
    }

    void LateUpdate()
    {
        if (view == null) return;
        if (Mathf.Abs(view.aspect - lastAspect) > .001f) Reframe();
        StepFollow(Time.deltaTime);
        UpdateDeckView();
        // Same-frame parallax: the city must read the final camera position, not last frame's.
        if (windowView != null) windowView.UpdateView(view);
    }
}

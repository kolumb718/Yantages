using UnityEngine;

// Fit the ship's visible geometry to the viewport, including after a window resize.
public sealed class ShipCameraFraming : MonoBehaviour
{
    Camera view;
    Vector3[] corners;
    Vector3 centre;
    float lastAspect;

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
        float tangent = Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f);
        float aspect = Mathf.Max(.1f, view.aspect);
        var inverse = Quaternion.Inverse(view.transform.rotation);
        var target = centre;
        for (int iteration = 0; iteration < 6; iteration++)
        {
            float distance = 0;
            foreach (var corner in corners)
            {
                var point = inverse * (corner - target);
                // A small margin leaves room around the ship without wasting the screen.
                distance = Mathf.Max(distance, Mathf.Abs(point.x) * 1.06f / (tangent * aspect) - point.z,
                    Mathf.Abs(point.y) * 1.06f / tangent - point.z);
            }
            view.transform.position = target - view.transform.forward * distance;
            var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in corners)
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
        lastAspect = view.aspect;
    }

    void LateUpdate()
    {
        if (view != null && Mathf.Abs(view.aspect - lastAspect) > .001f) Reframe();
    }
}

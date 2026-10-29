using System.Collections.Generic;
using UnityEngine;

// Third-person orbit and a separate cutaway overview, sharing deck visibility.
public sealed class ShipCameraFraming : MonoBehaviour
{
    Camera view;
    Transform passenger, hangar;
    GlassLiftVisuals lift;
    GameObject[] homeGroups;
    ShipWindowView windowView;
    Renderer[] occluders;
    Vector3[] homeCorners, hangarCorners;
    Vector3 homeCentre, hangarCentre, homeView, hangarView, velocity;
    Quaternion overviewRotation;
    float lastAspect, yaw = 180, pitch = 18, distance = ShipLayout.ThirdPersonDistance;
    bool placed, lastFollow;
    public bool Follow { get; set; } = true;
    public bool ThirdPerson => Follow;
    public Vector3 OverviewPosition => Vector3.Lerp(homeView, hangarView, HangarBlend());
    public float OrbitDistance => distance;

    static Vector3[] Corners(Bounds box)
    {
        var result = new Vector3[8];
        int i = 0;
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                    result[i++] = box.center + Vector3.Scale(box.extents,new Vector3(x*2-1,y*2-1,z*2-1));
        return result;
    }
    public void Initialize(Camera camera, Transform ship)
    {
        view = camera;
        overviewRotation = view.transform.rotation;
        view.nearClipPlane = .08f;
        windowView = GetComponent<ShipWindowView>();
        var renderers = ship.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        homeCorners = Corners(bounds); homeCentre = bounds.center;
        Reframe();
    }
    public void ConfigureHangar(Transform interior, Transform courier, GameObject[] home, GlassLiftVisuals visuals)
    {
        hangar = interior; passenger = courier; homeGroups = home; lift = visuals;
        var renderers = interior.GetComponentsInChildren<Renderer>(true);
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        hangarCentre = bounds.center; hangarCorners = Corners(bounds);
        var blockers = new List<Renderer>();
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is SkinnedMeshRenderer || renderer.GetComponent<TextMesh>() != null
                || (view.cullingMask & (1<<renderer.gameObject.layer))==0
                || renderer.transform.IsChildOf(courier) || renderer.transform.IsChildOf(visuals.Platform)
                || renderer.name.Contains("Glass")) continue;
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.renderQueue >= 3000) continue;
            blockers.Add(renderer);
        }
        occluders = blockers.ToArray();
        Reframe();
    }
    public void ApplyLook(Vector2 degrees, float zoom)
    {
        if (!Follow) return;
        yaw = Mathf.Repeat(yaw+degrees.x,360);
        pitch = Mathf.Clamp(pitch-degrees.y,-12,65);
        distance = Mathf.Clamp(distance+zoom,ShipLayout.ThirdPersonMinDistance,ShipLayout.ThirdPersonMaxDistance);
    }
    public void Reframe()
    {
        if (view == null || homeCorners == null) return;
        view.transform.rotation = overviewRotation; view.fieldOfView = 48;
        homeView = Fit(homeCorners,homeCentre);
        hangarView = hangarCorners == null ? homeView : Fit(hangarCorners,hangarCentre);
        lastAspect = view.aspect;
        if (windowView != null) windowView.SetReference(homeView);
        placed = false; UpdateDeckView();
    }
    Vector3 Fit(Vector3[] points, Vector3 centre)
    {
        float tangent = Mathf.Tan(view.fieldOfView*Mathf.Deg2Rad*.5f);
        var inverse = Quaternion.Inverse(overviewRotation);
        var target = centre;
        for (int iteration = 0; iteration < 6; iteration++)
        {
            float range = 0;
            foreach (var corner in points)
            {
                var p = inverse*(corner-target);
                range = Mathf.Max(range,Mathf.Abs(p.x)*1.06f/(tangent*view.aspect)-p.z,Mathf.Abs(p.y)*1.06f/tangent-p.z);
            }
            view.transform.position = target-view.transform.forward*range;
            var min = new Vector2(float.PositiveInfinity,float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            foreach (var corner in points)
            {
                Vector2 p = view.WorldToViewportPoint(corner);
                min = Vector2.Min(min,p); max = Vector2.Max(max,p);
            }
            var offset = (min+max)*.5f-Vector2.one*.5f;
            target += (view.transform.right*offset.x*view.aspect+view.transform.up*offset.y)*(2*range*tangent);
        }
        return view.transform.position;
    }
    float HangarBlend() => passenger == null ? 0 : Mathf.SmoothStep(0,1,Mathf.Clamp01(passenger.position.y/ShipLayout.HangarHeight));
    Vector3 ClearLine(Vector3 target, Vector3 desired)
    {
        var delta = desired-target;
        float range = delta.magnitude;
        if (range < .001f || occluders == null) return desired;
        var ray = new Ray(target,delta/range);
        float allowed = range;
        foreach (var renderer in occluders)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            var bounds = renderer.bounds; bounds.Expand(.18f);
            if (bounds.IntersectRay(ray,out float hit) && hit < allowed) allowed = Mathf.Max(.24f,hit-.12f);
        }
        return target+ray.direction*allowed;
    }
    void Visibility()
    {
        if (hangar == null || passenger == null) return;
        bool underground = passenger.position.y < -.2f, home = passenger.position.y > -1.1f;
        hangar.gameObject.SetActive(underground); lift.Underground.gameObject.SetActive(underground);
        lift.Upper.gameObject.SetActive(home);
        foreach (var group in homeGroups) group.SetActive(home);
    }
    void Place(float deltaTime, bool snap)
    {
        if (view == null) return;
        Visibility();
        if (Follow && passenger != null)
        {
            var rotation = Quaternion.Euler(pitch,yaw,0);
            var target = passenger.position+Vector3.up*ShipLayout.ThirdPersonFocusHeight;
            var desired = ClearLine(target,target-rotation*Vector3.forward*distance);
            var position = snap || !placed || lastFollow != Follow ? desired
                : Vector3.SmoothDamp(view.transform.position,desired,ref velocity,.08f,Mathf.Infinity,deltaTime);
            view.transform.position = ClearLine(target,position);
            view.transform.rotation = Quaternion.LookRotation(target-view.transform.position,Vector3.up);
            view.fieldOfView = 58;
        }
        else
        {
            view.transform.SetPositionAndRotation(OverviewPosition,overviewRotation);
            view.fieldOfView = 48; velocity = Vector3.zero;
        }
        placed = true; lastFollow = Follow;
    }
    public void UpdateDeckView() => Place(0,true);
    void LateUpdate()
    {
        if (view == null) return;
        if (Mathf.Abs(view.aspect-lastAspect) > .001f) Reframe();
        Place(Time.deltaTime,false);
        if (windowView != null) windowView.UpdateView(view);
    }
}

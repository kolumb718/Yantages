using System.Collections.Generic;
using UnityEngine;

// The owl's life aboard: it keeps Kristi company, roams the open air of both decks, rests and
// eats in its aviary, and boards the lift cabin with her. Flight is steered, never teleported.
public sealed class OwlCompanion : MonoBehaviour
{
    enum Mode { Follow, Roam, Hover, ToNest, Nest, Cabin }

    const float CruiseSpeed = 3.2f, FollowSpeed = 6.5f, BoardingSpeed = 9, Acceleration = 7;
    static readonly Vector3 CabinSeat = new Vector3(.45f, 1.05f, -.20f);
    // Open air of each level, above furniture tops and below the gallery or the roof lights.
    static readonly Bounds LowerAir = Box(new Vector3(-12.5f, 2.5f, -3.6f), new Vector3(12.5f, 3.3f, 4.2f));
    static readonly Bounds UpperAir = Box(new Vector3(-12.5f, 5.9f, -3.3f), new Vector3(1.0f, 6.5f, 2.4f));
    // Air in front of the gallery edge joins both levels; level changes pass through it.
    const float TransitZ = 4.0f, LowerTransitY = 2.8f, UpperTransitY = 5.8f, LevelSplit = 4.5f;
    static readonly Bounds[] Solid =
    {
        Box(new Vector3(-1.3f, -9, .35f), new Vector3(1.9f, 7, 3.25f)),  // glass lift shaft
        ShipLayout.EvaWardrobeAirObstacle,
    };

    Transform courier;
    OwlAviary aviary;
    Animator animator;
    readonly Queue<Vector3> route = new Queue<Vector3>();
    readonly System.Random random = new System.Random(7);
    Mode mode;
    float timer, restCooldown = 20, eatClock;
    Vector3 velocity;

    public bool CabinTravel { get; set; }
    public bool Resting => mode == Mode.Nest;
    public string Activity => mode.ToString();

    static Bounds Box(Vector3 min, Vector3 max)
    {
        var bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    public void Begin(Transform owner)
    {
        courier = owner;
        animator = GetComponentInChildren<Animator>();
        transform.SetPositionAndRotation(FollowPoint(), courier.rotation);
        mode = Mode.Follow;
        timer = 6;
    }

    public void UseAviary(OwlAviary nest) => aviary = nest;

    // Sends the owl to rest now (used by checks; the owl also decides this on its own).
    public void SendToNest()
    {
        if (aviary != null && !CabinTravel) GoToNest();
    }

    Vector3 FollowPoint() => courier.position - courier.forward * .65f + Vector3.right * .75f
        + Vector3.up * (1.05f + Mathf.Sin(Time.time * 2.2f) * .045f);

    float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (courier == null || dt <= 0) return;
        if (CabinTravel) { BoardCabin(dt); return; }
        if (mode == Mode.Cabin) { mode = Mode.Follow; timer = 4; }
        restCooldown -= dt;
        // Below deck the home is cut away; the owl stays with Kristi.
        bool home = courier.position.y > -1;
        if (!home && mode != Mode.Follow) { route.Clear(); mode = Mode.Follow; timer = 4; }

        switch (mode)
        {
            case Mode.Follow:
                Steer(FollowPoint(), FollowSpeed, dt);
                if ((timer -= dt) <= 0 && home) Decide();
                break;
            case Mode.Roam:
            case Mode.ToNest:
                if (route.Count > 0 && Steer(route.Peek(), CruiseSpeed, dt)) route.Dequeue();
                if (route.Count == 0) Arrive();
                break;
            case Mode.Hover:
                Hold(dt);
                if ((timer -= dt) <= 0) Decide();
                break;
            case Mode.Nest:
                Rest(dt);
                break;
        }
        if (animator != null) animator.speed = mode == Mode.Nest ? .3f : 1;
    }

    void Decide()
    {
        double roll = random.NextDouble();
        if (aviary != null && restCooldown <= 0 && roll < .35) GoToNest();
        else if (roll < .75) Roam(random.Next(2, 4));
        else { mode = Mode.Follow; timer = Range(5, 9); }
    }

    void Roam(int stops)
    {
        route.Clear();
        var from = transform.position;
        for (int i = 0; i < stops; i++)
        {
            var air = random.NextDouble() < .6 ? LowerAir : UpperAir;
            Vector3 point;
            int attempts = 0;
            do point = new Vector3(Range(air.min.x, air.max.x), Range(air.min.y, air.max.y), Range(air.min.z, air.max.z));
            while (Blocked(point) && ++attempts < 12);
            Plan(from, point);
            from = point;
        }
        mode = Mode.Roam;
    }

    void GoToNest()
    {
        route.Clear();
        var above = aviary.Doorway + new Vector3(0, 1.35f, .6f);
        Plan(transform.position, above);
        route.Enqueue(aviary.Doorway);
        route.Enqueue(aviary.Perch);
        mode = Mode.ToNest;
    }

    void Arrive()
    {
        if (mode == Mode.ToNest)
        {
            mode = Mode.Nest;
            timer = Range(9, 15);
            eatClock = 0;
            velocity = Vector3.zero;
            return;
        }
        mode = Mode.Hover;
        timer = Range(1.2f, 2.8f);
    }

    // Perched in the aviary: faces the open door and pecks at the seed bowl now and then.
    void Rest(float dt)
    {
        // Settle the last few centimetres onto the branch instead of snapping.
        transform.position = Vector3.MoveTowards(transform.position, aviary.Perch, 1.2f * dt);
        eatClock += dt;
        float peck = Mathf.Clamp01(Mathf.Sin(eatClock * Mathf.PI / 1.4f) * 3 - 2);
        var towardBowl = aviary.Bowl - aviary.Perch;
        towardBowl.y = 0;
        var facing = Quaternion.LookRotation(Vector3.Slerp(Vector3.forward, towardBowl.normalized, peck));
        transform.rotation = Quaternion.Slerp(transform.rotation, facing * Quaternion.Euler(28 * peck, 0, 0), dt * 6);
        if ((timer -= dt) > 0) return;
        restCooldown = Range(25, 40);
        route.Clear();
        route.Enqueue(aviary.Doorway);
        route.Enqueue(aviary.Doorway + new Vector3(0, 1.35f, .6f));
        mode = Mode.Roam;
    }

    // Waypoints from a to b: level changes go through the open air in front of the gallery,
    // and paths that would cut through the lift shaft or the wardrobe swing out in front of them.
    void Plan(Vector3 from, Vector3 to)
    {
        bool fromUpper = from.y > LevelSplit, toUpper = to.y > LevelSplit;
        if (fromUpper != toUpper)
        {
            route.Enqueue(new Vector3(from.x, fromUpper ? UpperTransitY : LowerTransitY, TransitZ));
            route.Enqueue(new Vector3(to.x, toUpper ? UpperTransitY : LowerTransitY, TransitZ));
        }
        else if (Crosses(from, to))
            route.Enqueue(new Vector3((from.x + to.x) * .5f, (from.y + to.y) * .5f, TransitZ));
        route.Enqueue(to);
    }

    static bool Blocked(Vector3 point)
    {
        foreach (var solid in Solid)
        {
            var padded = solid;
            padded.Expand(.8f);
            if (padded.Contains(point)) return true;
        }
        return false;
    }

    static bool Crosses(Vector3 from, Vector3 to)
    {
        for (int i = 1; i < 12; i++)
            if (Blocked(Vector3.Lerp(from, to, i / 12f))) return true;
        return false;
    }

    // Arrive-steering with banked turns; returns true within reach of the target.
    bool Steer(Vector3 target, float maxSpeed, float dt)
    {
        var offset = target - transform.position;
        float distance = offset.magnitude;
        var desired = distance > .001f ? offset / distance * Mathf.Min(maxSpeed, distance * 2.5f) : Vector3.zero;
        var previous = velocity;
        velocity = Vector3.MoveTowards(velocity, desired, Acceleration * dt);
        transform.position += velocity * dt;
        Face(velocity, (velocity - previous) / dt, dt);
        return distance < .25f;
    }

    void Hold(float dt)
    {
        velocity = Vector3.MoveTowards(velocity, Vector3.zero, Acceleration * dt);
        transform.position += velocity * dt + Vector3.up * Mathf.Cos(Time.time * 2.4f) * .05f * dt;
        var toCourier = courier.position - transform.position;
        Face(toCourier.normalized * .5f, Vector3.zero, dt);
    }

    void Face(Vector3 heading, Vector3 acceleration, float dt)
    {
        heading.y = 0;
        if (heading.sqrMagnitude < .04f) return;
        var yaw = Quaternion.LookRotation(heading);
        // Lean into turns like a bird, proportional to sideways acceleration.
        float bank = Mathf.Clamp(-Vector3.Dot(acceleration, yaw * Vector3.right) * 4, -30, 30);
        transform.rotation = Quaternion.Slerp(transform.rotation, yaw * Quaternion.Euler(0, 0, bank), dt * 5);
    }

    void BoardCabin(float dt)
    {
        var seat = courier.position + CabinSeat;
        route.Clear();
        velocity = Vector3.zero;
        if (mode != Mode.Cabin)
        {
            float distance = Vector3.Distance(transform.position, seat);
            transform.position = Vector3.MoveTowards(transform.position, seat, Mathf.Max(BoardingSpeed, distance * 6) * dt);
            if (Vector3.Distance(transform.position, seat) > .03f) { Face(seat - transform.position, Vector3.zero, dt); return; }
            mode = Mode.Cabin;
        }
        // Aboard: moves exactly with the platform.
        transform.SetPositionAndRotation(seat, courier.rotation);
    }
}

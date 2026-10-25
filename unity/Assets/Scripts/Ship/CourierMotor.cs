using System.Collections.Generic;
using UnityEngine;

// Walking on the current deck: floor rectangle, furniture footprints and the lift's glass.
public sealed class CourierMotor
{
    public const float WalkSpeed = 4, SprintSpeed = 6;
    public const float Radius = .22f;
    const float TurnDegreesPerSecond = 720;

    readonly ShipLift lift;
    readonly Dictionary<int, List<Rect>> obstacles = new Dictionary<int, List<Rect>>();
    Quaternion facing;

    public Transform Courier { get; }
    public int Deck { get; set; }

    public CourierMotor(Transform courier, ShipLift lift)
    {
        Courier = courier;
        this.lift = lift;
        facing = courier.rotation;
    }

    // Blocks the footprint of everything rendered under root; inset forgives overhanging leaves.
    public void AddObstacle(Transform root, float inset)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        var footprint = Rect.MinMaxRect(bounds.min.x + inset, bounds.min.z + inset,
            bounds.max.x - inset, bounds.max.z - inset);
        if (footprint.width <= 0 || footprint.height <= 0) return;
        int deck = ShipLayout.DeckAt(bounds.min.y + .05f);
        if (!obstacles.TryGetValue(deck, out var list)) obstacles.Add(deck, list = new List<Rect>());
        list.Add(footprint);
    }

    // World-space direction; a length below 1 (analogue stick) walks proportionally slower.
    public void Move(Vector3 direction, float distance)
    {
        if (lift != null && lift.IsRiding) return;
        direction.y = 0;
        direction = Vector3.ClampMagnitude(direction, 1);
        var from = Courier.position;
        var target = from + direction * distance;
        var area = ShipLayout.WalkArea(Deck);
        target.x = Mathf.Clamp(target.x, area.xMin, area.xMax);
        target.z = Mathf.Clamp(target.z, area.yMin, area.yMax);
        if (obstacles.TryGetValue(Deck, out var footprints)) target = FloorSweep.Move(from, target, footprints, Radius);
        Courier.position = lift != null ? lift.ConstrainMovement(from, target, Deck) : target;
        if (direction.sqrMagnitude > .0001f) facing = Quaternion.LookRotation(direction);
    }

    // Turning is eased separately so a quick tap does not snap the body around.
    public void Turn(float deltaTime)
    {
        Courier.rotation = Quaternion.RotateTowards(Courier.rotation, facing, TurnDegreesPerSecond * deltaTime);
    }
}

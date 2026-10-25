using System;
using UnityEngine;

public enum LiftPrompt { None, RidingToHangar, RidingToCabin, RidingToLounge, Busy, Call, EnterFromLeft, LoungeChoices, ToLounge }

// Three-stop lift: hangar (-1), living deck (0), cabin (1). Owns travel, passengers and the glass walls.
public sealed class ShipLift : MonoBehaviour
{
    public const float TravelSeconds = 6f;
    const float ClosingSeconds = .4f;
    const float OpeningSeconds = .3f;
    const float HalfWidth = 1.15f, HalfDepth = 1.05f;
    static Vector3 Centre => ShipLayout.LiftCentre;

    GlassLiftVisuals visuals;
    Transform courier, originalParent;
    CourierCharacters characters;
    float elapsed, startHeight;
    int origin, destination;
    bool carrying, cabinTravel;
    readonly Rect[] walls = new Rect[5];

    public bool IsMoving { get; private set; }
    public bool IsRiding => IsMoving && carrying;
    public int CurrentDeck { get; private set; }
    public float Progress => Mathf.Clamp01((elapsed-ClosingSeconds)/TravelSeconds);
    public Transform Platform => visuals.Platform;
    public GlassLiftVisuals Visuals => visuals;
    public int DestinationDeck => destination;
    public event Action<int> PassengerArrived;

    public void Build(Transform ship, Transform passenger, CourierCharacters presentation)
    {
        courier = passenger;
        characters = presentation;
        visuals = gameObject.AddComponent<GlassLiftVisuals>();
        visuals.Build(ship, Centre);
        visuals.SetIndicators(CurrentDeck, false);
        UpdateDoors();
    }

    public bool IsNear(Vector3 position, int deck)
    {
        var point = position-Centre;
        return Mathf.Abs(position.y-ShipLayout.FloorHeight(deck)) < .25f
            && Mathf.Abs(point.z) < .90f && point.x > -1.85f && point.x < .95f;
    }

    bool Inside(Vector3 position)
    {
        var point = position-Centre;
        return Mathf.Abs(point.x) <= HalfWidth-CourierMotor.Radius && Mathf.Abs(point.z) <= HalfDepth-CourierMotor.Radius;
    }

    public bool TryUse(int passengerDeck, int direction = 1)
    {
        if (passengerDeck < ShipLayout.HangarDeck || passengerDeck > ShipLayout.CabinDeck) return false;
        if (IsMoving || !IsNear(courier.position, passengerDeck)) return false;
        if (CurrentDeck != passengerDeck)
        {
            Begin(passengerDeck, false);
            return true;
        }
        if (!Inside(courier.position)) return false;
        Begin(passengerDeck == ShipLayout.LivingDeck ? (direction < 0 ? ShipLayout.HangarDeck : ShipLayout.CabinDeck)
            : ShipLayout.LivingDeck, true);
        return true;
    }

    void Begin(int target, bool withPassenger)
    {
        origin = CurrentDeck;
        destination = target;
        carrying = withPassenger;
        elapsed = 0;
        startHeight = Platform.localPosition.y;
        IsMoving = true;
        visuals.SetIndicators(CurrentDeck, true);
        SetCabinTravel(carrying);
        if (!carrying) return;
        originalParent = courier.parent;
        courier.SetParent(Platform, true);
        var local = courier.localPosition;
        courier.localPosition = new Vector3(Mathf.Clamp(local.x, -.45f, .45f), 0, Mathf.Clamp(local.z, -.40f, .40f));
    }

    void SetCabinTravel(bool active)
    {
        if (cabinTravel == active) return;
        cabinTravel = active;
        characters.SetLiftTravel(active);
    }

    void Update()
    {
        if (!IsMoving)
        {
            SetCabinTravel(Inside(courier.position) && Mathf.Abs(courier.position.y-ShipLayout.FloorHeight(CurrentDeck)) < .25f);
            return;
        }
        elapsed += Time.deltaTime;
        var position = Platform.localPosition;
        position.y = Mathf.Lerp(startHeight, ShipLayout.FloorHeight(destination), Mathf.SmoothStep(0, 1, Progress));
        Platform.localPosition = position;
        UpdateDoors();
        if (elapsed < ClosingSeconds+TravelSeconds+OpeningSeconds) return;

        CurrentDeck = destination;
        IsMoving = false;
        visuals.SetIndicators(CurrentDeck, false);
        UpdateDoors();
        if (carrying)
        {
            courier.SetParent(originalParent, true);
            var landed = courier.position;
            landed.y = ShipLayout.FloorHeight(CurrentDeck);
            courier.position = landed;
            PassengerArrived?.Invoke(CurrentDeck);
        }
        carrying = false;
    }

    // Doors slide shut before departure and open on arrival instead of snapping.
    float DoorOpening(int level)
    {
        if (!IsMoving) return level == CurrentDeck ? 1 : 0;
        if (level == origin && elapsed < ClosingSeconds) return 1-elapsed/ClosingSeconds;
        if (level == destination) return Mathf.Clamp01((elapsed-ClosingSeconds-TravelSeconds)/OpeningSeconds);
        return 0;
    }

    void UpdateDoors()
    {
        for (int level = ShipLayout.HangarDeck; level <= ShipLayout.CabinDeck; level++)
            visuals.SetDoor(level, Mathf.SmoothStep(0, 1, DoorOpening(level)));
    }

    public LiftPrompt Prompt(Vector3 position, int deck)
    {
        if (IsRiding) return destination == ShipLayout.HangarDeck ? LiftPrompt.RidingToHangar
            : destination == ShipLayout.CabinDeck ? LiftPrompt.RidingToCabin : LiftPrompt.RidingToLounge;
        if (!IsNear(position, deck)) return LiftPrompt.None;
        if (IsMoving) return LiftPrompt.Busy;
        if (CurrentDeck != deck) return LiftPrompt.Call;
        if (!Inside(position)) return LiftPrompt.EnterFromLeft;
        return deck == ShipLayout.LivingDeck ? LiftPrompt.LoungeChoices : LiftPrompt.ToLounge;
    }

    // Glass and closed doors as swept walls. The open doorway is on the left wall:
    // it joins the actual deck walkway.
    public Vector3 ConstrainMovement(Vector3 from, Vector3 to, int deck)
    {
        if (IsRiding) return from;
        bool open = !IsMoving && CurrentDeck == deck;
        float x0 = Centre.x-HalfWidth, x1 = Centre.x+HalfWidth;
        float z0 = Centre.z-HalfDepth, z1 = Centre.z+HalfDepth;
        const float wall = .045f;
        walls[0] = Rect.MinMaxRect(x0-wall, z0-wall, x1+wall, z0+wall);
        walls[1] = Rect.MinMaxRect(x0-wall, z1-wall, x1+wall, z1+wall);
        walls[2] = Rect.MinMaxRect(x1-wall, z0-wall, x1+wall, z1+wall);
        walls[3] = Rect.MinMaxRect(x0-wall, z0-wall, x0+wall, open ? Centre.z-.65f : z1+wall);
        walls[4] = Rect.MinMaxRect(x0-wall, open ? Centre.z+.65f : z1+wall, x0+wall, z1+wall);
        return FloorSweep.Move(from, to, walls, CourierMotor.Radius);
    }
}

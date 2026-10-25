using System;
using UnityEngine;

// Three-stop lift: hangar (-1), living deck (0), cabin (1).
public sealed class ShipLift : MonoBehaviour
{
    public const float DeckHeight = 4.1f;
    public const float HangarHeight = -8.2f;
    public static float FloorHeight(int deck) => deck < 0 ? HangarHeight : deck * DeckHeight;
    public const float TravelSeconds = 6f;
    const float ClosingSeconds = .4f;
    const float OpeningSeconds = .3f;
    static readonly Vector3 Centre = new Vector3(.3f, 0, 1.8f);
    const float HalfWidth = 1.15f, HalfDepth = 1.05f, Radius = .22f;
    GlassLiftVisuals visuals;
    Transform courier, originalParent;
    CourierCharacters characters;
    float elapsed, startHeight;
    int destination;
    bool carrying;

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
        visuals.SetState(0, false, true);
    }

    public bool IsNear(Vector3 position, int deck)
    {
        var point = position-Centre;
        return Mathf.Abs(position.y-FloorHeight(deck)) < .25f
            && Mathf.Abs(point.z) < .90f && point.x > -1.85f && point.x < .95f;
    }

    bool Inside(Vector3 position)
    {
        var point = position-Centre;
        return Mathf.Abs(point.x) <= HalfWidth-Radius && Mathf.Abs(point.z) <= HalfDepth-Radius;
    }

    public bool TryUse(int passengerDeck, int direction = 1)
    {
        if (passengerDeck < -1 || passengerDeck > 1) return false;
        if (IsMoving || !IsNear(courier.position, passengerDeck)) return false;
        if (CurrentDeck != passengerDeck)
        {
            Begin(passengerDeck, false);
            return true;
        }
        if (!Inside(courier.position)) return false;
        Begin(passengerDeck == 0 ? (direction < 0 ? -1 : 1) : 0, true);
        return true;
    }

    void Begin(int target, bool withPassenger)
    {
        destination = target;
        carrying = withPassenger;
        elapsed = 0;
        startHeight = Platform.localPosition.y;
        IsMoving = true;
        visuals.SetState(CurrentDeck, true, false);
        characters.SetLiftTravel(carrying);
        if (!carrying) return;
        originalParent = courier.parent;
        courier.SetParent(Platform, true);
        var local = courier.localPosition;
        courier.localPosition = new Vector3(Mathf.Clamp(local.x, -.45f, .45f), 0, Mathf.Clamp(local.z, -.40f, .40f));
        characters.SetLiftTravel(true);
    }

    void Update()
    {
        if (!IsMoving)
        {
            characters.SetLiftTravel(Inside(courier.position) && Mathf.Abs(courier.position.y-FloorHeight(CurrentDeck)) < .25f);
            return;
        }
        elapsed += Time.deltaTime;
        float blend = Mathf.SmoothStep(0, 1, Progress);
        var position = Platform.localPosition;
        position.y = Mathf.Lerp(startHeight, FloorHeight(destination), blend);
        Platform.localPosition = position;
        if (elapsed < ClosingSeconds+TravelSeconds+OpeningSeconds) return;

        CurrentDeck = destination;
        IsMoving = false;
        visuals.SetState(CurrentDeck, false, true);
        if (carrying)
        {
            courier.SetParent(originalParent, true);
            var landed = courier.position;
            landed.y = FloorHeight(CurrentDeck);
            courier.position = landed;
            characters.SetLiftTravel(true);
            PassengerArrived?.Invoke(CurrentDeck);
        }
        carrying = false;
    }

    public string Hint(Vector3 position, int deck)
    {
        if (IsRiding) return "ЛИФТ · " + (destination == -1 ? "В АНГАР" : destination == 1 ? "В КАЮТУ" : "В ГОСТИНУЮ");
        if (!IsNear(position, deck)) return "";
        if (IsMoving) return "ЛИФТ · ДВИЖЕТСЯ";
        if (CurrentDeck != deck) return "E · Вызвать лифт";
        if (!Inside(position)) return "Зайдите в лифт через левую дверь";
        return deck == 0 ? "E · В каюту     R · В ангар" : "E · В гостиную";
    }

    // The prototype uses explicit movement, so glass and closed doors need swept bounds.
    // The open doorway is on the left wall: it joins the actual upper deck walkway.
    public Vector3 ConstrainMovement(Vector3 from, Vector3 to, int deck)
    {
        if (IsRiding) return from;
        bool open = !IsMoving && CurrentDeck == deck;
        float x0 = Centre.x-HalfWidth, x1 = Centre.x+HalfWidth;
        float z0 = Centre.z-HalfDepth, z1 = Centre.z+HalfDepth;
        const float wall = .045f;
        var walls = new[]
        {
            Rect.MinMaxRect(x0-wall, z0-wall, x1+wall, z0+wall),
            Rect.MinMaxRect(x0-wall, z1-wall, x1+wall, z1+wall),
            Rect.MinMaxRect(x1-wall, z0-wall, x1+wall, z1+wall),
            Rect.MinMaxRect(x0-wall, z0-wall, x0+wall, open ? Centre.z-.65f : z1+wall),
            Rect.MinMaxRect(x0-wall, open ? Centre.z+.65f : z1+wall, x0+wall, z1+wall),
        };
        var result = from;
        result.x = SweepAxis(from.x, to.x, from.z, walls, true);
        result.z = SweepAxis(from.z, to.z, result.x, walls, false);
        result.y = to.y;
        return result;
    }

    static float SweepAxis(float from, float to, float cross, Rect[] walls, bool horizontal)
    {
        float result = to;
        foreach (var wall in walls)
        {
            if (wall.width <= 0 || wall.height <= 0) continue;
            float crossMin = (horizontal ? wall.yMin : wall.xMin)-Radius;
            float crossMax = (horizontal ? wall.yMax : wall.xMax)+Radius;
            if (cross <= crossMin || cross >= crossMax) continue;
            float minimum = (horizontal ? wall.xMin : wall.yMin)-Radius;
            float maximum = (horizontal ? wall.xMax : wall.yMax)+Radius;
            if (to > from && from <= minimum && result > minimum) result = minimum;
            if (to < from && from >= maximum && result < maximum) result = maximum;
        }
        return result;
    }
}

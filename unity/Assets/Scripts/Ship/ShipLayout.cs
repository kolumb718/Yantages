using UnityEngine;

// Shared dimensions of the cutaway ship. Movement, lift, camera and scenery read the same numbers.
public static class ShipLayout
{
    public const int HangarDeck = -1, LivingDeck = 0, CabinDeck = 1;
    public const float DeckHeight = 4.1f;
    public const float HangarHeight = -8.2f;
    // One Unity unit is one metre. Household objects share this human scale.
    public const float CourierHeight = 1.80f;
    public const float ThirdPersonDistance = 3.5f, ThirdPersonMinDistance = .75f, ThirdPersonMaxDistance = 6f;
    public const float ThirdPersonFocusHeight = 1.25f;
    public static readonly Vector3 BedSize = new Vector3(1.60f, 1.05f, 2.15f);
    public static readonly Vector3 SofaSize = new Vector3(2.20f, .85f, .95f);
    public static readonly Vector3 BookshelfSize = new Vector3(1.15f, 2.00f, .40f);
    public const float MonsteraHeight = 1.35f, SnakePlantHeight = 1.10f, PothosHeight = .45f;
    public const float WardrobeHeight = 2.50f, EvaSuitHeight = 1.95f;
    public static readonly Vector3 CoffeeTableSize = new Vector3(1.10f, .06f, .60f);
    public const float CoffeeTableTop = .48f, GalleyWorktopHeight = .95f;
    public static readonly Vector3 GalleySize = new Vector3(2.40f, .88f, .65f);
    public const float TerminalHeight = 1.60f, TerminalScreenHeight = 1.35f;
    public static readonly Vector3 TerminalBodySize = new Vector3(.70f, 1.05f, .45f);
    public const float ArmchairHeight = .90f;
    public const float WindowLeft = 3f, WindowRight = 13f, WindowBottom = 0f, WindowHeight = 5f;
    public const float WindowTop = WindowBottom + WindowHeight;
    public static Vector4 WindowAperture => new Vector4(WindowLeft + .12f, WindowBottom + .10f,
        WindowRight - .12f, WindowTop - .10f);
    // The authored diorama occupied Y=1.5..7.5; remap it as one coherent composition.
    public static float WindowY(float authoredY) => WindowBottom + (authoredY - 1.5f) * (WindowHeight / 6f);
    public static readonly Vector3 CourierStart = new Vector3(-4, 0, .6f);
    public static readonly Vector3 LiftCentre = new Vector3(.3f, 0, 1.8f);
    public static readonly Vector3 TerminalPosition = new Vector3(.4f, 0, -.6f);
    public const float TerminalReach = 1.35f;

    public static float FloorHeight(int deck) => deck < 0 ? HangarHeight : deck * DeckHeight;

    public static int DeckAt(float height) =>
        height < HangarHeight * .5f ? HangarDeck : height > DeckHeight * .5f ? CabinDeck : LivingDeck;

    // Walkable floor as (x, z) per deck: the whole visible floor inside the hull walls. Furniture,
    // the parked craft and the lift shaft block movement as obstacles, not as invisible limits.
    public static Rect WalkArea(int deck) =>
        deck < 0 ? Rect.MinMaxRect(-17.2f, -7.2f, 17.2f, 5.4f)
        : deck == CabinDeck ? Rect.MinMaxRect(-13.2f, -4.4f, 1.55f, 2.75f)
        : Rect.MinMaxRect(-13.2f, -4.55f, 13.2f, 4.4f);
}

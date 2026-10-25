using UnityEngine;

// Shared dimensions of the cutaway ship. Movement, lift, camera and scenery read the same numbers.
public static class ShipLayout
{
    public const int HangarDeck = -1, LivingDeck = 0, CabinDeck = 1;
    public const float DeckHeight = 4.1f;
    public const float HangarHeight = -8.2f;
    public static readonly Vector3 CourierStart = new Vector3(-4, 0, .6f);
    public static readonly Vector3 LiftCentre = new Vector3(.3f, 0, 1.8f);
    public static readonly Vector3 TerminalPosition = new Vector3(.4f, 0, -.6f);
    public const float TerminalReach = 2;

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

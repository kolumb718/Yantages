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

    // Walkable floor as (x, z) per deck; the hangar promenade stays clear of the parked craft.
    public static Rect WalkArea(int deck) =>
        deck < 0 ? Rect.MinMaxRect(-17.2f, 1, 17.2f, 5.4f)
        : deck == CabinDeck ? Rect.MinMaxRect(-13.2f, -.8f, 1, 2.5f)
        : Rect.MinMaxRect(-13.2f, -.8f, 13.2f, 4.4f);
}

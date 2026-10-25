using UnityEngine;

public sealed class DeliveryOrder
{
    public string Title, Route, Client, Cargo, Note, NextStep;
    public int Reward;

    public static readonly DeliveryOrder NeptuneSoup = new DeliveryOrder
    {
        Title = "Ужин на краю Солнечной системы",
        Route = "ЗЕМЛЯ → НЕПТУН",
        Client = "Мира · станция «Тритон»",
        Cargo = "Горячий суп с лунными грибами",
        Reward = 240,
        Note = "«Не переворачивать. Суп живой.\nИ захватите ложку».",
        NextStep = "Забрать суп на Земле",
    };
}

// The order terminal on the living deck. The HUD reads this state; input changes it.
public sealed class OrderTerminal
{
    public DeliveryOrder Order { get; } = DeliveryOrder.NeptuneSoup;
    public bool IsOpen { get; private set; }
    public bool Accepted { get; private set; }

    public static bool InReach(Vector3 position, int deck) =>
        deck == ShipLayout.LivingDeck && Vector3.Distance(position, ShipLayout.TerminalPosition) < ShipLayout.TerminalReach;

    public void Open() => IsOpen = true;
    public void Close() => IsOpen = false;
    public void Accept() => Accepted = true;
}

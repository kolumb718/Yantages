using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class AtlasPlanet
{
    public string id, name, subtitle, climate, population, description, history, inhabitants, flora, fauna, cuisine;
    public string color, ocean, land;
    public float x, y;
    public int style;
    public bool rings;
    public DeliveryOrder order;
    public Color Colour => ColorUtility.TryParseHtmlString(color,out var value) ? value : Color.white;
    public string Section(int index) => index switch {
        0 => description+"\n\nКлимат: "+climate+"\n\nПоселения: "+population,
        1 => history,
        2 => inhabitants,
        3 => flora,
        4 => fauna,
        _ => cuisine,
    };
}

// Authorable fiction and delivery data are packaged as a Resources TextAsset, including in players.
public sealed class GalaxyCatalog
{
    [Serializable] sealed class FileData { public AtlasPlanet[] planets; }
    readonly AtlasPlanet[] planets;
    public int Count => planets.Length;
    public AtlasPlanet this[int index] => planets[index];
    public GalaxyCatalog()
    {
        var source=Resources.Load<TextAsset>("Galaxy/planets");
        if(source==null) throw new MissingReferenceException("Missing galaxy planet catalog.");
        planets=JsonUtility.FromJson<FileData>(source.text)?.planets;
        if(planets==null || planets.Length==0) throw new InvalidOperationException("Empty galaxy catalog.");
        var ids=new HashSet<string>();
        foreach(var planet in planets)
        {
            if(planet==null || string.IsNullOrEmpty(planet.id) || !ids.Add(planet.id)
                || string.IsNullOrEmpty(planet.name) || string.IsNullOrEmpty(planet.history)
                || string.IsNullOrEmpty(planet.inhabitants) || string.IsNullOrEmpty(planet.flora)
                || string.IsNullOrEmpty(planet.fauna) || planet.order==null)
                throw new InvalidOperationException("Incomplete or duplicate planet record: "+planet?.id);
            // The first delivery remains the authoritative existing object, without a second copy.
            if(planet.id=="neptune") planet.order=DeliveryOrder.NeptuneSoup;
            planet.order.PlanetId=planet.id;
        }
    }
    public int IndexOf(string id) => Array.FindIndex(planets,p=>p.id==id);
}

// Atlas navigation and the shared order state; drawing and pointer hit boxes live in GalaxyMapView.
public sealed class GalaxyAtlas
{
    public static readonly string[] Tabs={"Облик","История","Жители","Флора","Фауна","Заказ"};
    readonly OrderTerminal terminal;
    bool held;
    public GalaxyCatalog Catalog { get; } = new GalaxyCatalog();
    public bool IsOpen { get; private set; }
    public bool ShowingPlanet { get; private set; }
    public int Selected { get; private set; }
    public int Tab { get; private set; }
    public int Revision { get; private set; }
    public AtlasPlanet Planet => Catalog[Selected];
    public OrderTerminal Terminal => terminal;
    public GalaxyAtlas(OrderTerminal orders) { terminal=orders; Selected=Mathf.Max(0,Catalog.IndexOf("neptune")); }
    public static bool InReach(Vector3 position,int deck) => deck==ShipLayout.LivingDeck
        && Vector3.Distance(position,new Vector3(ShipLayout.HologramCentre.x,0,ShipLayout.HologramCentre.z))<=ShipLayout.HologramReach;
    public static bool HitsHologram(Ray ray)
    {
        var to=ShipLayout.HologramCentre-ray.origin;
        float along=Vector3.Dot(to,ray.direction);
        return along>0 && (to-ray.direction*along).sqrMagnitude<=ShipLayout.HologramPickRadius*ShipLayout.HologramPickRadius;
    }
    public void Open()
    {
        IsOpen=true; ShowingPlanet=false; Tab=0; held=false;
        int index=Catalog.IndexOf(terminal.Order.PlanetId);
        if(index>=0) Selected=index;
        Revision++;
    }
    public void Close() { IsOpen=false; Revision++; }
    public void Back() { if(ShowingPlanet){ShowingPlanet=false;Revision++;} else Close(); }
    public bool OpenPlanet(int index)
    {
        if(!IsOpen || index<0 || index>=Catalog.Count) return false;
        Selected=index; ShowingPlanet=true; Tab=0; Revision++; return true;
    }
    public bool SelectTab(int index)
    {
        if(!IsOpen || !ShowingPlanet || index<0 || index>=Tabs.Length) return false;
        Tab=index; Revision++; return true;
    }
    public void Navigate(Vector2 axis)
    {
        if(axis.sqrMagnitude<.16f){held=false;return;}
        if(!IsOpen || held)return;
        held=true;
        int step=(Mathf.Abs(axis.x)>=Mathf.Abs(axis.y)?axis.x:-axis.y)>0?1:-1;
        if(ShowingPlanet) Tab=(Tab+step+Tabs.Length)%Tabs.Length;
        else Selected=(Selected+step+Catalog.Count)%Catalog.Count;
        Revision++;
    }
    public bool AcceptOrder()
    {
        if(!IsOpen || !ShowingPlanet || Tab!=5) return false;
        bool accepted=terminal.TryAccept(Planet.order);
        Revision++;
        return accepted;
    }
    public void Interact()
    {
        if(!ShowingPlanet)OpenPlanet(Selected);
        else if(Tab!=5)SelectTab(5);
        else AcceptOrder();
    }
}

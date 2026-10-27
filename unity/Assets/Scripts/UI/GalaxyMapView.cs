using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// A real orthographic overlay camera renders the atlas, including all text and controls.
// The same camera can be captured in batch checks; there is no second mock-up renderer.
public sealed class GalaxyMapView : MonoBehaviour
{
    readonly SceneKit kit=new SceneKit();
    readonly List<(Rect area,Action action)> buttons=new List<(Rect,Action)>();
    readonly List<Transform> globes=new List<Transform>();
    readonly Mesh[] starMeshes=new Mesh[3];
    static readonly Vector3 Origin=new Vector3(1000,1000,0);
    static readonly Color Ink=new Color(.96f,.91f,.80f), Muted=new Color(.60f,.65f,.69f),
        Gold=new Color(.91f,.68f,.39f), Mint=new Color(.54f,.83f,.71f);
    GalaxyAtlas atlas;
    CourierInput input;
    Transform root,page;
    Camera gameCamera;
    int originalMask,revision=-1;
    Material ink,muted,gold,mint,panel,edge,background;
    Material[] worlds;
    public Camera ViewCamera {get;private set;}
    public bool Visible => ViewCamera!=null && ViewCamera.enabled;
    public GalaxyAtlas Atlas => atlas;

    public void Initialize(GalaxyAtlas model,CourierInput controls,Camera sceneCamera)
    {
        atlas=model; input=controls; gameCamera=sceneCamera; originalMask=gameCamera.cullingMask;
        gameCamera.cullingMask &= ~(1<<5);
        root=new GameObject("Galactic delivery atlas").transform;
        root.position=Origin;
        ink=kit.Unlit("Atlas ivory",Ink); muted=kit.Unlit("Atlas muted",Muted);
        gold=kit.Unlit("Atlas gold",Gold); mint=kit.Unlit("Atlas mint",Mint);
        panel=kit.Unlit("Atlas cards",new Color(.065f,.085f,.12f));
        edge=kit.Unlit("Atlas edge",new Color(.16f,.21f,.25f));
        background=kit.Unlit("Atlas selected",new Color(.14f,.23f,.24f));
        var camera=new GameObject("Galactic atlas camera");
        camera.transform.SetParent(root,false); camera.transform.localPosition=new Vector3(0,0,-25);
        ViewCamera=camera.AddComponent<Camera>();
        ViewCamera.orthographic=true; ViewCamera.orthographicSize=5.2f;
        ViewCamera.nearClipPlane=.1f; ViewCamera.farClipPlane=60;
        ViewCamera.cullingMask=1<<5; ViewCamera.depth=sceneCamera.depth+100;
        ViewCamera.clearFlags=CameraClearFlags.SolidColor;
        ViewCamera.backgroundColor=new Color(.022f,.031f,.055f);
        ViewCamera.enabled=false;
        worlds=new Material[atlas.Catalog.Count];
        var shader=ShipShaders.GalaxyPlanet;
        if(shader==null || !shader.isSupported)throw new MissingReferenceException("Galaxy planet shader unavailable.");
        for(int i=0;i<worlds.Length;i++)
        {
            var planet=atlas.Catalog[i];
            var material=kit.Custom("Atlas world / "+planet.id,shader);
            ColorUtility.TryParseHtmlString(planet.ocean,out var ocean);
            ColorUtility.TryParseHtmlString(planet.land,out var land);
            material.SetColor("_Ocean",ocean); material.SetColor("_Land",land);
            material.SetFloat("_Style",planet.style); material.SetFloat("_Seed",i*2.37f);
            worlds[i]=material;
        }
    }

    void LateUpdate()
    {
        if(atlas==null)return;
        ViewCamera.enabled=atlas.IsOpen;
        ViewCamera.orthographicSize=Mathf.Max(5.2f,8.3f/Mathf.Max(.1f,ViewCamera.aspect));
        if(!atlas.IsOpen)return;
        if(revision!=atlas.Revision) Refresh();
        foreach(var globe in globes) globe.Rotate(Vector3.up,Time.unscaledDeltaTime*4,Space.Self);
    }

    public void Refresh()
    {
        if(page!=null){page.gameObject.SetActive(false);Destroy(page.gameObject);}
        page=new GameObject("Atlas page").transform; page.SetParent(root,false);
        buttons.Clear(); globes.Clear(); revision=atlas.Revision;
        Label("Atlas title","ОРБИТА / АТЛАС ДОСТАВОК",-7.65f,4.8f,.23f,Gold);
        Label("Atlas subtitle","СЕКТОР МАЛЫХ ОГНЕЙ  ·  "+atlas.Catalog.Count+" МИРОВ",-7.65f,4.40f,.13f,Muted);
        Button("Закрыть",new Rect(6.5f,4.35f,1.15f,.45f),atlas.Close);
        Box("Atlas right panel",new Rect(.35f,-4.45f,7.3f,8.52f),panel,.2f);
        if(atlas.ShowingPlanet) DrawPlanet(); else DrawGalaxy();
        string nav=input.UsingGamepad?"L-стик — выбор":"WASD / стрелки — выбор";
        Label("Atlas footer",nav+"    "+input.Label(CourierAction.Interact)+" — открыть / заказ    "
            +input.Label(CourierAction.Cancel)+" — назад",-7.65f,-4.77f,.135f,Muted);
        foreach(var transform in root.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=5;
    }

    public void HandleInput(CourierInput controls)
    {
        atlas.Navigate(controls.Move);
        if(controls.Pressed(CourierAction.Cancel)){atlas.Back();return;}
        if(controls.Pressed(CourierAction.Interact))atlas.Interact();
        if(controls.ClickPressed) Click(controls.PointerPosition);
    }

    public bool Click(Vector2 screenPosition)
    {
        if(!atlas.IsOpen || ViewCamera==null)return false;
        var ray=ViewCamera.ScreenPointToRay(screenPosition);
        if(Mathf.Abs(ray.direction.z)<.001f)return false;
        var local=ray.origin+ray.direction*((Origin.z-ray.origin.z)/ray.direction.z)-Origin;
        // Reverse order lets the topmost visual button own overlapping hit boxes.
        for(int i=buttons.Count-1;i>=0;i--)
            if(buttons[i].area.Contains(new Vector2(local.x,local.y))){buttons[i].action();return true;}
        return false;
    }

    public Vector2 ButtonScreenCentre(int index)
    {
        var point=buttons[index].area.center;
        return ViewCamera.WorldToScreenPoint(Origin+new Vector3(point.x,point.y,0));
    }
    public int ButtonCount => buttons.Count;
    public string[] LayoutIssues()
    {
        var issues=new List<string>();
        if(page==null)return issues.ToArray();
        foreach(var text in page.GetComponentsInChildren<TextMesh>())
        {
            var bounds=text.GetComponent<Renderer>().bounds;
            if(bounds.min.x<Origin.x-8.2f || bounds.max.x>Origin.x+8.2f
                || bounds.min.y<Origin.y-5.1f || bounds.max.y>Origin.y+5.1f)
                issues.Add(text.name+" leaves the atlas canvas: "+bounds);
        }
        return issues.ToArray();
    }

    void Box(string name,Rect area,Material material,float z=0)
        => kit.Box(name,new Vector3(area.center.x,area.center.y,z),new Vector3(area.width,area.height,.015f),material,page);

    TextMesh Label(string name,string text,float x,float y,float size,Color color)
    {
        var label=kit.Caption(name,text,new Vector3(x,y,-.9f),size,color,page);
        label.transform.localRotation=Quaternion.identity;
        label.anchor=TextAnchor.UpperLeft; label.alignment=TextAlignment.Left; label.lineSpacing=1.15f;
        return label;
    }

    void Button(string text,Rect area,Action action,bool selected=false,bool enabled=true)
    {
        Box("Atlas button / "+text,area,selected?background:panel,-.25f);
        Box("Atlas button accent",new Rect(area.x,area.y,.025f,area.height),selected?mint:edge,-.3f);
        Label("Atlas button label",text,area.x+.12f,area.yMax-.11f,.16f,enabled?(selected?Mint:Ink):Muted);
        if(enabled)buttons.Add((area,action));
    }

    static string Wrap(string text,int columns)
    {
        var result=new StringBuilder(); int line=0;
        foreach(string paragraph in text.Replace("\r","").Split('\n'))
        {
            if(result.Length>0)result.Append('\n');
            line=0;
            foreach(string word in paragraph.Split(' '))
            {
                if(line>0 && line+word.Length+1>columns){result.Append('\n');line=0;}
                if(line>0){result.Append(' ');line++;}
                result.Append(word);line+=word.Length;
            }
        }
        return result.ToString();
    }

    void Line(string name,Vector3[] points,Material material,float width,bool loop=false)
    {
        var line=new GameObject(name).AddComponent<LineRenderer>();
        line.transform.SetParent(page,false); line.useWorldSpace=false;
        line.sharedMaterial=material; line.widthMultiplier=width; line.positionCount=points.Length;
        line.SetPositions(points);line.loop=loop;line.numCornerVertices=2;
    }

    void Circle(string name,Vector3 centre,float radius,Material material,float width,float squash=1)
    {
        var points=new Vector3[96];
        for(int i=0;i<points.Length;i++)
        {
            float angle=i*Mathf.PI*2/points.Length;
            points[i]=centre+new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius*squash,0);
        }
        Line(name,points,material,width,true);
    }

    Vector2 MapPoint(AtlasPlanet planet)=>new Vector2(-7.35f+planet.x*7.5f,-3.45f+planet.y*6.7f);

    void DrawStars()
    {
        var random=new System.Random(71);
        // Three logarithmic arms make the atlas read as a galaxy, rather than a solar-system list.
        for(int arm=0;arm<3;arm++)
        {
            var points=new Vector3[100];
            for(int i=0;i<points.Length;i++)
            {
                float t=i/99f,r=.15f+t*3.5f,a=t*5.6f+arm*Mathf.PI*2/3;
                points[i]=new Vector3(-3.8f+Mathf.Cos(a)*r,Mathf.Sin(a)*r*.85f,.15f);
            }
            Line("Galaxy spiral arm",points,edge,.035f);
        }
        for(int group=0;group<3;group++)
        {
            var vertices=new List<Vector3>(); var indices=new List<int>();
            for(int i=0;i<75;i++)
            {
                float x=-7.5f+(float)random.NextDouble()*7.4f,y=-3.9f+(float)random.NextDouble()*7.5f;
                float radius=.008f+(float)random.NextDouble()*.012f;
                int n=vertices.Count;
                vertices.Add(new Vector3(x-radius,y-radius,.08f)); vertices.Add(new Vector3(x-radius,y+radius,.08f));
                vertices.Add(new Vector3(x+radius,y+radius,.08f)); vertices.Add(new Vector3(x+radius,y-radius,.08f));
                indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            if(starMeshes[group]==null)
                starMeshes[group]=kit.Own(new Mesh{name="Atlas star field",vertices=vertices.ToArray(),triangles=indices.ToArray()});
            var stars=new GameObject("Atlas stars");stars.transform.SetParent(page,false);
            stars.AddComponent<MeshFilter>().sharedMesh=starMeshes[group];
            stars.AddComponent<MeshRenderer>().sharedMaterial=group==0?gold:group==1?muted:edge;
        }
    }

    void World(int index,Vector3 position,float radius)
    {
        var sphere=kit.Shape("Atlas planet / "+atlas.Catalog[index].id,PrimitiveType.Sphere,position,
            Vector3.one*(radius*2),worlds[index],page).transform;
        globes.Add(sphere);
        if(atlas.Catalog[index].rings)
            Circle("Atlas planetary rings",position+Vector3.forward*.12f,radius*1.5f,gold,.015f,.35f);
    }

    void DrawGalaxy()
    {
        DrawStars();
        var home=MapPoint(atlas.Catalog[Mathf.Max(0,atlas.Catalog.IndexOf("earth"))]);
        for(int i=0;i<atlas.Catalog.Count;i++)
        {
            int index=i; var planet=atlas.Catalog[i];var at=MapPoint(planet);
            bool selected=i==atlas.Selected;
            if(planet.id!="earth")Line("Delivery route",new[]{new Vector3(home.x,home.y,-.1f),new Vector3(at.x,at.y,-.1f)},selected?gold:edge,selected?.025f:.009f);
            World(i,new Vector3(at.x,at.y,-.35f),selected?.34f:.27f);
            if(selected)Circle("Selected destination",new Vector3(at.x,at.y,-.15f),.46f,mint,.018f);
            Label("Map planet name",planet.name,at.x-.53f,at.y-.47f,.16f,selected?Mint:Ink);
            Label("Map order badge",planet.order.Reward+" кр.",at.x-.30f,at.y-.73f,.12f,Gold);
            buttons.Add((new Rect(at.x-.65f,at.y-.9f,1.35f,1.35f),()=>atlas.OpenPlanet(index)));
        }
        Label("Routes heading","МИРЫ И ЗАКАЗЫ",.75f,3.72f,.27f,Ink);
        Label("Routes count","ЕДА, КОТОРУЮ ЖДУТ ДАЛЕКО ОТ ДОМА",.75f,3.28f,.12f,Gold);
        for(int i=0;i<atlas.Catalog.Count;i++)
        {
            int index=i;var planet=atlas.Catalog[i];float y=2.15f-i*.85f;
            Button(planet.name+"   ·   "+planet.order.Reward+" кр.",new Rect(.75f,y,6.45f,.64f),()=>atlas.OpenPlanet(index),i==atlas.Selected);
            Label("Route food",planet.order.Cargo,.92f,y+.22f,.12f,Muted);
        }
        Label("Atlas instruction","ВЫБЕРИ ПЛАНЕТУ НА КАРТЕ ИЛИ В СПИСКЕ",.75f,-3.48f,.12f,Gold);
        Label("Atlas scope","Атлас показывает миры и заявки.\nОтправление — через подготовку доставки.",.75f,-3.78f,.14f,Muted);
    }

    void DrawPlanet()
    {
        var planet=atlas.Planet;
        Button("← К галактике",new Rect(-7.6f,3.5f,2.5f,.48f),atlas.Back);
        Circle("Planet display orbit",new Vector3(-4.1f,.2f,.15f),2.3f,edge,.015f,.9f);
        World(atlas.Selected,new Vector3(-4.1f,.3f,-.25f),1.55f);
        Label("Planet portrait name",planet.name.ToUpperInvariant(),-7.25f,-1.85f,.43f,planet.Colour);
        Label("Planet subtitle",Wrap(planet.subtitle,39),-7.25f,-2.5f,.18f,Ink);
        Label("Planet delivery identity","МАРШРУТ  /  "+planet.order.Route,-7.25f,-3.36f,.13f,Gold);
        Label("Planet field notes","Записки курьеров «Орбиты»\nКультуры, биомы и маленькие истории.",-7.25f,-3.75f,.145f,Muted);
        Label("Planet page title",planet.name,.75f,3.78f,.38f,Ink);
        Label("Planet page subtitle",Wrap(planet.subtitle,57),.75f,3.24f,.14f,Muted);
        for(int i=0;i<GalaxyAtlas.Tabs.Length;i++)
        {
            int index=i;
            Button(GalaxyAtlas.Tabs[i],new Rect(.75f+(i%3)*2.16f,2.18f-(i/3)*.51f,2.05f,.42f),()=>atlas.SelectTab(index),i==atlas.Tab);
        }
        if(atlas.Tab==5) DrawOrder(planet);
        else
        {
            Label("Planet section heading",GalaxyAtlas.Tabs[atlas.Tab].ToUpperInvariant(),.8f,1.44f,.15f,Gold);
            Label("Planet lore text",Wrap(planet.Section(atlas.Tab),57),.8f,1.05f,.18f,Ink);
            Button("Посмотреть заказ →",new Rect(.8f,-3.95f,6.3f,.55f),()=>atlas.SelectTab(5));
        }
    }

    void DrawOrder(AtlasPlanet planet)
    {
        var order=planet.order;
        Label("Planet cuisine",Wrap(planet.cuisine,64),.8f,1.37f,.145f,Muted);
        Label("Food order heading",Wrap(order.Title,47),.8f,-.45f,.23f,Gold);
        Label("Food order client",Wrap("Для кого: "+order.Client,58),.8f,-1.05f,.16f,Ink);
        Label("Food order cargo",Wrap(order.Cargo,57),.8f,-1.48f,.18f,Ink);
        Label("Food order reward","ОПЛАТА  /  "+order.Reward+" КРЕДИТОВ",.8f,-1.95f,.145f,Mint);
        Label("Food order note",Wrap(order.Note.Replace("\n"," "),67),.8f,-2.37f,.14f,Muted);
        string action="Взять заказ · "+order.Reward+" кр.";
        bool canAccept=!atlas.Terminal.Accepted;
        if(!canAccept) action=atlas.Terminal.Order.PlanetId==planet.id?"Заказ принят · см. терминал":"В работе: "+atlas.Terminal.Order.Client;
        Button(action,new Rect(.8f,-3.95f,6.3f,.55f),()=>atlas.AcceptOrder(),true,canAccept);
    }

    void OnDestroy()
    {
        if(gameCamera!=null)gameCamera.cullingMask=originalMask;
        if(root!=null)Destroy(root.gameObject);
        kit.Release();
    }
}

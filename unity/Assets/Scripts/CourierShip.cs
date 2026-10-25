using UnityEngine;
using UnityEngine.Rendering;

// Built-in renderer; imported character presentation is handled separately.
[RequireComponent(typeof(CourierCharacters))]
[RequireComponent(typeof(ShipFurnishings))]
public sealed class CourierShip : MonoBehaviour
{
    Transform player;
    int deck;
    bool terminal, accepted;
    ShipLift liftController;
    GUIStyle title, text;

    Material Material(string hex, bool glow = false)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var color);
        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        if (glow) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 1.5f); }
        return material;
    }

    GameObject Box(Vector3 pos, Vector3 size, string color, bool glow = false, Transform parent = null)
    {
        var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.transform.SetParent(parent != null ? parent : transform, false);
        item.transform.localPosition = pos;
        item.transform.localScale = size;
        item.GetComponent<Renderer>().sharedMaterial = Material(color, glow);
        // Movement uses explicit deck bounds, like the original prototype.
        Destroy(item.GetComponent<Collider>());
        return item;
    }

    void Lamp(Vector3 pos, string hex, float power, float range)
    {
        var light = new GameObject("Cabin light").AddComponent<Light>();
        light.transform.SetParent(transform);
        light.transform.position = pos;
        ColorUtility.TryParseHtmlString("#" + hex, out var color);
        light.type = LightType.Point; light.color = color;
        light.intensity = power; light.range = range;
    }

    void Start()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.40f, .34f, .48f);
        var camera = Camera.main;
        if (camera == null) camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(16, 14, 29);
        camera.transform.LookAt(new Vector3(0, 3.4f, 0));
        camera.fieldOfView = 48; camera.backgroundColor = new Color(.065f, .045f, .10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        // The floor opening continues the lift shaft down into the hangar.
        Box(new Vector3(-7.425f,-.25f,0), new Vector3(13.15f,.5f,10), "283040");
        Box(new Vector3(7.725f,-.25f,0), new Vector3(12.55f,.5f,10), "283040");
        Box(new Vector3(.3f,-.25f,-2.125f), new Vector3(2.3f,.5f,5.75f), "283040");
        Box(new Vector3(.3f,-.25f,3.925f), new Vector3(2.3f,.5f,2.15f), "283040");
        Box(new Vector3(0,4,-5), new Vector3(28,8.4f,.25f), "353347");
        Box(new Vector3(-14,4,0), new Vector3(.25f,8,10), "353347");
        for (int x=-13; x<14; x++)
        {
            if (x >= -1 && x <= 1)
            {
                Box(new Vector3(x,.025f,-2.075f),new Vector3(.94f,.05f,5.65f),"48414C");
                Box(new Vector3(x,.025f,3.875f),new Vector3(.94f,.05f,2.05f),"48414C");
            }
            else Box(new Vector3(x,.025f,0),new Vector3(.94f,.05f,9.8f),"48414C");
        }
        // Four slabs leave a real opening around the glass lift shaft.
        Box(new Vector3(-7.325f,3.9f,-.8f),new Vector3(12.95f,.3f,7.8f),"63505E");
        Box(new Vector3(.3f,3.9f,-1.975f),new Vector3(2.3f,.3f,5.45f),"63505E");
        Box(new Vector3(1.625f,3.9f,-.8f),new Vector3(.35f,.3f,7.8f),"63505E");
        Box(new Vector3(.3f,3.9f,2.975f),new Vector3(2.3f,.3f,.25f),"63505E");
        for (int x=-13; x<2; x+=2)
            Box(new Vector3(x,4.65f,3),new Vector3(.06f,1.2f,.06f),"BC9690");
        Box(new Vector3(-6,5.25f,3),new Vector3(15.6f,.06f,.06f),"D1A58D");
        Box(new Vector3(-6,4,3.1f),new Vector3(15.6f,.05f,.05f),"ED8EC6",true);
        // Downstairs sofa, rug, table and galley.
        Box(new Vector3(-9,.06f,1),new Vector3(6,.05f,4),"775B78");
        Box(new Vector3(-9,.6f,.6f),new Vector3(2.5f,.18f,1.4f),"B98E77");
        Box(new Vector3(-3,.9f,-3.8f),new Vector3(4.5f,1.8f,1.5f),"526467");
        Box(new Vector3(-3,1.85f,-3.8f),new Vector3(4.7f,.12f,1.7f),"C6AA90");
        Box(new Vector3(.4f,.7f,-1.9f),new Vector3(1.5f,1.4f,.9f),"283E42");
        Box(new Vector3(.4f,1.85f,-2),new Vector3(1.6f,1.1f,.3f),"526467");
        Box(new Vector3(.4f,1.85f,-1.83f),new Vector3(1.3f,.8f,.04f),"8CD5A9",true);
        // Bedroom and reading nook above.
        Box(new Vector3(-4.6f,4.6f,-2),new Vector3(1.5f,.9f,1.5f),"B37A88");
        Box(new Vector3(-4.6f,5.2f,-2.6f),new Vector3(1.5f,1.2f,.25f),"B37A88");
        gameObject.AddComponent<ShipWindowView>().Build(transform);
        Box(new Vector3(8,.9f,-3),new Vector3(6,1.8f,1.6f),"283949");
        for(int x=6;x<=10;x+=2) Box(new Vector3(x,1.85f,-3),new Vector3(1.2f,.08f,.8f),"87C5C8",true);
        for(int i=0;i<17;i++) Box(new Vector3(-13+i*.9f,7.4f-Mathf.Sin(i/16f*Mathf.PI)*.6f,1.6f),new Vector3(.15f,.2f,.15f),"FFD2A0",true);
        Lamp(new Vector3(-10,2,1),"FFC498",3,9);
        Lamp(new Vector3(-8,6,-1),"FFC498",3,9);
        Lamp(new Vector3(7,4,-2),"BA83E8",4,12);
        GetComponent<ShipFurnishings>().Build();
        gameObject.AddComponent<ShipInteriorDetails>().Build(transform);
        gameObject.AddComponent<ShipWardrobe>().Build(transform);
        var homeGroups = new GameObject[transform.childCount];
        for (int i = 0; i < homeGroups.Length; i++) homeGroups[i] = transform.GetChild(i).gameObject;
        player=GetComponent<CourierCharacters>().CreateCourier(new Vector3(-4,0,.6f));
        liftController=gameObject.AddComponent<ShipLift>();
        liftController.Build(transform,player,GetComponent<CourierCharacters>());
        liftController.PassengerArrived += floor => deck=floor;
        var framing = gameObject.AddComponent<ShipCameraFraming>();
        framing.Initialize(camera, transform);
        var hangar = gameObject.AddComponent<ShipHangar>();
        hangar.Build(transform);
        framing.ConfigureHangar(hangar.Interior, player, homeGroups, liftController.Visuals);
    }

    bool AtLift() => liftController != null && liftController.IsNear(player.position,deck);
    bool AtTerminal() => deck==0 && Vector3.Distance(player.position,new Vector3(.4f,0,-.6f))<2;
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)) terminal=false;
        if(terminal || (liftController != null && liftController.IsRiding)) return;
        float x=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
        float z=(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0)-(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0);
        MoveCourier(new Vector3(x,0,z),Time.deltaTime*(Input.GetKey(KeyCode.LeftShift)?6:4));
        if(Input.GetKeyDown(KeyCode.E)) Interact();
        if(Input.GetKeyDown(KeyCode.R)) Descend();
    }

    void MoveCourier(Vector3 direction, float distance)
    {
        if(liftController != null && liftController.IsRiding) return;
        direction.Normalize();
        var pos=player.position+direction*distance;
        pos.x=Mathf.Clamp(pos.x,deck < 0 ? -17.2f : -13.2f,deck < 0 ? 17.2f : deck==1?1:13.2f);
        // Hangar promenade stays clear of the parked craft and service equipment.
        pos.z=Mathf.Clamp(pos.z,deck < 0 ? 1f : -.8f,deck < 0 ? 5.4f : deck==1?2.5f:4.4f);
        player.position=liftController != null ? liftController.ConstrainMovement(player.position,pos,deck) : pos;
        if(direction.sqrMagnitude>0) player.rotation=Quaternion.LookRotation(direction);
    }

    void Interact()
    {
        if(liftController != null && liftController.IsRiding) return;
        if(AtLift()) liftController.TryUse(deck);
        else if(AtTerminal()) terminal=true;
    }

    void Descend()
    {
        if (!terminal && AtLift()) liftController.TryUse(deck, -1);
    }

    void OnGUI()
    {
        if(player==null) return;
        if(title==null) { title=new GUIStyle(GUI.skin.label){fontSize=25}; text=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true}; }
        GUI.Label(new Rect(28,22,650,40),"ОРБИТА / МЕЖПЛАНЕТНАЯ ДОСТАВКА",title);
        GUI.Label(new Rect(28,65,650,32),deck < 0 ? "УРОВЕНЬ 00 · ПОДЗЕМНЫЙ АНГАР" : deck==0?"ПАЛУБА 01 · ГОСТИНАЯ И МОСТИК":"ПАЛУБА 02 · ДОМА, НАВЕРХУ",text);
        GUI.Label(new Rect(28,Screen.height-44,1100,35),"WASD / стрелки — ходить · Shift — быстрее · E — терминал / лифт · R — вниз в ангар · Esc — закрыть",text);
        if(!terminal) { GUI.Label(new Rect(Screen.width/2-220,Screen.height-105,600,40),
            liftController != null && (AtLift() || liftController.IsRiding) ? liftController.Hint(player.position,deck)
            : AtTerminal()?"E · Открыть заказ":"",text); return; }
        var panel=new Rect(Screen.width/2-245,Screen.height/2-215,490,430);
        GUI.Box(panel,""); GUILayout.BeginArea(new Rect(panel.x+25,panel.y+20,440,390));
        GUILayout.Label("Ужин на краю Солнечной системы",title);
        GUILayout.Space(20); GUILayout.Label("ЗЕМЛЯ → НЕПТУН\n\nМира · станция «Тритон»\nГорячий суп с лунными грибами\nВознаграждение: 240 кредитов\n\n«Не переворачивать. Суп живой.\nИ захватите ложку».",text);
        GUILayout.Space(20); GUI.enabled=!accepted;
        if(GUILayout.Button(accepted?"Заказ принят ✓":"Принять заказ",GUILayout.Height(40))) accepted=true;
        GUI.enabled=true;
        if(accepted) GUILayout.Label("Следующий шаг: забрать суп на Земле.",text);
        if(GUILayout.Button("Закрыть")) terminal=false;
        GUILayout.EndArea();
    }
}


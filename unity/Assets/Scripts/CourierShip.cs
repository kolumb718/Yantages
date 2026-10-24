using UnityEngine;
using UnityEngine.Rendering;

// Built-in renderer, no imported assets or external packages required.
public sealed class CourierShip : MonoBehaviour
{
    Transform player;
    int deck;
    bool terminal, accepted;
    readonly Vector3 lift = new Vector3(.3f, 0, 1.8f);
    GUIStyle title, text;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindFirstObjectByType<CourierShip>() == null)
            new GameObject("Courier ship").AddComponent<CourierShip>();
    }

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
        Box(new Vector3(0,-.25f,0), new Vector3(28,.5f,10), "283040");
        Box(new Vector3(0,4,-5), new Vector3(28,8.4f,.25f), "353347");
        Box(new Vector3(-14,4,0), new Vector3(.25f,8,10), "353347");
        for (int x=-13; x<14; x++)
            Box(new Vector3(x,.025f,0),new Vector3(.94f,.05f,9.8f),"48414C");
        Box(new Vector3(-6,3.9f,-.8f),new Vector3(15.6f,.3f,7.8f),"63505E");
        for (int x=-13; x<2; x+=2)
            Box(new Vector3(x,4.65f,3),new Vector3(.06f,1.2f,.06f),"BC9690");
        Box(new Vector3(-6,5.25f,3),new Vector3(15.6f,.06f,.06f),"D1A58D");
        Box(new Vector3(-6,4,3.1f),new Vector3(15.6f,.05f,.05f),"ED8EC6",true);
        // Downstairs sofa, rug, table and galley.
        Box(new Vector3(-9,.06f,1),new Vector3(6,.05f,4),"775B78");
        Box(new Vector3(-10,.6f,-1.8f),new Vector3(5,.9f,1.7f),"82637F");
        Box(new Vector3(-10,1.2f,-2.5f),new Vector3(5,1.3f,.3f),"82637F");
        Box(new Vector3(-9,.6f,.6f),new Vector3(2.5f,.18f,1.4f),"B98E77");
        Box(new Vector3(-3,.9f,-3.8f),new Vector3(4.5f,1.8f,1.5f),"526467");
        Box(new Vector3(-3,1.85f,-3.8f),new Vector3(4.7f,.12f,1.7f),"C6AA90");
        Box(new Vector3(.4f,.7f,-1.9f),new Vector3(1.5f,1.4f,.9f),"283E42");
        Box(new Vector3(.4f,1.85f,-2),new Vector3(1.6f,1.1f,.3f),"526467");
        Box(new Vector3(.4f,1.85f,-1.83f),new Vector3(1.3f,.8f,.04f),"8CD5A9",true);
        // Bedroom and reading nook above.
        Box(new Vector3(-10,4.45f,-2.5f),new Vector3(4.3f,.7f,2.6f),"DAC3AB");
        Box(new Vector3(-9.5f,4.9f,-2.5f),new Vector3(3,.2f,2.6f),"A47691");
        Box(new Vector3(-11.6f,4.95f,-2.5f),new Vector3(.8f,.3f,1.8f),"E6C8AD");
        Box(new Vector3(-4.6f,4.6f,-2),new Vector3(1.5f,.9f,1.5f),"B37A88");
        Box(new Vector3(-4.6f,5.2f,-2.6f),new Vector3(1.5f,1.2f,.25f),"B37A88");
        for (int row=0;row<3;row++) {
            float y=4.3f+row*.9f;
            Box(new Vector3(-6,y,-4.6f),new Vector3(4,.1f,.6f),"AE8879");
            for(int i=0;i<10;i++) Box(new Vector3(-7.7f+i*.36f,y+.3f,-4.6f),new Vector3(.22f,.5f,.4f), i%2==0?"A386A9":"8AA99E");
        }
        // Taipei-inspired dusk, windows and a tall neon skyline.
        Box(new Vector3(8,4.5f,-4.8f),new Vector3(10,6,.1f),"674778",true);
        for(int i=0;i<18;i++) {
            float h=.7f+(i*7%11)*.32f, x=3.4f+i*.53f;
            Box(new Vector3(x,1.5f+h/2,-4.65f),new Vector3(.45f,h,.08f),"292A44");
            for(int j=0;j<6;j++) Box(new Vector3(x,1.6f+j*h/7,-4.59f),new Vector3(.24f,.04f,.02f),"EDA6CC",true);
        }
        Box(new Vector3(8,4.6f,-4.5f),new Vector3(.6f,5,.12f),"33314C");
        Box(new Vector3(8,7.5f,-4.5f),new Vector3(.06f,1,.06f),"EDB0D6",true);
        Box(new Vector3(8,.9f,-3),new Vector3(6,1.8f,1.6f),"283949");
        for(int x=6;x<=10;x+=2) Box(new Vector3(x,1.85f,-3),new Vector3(1.2f,.08f,.8f),"87C5C8",true);
        foreach(float y in new[]{.08f,4.1f}) {
            Box(new Vector3(.3f,y,1.8f),new Vector3(2.3f,.08f,2.1f),"8C729D");
            Box(new Vector3(.3f,y+.06f,2.86f),new Vector3(2.3f,.04f,.04f),"9CD8D2",true);
        }
        foreach(var p in new[]{new Vector3(-13,0,2),new Vector3(12,0,-2),new Vector3(-12,4.1f,1.5f)}) {
            Box(p+Vector3.up*.3f,new Vector3(.65f,.6f,.65f),"B38270");
            for(int i=0;i<3;i++) Box(p+new Vector3((i-1)*.22f,1+i*.12f,0),new Vector3(.25f,1.1f,.25f),"739D86").transform.Rotate(0,0,(i-1)*20);
        }
        for(int i=0;i<17;i++) Box(new Vector3(-13+i*.9f,7.4f-Mathf.Sin(i/16f*Mathf.PI)*.6f,1.6f),new Vector3(.15f,.2f,.15f),"FFD2A0",true);
        Lamp(new Vector3(-10,2,1),"FFC498",3,9);
        Lamp(new Vector3(-8,6,-1),"FFC498",3,9);
        Lamp(new Vector3(7,4,-2),"BA83E8",4,12);
        player=new GameObject("Courier").transform;
        player.SetParent(transform); player.position=new Vector3(-4,0,.6f);
        Box(new Vector3(0,.85f,0),new Vector3(.5f,.7f,.38f),"C19B66",false,player);
        Box(new Vector3(0,1.45f,0),new Vector3(.4f,.42f,.4f),"B59B84",false,player);
        foreach(float x in new[]{-.15f,.15f}) Box(new Vector3(x,.25f,0),new Vector3(.17f,.5f,.22f),"252D2B",false,player);
    }

    bool AtLift() => Vector2.Distance(new Vector2(player.position.x,player.position.z),new Vector2(lift.x,lift.z))<1.1f;
    bool AtTerminal() => deck==0 && Vector3.Distance(player.position,new Vector3(.4f,0,-.6f))<2;
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)) terminal=false;
        if(terminal) return;
        float x=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
        float z=(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0)-(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0);
        var direction=new Vector3(x,0,z).normalized;
        var pos=player.position+direction*Time.deltaTime*(Input.GetKey(KeyCode.LeftShift)?6:4);
        pos.x=Mathf.Clamp(pos.x,-13.2f,deck==1?1:13.2f);
        pos.z=Mathf.Clamp(pos.z,-.8f,deck==1?2.5f:4.4f);
        player.position=pos;
        if(direction.sqrMagnitude>0) player.rotation=Quaternion.LookRotation(direction);
        if(Input.GetKeyDown(KeyCode.E)) {
            if(AtLift()) { deck=1-deck; player.position=new Vector3(.3f,deck==1?4.1f:0,1.8f); }
            else if(AtTerminal()) terminal=true;
        }
    }

    void OnGUI()
    {
        if(player==null) return;
        if(title==null) { title=new GUIStyle(GUI.skin.label){fontSize=25}; text=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true}; }
        GUI.Label(new Rect(28,22,650,40),"ОРБИТА / МЕЖПЛАНЕТНАЯ ДОСТАВКА",title);
        GUI.Label(new Rect(28,65,650,32),deck==0?"ПАЛУБА 01 · ГОСТИНАЯ И МОСТИК":"ПАЛУБА 02 · ДОМА, НАВЕРХУ",text);
        GUI.Label(new Rect(28,Screen.height-44,1000,35),"WASD / стрелки — ходить · Shift — быстрее · E — терминал / лифт · Esc — закрыть",text);
        if(!terminal) { GUI.Label(new Rect(Screen.width/2-180,Screen.height-105,500,40),AtLift()?"E · Лифт — "+(deck==0?"в каюту":"в гостиную"):AtTerminal()?"E · Открыть заказ":"",text); return; }
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


using System.Collections.Generic;
using UnityEngine;

// Personal objects, lounge and parcel storage. Decorative objects have no pickup behaviour;
// only the furniture roots are returned as movement obstacles.
public sealed class LivingDeckDressing : MonoBehaviour
{
    readonly SceneKit kit = new SceneKit();
    readonly List<Transform> obstacles = new List<Transform>();
    Material walnut, honey, cream, rust, sage, brass, dark, paper, blue, light;
    Transform root;
    public Transform[] Obstacles => obstacles.ToArray();

    public void Build(Transform ship)
    {
        if (root != null) return;
        root = Group("Living deck belongings",Vector3.zero,ship);
        walnut = kit.Flat("684937"); honey = kit.Flat("B78959"); cream = kit.Flat("E5D5B5");
        rust = kit.Flat("B36148"); sage = kit.Flat("839B82"); brass = kit.Flat("AC8855");
        dark = kit.Flat("343D42"); paper = kit.Flat("EDE1C6"); blue = kit.Flat("607D88");
        light = kit.Emissive("Lounge lamp shade glow",new Color(1,.73f,.43f),.35f);
        BuildSideTable();
        BuildCoffeeObjects();
        BuildSofaObjects();
        BuildMemoryBoard();
        BuildParcelShelf();
        BuildStorageBench();
    }

    static Transform Group(string name, Vector3 position, Transform parent)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent,false);
        group.localPosition = position;
        return group;
    }
    void Box(string name,Vector3 position,Vector3 size,Material material,Transform parent)
        => kit.Box(name,position,size,material,parent);
    void Sphere(string name,Vector3 position,Vector3 size,Material material,Transform parent)
        => kit.Shape(name,PrimitiveType.Sphere,position,size,material,parent);
    void Cylinder(string name,Vector3 position,float radius,float height,Material material,Transform parent)
        => kit.Shape(name,PrimitiveType.Cylinder,position,new Vector3(radius*2,height*.5f,radius*2),material,parent);

    void BuildSideTable()
    {
        var table = Group("Lounge lamp side table",ShipLayout.LoungeSideTablePosition,root);
        var size = ShipLayout.LoungeSideTableSize;
        Box("Side table top",new Vector3(0,size.y-.035f,0),new Vector3(size.x,.07f,size.z),walnut,table);
        Box("Side table lower shelf",new Vector3(0,.15f,0),new Vector3(size.x-.04f,.04f,size.z-.04f),honey,table);
        foreach(float x in new[]{-.19f,.19f}) foreach(float z in new[]{-.19f,.19f})
            Box("Side table leg",new Vector3(x,size.y*.5f,z),new Vector3(.045f,size.y,.045f),walnut,table);
        Book(table,new Vector3(0,.19f,0),.28f,.19f,.045f,sage,"Side table travel journal");
        Cylinder("Lounge lamp foot",new Vector3(0,size.y+.018f,0),.12f,.035f,brass,table);
        kit.Tube("Lounge lamp stem",new Vector3(0,size.y+.035f,0),new Vector3(0,1.09f,0),.017f,brass,table);
        Shade(table,new Vector3(0,1.10f,0));
        kit.PointLight("Lounge reading light",new Vector3(0,1.02f,.03f),new Color(1,.76f,.51f),.65f,2.6f,table);
        obstacles.Add(table);
    }

    void Shade(Transform parent,Vector3 centre)
    {
        const int segments=16;
        var vertices=new Vector3[segments*2];
        var triangles=new int[segments*6];
        for(int i=0;i<segments;i++)
        {
            float a=i*Mathf.PI*2/segments;
            vertices[i]=new Vector3(Mathf.Cos(a)*.19f,-.10f,Mathf.Sin(a)*.19f);
            vertices[i+segments]=new Vector3(Mathf.Cos(a)*.115f,.10f,Mathf.Sin(a)*.115f);
            int n=(i+1)%segments,t=i*6;
            triangles[t]=i; triangles[t+1]=i+segments; triangles[t+2]=n;
            triangles[t+3]=n; triangles[t+4]=i+segments; triangles[t+5]=n+segments;
        }
        var mesh=kit.Own(new Mesh{name="Lounge tapered lampshade",vertices=vertices,triangles=triangles});
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var shade=Group("Lounge reading lampshade",centre,parent).gameObject;
        shade.AddComponent<MeshFilter>().sharedMesh=mesh;
        shade.AddComponent<MeshRenderer>().sharedMaterial=light;
    }

    void Book(Transform parent,Vector3 position,float width,float depth,float thickness,Material cover,string name)
    {
        Box(name+" pages",position,new Vector3(width-.012f,thickness,depth-.012f),paper,parent);
        foreach(float side in new[]{-1f,1f})
            Box(name+" cover",position+Vector3.up*side*(thickness*.5f+.003f),new Vector3(width,.006f,depth),cover,parent);
        Box(name+" spine",position+Vector3.left*width*.5f,new Vector3(.012f,thickness+.012f,depth),cover,parent);
    }

    void Mug(Transform parent,Vector3 position,Material material,string name)
    {
        // Dark inset communicates an open cup without another transparent material.
        Cylinder(name,position+Vector3.up*.048f,.041f,.096f,material,parent);
        Cylinder(name+" coffee",position+Vector3.up*.096f,.033f,.003f,dark,parent);
        kit.Tube(name+" handle top",position+new Vector3(.035f,.078f,0),position+new Vector3(.067f,.07f,0),.009f,material,parent);
        kit.Tube(name+" handle outside",position+new Vector3(.067f,.07f,0),position+new Vector3(.067f,.027f,0),.009f,material,parent);
        kit.Tube(name+" handle bottom",position+new Vector3(.067f,.027f,0),position+new Vector3(.035f,.022f,0),.009f,material,parent);
    }

    void BuildCoffeeObjects()
    {
        var table=Group("Lounge coffee table belongings",ShipLayout.LoungeCoffeeDetailPosition,root);
        Book(table,new Vector3(-.25f,.025f,-.12f),.24f,.18f,.035f,blue,"Lounge star atlas");
        Box("Atlas paper bookmark",new Vector3(-.19f,.047f,.002f),new Vector3(.025f,.003f,.08f),rust,table);
        Mug(table,new Vector3(.24f,.005f,-.12f),cream,"Lounge coffee mug");
        Cylinder("Lounge snack plate",new Vector3(.20f,.012f,.13f),.085f,.013f,cream,table);
        for(int i=0;i<3;i++) Cylinder("Lounge biscuit",new Vector3(.16f+i*.035f,.026f,.13f+(i%2)*.025f),.025f,.012f,honey,table);
        Box("Music player",new Vector3(-.24f,.018f,.13f),new Vector3(.13f,.03f,.08f),dark,table);
        Box("Music player amber display",new Vector3(-.24f,.035f,.13f),new Vector3(.08f,.003f,.04f),brass,table);
    }

    void BuildSofaObjects()
    {
        var sofa=Group("Lounge sofa soft furnishings",ShipLayout.LoungeSofaDetailPosition,root);
        var cushion=kit.Shape("Lounge sofa cushion",PrimitiveType.Sphere,new Vector3(-.28f,.65f,.18f),
            new Vector3(.34f,.29f,.17f),sage,sofa);
        cushion.transform.localRotation=Quaternion.Euler(-12,0,-8);
        Box("Sofa folded throw",new Vector3(.37f,.535f,.24f),new Vector3(.45f,.045f,.44f),sage,sofa);
        for(int i=0;i<4;i++) Box("Throw woven stripe",new Vector3(.20f+i*.10f,.56f,.24f),new Vector3(.022f,.007f,.44f),cream,sofa);
    }

    void BuildMemoryBoard()
    {
        var board=Group("Kristi travel memory board",ShipLayout.LoungeMemoryBoardPosition,root);
        Box("Memory board wood frame",Vector3.zero,new Vector3(1.22f,.82f,.055f),walnut,board);
        Box("Memory board cork",new Vector3(0,0,.034f),new Vector3(1.14f,.74f,.018f),honey,board);
        for(int i=0;i<3;i++)
        {
            float x=-.37f+i*.37f;
            var postcard=Group("Travel postcard "+i,new Vector3(x,.055f,.05f),board);
            postcard.localRotation=Quaternion.Euler(0,0,(i-1)*7);
            Box("Postcard paper",Vector3.zero,new Vector3(.29f,.36f,.012f),paper,postcard);
            Box("Postcard sky",new Vector3(0,.027f,.008f),new Vector3(.25f,.24f,.005f),i==1?sage:blue,postcard);
            Sphere("Postcard distant moon",new Vector3(.055f,.075f,.013f),new Vector3(.07f,.07f,.008f),cream,postcard);
            Box("Postcard horizon",new Vector3(0,-.035f,.014f),new Vector3(.25f,.08f,.006f),rust,postcard);
            Sphere("Postcard pin",new Vector3(0,.157f,.025f),Vector3.one*.025f,i==1?rust:sage,postcard);
        }
        var caption=kit.Caption("Memory board note","ДОМОЙ ПОСЛЕ РЕЙСА",new Vector3(0,-.29f,.055f),.075f,cream.color,board);
        caption.transform.localRotation=Quaternion.Euler(0,180,0);
    }

    void BuildParcelShelf()
    {
        var shelf=Group("Living parcel rack",ShipLayout.LivingParcelShelfPosition,root);
        var size=ShipLayout.LivingParcelShelfSize;
        foreach(float x in new[]{-size.x*.47f,size.x*.47f})
            Box("Parcel rack upright",new Vector3(x,size.y*.5f,0),new Vector3(.055f,size.y,size.z),walnut,shelf);
        for(int i=0;i<4;i++)
        {
            float y=.09f+i*.56f;
            Box("Parcel rack shelf",new Vector3(0,y,0),new Vector3(size.x,.045f,size.z),honey,shelf);
            if(i<3) Parcel(shelf,new Vector3(i%2==0?-.15f:.12f,y+.19f,0),new Vector3(.59f,.33f,.42f),i==1?sage:cream);
        }
        kit.Caption("Parcel storage sign","ПОСЫЛКИ / 07",new Vector3(0,1.85f,.29f),.095f,cream.color,shelf);
        obstacles.Add(shelf);
    }

    void Parcel(Transform parent,Vector3 position,Vector3 size,Material material)
    {
        Box("Stored courier parcel",position,size,material,parent);
        Box("Parcel tape front",position+Vector3.forward*(size.z*.5f+.004f),new Vector3(.065f,size.y,.008f),rust,parent);
        Box("Parcel tape lid",position+Vector3.up*(size.y*.5f+.004f),new Vector3(.065f,.008f,size.z),rust,parent);
        Box("Parcel address label",position+new Vector3(size.x*.2f,0,size.z*.5f+.01f),new Vector3(.14f,.08f,.008f),paper,parent);
    }

    void BuildStorageBench()
    {
        var bench=Group("Living storage bench",ShipLayout.LivingStorageBenchPosition,root);
        var size=ShipLayout.LivingStorageBenchSize;
        Box("Storage chest",new Vector3(0,size.y*.45f,0),new Vector3(size.x,size.y*.9f,size.z),walnut,bench);
        Box("Storage bench seat",new Vector3(0,size.y-.035f,0),new Vector3(size.x+.02f,.07f,size.z+.02f),sage,bench);
        foreach(float x in new[]{-.42f,.42f})
            Box("Storage chest brass latch",new Vector3(x,.37f,size.z*.5f+.012f),new Vector3(.065f,.10f,.025f),brass,bench);
        Book(bench,new Vector3(.28f,size.y+.027f,-.06f),.25f,.19f,.04f,rust,"Bench route log");
        obstacles.Add(bench);
    }

    void OnDestroy()=>kit.Release();
}

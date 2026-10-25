using UnityEngine;

// Composition root of the ship scene: builds every part at Play, then routes input to the
// courier, lift, terminal and camera. Each part owns its own state; this class only wires them.
[RequireComponent(typeof(CourierCharacters))]
[RequireComponent(typeof(ShipFurnishings))]
public sealed class CourierShip : MonoBehaviour
{
    // Imported props the courier walks around; the inset forgives overhanging leaves and cushions.
    static readonly (string name, float inset)[] PropObstacles =
    {
        ("Terracotta sofa", .05f), ("Monstera downstairs", .2f), ("Snake plant bridge", .15f),
        ("Cabin bed", .05f), ("Wood bookshelf", .05f), ("Monstera upstairs", .2f),
        ("Pothos reading nook", .1f), ("Equipment wardrobe", .05f),
    };

    CourierInput input;
    ShipInterior interior;
    ShipCameraFraming framing;
    Camera view;

    public CourierMotor Motor { get; private set; }
    public ShipLift Lift { get; private set; }
    public OrderTerminal Terminal { get; } = new OrderTerminal();
    public CourierInput Controls => input;
    public int Deck => Motor.Deck;
    public bool Ready => Motor != null;
    public bool AtTerminal => Ready && OrderTerminal.InReach(Motor.Courier.position, Deck);
    public bool AtLift => Ready && Lift.IsNear(Motor.Courier.position, Deck);
    public LiftPrompt LiftPrompt => Ready ? Lift.Prompt(Motor.Courier.position, Deck) : LiftPrompt.None;

    void Start()
    {
        view = PrepareCamera();
        interior = gameObject.AddComponent<ShipInterior>();
        interior.Build(transform);
        gameObject.AddComponent<ShipWindowView>().Build(transform);
        GetComponent<ShipFurnishings>().Build();
        gameObject.AddComponent<ShipInteriorDetails>().Build(transform);
        gameObject.AddComponent<ShipWardrobe>().Build(transform);
        // Everything built so far is the home; the camera hides it as a cutaway below deck.
        var homeGroups = new GameObject[transform.childCount];
        for (int i = 0; i < homeGroups.Length; i++) homeGroups[i] = transform.GetChild(i).gameObject;

        var characters = GetComponent<CourierCharacters>();
        var courier = characters.CreateCourier(ShipLayout.CourierStart);
        Lift = gameObject.AddComponent<ShipLift>();
        Lift.Build(transform, courier, characters);
        Motor = new CourierMotor(courier, Lift);
        foreach (var obstacle in interior.Obstacles) Motor.AddObstacle(obstacle, .02f);
        foreach (var (name, inset) in PropObstacles)
        {
            var prop = transform.Find("Cabin furnishings/" + name);
            if (prop == null) prop = transform.Find(name);
            if (prop != null) Motor.AddObstacle(prop, inset);
        }
        Lift.PassengerArrived += deck => Motor.Deck = deck;

        framing = gameObject.AddComponent<ShipCameraFraming>();
        framing.Initialize(view, transform);
        var hangar = gameObject.AddComponent<ShipHangar>();
        hangar.Build(transform);
        framing.ConfigureHangar(hangar.Interior, courier, homeGroups, Lift.Visuals);

        input = new CourierInput();
        gameObject.AddComponent<ShipHud>().Bind(this);
    }

    Camera PrepareCamera()
    {
        var camera = Camera.main;
        if (camera == null) camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(16, 14, 29);
        camera.transform.LookAt(new Vector3(0, 3.4f, 0));
        camera.fieldOfView = 48;
        camera.backgroundColor = new Color(.065f, .045f, .10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        return camera;
    }

    void OnDestroy() => input?.Dispose();

    void Update()
    {
        if (!Ready) return;
        interior.SetTerminalHighlight(AtTerminal && !Terminal.IsOpen);
        Motor.Turn(Time.deltaTime);
        if (input.Pressed(CourierAction.Camera)) framing.Follow = !framing.Follow;
        if (Terminal.IsOpen)
        {
            if (input.Pressed(CourierAction.Cancel)) Terminal.Close();
            else if (input.Pressed(CourierAction.Interact)) AcceptOrClose();
            return;
        }
        if (Lift.IsRiding) return;
        float speed = input.Held(CourierAction.Sprint) ? CourierMotor.SprintSpeed : CourierMotor.WalkSpeed;
        Motor.Move(ScreenToDeck(input.Move), speed * Time.deltaTime);
        if (input.Pressed(CourierAction.Interact)) Interact();
        else if (input.Pressed(CourierAction.Descend)) Descend();
    }

    // Input is screen-relative; the cutaway is viewed from +Z, so world +X is screen-left.
    Vector3 ScreenToDeck(Vector2 stick)
    {
        var camera = view.transform;
        return new Vector3(stick.x * Mathf.Sign(camera.right.x), 0, stick.y * Mathf.Sign(camera.forward.z));
    }

    public void Interact()
    {
        if (Lift.IsRiding) return;
        if (AtLift) Lift.TryUse(Deck);
        else if (AtTerminal) Terminal.Open();
    }

    public void Descend()
    {
        if (!Terminal.IsOpen && AtLift) Lift.TryUse(Deck, -1);
    }

    public void AcceptOrClose()
    {
        if (Terminal.Accepted) Terminal.Close();
        else Terminal.Accept();
    }
}

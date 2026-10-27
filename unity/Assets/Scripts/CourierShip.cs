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
        ("Pothos reading nook", .1f),
    };

    static readonly System.Collections.Generic.HashSet<string> HangarObstacles = new System.Collections.Generic.HashSet<string>
    {
        "Hangar fuel station", "Hangar fuel canister", "Hangar cargo crate", "Hangar structural column",
    };

    CourierInput input;
    ShipInterior interior;
    ShipCameraFraming framing;
    ShipInteriorDetails details;
    Camera view;

    public CourierMotor Motor { get; private set; }
    public ShipLift Lift { get; private set; }
    public OrderTerminal Terminal { get; } = new OrderTerminal();
    public CourierOutfits Outfits { get; private set; }
    public GalaxyAtlas Galaxy { get; private set; }
    public GalaxyMapView GalaxyView { get; private set; }
    public CourierInput Controls => input;
    public int Deck => Motor.Deck;
    public bool Ready => Motor != null;
    public bool AtTerminal => Ready && OrderTerminal.InReach(Motor.Courier.position, Deck);
    public bool AtGalaxy => Ready && GalaxyAtlas.InReach(Motor.Courier.position,Deck);
    public bool AtWardrobe => Ready && Deck == ShipLayout.CabinDeck
        && Vector3.Distance(Motor.Courier.position, ShipLayout.ClothingWardrobeApproach) <= ShipLayout.ClothingWardrobeReach;
    public bool AtLift => Ready && Lift.IsNear(Motor.Courier.position, Deck);
    public LiftPrompt LiftPrompt => Ready ? Lift.Prompt(Motor.Courier.position, Deck) : LiftPrompt.None;

    void Start()
    {
        view = PrepareCamera();
        interior = gameObject.AddComponent<ShipInterior>();
        interior.Build(transform);
        gameObject.AddComponent<ShipWindowView>().Build(transform);
        GetComponent<ShipFurnishings>().Build();
        var living = gameObject.AddComponent<LivingDeckDressing>();
        living.Build(transform);
        var galley = gameObject.AddComponent<LivingGalleyDetails>();
        galley.Build(transform);
        details=gameObject.AddComponent<ShipInteriorDetails>();
        details.Build(transform);
        var wardrobe = gameObject.AddComponent<CabinWardrobe>();
        wardrobe.Build(transform);
        var aviary = gameObject.AddComponent<OwlAviary>();
        aviary.Build(transform);
        // Everything built so far is the home; the camera hides it as a cutaway below deck.
        var homeGroups = new GameObject[transform.childCount];
        for (int i = 0; i < homeGroups.Length; i++) homeGroups[i] = transform.GetChild(i).gameObject;

        var characters = GetComponent<CourierCharacters>();
        var courier = characters.CreateCourier(ShipLayout.CourierStart);
        courier.rotation = Quaternion.Euler(0,180,0);
        Outfits = courier.gameObject.AddComponent<CourierOutfits>();
        Outfits.Initialize(courier);
        Lift = gameObject.AddComponent<ShipLift>();
        Lift.Build(transform, courier, characters);
        Motor = new CourierMotor(courier, Lift);
        Motor.AddObstacle(wardrobe.Root, .02f);
        foreach (var obstacle in living.Obstacles) Motor.AddObstacle(obstacle, .02f);
        foreach (var obstacle in galley.Obstacles) Motor.AddObstacle(obstacle, .02f);
        foreach (var obstacle in interior.Obstacles) Motor.AddObstacle(obstacle, .02f);
        Motor.AddObstacle(aviary.Root, .05f);
        foreach (var (name, inset) in PropObstacles)
        {
            var prop = transform.Find("Cabin furnishings/" + name);
            if (prop == null) prop = transform.Find(name);
            if (prop != null) Motor.AddObstacle(prop, inset);
        }
        Lift.PassengerArrived += deck => Motor.Deck = deck;
        characters.Owl.UseAviary(aviary);

        framing = gameObject.AddComponent<ShipCameraFraming>();
        framing.Initialize(view, transform);
        var hangar = gameObject.AddComponent<ShipHangar>();
        hangar.Build(transform);
        var equipment = gameObject.AddComponent<ShipWardrobe>();
        equipment.Build(hangar.Interior);
        Motor.AddObstacle(equipment.Root, .05f);
        // Registered while the hangar is still active: hidden renderers report empty bounds.
        Motor.AddObstacle(hangar.Craft, .1f);
        foreach (Transform part in hangar.Interior)
            if (HangarObstacles.Contains(part.name)) Motor.AddObstacle(part, .02f);
        framing.ConfigureHangar(hangar.Interior, courier, homeGroups, Lift.Visuals);

        input = new CourierInput();
        Galaxy=new GalaxyAtlas(Terminal);
        GalaxyView=gameObject.AddComponent<GalaxyMapView>();
        GalaxyView.Initialize(Galaxy,input,view);
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
        details.HighlightAtlas(AtGalaxy);
        Motor.Turn(Time.deltaTime);
        if(Galaxy.IsOpen) { GalaxyView.HandleInput(input); return; }
        if (Outfits.IsOpen)
        {
            Outfits.Navigate(input.Move.x);
            if (input.Pressed(CourierAction.Cancel)) Outfits.Close();
            else if (input.Pressed(CourierAction.Interact)) Outfits.Confirm();
            return;
        }
        if (input.Pressed(CourierAction.Camera)) framing.Follow = !framing.Follow;
        framing.ApplyLook(input.LookDelta(Time.deltaTime),input.ZoomDelta);
        if (Terminal.IsOpen)
        {
            if (input.Pressed(CourierAction.Cancel)) Terminal.Close();
            else if (input.Pressed(CourierAction.Interact)) AcceptOrClose();
            return;
        }
        if (Lift.IsRiding) return;
        if (AtGalaxy && input.ClickPressed && GalaxyAtlas.HitsHologram(view.ScreenPointToRay(input.PointerPosition)))
        { Galaxy.Open(); return; }
        float speed = input.Held(CourierAction.Sprint) ? CourierMotor.SprintSpeed : CourierMotor.WalkSpeed;
        Motor.Move(MovementDirection(input.Move), speed * Time.deltaTime);
        if (input.Pressed(CourierAction.Interact)) Interact();
        else if (input.Pressed(CourierAction.Descend)) Descend();
    }

    // Project the camera basis onto the deck so orbiting does not mirror or skew WASD.
    public Vector3 MovementDirection(Vector2 stick)
    {
        var camera = view.transform;
        var right = Vector3.ProjectOnPlane(camera.right,Vector3.up).normalized;
        var forward = Vector3.ProjectOnPlane(camera.forward,Vector3.up).normalized;
        return Vector3.ClampMagnitude(right*stick.x+forward*stick.y,1);
    }

    public void Interact()
    {
        if (Lift.IsRiding) return;
        if (Outfits.IsOpen) return;
        if (Galaxy.IsOpen) return;
        if (AtGalaxy) Galaxy.Open();
        else if (AtWardrobe) Outfits.Open();
        else if (AtLift) Lift.TryUse(Deck);
        else if (AtTerminal) Terminal.Open();
    }

    public void Descend()
    {
        if (!Terminal.IsOpen && !Outfits.IsOpen && !Galaxy.IsOpen && AtLift) Lift.TryUse(Deck, -1);
    }

    public void AcceptOrClose()
    {
        if (Terminal.Accepted) Terminal.Close();
        else Terminal.Accept();
    }
}

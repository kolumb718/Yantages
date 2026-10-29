using UnityEngine;

// Presentation of the imported characters; movement and interactions stay in CourierShip.
// Drives Kristi's locomotion blend from her actual speed and hands the owl to OwlCompanion.
public sealed class CourierCharacters : MonoBehaviour
{
    public const string DisplayName = "Кристи";
    // Ground speed at which each generated cycle's feet do not slide (blend-tree thresholds, m/s).
    public const float WalkCycleSpeed = CourierMotor.WalkSpeed, RunCycleSpeed = CourierMotor.SprintSpeed;
    static readonly int SpeedId = Animator.StringToHash("Speed");

    [SerializeField] GameObject courierPrefab;
    [SerializeField] GameObject owlPrefab;

    Transform courier;
    Animator courierAnimator;
    Vector3 previousPosition;
    CourierLegPlacement legs;
    public CourierLegPlacement Legs => legs;

    public OwlCompanion Owl { get; private set; }

    public Transform CreateCourier(Vector3 position)
    {
        if (courierPrefab == null || owlPrefab == null)
            throw new MissingReferenceException("Assign both character prefabs on CourierCharacters.");

        courier = new GameObject("Courier").transform;
        courier.SetParent(transform, false);
        courier.position = position;
        var visual = Instantiate(courierPrefab, courier, false);
        visual.name = "Kristi";
        courierAnimator = visual.GetComponentInChildren<Animator>();
        if(courierAnimator!=null && courierAnimator.isHuman)
        {
            courierAnimator.Rebind();courierAnimator.Update(0);
            legs=new CourierLegPlacement(courier,courierAnimator);
        }
        previousPosition = position;

        var owl = Instantiate(owlPrefab, transform, false);
        owl.name = "Space owl companion";
        Owl = owl.AddComponent<OwlCompanion>();
        Owl.Begin(courier);
        return courier;
    }

    // ShipLift reports when the courier stands in the glass cabin; the owl joins her there.
    public void SetLiftTravel(bool active)
    {
        if (Owl != null) Owl.CabinTravel = active;
    }

    void LateUpdate()
    {
        if (courier == null || courierAnimator == null || Time.deltaTime <= 0) return;
        var step = courier.position - previousPosition;
        previousPosition = courier.position;
        step.y = 0;
        float speed = step.magnitude / Time.deltaTime;
        // A teleport (respawn, test placement) is not a stride.
        if (speed > 12) speed = 0;
        courierAnimator.SetFloat(SpeedId, speed, .1f, Time.deltaTime);
        // Above the run cycle's natural pace, play it faster instead of letting feet slide.
        courierAnimator.speed = Mathf.Max(1, speed / RunCycleSpeed);
        legs?.Update(speed,Time.deltaTime);
    }
}

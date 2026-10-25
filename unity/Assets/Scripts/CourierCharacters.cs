using UnityEngine;

// Presentation of the imported characters; movement and interactions stay in CourierShip.
public sealed class CourierCharacters : MonoBehaviour
{
    public const string DisplayName = "Кристи";
    [SerializeField] GameObject courierPrefab;
    [SerializeField] GameObject owlPrefab;

    Transform courier;
    Transform owl;
    Vector3 previousPosition;
    bool liftTravel;

    public void SetLiftTravel(bool active)
    {
        liftTravel = active;
    }

    public Transform CreateCourier(Vector3 position)
    {
        if (courierPrefab == null || owlPrefab == null)
            throw new MissingReferenceException("Assign both character prefabs on CourierCharacters.");

        courier = new GameObject("Courier").transform;
        courier.SetParent(transform, false);
        courier.position = position;
        var visual = Instantiate(courierPrefab, courier, false);
        visual.name = "Courier girl";

        owl = Instantiate(owlPrefab, transform, false).transform;
        owl.name = "Space owl companion";
        SnapCompanion();
        return courier;
    }

    Vector3 CompanionPosition()
    {
        return courier.position - courier.forward * .65f + Vector3.right * .75f
            + Vector3.up * (1.05f + Mathf.Sin(Time.time * 2.2f) * .045f);
    }

    public void SnapCompanion()
    {
        if (courier == null || owl == null) return;
        if (liftTravel)
        {
            // Stay inside the glass cabin, moving at exactly the passenger's speed.
            owl.position = courier.position + new Vector3(.45f, 1.05f, -.20f);
            owl.rotation = courier.rotation;
            previousPosition = courier.position;
            return;
        }
        owl.position = CompanionPosition();
        owl.rotation = courier.rotation;
        previousPosition = courier.position;
    }

    void LateUpdate()
    {
        if (courier == null || owl == null) return;
        if (liftTravel)
        {
            SnapCompanion();
            return;
        }
        // Deck changes must not make the owl fly through the intervening floor.
        if (Mathf.Abs(courier.position.y - previousPosition.y) > 1)
            SnapCompanion();
        else
        {
            float blend = 1 - Mathf.Exp(-5 * Time.deltaTime);
            owl.position = Vector3.Lerp(owl.position, CompanionPosition(), blend);
            owl.rotation = Quaternion.Slerp(owl.rotation, courier.rotation, blend);
        }
        previousPosition = courier.position;
    }
}

using UnityEngine;

// Authored cabin props replace the initial cube furniture while preserving routes.
public sealed class ShipFurnishings : MonoBehaviour
{
    [SerializeField] GameObject cabinBedPrefab;
    [SerializeField] GameObject bookshelfPrefab;
    [SerializeField] GameObject sofaPrefab;
    [SerializeField] GameObject monsteraPrefab;
    [SerializeField] GameObject snakePlantPrefab;
    [SerializeField] GameObject pothosPrefab;
    bool built;

    public void Build()
    {
        if (built) return;
        if (cabinBedPrefab == null || bookshelfPrefab == null || sofaPrefab == null
            || monsteraPrefab == null || snakePlantPrefab == null || pothosPrefab == null)
            throw new MissingReferenceException("Assign all six furniture prefabs on ShipFurnishings.");

        var furnishings = new GameObject("Cabin furnishings").transform;
        furnishings.SetParent(transform, false);
        Place(cabinBedPrefab, "Cabin bed", new Vector3(-10, ShipLayout.DeckHeight, -2.5f), 90, furnishings);
        Place(bookshelfPrefab, "Wood bookshelf", new Vector3(-6, ShipLayout.DeckHeight, -4.1f), 0, furnishings);
        Place(sofaPrefab, "Terracotta sofa", new Vector3(-10, 0, -1.8f), 0, furnishings);
        Place(monsteraPrefab, "Monstera downstairs", new Vector3(-13, 0, 2), -15, furnishings);
        Place(snakePlantPrefab, "Snake plant bridge", new Vector3(12, 0, -2), 10, furnishings);
        Place(monsteraPrefab, "Monstera upstairs", new Vector3(-12, ShipLayout.DeckHeight, 1.5f), 20, furnishings);
        Place(pothosPrefab, "Pothos reading nook", new Vector3(-7.9f, ShipLayout.DeckHeight, -3.6f), -20, furnishings);
        built = true;
    }

    static void Place(GameObject prefab, string name, Vector3 position, float yaw, Transform parent)
    {
        var prop = Instantiate(prefab, parent, false);
        prop.name = name;
        prop.transform.localPosition = position;
        prop.transform.localRotation = Quaternion.Euler(0, yaw, 0);
    }
}

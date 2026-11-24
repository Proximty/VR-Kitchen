using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zorgt ervoor dat de te gebruiken objecten (ingrediënten / benodigdheden)
/// op willekeurige plekken in de keuken terechtkomen (over de verschillende soorten plekken).
/// Werkt automatisch met de bestaande 'Random spawn ingredients' in de 5 keuken-prefabs,
/// en kan ook met custom 'KitchenSpawnSpot' componenten werken.
/// </summary>
public class KitchenObjectRandomizer : MonoBehaviour
{
    public static KitchenObjectRandomizer Instance { get; private set; }

    [Header("Instellingen")]
    [Tooltip("Willekeurige kleine Y-rotatie toevoegen aan objecten zodat ze er natuurlijk bijliggen")]
    public bool addRandomYJitter = true;

    [Tooltip("Extra prefab-objecten die je optioneel op random plekken wilt laten spawnen")]
    public List<GameObject> optionalPrefabsToSpawn = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Randomiseert alle te gebruiken objecten in de opgegeven keuken over de beschikbare plekken.
    /// </summary>
    public static void RandomizeKitchenObjects(GameObject kitchen)
    {
        if (kitchen == null) return;

        // 1. Zoek naar de container 'Random spawn ingredients' in de keuken
        Transform randomSpawnContainer = null;
        Transform[] allTransforms = kitchen.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t.name.ToLower().Contains("random spawn"))
            {
                randomSpawnContainer = t;
                break;
            }
        }

        // Als er een container is gevonden met items/plekken
        if (randomSpawnContainer != null && randomSpawnContainer.childCount > 1)
        {
            ShuffleContainerChildrenSpots(randomSpawnContainer);
            return;
        }

        // 2. Fallback: Zoek naar specifieke KitchenSpawnSpot componenten
        KitchenSpawnSpot[] spawnSpots = kitchen.GetComponentsInChildren<KitchenSpawnSpot>(true);
        if (spawnSpots.Length > 0)
        {
            DistributeObjectsOverSpots(kitchen, spawnSpots);
        }
    }

    /// <summary>
    /// Pakt alle objecten onder de container, leest hun spawn-locaties uit en husselt ze
    /// zodat elk object op een andere willekeurige plek terechtkomt.
    /// </summary>
    private static void ShuffleContainerChildrenSpots(Transform container)
    {
        int count = container.childCount;
        if (count <= 1) return;

        List<Transform> items = new List<Transform>();
        List<Vector3> localPositions = new List<Vector3>();
        List<Quaternion> localRotations = new List<Quaternion>();

        for (int i = 0; i < count; i++)
        {
            Transform child = container.GetChild(i);
            items.Add(child);
            localPositions.Add(child.localPosition);
            localRotations.Add(child.localRotation);
        }

        // Fisher-Yates shuffle op de posities en rotaties van de plekken
        for (int i = localPositions.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);

            Vector3 tempPos = localPositions[i];
            localPositions[i] = localPositions[rnd];
            localPositions[rnd] = tempPos;

            Quaternion tempRot = localRotations[i];
            localRotations[i] = localRotations[rnd];
            localRotations[rnd] = tempRot;
        }

        // Wijs elk object toe aan een gehusselde plek
        for (int i = 0; i < items.Count; i++)
        {
            items[i].localPosition = localPositions[i];
            items[i].localRotation = localRotations[i];

            // Optionele kleine natuurlijke rotatie-variatie
            float randomY = Random.Range(-25f, 25f);
            items[i].Rotate(0f, randomY, 0f, Space.Self);
        }

        Debug.Log($"[KitchenObjectRandomizer] {count} objecten onder '{container.name}' succesvol gehusseld over de plekken!");
    }

    /// <summary>
    /// Verdeelt gevonden objecten over gemarkeerde KitchenSpawnSpot plekken.
    /// </summary>
    private static void DistributeObjectsOverSpots(GameObject kitchen, KitchenSpawnSpot[] spots)
    {
        List<KitchenSpawnSpot> availableSpots = new List<KitchenSpawnSpot>(spots);

        // Shuffle de beschikbare plekken
        for (int i = availableSpots.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            var temp = availableSpots[i];
            availableSpots[i] = availableSpots[rnd];
            availableSpots[rnd] = temp;
        }

        // Zoek objecten die verplaatst moeten worden
        List<Transform> movableObjects = new List<Transform>();
        Transform[] children = kitchen.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            string lower = child.name.ToLower();
            if (lower.Contains("burger") || lower.Contains("cheese") || lower.Contains("tomato") ||
                lower.Contains("lettuce") || lower.Contains("cucumber") || lower.Contains("pickle") ||
                lower.Contains("ham") || lower.Contains("egg") || lower.Contains("sauce"))
            {
                if (!child.name.ToLower().Contains("sm_bld") && !child.name.ToLower().Contains("counter"))
                {
                    movableObjects.Add(child);
                }
            }
        }

        int spotIndex = 0;
        foreach (Transform obj in movableObjects)
        {
            if (spotIndex >= availableSpots.Count) break;

            KitchenSpawnSpot spot = availableSpots[spotIndex];
            obj.position = spot.transform.position;
            obj.rotation = spot.transform.rotation;
            spot.isOccupied = true;
            spotIndex++;
        }

        Debug.Log($"[KitchenObjectRandomizer] {spotIndex} objecten verdeeld over {spots.Length} KitchenSpawnSpots.");
    }
}

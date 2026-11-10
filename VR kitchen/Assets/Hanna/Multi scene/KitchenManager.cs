using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Beheert het spawnen, wisselen en roteren/husselen van verschillende keuken-prefabs.
/// Zorgt ervoor dat na iedere voltooide order (of op commando) de keuken automatisch
/// wordt gehusseld (willekeurige prefab en/of willekeurige rotatie) en dat het 2de display
/// direct vlekkeloos meegaat.
/// Werkt volledig zelfstandig zonder dat Nathan's scripts zwaar aangepast hoeven te worden.
/// </summary>
public class KitchenManager : MonoBehaviour
{
    public static KitchenManager Instance { get; private set; }

    [Header("Keuken Prefabs")]
    [Tooltip("Sleep hier al je keuken prefabs in (bijv. prefab 1 t/m 5)")]
    public List<GameObject> kitchenPrefabs = new List<GameObject>();

    [Header("Spawn Instellingen")]
    [Tooltip("Plek waar de keuken moet spawnen. Indien leeg, gebruikt hij de transform van dit object.")]
    public Transform spawnPoint;

    [Tooltip("Huidige actieve prefab index")]
    public int currentKitchenIndex = 0;

    [Header("Hussel / Shuffle Instellingen")]
    [Tooltip("Kies een willekeurige rotatie (0, 90, 180 of 270 graden) bij het husselen")]
    public bool shuffleYRotation = true;

    [Tooltip("Zorgt dat bij het husselen altijd een ANDERE prefab wordt gekozen dan de huidige (als er >1 prefabs zijn)")]
    public bool avoidSamePrefabTwice = true;

    [Header("Rotatie Instellingen")]
    [Tooltip("Laat de keuken ook fysiek roteren bij standaard NextKitchen wissels")]
    public bool applyYRotationOnSwitch = false;
    public float rotationStepDegrees = 90f;
    private float currentYRotation = 0f;

    [Header("Test & Automatisering")]
    [Tooltip("Sneltoetsen in Play Mode: 'R' voor volgende keuken, 'H' voor willekeurig husselen")]
    public bool enableKeyboardShortcuts = true;
    public KeyCode nextKitchenKey = KeyCode.R;
    public KeyCode shuffleKitchenKey = KeyCode.H;

    [Tooltip("Automatisch wisselen na X seconden (0 = uitgeschakeld)")]
    public float autoRotateInterval = 0f;
    private float autoRotateTimer = 0f;

    // Huidige geïnstantieerde keuken in de scene
    public GameObject CurrentKitchenInstance { get; private set; }

    // Event waar de Display 2 manager direct naar luistert
    public event Action<GameObject> OnKitchenChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        // Als er nog geen actieve keuken is, spawn de eerste prefab
        if (CurrentKitchenInstance == null && kitchenPrefabs.Count > 0)
        {
            SpawnKitchen(currentKitchenIndex, currentYRotation);
        }
    }

    private void Update()
    {
        // Sneltoetsen voor testen tijdens development
        if (enableKeyboardShortcuts)
        {
            if (Input.GetKeyDown(nextKitchenKey))
            {
                NextKitchen();
            }
            else if (Input.GetKeyDown(shuffleKitchenKey))
            {
                ShuffleKitchen();
            }
        }

        // Optionele automatische timer
        if (autoRotateInterval > 0f)
        {
            autoRotateTimer += Time.deltaTime;
            if (autoRotateTimer >= autoRotateInterval)
            {
                autoRotateTimer = 0f;
                ShuffleKitchen();
            }
        }
    }

    /// <summary>
    /// Husselt de keuken: kiest een willekeurige prefab én optioneel een willekeurige rotatiehoek (0, 90, 180, 270 graden).
    /// Kan rechtstreeks worden aangeroepen na elke order.
    /// </summary>
    public void ShuffleKitchen()
    {
        if (kitchenPrefabs == null || kitchenPrefabs.Count == 0)
        {
            Debug.LogWarning("[KitchenManager] Kan niet husselen: geen keuken-prefabs toegewezen!");
            return;
        }

        // 1. Kies een willekeurige nieuwe index
        int newIndex = currentKitchenIndex;
        if (kitchenPrefabs.Count > 1 && avoidSamePrefabTwice)
        {
            int attempts = 0;
            while (newIndex == currentKitchenIndex && attempts < 10)
            {
                newIndex = UnityEngine.Random.Range(0, kitchenPrefabs.Count);
                attempts++;
            }
        }
        else
        {
            newIndex = UnityEngine.Random.Range(0, kitchenPrefabs.Count);
        }
        currentKitchenIndex = newIndex;

        // 2. Kies willekeurige rotatie indien gewenst (kwartslagen: 0, 90, 180 of 270 graden)
        if (shuffleYRotation)
        {
            int randomQuarter = UnityEngine.Random.Range(0, 4); // 0, 1, 2 of 3
            currentYRotation = randomQuarter * 90f;
        }

        Debug.Log($"[KitchenManager] Gehusseld naar prefab [{currentKitchenIndex}] met rotatie {currentYRotation}°");

        // 3. Spawn de nieuwe gehusselde keuken
        SpawnKitchen(currentKitchenIndex, currentYRotation);
    }

    /// <summary>
    /// Wisselt naar de volgende keuken in volgorde (circulair).
    /// </summary>
    public void NextKitchen()
    {
        if (kitchenPrefabs == null || kitchenPrefabs.Count == 0) return;

        int nextIndex = (currentKitchenIndex + 1) % kitchenPrefabs.Count;
        if (applyYRotationOnSwitch)
        {
            currentYRotation = (currentYRotation + rotationStepDegrees) % 360f;
        }
        currentKitchenIndex = nextIndex;
        SpawnKitchen(currentKitchenIndex, currentYRotation);
    }

    /// <summary>
    /// Wisselt naar de vorige keuken in volgorde.
    /// </summary>
    public void PreviousKitchen()
    {
        if (kitchenPrefabs == null || kitchenPrefabs.Count == 0) return;

        int prevIndex = currentKitchenIndex - 1;
        if (prevIndex < 0) prevIndex = kitchenPrefabs.Count - 1;
        currentKitchenIndex = prevIndex;
        SpawnKitchen(currentKitchenIndex, currentYRotation);
    }

    /// <summary>
    /// Wisselt direct naar een specifiek prefab index.
    /// </summary>
    public void SwitchKitchen(int newIndex, float yRotation = 0f)
    {
        if (kitchenPrefabs == null || kitchenPrefabs.Count == 0) return;

        currentKitchenIndex = Mathf.Clamp(newIndex, 0, kitchenPrefabs.Count - 1);
        currentYRotation = yRotation;
        SpawnKitchen(currentKitchenIndex, currentYRotation);
    }

    /// <summary>
    /// Spawnt het geselecteerde prefab en ruimt de vorige keuken 100% op.
    /// </summary>
    private void SpawnKitchen(int index, float yRot)
    {
        if (index < 0 || index >= kitchenPrefabs.Count) return;

        GameObject prefabToSpawn = kitchenPrefabs[index];
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"[KitchenManager] Prefab op index {index} is leeg!");
            return;
        }

        // 1. Ruim de oude keuken grondig op
        if (CurrentKitchenInstance != null)
        {
            Destroy(CurrentKitchenInstance);
            CurrentKitchenInstance = null;
        }

        // 2. Bepaal spawnpositie en rotatie
        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion baseRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
        Quaternion finalRot = Quaternion.Euler(baseRot.eulerAngles.x, baseRot.eulerAngles.y + yRot, baseRot.eulerAngles.z);

        // 3. Instantieer de nieuwe keuken
        CurrentKitchenInstance = Instantiate(prefabToSpawn, pos, finalRot);
        CurrentKitchenInstance.name = $"{prefabToSpawn.name}_Active";

        // 3b. Randomiseer de te gebruiken objecten over de verschillende soorten plekken
        KitchenObjectRandomizer.RandomizeKitchenObjects(CurrentKitchenInstance);

        Debug.Log($"[KitchenManager] Nieuwe keuken gespawned: {CurrentKitchenInstance.name} (Index: {index}, Y-Rot: {yRot}°)");

        // 4. Informeer alle luisteraars (waaronder de Display 2 manager)
        OnKitchenChanged?.Invoke(CurrentKitchenInstance);
    }

    /// <summary>
    /// Fysieke extra rotatie toevoegen aan de actieve keuken.
    /// </summary>
    public void RotateKitchenOrientation(float degrees = 90f)
    {
        if (CurrentKitchenInstance != null)
        {
            currentYRotation = (currentYRotation + degrees) % 360f;
            CurrentKitchenInstance.transform.Rotate(0f, degrees, 0f, Space.World);
            OnKitchenChanged?.Invoke(CurrentKitchenInstance);
        }
    }
}

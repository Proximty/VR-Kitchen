using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Beheert de knipperende hulplampjes / markers op de helper UI (Display 2 / top-down tafeldisplay).
/// Detecteert automatisch de voorwerpen en ingrediënten die al in de keuken-prefabs liggen
/// (zoals Burger, Kaas, Sla, Brood, Bord, Fornuis, Koelkast, Frituur, Snijplank, etc.).
/// Groepeert automatisch gestapelde/nabije items (bijv. 6 stukken brood op 1 plek -> 1 lampje)
/// en toont geen storende tekstlabels.
/// </summary>
public class KitchenTopDownDisplayManager : MonoBehaviour
{
    public static KitchenTopDownDisplayManager Instance { get; private set; }

    [Header("Display 2 Camera & Canvas")]
    [Tooltip("De camera die van bovenaf op de keuken kijkt (Display 2). Indien leeg, zoekt het script automatisch.")]
    public Camera topDownCamera;

    [Tooltip("Het Canvas dat op Display 2 staat (of het top-down WorldSpace canvas). Indien leeg, zoekt het script automatisch.")]
    public Canvas display2Canvas;

    [Tooltip("Container (RectTransform) waar markers in geplaatst worden. Wordt aangemaakt indien leeg.")]
    public RectTransform markerContainer;

    [Header("Marker Prefab (Optioneel)")]
    [Tooltip("Optionele custom prefab voor een marker. Als dit leeg is, wordt er automatisch een knipperend lampje gegenereerd.")]
    public GameObject uiMarkerPrefab;

    [Header("Knipper / Lampje Instellingen")]
    public float blinkSpeed = 4.0f;
    public float minScale = 0.85f;
    public float maxScale = 1.25f;
    [Range(0.1f, 1f)] public float minAlpha = 0.4f;
    [Range(0.1f, 1f)] public float maxAlpha = 1.0f;

    [Header("Groepering & Detectie")]
    [Tooltip("Minimale afstand in meters tussen twee lampjes van hetzelfde type zodat gegroepeerde items (zoals 6 broodjes) maar 1 lampje krijgen.")]
    public float minGroupingDistance = 1.2f;

    [FormerlySerializedAs("autoDetectKnownIngredients")]
    [Tooltip("Detecteert automatisch alle ingrediënten en keukenapparaten die in de keuken-prefab liggen.")]
    public bool autoDetectKitchenObjects = true;

    // Backwards-compatibele alias voor serialized scene files
    public bool autoDetectKnownIngredients
    {
        get => autoDetectKitchenObjects;
        set => autoDetectKitchenObjects = value;
    }

    // Interne tracking voor actieve markers
    private class ActiveMarker
    {
        public Transform targetTransform;
        public GameObject markerGameObject;
        public RectTransform rectTransform;
        public Image mainImage;
        public Image glowImage;
        public Color baseColor;
    }

    private readonly List<ActiveMarker> activeMarkers = new List<ActiveMarker>();
    private RectTransform canvasRect;
    private Camera generatedTopDownCamera;
    private bool hasLoggedSetup = false;

    private static Sprite cachedCircleSprite = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Activeer Display 2 als die fysiek aangesloten is
        if (Display.displays.Length > 1 && !Display.displays[1].active)
        {
            Display.displays[1].Activate();
        }
    }

    private void Start()
    {
        SetupDisplayAndCamera();

        // Koppel direct aan KitchenManager events
        if (KitchenManager.Instance != null)
        {
            KitchenManager.Instance.OnKitchenChanged += HandleKitchenChanged;
            if (KitchenManager.Instance.CurrentKitchenInstance != null)
            {
                HandleKitchenChanged(KitchenManager.Instance.CurrentKitchenInstance);
            }
        }

        // Als er nog geen markers zijn, zoek in de scene naar een reeds aanwezige keuken
        if (activeMarkers.Count == 0)
        {
            FindAndHighlightActiveKitchen();
        }
    }

    private void OnDestroy()
    {
        if (KitchenManager.Instance != null)
        {
            KitchenManager.Instance.OnKitchenChanged -= HandleKitchenChanged;
        }
        ClearAllMarkers();

        if (generatedTopDownCamera != null)
        {
            Destroy(generatedTopDownCamera.gameObject);
        }
    }

    /// <summary>
    /// Initialiseert Canvas en Camera voor de UI.
    /// </summary>
    public void SetupDisplayAndCamera(Vector3? kitchenCenter = null)
    {
        // 1. Zoek of valideer Canvas
        if (display2Canvas == null)
        {
            Canvas[] allCanvases = FindObjectsByType<Canvas>();
            foreach (Canvas c in allCanvases)
            {
                if (c.targetDisplay == 1 || c.renderMode == RenderMode.WorldSpace)
                {
                    display2Canvas = c;
                    break;
                }
            }
        }

        if (display2Canvas != null)
        {
            canvasRect = display2Canvas.GetComponent<RectTransform>();

            if (markerContainer == null)
            {
                Transform existing = display2Canvas.transform.Find("MarkerContainer");
                if (existing != null)
                {
                    markerContainer = existing.GetComponent<RectTransform>();
                }
                else
                {
                    GameObject containerObj = new GameObject("MarkerContainer", typeof(RectTransform));
                    containerObj.transform.SetParent(display2Canvas.transform, false);
                    markerContainer = containerObj.GetComponent<RectTransform>();
                    markerContainer.anchorMin = Vector2.zero;
                    markerContainer.anchorMax = Vector2.one;
                    markerContainer.offsetMin = Vector2.zero;
                    markerContainer.offsetMax = Vector2.zero;
                }
            }
        }

        // 2. Zoek of valideer de Top-Down Camera voor Display 2
        if (topDownCamera == null)
        {
            Camera[] allCams = Camera.allCameras;

            // Zoek camera die specifiek Display 2 (index 1) rendert
            foreach (Camera cam in allCams)
            {
                if (cam.targetDisplay == 1)
                {
                    topDownCamera = cam;
                    break;
                }
            }

            // Zoek anders naar een camera met "top" in de naam
            if (topDownCamera == null)
            {
                foreach (Camera cam in allCams)
                {
                    if (cam.name.ToLower().Contains("top"))
                    {
                        topDownCamera = cam;
                        break;
                    }
                }
            }

            // Alleen als er echt geen Display 2 camera is, maak een fallback orthographic camera
            if (topDownCamera == null)
            {
                if (generatedTopDownCamera == null)
                {
                    GameObject camObj = new GameObject("_GeneratedTopDownCamera");
                    generatedTopDownCamera = camObj.AddComponent<Camera>();
                    generatedTopDownCamera.orthographic = true;
                    generatedTopDownCamera.orthographicSize = 4.5f;
                    generatedTopDownCamera.nearClipPlane = 0.1f;
                    generatedTopDownCamera.farClipPlane = 50f;
                    generatedTopDownCamera.clearFlags = CameraClearFlags.Depth;
                    generatedTopDownCamera.targetDisplay = 1;
                    generatedTopDownCamera.enabled = true;
                }

                Vector3 center = kitchenCenter ?? (KitchenManager.Instance?.spawnPoint != null ? KitchenManager.Instance.spawnPoint.position : transform.position);
                generatedTopDownCamera.transform.position = new Vector3(center.x, center.y + 8f, center.z);
                generatedTopDownCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                topDownCamera = generatedTopDownCamera;
            }
        }

        if (!hasLoggedSetup && display2Canvas != null && topDownCamera != null)
        {
            hasLoggedSetup = true;
            if (display2Canvas.renderMode == RenderMode.WorldSpace && display2Canvas.worldCamera == null)
            {
                display2Canvas.worldCamera = topDownCamera;
            }
            Debug.Log($"[KitchenTopDownDisplayManager] Setup gereed: Canvas='{display2Canvas.name}' (Mode: {display2Canvas.renderMode}), Camera='{topDownCamera.name}' (Display: {topDownCamera.targetDisplay})");
        }
    }

    /// <summary>
    /// Zoekt in de actieve scene naar een aanwezige keuken en plaatst direct lampjes.
    /// </summary>
    public void FindAndHighlightActiveKitchen()
    {
        GameObject activeKitchen = null;
        if (KitchenManager.Instance != null && KitchenManager.Instance.CurrentKitchenInstance != null)
        {
            activeKitchen = KitchenManager.Instance.CurrentKitchenInstance;
        }
        else
        {
            // Zoek in actieve scene naar actieve keukenobjecten
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var go in rootObjects)
            {
                if (go.activeInHierarchy && go.name.ToLower().Contains("kitchen prefab"))
                {
                    activeKitchen = go;
                    break;
                }
            }
        }

        if (activeKitchen != null)
        {
            HandleKitchenChanged(activeKitchen);
        }
    }

    /// <summary>
    /// Wordt aangeroepen wanneer een keuken gespawned, gewisseld of geroteerd wordt.
    /// Plaatst knipperende lampjes op de unieke plekken van alle voorwerpen in de keuken.
    /// </summary>
    public void HandleKitchenChanged(GameObject newKitchen)
    {
        ClearAllMarkers();

        if (newKitchen == null) return;

        SetupDisplayAndCamera(newKitchen.transform.position);

        if (markerContainer == null) return;

        // Positioneer optioneel gegenereerde camera boven deze keuken
        if (generatedTopDownCamera != null)
        {
            generatedTopDownCamera.transform.position = new Vector3(newKitchen.transform.position.x, newKitchen.transform.position.y + 8f, newKitchen.transform.position.z);
        }

        // Zoek alle unieke plekken van voorwerpen die in deze keuken-prefab liggen
        List<(Transform targetTransform, string label, Color color)> targets = FindTargetsInKitchen(newKitchen);

        // Maak voor elke unieke plek 1 knipperend lampje aan (zonder tekst)
        foreach (var item in targets)
        {
            CreateMarker(item.targetTransform, item.label, item.color);
        }

        Debug.Log($"[KitchenTopDownDisplayManager] {activeMarkers.Count} lampjes geplaatst op unieke plekken van '{newKitchen.name}'");
    }

    /// <summary>
    /// Voegt een doeltarget alleen toe als er nog niet al een lampje van hetzelfde type op die plek (< minGroupingDistance) staat.
    /// Zorgt dat bijv. 6 broodjes op 1 stapel/schaal maar 1 lampje krijgen.
    /// </summary>
    private void TryAddTarget(List<(Transform targetTransform, string label, Color color)> list, Transform target, string label, Color color)
    {
        if (target == null) return;

        Vector3 targetPos = target.position;
        foreach (var existing in list)
        {
            if (existing.targetTransform == null) continue;

            if (existing.label == label)
            {
                float dist = Vector3.Distance(existing.targetTransform.position, targetPos);
                if (dist < minGroupingDistance)
                {
                    // Er staat al een lampje voor dit type op deze plek
                    return;
                }
            }
        }

        list.Add((target, label, color));
    }

    /// <summary>
    /// Scant de keuken-prefab grondig naar alle aanwezige ingrediënten en stations met automatische afstandsgroepering.
    /// </summary>
    private List<(Transform targetTransform, string label, Color color)> FindTargetsInKitchen(GameObject kitchen)
    {
        var result = new List<(Transform targetTransform, string label, Color color)>();
        var foundTransforms = new HashSet<Transform>();

        // 1. Zoek expliciete KitchenMarkerTarget componenten
        KitchenMarkerTarget[] explicitTargets = kitchen.GetComponentsInChildren<KitchenMarkerTarget>(true);
        foreach (var target in explicitTargets)
        {
            if (target != null && target.isHighlighted && !foundTransforms.Contains(target.transform))
            {
                TryAddTarget(result, target.transform, target.labelName, target.markerColor);
                foundTransforms.Add(target.transform);
            }
        }

        if (!autoDetectKitchenObjects) return result;

        // 2. Zoek alle ingrediënten met IngredientDataChecker (de voorwerpen die in de prefab liggen)
        IngredientDataChecker[] checkers = kitchen.GetComponentsInChildren<IngredientDataChecker>(true);
        foreach (var checker in checkers)
        {
            if (checker == null || foundTransforms.Contains(checker.transform)) continue;

            string label = GetFriendlyIngredientName(checker.gameObject.name, checker.ingredientData);
            Color color = GetColorForIngredient(label, checker.ingredientData);

            TryAddTarget(result, checker.transform, label, color);
            foundTransforms.Add(checker.transform);
        }

        // 3. Zoek het bord (FoodPlate)
        FoodPlate[] plates = kitchen.GetComponentsInChildren<FoodPlate>(true);
        foreach (var plate in plates)
        {
            if (plate != null && !foundTransforms.Contains(plate.transform))
            {
                TryAddTarget(result, plate.transform, "Bord", new Color(0.4f, 0.9f, 1f));
                foundTransforms.Add(plate.transform);
            }
        }

        // 4. Zoek apparaten en losse voorwerpen op naam in de prefab
        Transform[] allTransforms = kitchen.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t == null || foundTransforms.Contains(t)) continue;

            string lower = t.name.ToLower();

            // Sla muren, vloeren, counters en sub-onderdelen over
            if (lower.Contains("sm_bld") || lower.Contains("floor") || lower.Contains("wall") ||
                lower.Contains("counter_section") || lower.Contains("counter_end") || lower.Contains("counter_corner") ||
                lower.Contains("knife") || lower.Contains("fryer_net") || lower.Contains("friespile"))
            {
                continue;
            }

            if (lower.Contains("stove") || lower.Contains("oven"))
            {
                TryAddTarget(result, t, "Fornuis", new Color(1f, 0.45f, 0.1f));
                foundTransforms.Add(t);
            }
            else if (lower.Contains("chopping") || lower.Contains("cutting") || lower.Contains("snijplank"))
            {
                TryAddTarget(result, t, "Snijplank", new Color(0.85f, 0.65f, 0.45f));
                foundTransforms.Add(t);
            }
            else if (lower.Contains("fryer") || lower.Contains("frituur"))
            {
                TryAddTarget(result, t, "Frituur", new Color(1f, 0.6f, 0.2f));
                foundTransforms.Add(t);
            }
            else if (lower.Contains("fridge") || lower.Contains("freezer") || lower.Contains("koelkast"))
            {
                TryAddTarget(result, t, "Koelkast", new Color(0.6f, 0.85f, 1f));
                foundTransforms.Add(t);
            }
            else if (lower.Contains("burger") || lower.Contains("meat") || lower.Contains("vlees"))
            {
                TryAddTarget(result, t, "Burger", new Color(0.8f, 0.35f, 0.15f));
                foundTransforms.Add(t);
            }
            else if (lower.Contains("cheese") || lower.Contains("kaas"))
            {
                TryAddTarget(result, t, "Kaas", new Color(1f, 0.9f, 0.1f));
                foundTransforms.Add(t);
            }
            else if (lower.Contains("lettuce") || lower.Contains("sla"))
            {
                TryAddTarget(result, t, "Sla", new Color(0.2f, 0.95f, 0.2f));
                foundTransforms.Add(t);
            }
            else if (lower.Contains("bun") || lower.Contains("brood"))
            {
                TryAddTarget(result, t, "Brood", new Color(0.9f, 0.65f, 0.3f));
                foundTransforms.Add(t);
            }
            else if (lower.Contains("tomato") || lower.Contains("tomaat"))
            {
                TryAddTarget(result, t, "Tomaat", new Color(1f, 0.2f, 0.2f));
                foundTransforms.Add(t);
            }
            else if (lower.Contains("cucumber") || lower.Contains("pickle") || lower.Contains("komkommer"))
            {
                TryAddTarget(result, t, "Komkommer", new Color(0.3f, 0.85f, 0.4f));
                foundTransforms.Add(t);
            }
            else if (lower.Contains("ham") || lower.Contains("bacon"))
            {
                TryAddTarget(result, t, "Ham", new Color(0.95f, 0.4f, 0.55f));
                foundTransforms.Add(t);
            }
        }

        return result;
    }

    private string GetFriendlyIngredientName(string rawName, IngredientData data)
    {
        if (data != null)
        {
            switch (data.Ingedient)
            {
                case 0: return "Ham";
                case 1: return "Kaas";
                case 2: return "Burger";
                case 3: return "Sla";
                case 4: return "Tomaat";
                case 5: return "Komkommer";
                case 6: return "Brood";
            }
        }

        string l = rawName.ToLower();
        if (l.Contains("cheese") || l.Contains("kaas")) return "Kaas";
        if (l.Contains("burger") || l.Contains("meat") || l.Contains("vlees")) return "Burger";
        if (l.Contains("lettuce") || l.Contains("sla")) return "Sla";
        if (l.Contains("bun") || l.Contains("brood")) return "Brood";
        if (l.Contains("tomato") || l.Contains("tomaat")) return "Tomaat";
        if (l.Contains("cucumber") || l.Contains("pickle") || l.Contains("komkommer")) return "Komkommer";
        if (l.Contains("ham") || l.Contains("bacon")) return "Ham";

        return rawName.Replace("(Clone)", "").Replace(" Variant", "").Trim();
    }

    private Color GetColorForIngredient(string label, IngredientData data)
    {
        if (data != null)
        {
            switch (data.Ingedient)
            {
                case 0: return new Color(0.95f, 0.4f, 0.55f); // Ham: roze
                case 1: return new Color(1f, 0.9f, 0.1f);      // Kaas: geel
                case 2: return new Color(0.8f, 0.35f, 0.15f); // Burger: bruin
                case 3: return new Color(0.2f, 0.95f, 0.2f);  // Sla: frisgroen
                case 4: return new Color(1f, 0.2f, 0.2f);      // Tomaat: helderrood
                case 5: return new Color(0.3f, 0.85f, 0.4f);  // Komkommer: groen
                case 6: return new Color(0.9f, 0.65f, 0.3f);  // Brood: goudbruin
            }
        }

        string l = label.ToLower();
        if (l.Contains("kaas") || l.Contains("cheese")) return new Color(1f, 0.9f, 0.1f);
        if (l.Contains("burger") || l.Contains("meat")) return new Color(0.8f, 0.35f, 0.15f);
        if (l.Contains("sla") || l.Contains("lettuce")) return new Color(0.2f, 0.95f, 0.2f);
        if (l.Contains("brood") || l.Contains("bun")) return new Color(0.9f, 0.65f, 0.3f);
        if (l.Contains("tomaat") || l.Contains("tomato")) return new Color(1f, 0.2f, 0.2f);
        if (l.Contains("komkommer") || l.Contains("cucumber")) return new Color(0.3f, 0.85f, 0.4f);
        if (l.Contains("ham")) return new Color(0.95f, 0.4f, 0.55f);

        return Color.yellow;
    }

    /// <summary>
    /// Maakt een knipperend lampje UI-element aan in de container.
    /// </summary>
    private void CreateMarker(Transform target, string label, Color color)
    {
        GameObject markerObj;
        Image mainImg = null;
        Image glowImg = null;

        if (uiMarkerPrefab != null)
        {
            markerObj = Instantiate(uiMarkerPrefab, markerContainer);
            mainImg = markerObj.GetComponent<Image>() ?? markerObj.GetComponentInChildren<Image>();

            // Verberg eventuele teksten in een custom prefab ("en der mag geen tekst bij")
            Text[] txts = markerObj.GetComponentsInChildren<Text>(true);
            foreach (var t in txts) t.gameObject.SetActive(false);
        }
        else
        {
            markerObj = CreateDefaultLampjeObject(label, color, out mainImg, out glowImg);
            markerObj.transform.SetParent(markerContainer, false);
        }

        RectTransform rTransform = markerObj.GetComponent<RectTransform>();

        activeMarkers.Add(new ActiveMarker
        {
            targetTransform = target,
            markerGameObject = markerObj,
            rectTransform = rTransform,
            mainImage = mainImg,
            glowImage = glowImg,
            baseColor = color
        });
    }

    private static Sprite GetOrCreateCircleSprite()
    {
        if (cachedCircleSprite != null) return cachedCircleSprite;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Proc_CircleSprite",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (size - 1) * 0.5f;
        float radius = size * 0.5f - 1.5f;

        Color[] colors = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(colors);
        tex.Apply();

        cachedCircleSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedCircleSprite;
    }

    /// <summary>
    /// Genereert een mooi knipperend rond lampje met gloed (zonder enige tekst).
    /// </summary>
    private GameObject CreateDefaultLampjeObject(string label, Color color, out Image mainImg, out Image glowImg)
    {
        Sprite circleSprite = GetOrCreateCircleSprite();

        // 1. Root object
        GameObject root = new GameObject($"Lampje_{label}", typeof(RectTransform));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(34f, 34f);

        // 2. Glow ring (buitenste pulserende gloed)
        GameObject glowObj = new GameObject("Glow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        glowObj.transform.SetParent(root.transform, false);
        RectTransform glowRt = glowObj.GetComponent<RectTransform>();
        glowRt.sizeDelta = new Vector2(50f, 50f);
        glowImg = glowObj.GetComponent<Image>();
        if (circleSprite != null) glowImg.sprite = circleSprite;
        glowImg.color = new Color(color.r, color.g, color.b, 0.35f);

        // 3. Hoofd lampje (heldere kern van het lichtje)
        GameObject coreObj = new GameObject("Core", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        coreObj.transform.SetParent(root.transform, false);
        RectTransform coreRt = coreObj.GetComponent<RectTransform>();
        coreRt.sizeDelta = new Vector2(26f, 26f);
        mainImg = coreObj.GetComponent<Image>();
        if (circleSprite != null) mainImg.sprite = circleSprite;
        mainImg.color = color;

        // GEEN tekstlabel aanwezig ("en der mag geen tekst bij")

        return root;
    }

    private void LateUpdate()
    {
        if (display2Canvas == null || canvasRect == null) return;

        // Als er nog geen markers zijn, zoek alsnog een actieve keuken
        if (activeMarkers.Count == 0)
        {
            FindAndHighlightActiveKitchen();
            if (activeMarkers.Count == 0) return;
        }

        // Bereken pulserende knipperwaarde
        float sinPulse = (Mathf.Sin(Time.time * blinkSpeed) + 1.0f) * 0.5f;
        float currentScale = Mathf.Lerp(minScale, maxScale, sinPulse);
        float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, sinPulse);

        bool isWorldSpace = (display2Canvas.renderMode == RenderMode.WorldSpace);
        Camera camForCanvas = isWorldSpace ? topDownCamera : null;

        for (int i = activeMarkers.Count - 1; i >= 0; i--)
        {
            ActiveMarker m = activeMarkers[i];

            if (m.targetTransform == null || !m.targetTransform.gameObject.activeInHierarchy)
            {
                if (m.markerGameObject != null) m.markerGameObject.SetActive(false);
                continue;
            }

            if (topDownCamera != null)
            {
                Vector3 targetWorldPos = m.targetTransform.position;
                Vector3 screenPoint = topDownCamera.WorldToScreenPoint(targetWorldPos);

                // Controleer of het object zich voor de camera bevindt
                if (screenPoint.z <= 0f)
                {
                    if (m.markerGameObject.activeSelf) m.markerGameObject.SetActive(false);
                    continue;
                }

                // Controleer of het object binnen het zichtbare display van de camera valt
                float margin = 10f;
                bool isInsideScreen = (screenPoint.x >= margin && screenPoint.x <= Screen.width - margin &&
                                       screenPoint.y >= margin && screenPoint.y <= Screen.height - margin);

                if (!isInsideScreen)
                {
                    if (m.markerGameObject.activeSelf) m.markerGameObject.SetActive(false);
                    continue;
                }

                if (!m.markerGameObject.activeSelf) m.markerGameObject.SetActive(true);

                // Projecteer het exacte schermpunt direct naar de lokale canvascoördinaten
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, camForCanvas, out Vector2 localPoint))
                {
                    m.rectTransform.anchoredPosition = localPoint;
                }
            }
            else
            {
                if (!m.markerGameObject.activeSelf) m.markerGameObject.SetActive(true);
            }

            // Pas knipperen en gloeien toe
            m.rectTransform.localScale = new Vector3(currentScale, currentScale, 1f);

            if (m.mainImage != null)
            {
                Color c = m.baseColor;
                c.a = currentAlpha;
                m.mainImage.color = c;
            }

            if (m.glowImage != null)
            {
                Color gc = m.baseColor;
                gc.a = currentAlpha * 0.45f;
                m.glowImage.color = gc;
            }
        }
    }

    /// <summary>
    /// Ruimt direct alle aangemaakte markers netjes op.
    /// </summary>
    public void ClearAllMarkers()
    {
        foreach (var m in activeMarkers)
        {
            if (m.markerGameObject != null)
            {
                Destroy(m.markerGameObject);
            }
        }
        activeMarkers.Clear();
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Beheert de knipperende hulplampjes / markers op Display 2 (de top-down helper display).
/// Werkt automatisch samen met KitchenManager: bij het wisselen of roteren van een keuken
/// worden alle markers direct en vlekkeloos vernieuwd op de juiste plekken.
/// </summary>
public class KitchenTopDownDisplayManager : MonoBehaviour
{
    [Header("Display 2 Camera & Canvas")]
    [Tooltip("De camera die op Display 2 (top-down) kijkt. Indien leeg, zoekt het script automatisch.")]
    public Camera topDownCamera;

    [Tooltip("Het Canvas dat op Display 2 staat. Indien leeg, zoekt het script automatisch.")]
    public Canvas display2Canvas;

    [Tooltip("Container (RectTransform) waar markers in geplaatst worden. Wordt aangemaakt indien leeg.")]
    public RectTransform markerContainer;

    [Header("Marker Prefab (Optioneel)")]
    [Tooltip("Optionele custom prefab voor een marker. Als dit leeg is, wordt er automatisch een mooie knipperende indicator gegenereerd.")]
    public GameObject uiMarkerPrefab;

    [Header("Knipper / Puls Instellingen")]
    public float blinkSpeed = 5.0f;
    public float minScale = 0.8f;
    public float maxScale = 1.3f;
    [Range(0.1f, 1f)] public float minAlpha = 0.3f;
    [Range(0.1f, 1f)] public float maxAlpha = 1.0f;

    [Header("Automatische Detectie")]
    [Tooltip("Scant automatisch naar bekende ingrediëntnamen (Burger, Kaas, Tomaat, etc.) als er geen KitchenMarkerTarget componenten zijn.")]
    public bool autoDetectKnownIngredients = true;

    // Interne tracking voor actieve markers
    private class ActiveMarker
    {
        public Transform targetTransform;
        public GameObject markerGameObject;
        public RectTransform rectTransform;
        public Image image;
        public Color baseColor;
    }

    private readonly List<ActiveMarker> activeMarkers = new List<ActiveMarker>();
    private RectTransform canvasRect;

    private static readonly string[] DefaultIngredientKeywords = new string[]
    {
        "burger", "cheese", "kaas", "tomato", "tomaat", "lettuce", "sla",
        "cucumber", "pickle", "komkommer", "ham", "egg", "ei", "sauce",
        "bun", "brood", "cutting", "stove", "fryer"
    };

    private void Awake()
    {
        // Activeer Display 2 als dat nog niet gedaan is
        if (Display.displays.Length > 1 && !Display.displays[1].active)
        {
            Display.displays[1].Activate();
        }
    }

    private void Start()
    {
        SetupDisplayAndCamera();

        // Koppel aan KitchenManager events
        if (KitchenManager.Instance != null)
        {
            KitchenManager.Instance.OnKitchenChanged += HandleKitchenChanged;
            if (KitchenManager.Instance.CurrentKitchenInstance != null)
            {
                HandleKitchenChanged(KitchenManager.Instance.CurrentKitchenInstance);
            }
        }
        else
        {
            // Zoek als fallback naar een bestaande actieve keuken in de scene
            GameObject activeKitchen = GameObject.Find("kitchen prefab 2") ?? GameObject.Find("kitchen prefab 1 Variant_Active");
            if (activeKitchen != null)
            {
                HandleKitchenChanged(activeKitchen);
            }
        }
    }

    private void OnDestroy()
    {
        if (KitchenManager.Instance != null)
        {
            KitchenManager.Instance.OnKitchenChanged -= HandleKitchenChanged;
        }
        ClearAllMarkers();
    }

    /// <summary>
    /// Zoekt de juiste camera (TargetDisplay 1) en canvas op Display 2.
    /// </summary>
    public void SetupDisplayAndCamera()
    {
        // 1. Zoek TopDown Camera indien niet ingesteld
        if (topDownCamera == null)
        {
            Camera[] allCams = Camera.allCameras;
            foreach (Camera cam in allCams)
            {
                // Display 2 = targetDisplay index 1
                if (cam.targetDisplay == 1)
                {
                    topDownCamera = cam;
                    break;
                }
            }

            if (topDownCamera == null)
            {
                GameObject camObj = GameObject.Find("Camera") ?? GameObject.FindWithTag("TopDownCamera");
                if (camObj != null)
                {
                    topDownCamera = camObj.GetComponent<Camera>();
                }
            }
        }

        // 2. Zoek Canvas voor Display 2
        if (display2Canvas == null)
        {
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas c in allCanvases)
            {
                if (c.targetDisplay == 1)
                {
                    display2Canvas = c;
                    break;
                }
            }

            if (display2Canvas == null && allCanvases.Length > 0)
            {
                display2Canvas = allCanvases[0];
            }
        }

        if (display2Canvas != null)
        {
            canvasRect = display2Canvas.GetComponent<RectTransform>();

            // Maak een marker container als die er nog niet is
            if (markerContainer == null)
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

    /// <summary>
    /// Wordt aangeroepen wanneer de keuken roteert of wisselt naar een nieuwe prefab.
    /// </summary>
    public void HandleKitchenChanged(GameObject newKitchen)
    {
        // 1. Oude markers 100% vlekkeloos opruimen
        ClearAllMarkers();

        if (newKitchen == null) return;

        if (topDownCamera == null || markerContainer == null)
        {
            SetupDisplayAndCamera();
        }

        if (markerContainer == null)
        {
            Debug.LogWarning("[KitchenTopDownDisplayManager] Geen markerContainer gevonden op Display 2!");
            return;
        }

        // 2. Zoek alle targets in de nieuwe keuken
        List<(Transform targetTransform, string label, Color color)> targets = FindTargetsInKitchen(newKitchen);

        // 3. Maak voor elk target een knipperende UI marker aan
        foreach (var item in targets)
        {
            CreateMarker(item.targetTransform, item.label, item.color);
        }

        Debug.Log($"[KitchenTopDownDisplayManager] {activeMarkers.Count} knipperlampjes geactiveerd voor {newKitchen.name}");
    }

    /// <summary>
    /// Scant de keuken-hiërarchie naar ingrediënten en stations.
    /// </summary>
    private List<(Transform targetTransform, string label, Color color)> FindTargetsInKitchen(GameObject kitchen)
    {
        var result = new List<(Transform targetTransform, string label, Color color)>();
        var foundTransforms = new HashSet<Transform>();

        // A. Zoek eerst naar expliciete KitchenMarkerTarget componenten
        KitchenMarkerTarget[] explicitTargets = kitchen.GetComponentsInChildren<KitchenMarkerTarget>(true);
        foreach (var target in explicitTargets)
        {
            if (target != null && target.isHighlighted && !foundTransforms.Contains(target.transform))
            {
                result.Add((target.transform, target.labelName, target.markerColor));
                foundTransforms.Add(target.transform);
            }
        }

        // B. Automatische detectie op basis van kind-objecten (bijv. "Ingrediets" parent of namen)
        if (autoDetectKnownIngredients)
        {
            Transform[] allChildren = kitchen.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (foundTransforms.Contains(child)) continue;

                string lowerName = child.name.ToLower();

                // Controleer of de transform direct onder een 'Ingrediets' mapje staat of matcht met ingrediënten
                bool isUnderIngredients = child.parent != null && child.parent.name.ToLower().Contains("ingredie");
                bool isMatch = false;

                foreach (string keyword in DefaultIngredientKeywords)
                {
                    if (lowerName.Contains(keyword))
                    {
                        isMatch = true;
                        break;
                    }
                }

                if (isUnderIngredients || (isMatch && !lowerName.Contains("sm_bld") && !lowerName.Contains("sm_prop_kitchen_counter")))
                {
                    Color markerColor = GetColorForIngredient(lowerName);
                    result.Add((child, child.name, markerColor));
                    foundTransforms.Add(child);
                }
            }
        }

        return result;
    }

    private Color GetColorForIngredient(string name)
    {
        if (name.Contains("cheese") || name.Contains("kaas")) return new Color(1f, 0.85f, 0.1f);
        if (name.Contains("tomato") || name.Contains("tomaat")) return new Color(1f, 0.2f, 0.2f);
        if (name.Contains("lettuce") || name.Contains("sla")) return new Color(0.2f, 0.9f, 0.2f);
        if (name.Contains("burger") || name.Contains("meat")) return new Color(0.7f, 0.35f, 0.15f);
        if (name.Contains("cucumber") || name.Contains("pickle")) return new Color(0.3f, 0.8f, 0.4f);
        if (name.Contains("egg") || name.Contains("ei")) return new Color(1f, 0.95f, 0.7f);
        if (name.Contains("ham") || name.Contains("bacon")) return new Color(0.9f, 0.4f, 0.5f);
        return Color.yellow; // Standaard signaalkleur
    }

    /// <summary>
    /// Maakt een marker UI-element aan gekoppeld aan een 3D transform.
    /// </summary>
    private void CreateMarker(Transform target, string label, Color color)
    {
        GameObject markerObj;

        if (uiMarkerPrefab != null)
        {
            markerObj = Instantiate(uiMarkerPrefab, markerContainer);
        }
        else
        {
            // Genereer automatisch een cirkelvormig knipperlichtje met glow
            markerObj = CreateDefaultMarkerObject(label, color);
            markerObj.transform.SetParent(markerContainer, false);
        }

        RectTransform rTransform = markerObj.GetComponent<RectTransform>();
        Image img = markerObj.GetComponentInChildren<Image>();

        if (img != null)
        {
            img.color = color;
        }

        activeMarkers.Add(new ActiveMarker
        {
            targetTransform = target,
            markerGameObject = markerObj,
            rectTransform = rTransform,
            image = img,
            baseColor = color
        });
    }

    /// <summary>
    /// Maakt procedureel een mooi knipperend lampje aan als er geen prefab is toegewezen.
    /// </summary>
    private GameObject CreateDefaultMarkerObject(string label, Color color)
    {
        GameObject go = new GameObject($"Marker_{label}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(32f, 32f);

        Image img = go.GetComponent<Image>();
        img.color = color;

        // Maak kind-object met tekstlabel
        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObj.transform.SetParent(go.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchoredPosition = new Vector2(0f, -22f);
        textRt.sizeDelta = new Vector2(100f, 20f);

        Text txt = textObj.GetComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 12;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        return go;
    }

    private void LateUpdate()
    {
        if (topDownCamera == null || canvasRect == null || activeMarkers.Count == 0) return;

        float sinPulse = (Mathf.Sin(Time.time * blinkSpeed) + 1.0f) * 0.5f;
        float currentScale = Mathf.Lerp(minScale, maxScale, sinPulse);
        float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, sinPulse);

        Vector2 canvasSize = canvasRect.rect.size;

        for (int i = activeMarkers.Count - 1; i >= 0; i--)
        {
            ActiveMarker m = activeMarkers[i];

            // Als het doeltarget verwijderd of inactief is, verberg marker
            if (m.targetTransform == null || !m.targetTransform.gameObject.activeInHierarchy)
            {
                if (m.markerGameObject != null) m.markerGameObject.SetActive(false);
                continue;
            }

            // Bereken de positie via de TopDown Camera
            Vector3 viewportPos = topDownCamera.WorldToViewportPoint(m.targetTransform.position);

            // Binnen het camerabereik van de top-down display?
            if (viewportPos.z > 0 && viewportPos.x >= 0f && viewportPos.x <= 1f && viewportPos.y >= 0f && viewportPos.y <= 1f)
            {
                if (!m.markerGameObject.activeSelf) m.markerGameObject.SetActive(true);

                // Converteer Viewport (0..1) naar Canvas lokale positie
                float uiX = (viewportPos.x - 0.5f) * canvasSize.x;
                float uiY = (viewportPos.y - 0.5f) * canvasSize.y;
                m.rectTransform.anchoredPosition = new Vector2(uiX, uiY);

                // Pas knipper- en pulsanimatie toe
                m.rectTransform.localScale = new Vector3(currentScale, currentScale, 1f);

                if (m.image != null)
                {
                    Color c = m.baseColor;
                    c.a = currentAlpha;
                    m.image.color = c;
                }
            }
            else
            {
                if (m.markerGameObject.activeSelf) m.markerGameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Ruimt direct alle aangemaakte markers op zonder geheugenlekken.
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

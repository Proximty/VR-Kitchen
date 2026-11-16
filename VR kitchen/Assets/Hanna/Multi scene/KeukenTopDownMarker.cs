using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Standalone script dat op een individueel object kan worden geplaatst om
/// een knipperende marker op Display 2 weer te geven.
/// </summary>
public class KeukenTopDownMarker : MonoBehaviour
{
    [Header("Instellingen")]
    public GameObject uiMarkerPrefab;    // Sleep hier je UI knipperbolletje prefab in
    public Transform canvasParent;       // Het UI Canvas dat op Display 2 staat
    public Camera topDownCamera;         // De camera die recht van boven naar de keuken kijkt

    private GameObject spawnedMarker;
    private RectTransform markerRect;
    private Image markerImage;

    [Header("Knipper Instellingen")]
    public float minScale = 0.8f;
    public float maxScale = 1.3f;
    public float knipperSnelheid = 5.0f;

    void Start()
    {
        // 1. Automatisch de top-down camera zoeken als deze niet is ingesteld
        if (topDownCamera == null)
        {
            Camera[] allCams = Camera.allCameras;
            foreach (Camera cam in allCams)
            {
                if (cam.targetDisplay == 1)
                {
                    topDownCamera = cam;
                    break;
                }
            }

            if (topDownCamera == null)
            {
                GameObject camObj = GameObject.Find("Camera");
                if (camObj != null) topDownCamera = camObj.GetComponent<Camera>();
            }
        }

        // 2. Zoek het canvas voor Display 2 als het leeg is
        if (canvasParent == null)
        {
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas c in allCanvases)
            {
                if (c.targetDisplay == 1)
                {
                    canvasParent = c.transform;
                    break;
                }
            }
        }

        // 3. Maak het knipperbolletje aan op het canvas
        if (canvasParent != null)
        {
            if (uiMarkerPrefab != null)
            {
                spawnedMarker = Instantiate(uiMarkerPrefab, canvasParent);
            }
            else
            {
                // Fallback eenvoudig cirkel UI-object
                spawnedMarker = new GameObject($"Marker_{gameObject.name}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                spawnedMarker.transform.SetParent(canvasParent, false);
                spawnedMarker.GetComponent<RectTransform>().sizeDelta = new Vector2(30f, 30f);
            }

            markerRect = spawnedMarker.GetComponent<RectTransform>();
            markerImage = spawnedMarker.GetComponent<Image>();
        }
    }

    void Update()
    {
        if (topDownCamera != null && spawnedMarker != null && canvasParent != null)
        {
            Vector3 viewportPos = topDownCamera.WorldToViewportPoint(transform.position);

            if (viewportPos.z > 0 && viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1)
            {
                spawnedMarker.SetActive(true);

                RectTransform canvasRect = canvasParent.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    float uiX = (viewportPos.x - 0.5f) * canvasRect.rect.width;
                    float uiY = (viewportPos.y - 0.5f) * canvasRect.rect.height;

                    markerRect.anchoredPosition = new Vector2(uiX, uiY);

                    float sinPulse = (Mathf.Sin(Time.time * knipperSnelheid) + 1.0f) * 0.5f;
                    float scaleFactor = Mathf.Lerp(minScale, maxScale, sinPulse);
                    markerRect.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

                    if (markerImage != null)
                    {
                        Color kleur = markerImage.color;
                        kleur.a = Mathf.Lerp(0.3f, 1.0f, sinPulse);
                        markerImage.color = kleur;
                    }
                }
            }
            else
            {
                spawnedMarker.SetActive(false);
            }
        }
    }

    void OnDestroy()
    {
        if (spawnedMarker != null)
        {
            Destroy(spawnedMarker);
        }
    }
}

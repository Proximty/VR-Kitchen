using UnityEngine;
using UnityEngine.UI;

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
            GameObject camObj = GameObject.FindWithTag("TopDownCamera"); // Geef je camera eventueel deze tag
            if (camObj != null) topDownCamera = camObj.GetComponent<Camera>();
        }

        // 2. Maak het knipperbolletje aan op het canvas
        if (uiMarkerPrefab != null && canvasParent != null)
        {
            spawnedMarker = Instantiate(uiMarkerPrefab, canvasParent);
            markerRect = spawnedMarker.GetComponent<RectTransform>();
            markerImage = spawnedMarker.GetComponent<Image>();
        }
    }

    void Update()
    {
        if (topDownCamera != null && spawnedMarker != null && canvasParent != null)
        {
            // Vertaal de 3D positie van deze prefab naar Viewport coördinaten (0.0 tot 1.0) van de top-down camera
            Vector3 viewportPos = topDownCamera.WorldToViewportPoint(transform.position);

            // Controleer of de prefab binnen het beeld van de camera valt
            if (viewportPos.z > 0 && viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1)
            {
                spawnedMarker.SetActive(true);

                // Reken viewport coördinaten om naar de afmetingen van het UI Canvas (RectTransform)
                RectTransform canvasRect = canvasParent.GetComponent<RectTransform>();

                float uiX = (viewportPos.x - 0.5f) * canvasRect.rect.width;
                float uiY = (viewportPos.y - 0.5f) * canvasRect.rect.height;

                markerRect.anchoredPosition = new Vector2(uiX, uiY);

                // Knipper- / pulserend effect (grootte en doorzichtigheid)
                float scaleFactor = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * knipperSnelheid) + 1.0f) / 2.0f);
                markerRect.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

                if (markerImage != null)
                {
                    Color kleur = markerImage.color;
                    kleur.a = Mathf.Lerp(0.3f, 1.0f, (Mathf.Sin(Time.time * knipperSnelheid) + 1.0f) / 2.0f);
                    markerImage.color = kleur;
                }
            }
            else
            {
                // Verberg het bolletje als de prefab buiten het camerabereik is
                spawnedMarker.SetActive(false);
            }
        }
    }

    void OnDestroy()
    {
        // Ruim het bolletje op als de keuken-prefab wordt verwijderd
        if (spawnedMarker != null)
        {
            Destroy(spawnedMarker);
        }
    }
}
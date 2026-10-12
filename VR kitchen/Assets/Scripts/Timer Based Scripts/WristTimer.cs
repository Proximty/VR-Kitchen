using UnityEngine;
using TMPro;

public class WristTimer : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public GameObject canvasObject;

    [Header("Timer Settings")]
    private float timeRemaining = 120f; // 120 seconds = 2 minutes
    private bool timerIsRunning = true;

    [Header("Tilt Detection Settings")]
    public Transform headTransform; // Drag your Main Camera (XR Camera) here
    public Transform handTransform; // Drag your hand/controller transform here

    [Tooltip("Adjust between 0 and 1. Higher values require a more direct palm-to-face angle.")]
    [Range(0f, 1f)]
    public float tiltThreshold = 0.5f;

    void Start()
    {
        // Automatically find the main camera if headTransform isn't assigned
        if (headTransform == null && Camera.main != null)
        {
            headTransform = Camera.main.transform;
        }

        if (handTransform == null)
        {
            handTransform = transform; // Defaults to this object if not set
        }
    }

    void Update()
    {
        // 1. Handle the 2-minute countdown
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else
            {
                timeRemaining = 0;
                timerIsRunning = false;
                timerText.text = "00:00";
            }
        }

        // 2. Handle Hand Tilt / Visibility Check
        CheckHandTilt();
    }

    void DisplayTime(float timeToDisplay)
    {
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void CheckHandTilt()
    {
        if (headTransform == null || handTransform == null) return;

        // Determine palm direction.
        Vector3 palmNormal = handTransform.up;

        // Direction from hand to the user's head/eyes
        Vector3 directionToHead = (headTransform.position - handTransform.position).normalized;

        // Use Dot Product to see if the palm is facing the head
        float dotProduct = Vector3.Dot(palmNormal, directionToHead);

        // If the dot product crosses our threshold, turn the canvas on; otherwise, turn it off
        if (dotProduct > tiltThreshold)
        {
            if (!canvasObject.activeSelf) canvasObject.SetActive(true);
        }
        else
        {
            if (canvasObject.activeSelf) canvasObject.SetActive(false);
        }
    }
}
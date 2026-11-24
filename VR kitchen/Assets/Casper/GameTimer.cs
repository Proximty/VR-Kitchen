using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Invisible 3-minute timer. Attach to any GameObject.
public class KitchenTimer : MonoBehaviour
{
    [SerializeField] private float duration = 180f; 
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private string sceneToLoad = "EndScene"; 

    public UnityEvent onTimerFinished;

    private float remaining;
    private bool running;

    public float TimeRemaining => remaining;

    private void Start()
    {
        remaining = duration;
        running = startOnAwake;
    }

    public void StartTimer() { running = true; }

    public void ResetTimer()
    {
        remaining = duration;
        running = false;
    }

    private void Update()
    {
        if (!running) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            remaining = 0f;
            running = false;
            onTimerFinished?.Invoke();
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}

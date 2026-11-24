using UnityEngine;
using UnityEngine.Splines;

public class CustomerPathMover : MonoBehaviour
{
    [Header("Spline Reference")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private float moveSpeed = 5f; // Units per second

    private bool isMoving = false;
    private float progress = 0f; // Normalized 0 to 1

    public void BeginMoving(SplineContainer targetSpline)
    {
        splineContainer = targetSpline;
        if (splineContainer == null)
        {
            Debug.LogError("Spline Container is missing!");
            return;     
        }

        isMoving = true;
        progress = 0f;
    }

    private void Update()
    {
        if (!isMoving || splineContainer == null) return;

        // Advance progress based on length and speed
        float length = splineContainer.CalculateLength();
        if (length > 0f)
        {
            progress += (moveSpeed * Time.deltaTime) / length;
        }

        progress = Mathf.Clamp01(progress);

        // Evaluate position and tangent (rotation) on the spline
        Vector3 position = splineContainer.EvaluatePosition(progress);
        transform.position = position;

        Vector3 tangent = splineContainer.EvaluateTangent(progress);
        if (tangent != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(tangent);
        }

        // Reached the end of the path
        if (progress >= 1f)
        {
            isMoving = false;
            OnExitCompleted();
        }
    }

    private void OnExitCompleted()
    {
        // Destroy or return the customer object to an object pool
        Destroy(gameObject);
    }
}
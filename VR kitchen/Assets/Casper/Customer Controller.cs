using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(Order))]
public class CustomerController : MonoBehaviour
{
    private Order customerOrder;

    [Header("Spline Movement References")]
    [SerializeField] private CustomerPathMover customMover; // Your custom script from earlier
    [SerializeField] private SplineContainer exitSpline;

    [Header("Waiting Configuration")]
    [SerializeField] private Transform assignedWaitingPoint; // The select point/slot where they wait
    private bool isWaiting = true;

    private void Awake()
    {
        customerOrder = GetComponent<Order>();
        if (customMover == null) customMover = GetComponent<CustomerPathMover>();
    }

    private void Start()
    {
        // Position the customer at their designated waiting spot immediately upon spawn
        if (assignedWaitingPoint != null)
        {
            transform.position = assignedWaitingPoint.position;
            transform.rotation = assignedWaitingPoint.rotation;
        }
    }

    /// <summary>
    /// Call this method from your OrderManager or GameController when the order is fulfilled.
    /// </summary>
    public void CompleteCustomerOrder()
    {
        if (!isWaiting) return; // Prevent triggering multiple times

        isWaiting = false;
        Debug.Log($"Order ready for {customerOrder.customerName}! Moving out along the spline.");

        // Hand over control to the spline movement system
        if (customMover != null && exitSpline != null)
        {
            customMover.BeginMoving(exitSpline);
        }
        else
        {
            Debug.LogWarning("Exit spline or path mover component is missing!");
        }
    }

    // Optional method if you want to assign their waiting point dynamically
    public void SetWaitingPoint(Transform newPoint)
    {
        assignedWaitingPoint = newPoint;
        if (isWaiting && assignedWaitingPoint != null)
        {
            transform.position = assignedWaitingPoint.position;
            transform.rotation = assignedWaitingPoint.rotation;
        }
    }
}
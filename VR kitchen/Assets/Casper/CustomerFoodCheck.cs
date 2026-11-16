using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class CustomerFoodCheck : MonoBehaviour
{
    [Header("Customer Settings")]
    [Tooltip("Index nummer van de customer (bijv. 1 voor Customer 1, 2 voor Customer 2)")]
    [SerializeField] private int customerID = 1;

    [Header("Gekoppelde Order")]
    [Tooltip("De gegenereerde order die aan deze customer is gekoppeld")]
    [SerializeField] public Order assignedOrder;

    [Header("Spline & Waiting Spot")]
    [SerializeField] private SplineAnimate splinePathFollower;
    [Tooltip("Drag the empty GameObject Transform for this customer's waiting spot here")]
    [SerializeField] private Transform waitingSpotTransform;
    [Tooltip("Delay in seconds before this customer starts walking (staggers them so they don't overlap)")]
    [SerializeField] private float startDelay = 0f;

    [Header("Animation")]
    [SerializeField] private Animator customerAnimator;
    [SerializeField] private string walkingBoolName = "IsWalking";
    [SerializeField] private string orderFulfilledBoolName = "OrderFulfilled";

    private float waitingSpotProgress = 0.5f;
    private bool hasStartedWalking = false;
    private bool hasReachedWaitingSpot = false;
    private bool isOrderFulfilled = false;

    public int CustomerID
    {
        get => customerID;
        set => customerID = value;
    }

    /// <summary>
    /// Plakt een gegenereerde order aan deze customer en synchroniseert het indexnummer.
    /// </summary>
    public void AssignOrder(Order order)
    {
        assignedOrder = order;
        if (order != null)
        {
            order.customerIndex = customerID;
        }
    }

    private void Start()
    {
        // Zorg dat we correct geabonneerd zijn op de OrderManager
        SubscribeToOrderManager();

        if (splinePathFollower != null)
        {
            // Automatically calculate where the waiting spot Transform sits on the spline (0.0 to 1.0)
            if (waitingSpotTransform != null && splinePathFollower.Container != null && splinePathFollower.Container.Spline != null)
            {
                Vector3 localSpotPos = splinePathFollower.Container.transform.InverseTransformPoint(waitingSpotTransform.position);
                SplineUtility.GetNearestPoint(splinePathFollower.Container.Spline, (float3)localSpotPos, out _, out waitingSpotProgress);
            }

            // Keep them parked at the start of the spline until their delay finishes
            splinePathFollower.NormalizedTime = 0f;
            splinePathFollower.Pause();
            SetWalkingAnimation(false);

            StartCoroutine(BeginWalkAfterDelay());
        }
    }

    private IEnumerator BeginWalkAfterDelay()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        hasStartedWalking = true;
        if (splinePathFollower != null)
        {
            splinePathFollower.Play();
        }
        SetWalkingAnimation(true);
    }

    private void Update()
    {
        if (hasStartedWalking && !hasReachedWaitingSpot && !isOrderFulfilled && splinePathFollower != null)
        {
            if (splinePathFollower.NormalizedTime >= waitingSpotProgress)
            {
                // Snap to the exact spline point closest to the waiting spot Transform and pause
                splinePathFollower.NormalizedTime = waitingSpotProgress;
                splinePathFollower.Pause();

                hasReachedWaitingSpot = true;
                SetWalkingAnimation(false);
            }
        }
    }

    private void OnEnable()
    {
        SubscribeToOrderManager();
    }

    private void OnDisable()
    {
        if (OrderManager.orderManager != null)
        {
            OrderManager.orderManager.OnOrderFulfilled -= HandleOrderFulfilled;
        }
    }

    private void SubscribeToOrderManager()
    {
        if (OrderManager.orderManager != null)
        {
            OrderManager.orderManager.OnOrderFulfilled -= HandleOrderFulfilled;
            OrderManager.orderManager.OnOrderFulfilled += HandleOrderFulfilled;
        }
    }

    private void HandleOrderFulfilled(int fulfilledCustomerID)
    {
        if (fulfilledCustomerID == customerID && !isOrderFulfilled)
        {
            isOrderFulfilled = true;

            if (customerAnimator != null)
            {
                customerAnimator.SetBool(orderFulfilledBoolName, true);
                SetWalkingAnimation(true);
            }

            if (splinePathFollower != null)
            {
                splinePathFollower.Play();
            }
        }
    }

    private void SetWalkingAnimation(bool isWalking)
    {
        if (customerAnimator != null && !string.IsNullOrEmpty(walkingBoolName))
        {
            customerAnimator.SetBool(walkingBoolName, isWalking);
        }
    }
}

// Behoud CustomerOrder alias voor backwards compatibility
public class CustomerOrder : CustomerFoodCheck
{
}

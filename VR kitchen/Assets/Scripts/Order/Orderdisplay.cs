using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class OrderStatusBoard : MonoBehaviour
{
    [Header("UI Containers")]
    [SerializeField] private Transform readyContainer;      // Grid Layout Group voor Ready
    [SerializeField] private Transform preparingContainer;  // Grid Layout Group voor Preparing
    [SerializeField] private TextMeshProUGUI clockText;

    [Header("Prefabs")]
    [SerializeField] private GameObject orderCardPrefab;

    [Header("Order Data")]
    [SerializeField] private List<Order> orders = new List<Order>();

    private void Start()
    {
        LoadDummyData();
        RefreshDisplay();
    }

    private void Update()
    {
        // Klok rechtsboven bijwerken
        if (clockText != null)
        {
            clockText.text = DateTime.Now.ToString("hh:mm tt, dddd - MMMM d");
        }
    }

    public void RefreshDisplay()
    {
        // Oude UI kaarten opruimen
        foreach (Transform child in readyContainer) Destroy(child.gameObject);
        foreach (Transform child in preparingContainer) Destroy(child.gameObject);

        // Nieuwe kaarten aanmaken
        foreach (var order in orders)
        {
            Transform targetContainer = (order.status == OrderStatus.Ready) ? readyContainer : preparingContainer;
            GameObject cardObj = Instantiate(orderCardPrefab, targetContainer);

            OrderCardUI cardUI = cardObj.GetComponent<OrderCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(order);
            }
        }
    }

    private void LoadDummyData()
    {
        orders = new List<Order>
        {
            // Ready Orders
            new Order { id = "0259", code = "R2-39859", customerName = "Olivia R.", status = OrderStatus.Ready },
            new Order { id = "0260", code = "R2-39860", customerName = "Daniel S.", status = OrderStatus.Ready },
            new Order { id = "0262", code = "R2-39862", customerName = "Rachel W.", status = OrderStatus.Ready },
            new Order { id = "0265", code = "R2-39865", customerName = "Samuel E.", status = OrderStatus.Ready },

            // Preparing Orders
            new Order { id = "0526", code = "R5-39826", customerName = "Millie C.", status = OrderStatus.Preparing },
            new Order { id = "0547", code = "R5-39847", customerName = "Dylan V.", status = OrderStatus.Preparing },
            new Order { id = "0589", code = "R5-39889", customerName = "Faith K.", status = OrderStatus.Preparing },
            new Order { id = "0597", code = "R5-39897", customerName = "Alexander F.", status = OrderStatus.Preparing }
        };
    }
}
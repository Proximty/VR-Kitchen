using UnityEngine;
using TMPro;

public class OrderCardUI : MonoBehaviour
{
    [Header("UI Componenten")]
    [SerializeField] private TextMeshProUGUI orderIdText;      // Toont "0259"
    [SerializeField] private TextMeshProUGUI customerNameText; // Toont "Olivia R."
    [SerializeField] private TextMeshProUGUI orderCodeText;   // Toont "[R2-39859]"

    public Order CurrentOrder { get; private set; }

    public void Setup(Order order)
    {
        CurrentOrder = order;

        if (orderIdText != null)
            orderIdText.text = order.id;

        if (customerNameText != null)
            customerNameText.text = order.customerName;

        if (orderCodeText != null)
            orderCodeText.text = $"[{order.code}]";
    }
}
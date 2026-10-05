using UnityEngine;
using TMPro;

public class OrderCardUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI orderIdText;      // Zwarte badge links
    [SerializeField] private TextMeshProUGUI customerNameText; // Klantnaam
    [SerializeField] private TextMeshProUGUI orderCodeText;   // Code onder de naam

    public void Setup(Order order)
    {
        if (orderIdText != null)
            orderIdText.text = order.id;

        if (customerNameText != null)
            customerNameText.text = order.customerName;

        if (orderCodeText != null)
            orderCodeText.text = $"[{order.code}]";
    }
}
using System.Collections.Generic;
using UnityEngine;

public class OrderStatusBoard : MonoBehaviour
{
    [Header("UI Instellingen")]
    [SerializeField] private Transform orderContainer; // Eén layout group/container voor de UI kaarten
    [SerializeField] private GameObject orderCardPrefab;

    [Header("Actieve Orders")]
    [SerializeField] private List<Order> orders = new List<Order>();

    private void Start()
    {
        LoadDummyData();
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        // Oude UI kaarten opruimen
        foreach (Transform child in orderContainer)
        {
            Destroy(child.gameObject);
        }

        // Nieuwe kaarten aanmaken op het scherm
        foreach (var order in orders)
        {
            GameObject cardObj = Instantiate(orderCardPrefab, orderContainer);

            OrderCardUI cardUI = cardObj.GetComponent<OrderCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(order);
            }
        }
    }

    private void LoadDummyData()
    {
        // Orders aanmaken ZONDER status, maar MET ingredientIndexen
        orders = new List<Order>
        {
            new Order
            {
                id = "0259",
                code = "R2-39859",
                customerName = "Olivia R.",
                ingredientIndexes = new List<int> { 6, 0, 1, 6 } // Brood, Ham, Kaas, Brood
            },
            new Order
            {
                id = "0260",
                code = "R2-39860",
                customerName = "Daniel S.",
                ingredientIndexes = new List<int> { 6, 2, 3, 4, 6 }
            },
            new Order
            {
                id = "0262",
                code = "R2-39862",
                customerName = "Rachel W.",
                ingredientIndexes = new List<int> { 6, 1, 5, 6 }
            },
            new Order
            {
                id = "0265",
                code = "R2-39865",
                customerName = "Samuel E.",
                ingredientIndexes = new List<int> { 6, 0, 2, 3, 6 }
            }
        };
    }

    /// <summary>
    /// Controleert of de ingrediënten van de speler kloppen met de geselecteerde order.
    /// </summary>
    public bool CheckOrder(Order orderToCheck, List<int> playerRecipe)
    {
        List<int> required = orderToCheck.ingredientIndexes;

        // 1. Controleer of het aantal ingrediënten klopt
        if (playerRecipe.Count != required.Count)
        {
            Debug.Log("Fout: Aantal ingrediënten klopt niet!");
            return false;
        }

        // 2. Controleer de volgorde van de indexen
        for (int i = 0; i < required.Count; i++)
        {
            if (playerRecipe[i] != required[i])
            {
                Debug.Log($"Fout op positie {i}: Verwacht index {required[i]}, maar kreeg {playerRecipe[i]}");
                return false;
            }
        }

        Debug.Log("Order Klopt Helemaal!");
        return true;
    }
}
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Toont actieve bestellingen op het UI-scherm (Display 2).
/// Bij het voltooien van een order wordt de orderkaart verwijderd en
/// het recept voor de volgende bestelling doorgegeven aan OrderManager.
/// </summary>
public class OrderStatusBoard : MonoBehaviour
{
    public static OrderStatusBoard Instance { get; private set; }

    [Header("UI Instellingen")]
    [SerializeField] private Transform orderContainer; // Layout group/container voor de UI kaarten
    [SerializeField] private GameObject orderCardPrefab;

    [Header("Actieve Orders")]
    [SerializeField] private List<Order> orders = new List<Order>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        LoadDummyData();
        RefreshDisplay();

        // Synchroniseer het eerste recept met OrderManager
        SyncCurrentOrderWithManager();
    }

    public void RefreshDisplay()
    {
        if (orderContainer == null) return;

        // Oude UI kaarten opruimen
        foreach (Transform child in orderContainer)
        {
            Destroy(child.gameObject);
        }

        if (orderCardPrefab == null) return;

        // Nieuwe kaarten aanmaken op het scherm
        foreach (var order in orders)
        {
            if (order == null) continue;
            GameObject cardObj = Instantiate(orderCardPrefab, orderContainer);

            OrderCardUI cardUI = cardObj.GetComponent<OrderCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(order);
            }
        }
    }

    /// <summary>
    /// Rondt de voorste bestelling af, laadt het volgende recept en husselt de keuken.
    /// </summary>
    public void CompleteFirstOrder()
    {
        if (orders.Count > 0)
        {
            Debug.Log($"[OrderStatusBoard] Order {orders[0].id} voltooid.");
            orders.RemoveAt(0);
            RefreshDisplay();

            // Stel het volgende recept in via Nathan's publieke velden
            SyncCurrentOrderWithManager();

            // Hussel de keuken direct na de order
            if (KitchenManager.Instance != null)
            {
                KitchenManager.Instance.ShuffleKitchen();
            }
        }
        else
        {
            Debug.Log("[OrderStatusBoard] Geen orders meer over!");
        }
    }

    private void SyncCurrentOrderWithManager()
    {
        if (OrderManager.orderManager != null && orders.Count > 0)
        {
            Order current = orders[0];
            if (current != null && current.ingredientIndexes != null && current.ingredientIndexes.Count > 0)
            {
                // Gebruik Nathan's bestaande publieke velden zonder zijn script te wijzigen
                OrderManager.orderManager.orderIngredients = new List<int>(current.ingredientIndexes);
                OrderManager.orderManager.orderIngredientAmount = current.ingredientIndexes.Count;
            }
        }
    }

    private void LoadDummyData()
    {
        // 0 = ham, 1 = kaas, 2 = burger/vlees, 3 = lettuce, 4 = tomato, 5 = cucumber, 6 = bun
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
                ingredientIndexes = new List<int> { 6, 2, 3, 4, 6 } // Brood, Burger, Sla, Tomaat, Brood
            },
            new Order
            {
                id = "0262",
                code = "R2-39862",
                customerName = "Rachel W.",
                ingredientIndexes = new List<int> { 6, 1, 5, 6 } // Brood, Kaas, Komkommer, Brood
            },
            new Order
            {
                id = "0265",
                code = "R2-39865",
                customerName = "Samuel E.",
                ingredientIndexes = new List<int> { 6, 0, 2, 3, 6 } // Brood, Ham, Burger, Sla, Brood
            }
        };
    }

    /// <summary>
    /// Controleert of de ingrediënten van de speler kloppen met de geselecteerde order.
    /// </summary>
    public bool CheckOrder(Order orderToCheck, List<int> playerRecipe)
    {
        if (orderToCheck == null || playerRecipe == null) return false;
        List<int> required = orderToCheck.ingredientIndexes;

        if (playerRecipe.Count != required.Count)
        {
            Debug.Log("Fout: Aantal ingrediënten klopt niet!");
            return false;
        }

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
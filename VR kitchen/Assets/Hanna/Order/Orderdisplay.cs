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

    [Header("Order Generatie")]
    [SerializeField] private int orderCount = 20; // Aantal gegenereerde orders

    [Header("Actieve Orders")]
    [SerializeField] private List<Order> orders = new List<Order>();

    // Originele ingrediëntenlijsten van de orders (ongewijzigd teruggezet)
    // 0 = ham, 1 = kaas, 2 = burger/vlees, 3 = lettuce, 4 = tomato, 5 = cucumber, 6 = bun
    private readonly List<List<int>> originalRecipes = new List<List<int>>
    {
        new List<int> { 6, 0, 1, 6 },       // Brood, Ham, Kaas, Brood
        new List<int> { 6, 2, 3, 4, 6 },    // Brood, Burger, Sla, Tomaat, Brood
        new List<int> { 6, 1, 5, 6 },       // Brood, Kaas, Komkommer, Brood
        new List<int> { 6, 0, 2, 3, 6 }     // Brood, Ham, Burger, Sla, Brood
    };

    // Verschillende klantennamen
    private readonly string[] customerNames = new string[]
    {
        "Olivia R.", "Daniel S.", "Rachel W.", "Samuel E.",
        "Emma B.", "Liam K.", "Noah V.", "Sophie M.",
        "Lucas D.", "Mila J.", "Finn B.", "Tess W.",
        "Sem K.", "Julia P.", "Milan H.", "Zoë L.",
        "Levi M.", "Sara D.", "Jesse B.", "Lotte V.",
        "Bram K.", "Eva N.", "Daan S.", "Lieke R.",
        "Luuk M.", "Sanne T.", "Thijs G.", "Fleur K."
    };

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
        GenerateOrders();
        RefreshDisplay();

        // Synchroniseer het eerste recept met OrderManager
        SyncCurrentOrderWithManager();
    }

    /// <summary>
    /// Genereert orders met verschillende namen, ordernummers en codes,
    /// gebruikmakend van de originele ingrediëntenlijst.
    /// </summary>
    private void GenerateOrders()
    {
        orders = new List<Order>();

        int startId = 259;
        int startCodeNumber = 39859;

        for (int i = 0; i < orderCount; i++)
        {
            // Kies een naam uit de namenlijst
            string name = customerNames[i % customerNames.Length];

            // Maak een oplopend ordernummer (bijv. 0259, 0260, ...)
            string id = (startId + i).ToString("D4");

            // Maak een unieke order code (bijv. R2-39859, R2-39860, ...)
            string code = $"R2-{startCodeNumber + i}";

            // Kies een van de originele ingrediëntenlijsten
            List<int> recipe = new List<int>(originalRecipes[i % originalRecipes.Count]);

            // Maak het Order component aan op een child GameObject
            GameObject orderObj = new GameObject($"Order_{id}");
            orderObj.transform.SetParent(this.transform);

            Order newOrder = orderObj.AddComponent<Order>();
            newOrder.id = id;
            newOrder.code = code;
            newOrder.customerName = name;
            newOrder.ingredientIndexes = recipe;

            orders.Add(newOrder);
        }
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
            Order completed = orders[0];
            Debug.Log($"[OrderStatusBoard] Order {completed.id} voltooid.");
            orders.RemoveAt(0);

            if (completed != null && completed.gameObject != null)
            {
                Destroy(completed.gameObject);
            }

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

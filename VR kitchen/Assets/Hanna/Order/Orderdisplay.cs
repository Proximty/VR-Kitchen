using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Toont actieve bestellingen op het UI-scherm (Display 2).
/// Genereert automatisch orders en koppelt deze aan de aanwezige customers op basis van een indexnummer.
/// Bij het voltooien van een order wordt de orderkaart verwijderd,
/// de bijbehorende customer via OrderManager aangestuurd en het volgende recept doorgestuurd.
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

    // Originele recepten met gerechtnaam en ingrediëntenlijsten
    // 0 = ham, 1 = kaas, 2 = burger/vlees, 3 = lettuce, 4 = tomato, 5 = cucumber, 6 = bun
    [System.Serializable]
    public class RecipeDefinition
    {
        public string dishName;
        public List<int> ingredientIndexes;

        public RecipeDefinition(string dishName, List<int> ingredientIndexes)
        {
            this.dishName = dishName;
            this.ingredientIndexes = ingredientIndexes;
        }
    }

    private readonly List<RecipeDefinition> originalRecipes = new List<RecipeDefinition>
    {
        new RecipeDefinition("Ham & Cheese Burger", new List<int> { 6, 0, 1, 6 }),        // Brood, Ham, Kaas, Brood
        new RecipeDefinition("Classic Burger", new List<int> { 6, 2, 3, 4, 6 }),          // Brood, Burger, Sla, Tomaat, Brood
        new RecipeDefinition("Fresh Cheeseburger", new List<int> { 6, 1, 5, 6 }),         // Brood, Kaas, Komkommer, Brood
        new RecipeDefinition("Club Burger", new List<int> { 6, 0, 2, 3, 6 })              // Brood, Ham, Burger, Sla, Brood
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
    /// Vindt de customers in de scene, geeft ze een indexnummer (indien nodig)
    /// en genereert bestellingen die aan de customers gekoppeld worden.
    /// </summary>
    private void GenerateOrders()
    {
        orders = new List<Order>();

        // 1. Zoek aanwezige customers in de scene
        CustomerFoodCheck[] foundCustomers = FindObjectsByType<CustomerFoodCheck>();
        List<CustomerFoodCheck> customerList = new List<CustomerFoodCheck>();
        if (foundCustomers != null)
        {
            foreach (var cust in foundCustomers)
            {
                if (cust != null) customerList.Add(cust);
            }
        }

        // Sorteer customers op hun huidige customerID zodat de volgorde logisch is
        customerList.Sort((a, b) => (a != null && b != null) ? a.CustomerID.CompareTo(b.CustomerID) : 0);

        // Zorg dat elke customer een geldig index nummer heeft (bijv. 1, 2, ...)
        for (int c = 0; c < customerList.Count; c++)
        {
            if (customerList[c] != null && customerList[c].CustomerID <= 0)
            {
                customerList[c].CustomerID = c + 1;
            }
        }

        int startId = 259;
        int startCodeNumber = 39859;

        // 2. Genereer orders en plak ze aan de customers
        for (int i = 0; i < orderCount; i++)
        {
            // Bepaal welk customer index nummer bij deze order hoort
            int assignedCustomerIndex;
            if (customerList.Count > 0)
            {
                assignedCustomerIndex = customerList[i % customerList.Count].CustomerID;
            }
            else
            {
                assignedCustomerIndex = (i % 2) + 1; // 1 of 2 als fallback
            }

            // Kies een naam uit de namenlijst
            string name = customerNames[i % customerNames.Length];

            // Maak een oplopend ordernummer (bijv. 0259, 0260, ...)
            string id = (startId + i).ToString("D4");

            // Maak een unieke order code (bijv. R2-39859, R2-39860, ...)
            string code = $"R2-{startCodeNumber + i}";

            // Kies een recept en de bijbehorende gerechtnaam
            RecipeDefinition recipeDef = originalRecipes[i % originalRecipes.Count];
            List<int> recipe = new List<int>(recipeDef.ingredientIndexes);
            string dishName = recipeDef.dishName;

            // Maak het Order component aan op een child GameObject
            GameObject orderObj = new GameObject($"Order_{id}");
            orderObj.transform.SetParent(this.transform);

            Order newOrder = orderObj.AddComponent<Order>();
            newOrder.id = id;
            newOrder.code = code;
            newOrder.customerName = name;
            newOrder.dishName = dishName;
            newOrder.customerIndex = assignedCustomerIndex;
            newOrder.ingredientIndexes = recipe;

            // Plak de order direct aan de customer als deze order als eerste voor die customer is
            if (i < customerList.Count && customerList[i] != null)
            {
                customerList[i].AssignOrder(newOrder);
                Debug.Log($"[OrderStatusBoard] Order {newOrder.id} ({newOrder.dishName}) gekoppeld aan Customer {customerList[i].CustomerID} ({customerList[i].name})");
            }

            orders.Add(newOrder);
        }
    }

    public void RefreshDisplay()
    {
        if (orderContainer == null) return;

        // Controleer of orderContainer en orderCardPrefab naar hetzelfde scene-object wijzen
        bool isScenePanel = false;
        if (orderCardPrefab != null && orderCardPrefab.scene.IsValid())
        {
            if (orderCardPrefab == orderContainer.gameObject || orderCardPrefab.transform.IsChildOf(orderContainer))
            {
                isScenePanel = true;
            }
        }

        // Als er in de scene maar één enkel OrderPanel paneel staat (zoals in Kitchen Modulars),
        // update dan gewoon dat paneel direct in plaats van duplicaten in zichzelf te nesten!
        if (isScenePanel)
        {
            OrderCardUI singleUI = orderContainer.GetComponent<OrderCardUI>() ?? orderContainer.GetComponentInChildren<OrderCardUI>();
            if (singleUI != null && orders.Count > 0 && orders[0] != null)
            {
                singleUI.Setup(orders[0]);
            }
            return;
        }

        // Als het een UI container is met een losse prefab (zoals in Order Display scene)
        if (orderCardPrefab == null) return;

        // Ruim alleen eerder aangemaakte UI-kaarten op
        for (int i = orderContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = orderContainer.GetChild(i);
            if (child != null && child.gameObject != orderCardPrefab)
            {
                Destroy(child.gameObject);
            }
        }

        // Toon de actieve orders (bijv. de eerste 4 tegelijk op het scherm)
        int maxVisible = Mathf.Min(orders.Count, 4);
        for (int i = 0; i < maxVisible; i++)
        {
            Order order = orders[i];
            if (order == null) continue;

            GameObject cardObj = Instantiate(orderCardPrefab, orderContainer);
            cardObj.SetActive(true);

            OrderCardUI cardUI = cardObj.GetComponent<OrderCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(order);
            }
        }
    }

    /// <summary>
    /// Rondt de voorste bestelling af, vervult de order voor de gekoppelde customer,
    /// laadt het volgende recept en husselt de keuken.
    /// </summary>
    public void CompleteFirstOrder()
    {
        if (orders.Count > 0)
        {
            Order completed = orders[0];
            int completedCustomerIndex = (completed != null) ? completed.customerIndex : 1;
            Debug.Log($"[OrderStatusBoard] Order {completed?.id} ({completed?.dishName}) voltooid voor Customer Index {completedCustomerIndex} ({completed?.customerName}).");

            orders.RemoveAt(0);

            // Stuur het signaal naar OrderManager dat de order voor deze specifieke customer index klaar is!
            if (OrderManager.orderManager != null)
            {
                OrderManager.orderManager.FulfillOrder(completedCustomerIndex);
            }

            // Ruim het losse order GameObject netjes op, maar raak nooit een customer of het bord zelf aan
            if (completed != null && completed.gameObject != null)
            {
                if (completed.gameObject.GetComponent<CustomerFoodCheck>() == null &&
                    completed.gameObject.GetComponent<CustomerController>() == null &&
                    completed.gameObject != this.gameObject)
                {
                    Destroy(completed.gameObject);
                }
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

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zorgt ervoor dat de keuken na iedere voltooide order automatisch wordt gehusseld,
/// ZONDER dat Nathan's scripts in 'Assets/Scripts/Ingredients' zwaar aangepast hoeven te worden.
/// 
/// Plaats dit script op het bord (de GameObject met FoodPlate) of in de scene.
/// </summary>
public class PlateOrderWatcher : MonoBehaviour
{
    [Header("Verwijzingen")]
    [Tooltip("Optionele directe verwijzing naar het bord. Indien leeg, zoekt het script automatisch naar FoodPlate.")]
    public FoodPlate targetPlate;

    // Lokale tracking van ingrediënten op het bord
    private readonly List<int> currentIngredientsOnPlate = new List<int>();
    private readonly List<GameObject> objectsOnPlate = new List<GameObject>();
    private bool isHandlingCompletion = false;

    private void Start()
    {
        if (targetPlate == null)
        {
            targetPlate = GetComponent<FoodPlate>() ?? FindAnyObjectByType<FoodPlate>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isHandlingCompletion) return;

        IngredientDataChecker checker = other.gameObject.GetComponent<IngredientDataChecker>();
        if (checker != null && checker.ingredientData != null)
        {
            currentIngredientsOnPlate.Add(checker.ingredientData.Ingedient);
            if (!objectsOnPlate.Contains(other.gameObject))
            {
                objectsOnPlate.Add(other.gameObject);
            }
            CheckIfOrderComplete();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isHandlingCompletion) return;

        IngredientDataChecker checker = other.gameObject.GetComponent<IngredientDataChecker>();
        if (checker != null && checker.ingredientData != null)
        {
            currentIngredientsOnPlate.Remove(checker.ingredientData.Ingedient);
            objectsOnPlate.Remove(other.gameObject);
        }
    }

    private void CheckIfOrderComplete()
    {
        if (isHandlingCompletion) return;
        if (OrderManager.orderManager == null || OrderManager.orderManager.orderIngredients == null)
            return;

        List<int> required = OrderManager.orderManager.orderIngredients;
        if (required.Count == 0) return;

        // Tel aantal juiste ingrediënten en aantal broodjes
        List<int> remaining = new List<int>(required);
        int matched = 0;
        int bunCount = 0;

        foreach (int item in currentIngredientsOnPlate)
        {
            if (item == 6) bunCount++; // Brood

            if (remaining.Contains(item))
            {
                remaining.Remove(item);
                matched++;
            }
        }

        // Als alles klopt OF als de burger is afgesloten met een 2e broodje
        bool isPerfect = (matched == required.Count && currentIngredientsOnPlate.Count == required.Count);
        bool isClosed = (bunCount >= 2 && currentIngredientsOnPlate.Count >= 3);

        if (isPerfect || isClosed)
        {
            OnOrderDone(matched, required.Count);
        }
    }

    private void OnOrderDone(int matched, int totalRequired)
    {
        isHandlingCompletion = true;

        // Bereken score op basis van juiste ingrediënten en tijd
        float timeTaken = Time.time - ScoreManager.orderStartTime;
        ScoreManager.AddScore(matched, totalRequired, timeTaken);

        // 1. Verwijder de klaargemaakte ingrediënten van het bord
        foreach (GameObject obj in objectsOnPlate)
        {
            if (obj != null) Destroy(obj);
        }
        objectsOnPlate.Clear();
        currentIngredientsOnPlate.Clear();

        // 2. Werk de UI kaart bij en laad de volgende order (OrderStatusBoard)
        if (OrderStatusBoard.Instance != null)
        {
            OrderStatusBoard.Instance.CompleteFirstOrder();
        }

        // 3. Hussel de keuken via KitchenManager!
        if (KitchenManager.Instance != null)
        {
            KitchenManager.Instance.ShuffleKitchen();
        }

        // 4. Laat eventuele klant vertrekken
        CustomerController customer = FindAnyObjectByType<CustomerController>();
        if (customer != null)
        {
            customer.CompleteCustomerOrder();
        }

        isHandlingCompletion = false;
    }
}

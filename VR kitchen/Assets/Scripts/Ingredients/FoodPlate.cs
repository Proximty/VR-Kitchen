using System.Collections.Generic;
using UnityEngine;

public class FoodPlate : MonoBehaviour
{
    [SerializeField] private List<int> ingredients;
    [SerializeField] private int ingredientCount;

    private void OnTriggerEnter(Collider other)
    {
        //add ingredient on the plate
        if (other.gameObject.GetComponent<IngredientDataChecker>() != null)
        {
            ingredients.Add(other.gameObject.GetComponent<IngredientDataChecker>().ingredientData.Ingedient);
        }

        CheckOrder();
    }

    private void OnTriggerExit(Collider other)
    {
        //remove ingredient on the plate
        if (other.gameObject.GetComponent<IngredientDataChecker>() != null)
        {
            ingredients.Remove(other.gameObject.GetComponent<IngredientDataChecker>().ingredientData.Ingedient);
        }
    }



    private void CheckOrder()
    {
        ingredientCount = 0;

        List<int> remainingOrder = new List<int>(OrderManager.orderManager.orderIngredients);

        for (int j = 0; j < ingredients.Count; j++)
        {
            int playerItem = ingredients[j];

            if (remainingOrder.Contains(playerItem))
            {
                remainingOrder.Remove(playerItem);
                ingredientCount++;
            }
        }

        if (ingredientCount == OrderManager.orderManager.orderIngredientAmount)
        {
            Debug.Log("meal = ready: " + string.Join(", ", ingredients));
        }
    }
}

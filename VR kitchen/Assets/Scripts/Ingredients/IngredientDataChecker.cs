using UnityEngine;

public class IngredientDataChecker : MonoBehaviour
{
    [SerializeField] private IngredientData ingredientData;
    [SerializeField] private Material cookedMaterial;
    [SerializeField] private Material burnedMaterial;

    public void IncreaseEdibleValue()
    {
        ingredientData.edible += Time.deltaTime * 20;
        Debug.Log(ingredientData.edible);
        if (cookedMaterial != null && ingredientData.edible >= 100 && ingredientData.edible <= 200)
        {
            GetComponent<Renderer>().material = cookedMaterial;
        }
        else if (burnedMaterial != null && ingredientData.edible >= 200)
        {
            GetComponent<Renderer>().material = burnedMaterial;
        }
    }
}

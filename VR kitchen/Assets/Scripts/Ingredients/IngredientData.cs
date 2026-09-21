using UnityEngine;

[CreateAssetMenu(fileName = "IngredientData", menuName = "Custom/IngredientData")]

public class IngredientData : ScriptableObject
{
    [SerializeField] private string[] Ingedient = {"Ham", "Cheese", "Lettuce", "Tomato", "Cucumber", "Picles", "Bread"};
    //edible is done with precentage (for example 0 = raw meat, 100 = cooked meat, 200 = burned meat)
    [SerializeField] private float edible;
}

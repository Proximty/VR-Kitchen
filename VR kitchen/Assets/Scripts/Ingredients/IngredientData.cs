using UnityEngine;

[CreateAssetMenu(fileName = "IngredientData", menuName = "Custom/IngredientData")]

public class IngredientData : ScriptableObject
{
    [SerializeField] public int Ingedient;
    //edible is done with precentage (for example 0 = raw meat, 100 = cooked meat, 200 = burned meat)
    [SerializeField] public float edible;
}

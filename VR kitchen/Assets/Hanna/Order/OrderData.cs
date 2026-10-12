using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Order: MonoBehaviour
{
    public string id;
    public string code;
    public string customerName;

    // Indexen uit IngredientData (bijv. 6 = Brood, 0 = Ham, 1 = Kaas)
    public List<int> ingredientIndexes = new List<int>();
}
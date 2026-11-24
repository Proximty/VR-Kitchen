using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Order: MonoBehaviour
{
    public string id;
    public string code;
    public string customerName;
    public string dishName; // Naam van het gerecht (bijv. "Classic Burger", "Tosti Ham & Kaas")
    public int customerIndex = 1; // Index van de customer waar deze order bij hoort

    // Indexen uit IngredientData (bijv. 6 = Brood, 0 = Ham, 1 = Kaas)
    public List<int> ingredientIndexes = new List<int>();

    /// <summary>
    /// Vertaalt een ingrediënt-index naar een leesbare naam.
    /// 0 = Ham, 1 = Kaas, 2 = Burger, 3 = Sla, 4 = Tomaat, 5 = Komkommer, 6 = Brood
    /// </summary>
    public static string GetIngredientName(int index)
    {
        switch (index)
        {
            case 0: return "Ham";
            case 1: return "Kaas";
            case 2: return "Burger";
            case 3: return "Sla";
            case 4: return "Tomaat";
            case 5: return "Komkommer";
            case 6: return "Brood";
            default: return $"Ingrediënt {index}";
        }
    }

    /// <summary>
    /// Geeft een overzichtelijke lijst van de benodigde ingrediënten (bijv. "Brood + Burger + Sla + Tomaat + Brood").
    /// </summary>
    public string GetIngredientsSummary()
    {
        if (ingredientIndexes == null || ingredientIndexes.Count == 0) return "";
        List<string> names = new List<string>();
        foreach (int idx in ingredientIndexes)
        {
            names.Add(GetIngredientName(idx));
        }
        return string.Join(" + ", names);
    }
}


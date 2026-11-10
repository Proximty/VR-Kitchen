using UnityEngine;

/// <summary>
/// Optionele component die je op elk object in een keuken-prefab kunt plaatsen.
/// Hiermee geef je een specifiek label, categorie of kleur aan voor Display 2.
/// </summary>
public class KitchenMarkerTarget : MonoBehaviour
{
    [Tooltip("Label dat op Display 2 wordt getoond (bijv. 'Burger', 'Kaas', 'Snijplank')")]
    public string labelName = "";

    [Tooltip("Kleur van het knipperende lampje op Display 2")]
    public Color markerColor = Color.yellow;

    [Tooltip("Geeft aan of deze marker momenteel moet knipperen/actief is")]
    public bool isHighlighted = true;

    private void Awake()
    {
        if (string.IsNullOrEmpty(labelName))
        {
            labelName = gameObject.name;
        }
    }
}

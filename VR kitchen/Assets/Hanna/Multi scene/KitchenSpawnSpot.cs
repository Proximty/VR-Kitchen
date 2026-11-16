using UnityEngine;

/// <summary>
/// Markering voor een plek waar objecten/ingrediënten willekeurig kunnen spawnen.
/// Hiermee kun je optioneel ook verschillende categorieën/soorten plekken aangeven
/// (bijv. aanrecht, schap, koeling/koelkast of algemeen).
/// </summary>
public class KitchenSpawnSpot : MonoBehaviour
{
    public enum SpotCategory
    {
        Any,       // Elke willekeurige plek
        Counter,   // Aanrecht / werkbank
        Shelf,     // Wandrek / schap
        Fridge,    // Koeling / koelkast
        Stove      // Fornuis / kookplaat
    }

    [Tooltip("Het type plek (bijv. Counter, Shelf, Fridge).")]
    public SpotCategory spotType = SpotCategory.Any;

    [Tooltip("Optioneel: Geef aan welk specifiek object hier bij voorkeur mag liggen.")]
    public string allowedTagOrName = "";

    [Tooltip("Is deze plek momenteel bezet door een object?")]
    public bool isOccupied = false;

    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
        Gizmos.DrawRay(transform.position, transform.up * 0.2f);
    }
}

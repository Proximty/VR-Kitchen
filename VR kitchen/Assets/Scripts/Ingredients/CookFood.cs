using UnityEngine;

public class CookFood : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Burger"))
        {
            other.gameObject.GetComponent<IngredientDataChecker>().IncreaseEdibleValue();
        }
    }
}

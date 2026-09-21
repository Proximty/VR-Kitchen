using UnityEngine;

public class Init : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.GetComponent<ViewManager>().Playscene("Order display");
    }

}
   

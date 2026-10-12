using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager orderManager;
    [SerializeField] public List<int> orderIngredients;
    [SerializeField] public int orderIngredientAmount;

    private void Awake()
    {
        orderManager = this;

        DontDestroyOnLoad(gameObject);

        if (orderManager == null)
        {
            Destroy(gameObject);
            return;
        }

    }

    private void Start()
    {
        orderIngredientAmount = orderIngredients.Count;
    }
}

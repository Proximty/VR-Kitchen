using System;
using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager orderManager;

    [SerializeField] public List<int> orderIngredients;
    [SerializeField] public int orderIngredientAmount;

    public event Action<int> OnOrderFulfilled;

    private void Awake()
    {
        if (orderManager != null && orderManager != this)
        {
            Destroy(gameObject);
            return;
        }

        orderManager = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (orderIngredients != null)
        {
            orderIngredientAmount = orderIngredients.Count;
        }
    }

    public void FulfillOrder(int customerIndex)
    {
        OnOrderFulfilled?.Invoke(customerIndex);
    }
}
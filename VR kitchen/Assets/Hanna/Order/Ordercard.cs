using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrderCardUI : MonoBehaviour
{
    [Header("UI Componenten")]
    [SerializeField] private TextMeshProUGUI orderIdText;      // Toont bijv. "#0259"
    [SerializeField] private TextMeshProUGUI customerNameText; // Toont klantnaam en/of ingrediënten
    [SerializeField] private TextMeshProUGUI orderCodeText;   // Toont het gerecht (of ordercode)
    [SerializeField] private TextMeshProUGUI dishNameText;    // Optioneel apart veld voor de gerechtnaam
    [SerializeField] private TextMeshProUGUI ingredientsText; // Optioneel apart veld voor de ingrediëntenlijst

    public Order CurrentOrder { get; private set; }

    private void Awake()
    {
        EnsureCanvasExists();
    }

    /// <summary>
    /// Zorgt ervoor dat er altijd een Canvas aanwezig is, zodat het paneel
    /// in Play mode direct rendert in de 3D-wereld (World Space).
    /// </summary>
    private void EnsureCanvasExists()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Canvas canvas = gameObject.GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
        }
    }

    public void Setup(Order order)
    {
        CurrentOrder = order;
        if (order == null) return;

        // 1. Order ID (bijv. "#0259")
        if (orderIdText != null)
        {
            orderIdText.text = $"#{order.id}";
        }

        // 2. Gerechtnaam op een apart veld indien aanwezig
        if (dishNameText != null)
        {
            dishNameText.text = !string.IsNullOrEmpty(order.dishName) ? order.dishName : $"Order #{order.id}";
        }

        // 3. Ingrediënten op een apart veld indien aanwezig
        if (ingredientsText != null)
        {
            ingredientsText.text = order.GetIngredientsSummary();
        }

        // 4. Order Code tekstvak: als er geen apart dishNameText veld is, tonen we hier het gerecht duidelijk en opvallend!
        if (orderCodeText != null)
        {
            if (dishNameText == null && !string.IsNullOrEmpty(order.dishName))
            {
                orderCodeText.text = order.dishName;
                orderCodeText.color = new Color(1f, 0.85f, 0.2f); // Duidelijke geel/gouden tint voor het gerecht
                orderCodeText.enableAutoSizing = true;
                orderCodeText.fontSizeMin = 8;
                orderCodeText.fontSizeMax = 14;

                // Zorg voor voldoende breedte voor langere gerechtnamen
                RectTransform codeRect = orderCodeText.rectTransform;
                if (codeRect != null && codeRect.sizeDelta.x < 130f)
                {
                    codeRect.sizeDelta = new Vector2(130f, codeRect.sizeDelta.y);
                }
            }
            else
            {
                orderCodeText.text = $"[{order.code}]";
            }
        }

        // 5. Klantnaam & wat ze moeten maken
        if (customerNameText != null)
        {
            string ingredients = order.GetIngredientsSummary();

            // Als er geen apart ingredientsText veld is, tonen we de ingrediënten onder de klantnaam
            if (ingredientsText == null && !string.IsNullOrEmpty(ingredients))
            {
                customerNameText.text = $"{order.customerName}\n<size=75%><color=#E0E0E0>{ingredients}</color></size>";

                // Vergroot het tekstvak zodat beide regels mooi passen
                RectTransform nameRect = customerNameText.rectTransform;
                if (nameRect != null && nameRect.sizeDelta.y < 38f)
                {
                    nameRect.sizeDelta = new Vector2(170f, 38f);
                }
            }
            else
            {
                customerNameText.text = order.customerName;
            }
        }
    }
}

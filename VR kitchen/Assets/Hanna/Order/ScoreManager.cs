using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scoresysteem met aparte puntentelling voor burgers en friet:
/// Burger = 100 punten, Fries = 50 punten, plus tijdsbonus!
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static int totalScore = 0;
    public static int totalBurgers = 0;
    public static int totalFries = 0;
    public static float orderStartTime = 0f;

    public static void StartOrder()
    {
        orderStartTime = Time.time;
    }

    /// <summary>
    /// Berekent de score op basis van geleverde gerechten: Burger = 100, Fries = 50, plus tijdsbonus.
    /// </summary>
    public static int AddScore(List<int> delivered, float timeTaken)
    {
        int points = 0;
        bool hasBurger = false;
        bool hasFries = false;

        if (delivered != null)
        {
            // Bevat de levering friet (index 7)?
            if (delivered.Contains(7))
            {
                hasFries = true;
                points += 50;
                totalFries++;
            }

            // Bevat de levering burger onderdelen?
            if (delivered.Exists(i => i != 7))
            {
                hasBurger = true;
                points += 100;
                totalBurgers++;
            }
        }

        if (points == 0) points = 100; // Standaard fallback

        // Tijdsbonus (binnen 60 sec = extra bonuspunten)
        int timeBonus = Mathf.Max(0, Mathf.RoundToInt((60f - timeTaken) * 5f));
        int earned = points + timeBonus;
        totalScore += earned;

        Debug.Log($"[Score] +{earned} punten! (Burger: {(hasBurger ? 100 : 0)}, Fries: {(hasFries ? 50 : 0)}, Tijd: {timeTaken:F1}s, Bonus: +{timeBonus}). Totale score: {totalScore}");
        return earned;
    }

    /// <summary>
    /// Overload voor compatibiliteit.
    /// </summary>
    public static int AddScore(int correct, int total, float timeTaken)
    {
        return AddScore(new List<int> { 2 }, timeTaken);
    }
}

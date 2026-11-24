using UnityEngine;

/// <summary>
/// Eenvoudig scoresysteem gebaseerd op tijd en het aantal juiste ingrediënten.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static int totalScore = 0;
    public static float orderStartTime = 0f;

    public static void StartOrder()
    {
        orderStartTime = Time.time;
    }

    public static int AddScore(int correct, int total, float timeTaken)
    {
        // 1. Punten voor juiste ingrediënten (100 per goed ingrediënt)
        int recipePoints = correct * 100;

        // 2. Tijdsbonus (hoe sneller binnen 60 seconden, hoe meer bonus)
        int timeBonus = Mathf.Max(0, Mathf.RoundToInt((60f - timeTaken) * 10f));

        int earned = recipePoints + timeBonus;
        totalScore += earned;

        Debug.Log($"[Score] +{earned} punten! ({correct}/{total} goed, tijd: {timeTaken:F1}s). Totale score: {totalScore}");
        return earned;
    }
}

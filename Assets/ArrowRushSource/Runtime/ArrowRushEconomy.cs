using UnityEngine;

/// <summary>Small persistent coin and hint wallet used by the scene-authored shop.</summary>
public static class ArrowRushEconomy
{
    private const string CoinsKey = "ArrowRush.Coins";
    private const string HintsKey = "ArrowRush.Hints";

    public static int Coins => Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, 0));
    public static int Hints => Mathf.Max(0, PlayerPrefs.GetInt(HintsKey, 3));

    public static bool TryConsumeHint()
    {
        if (Hints <= 0) return false;
        PlayerPrefs.SetInt(HintsKey, Hints - 1);
        PlayerPrefs.Save();
        return true;
    }

    public static bool TryBuyHints(int amount, int price)
    {
        if (amount <= 0 || price < 0 || Coins < price) return false;
        PlayerPrefs.SetInt(CoinsKey, Coins - price);
        PlayerPrefs.SetInt(HintsKey, Hints + amount);
        PlayerPrefs.Save();
        return true;
    }

    public static int AwardLevelClear(int stars)
    {
        int reward = 25 + Mathf.Clamp(stars, 1, 3) * 15;
        PlayerPrefs.SetInt(CoinsKey, Coins + reward);
        PlayerPrefs.Save();
        return reward;
    }
}

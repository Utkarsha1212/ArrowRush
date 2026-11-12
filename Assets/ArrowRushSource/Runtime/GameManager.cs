using UnityEngine;

/// <summary>Small persistent coin and hint wallet used by the scene-authored shop.</summary>
public static class GameManager
{
    private const string CoinsKey = "ArrowRush.Coins";
    private const string HintsKey = "ArrowRush.Hints";
    private const string HintButtonEnabledKey = "ArrowRush.HintsEnabled";

    public static int Coins => Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, 50));
    public static int Hints => Mathf.Max(0, PlayerPrefs.GetInt(HintsKey, 3));
    public static bool HintButtonEnabled => PlayerPrefs.GetInt(HintButtonEnabledKey, 1) == 1;

    public static void SetHintButtonEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(HintButtonEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void AddHints(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(HintsKey, Hints + amount);
        PlayerPrefs.Save();
    }

    public static bool TrySpendCoins(int amount)
    {
        if (amount <= 0 || Coins < amount) return false;
        PlayerPrefs.SetInt(CoinsKey, Coins - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static bool TryUseHint()
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

    public static int AwardCoinsForLevelStars(int stars)
    {
        int clampedStars = Mathf.Clamp(stars, 1, 3);
        int reward = clampedStars == 1 ? 1 : clampedStars == 2 ? 3 : 5;
        PlayerPrefs.SetInt(CoinsKey, Coins + reward);
        PlayerPrefs.Save();
        return reward;
    }
}

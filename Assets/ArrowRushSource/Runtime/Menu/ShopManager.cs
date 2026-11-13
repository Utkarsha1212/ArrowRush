using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FuncrateStudio;

public enum CoinPack
{
    Pack1 = 40,
    Pack2 = 120,
    Pack3 = 400
}

/// <summary>Handles the hint and rewarded coin purchases in the menu shop.</summary>
public sealed class ShopManager : MonoBehaviour
{
    [Header("Coins")]
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text statusText;

    [Header("Hint Buttons")]
    [SerializeField] private Button hintButton1;
    [SerializeField] private Button hintButton2;
    [SerializeField] private Button hintButton3;

    [Header("Coin Buttons")]
    [SerializeField] private Button coinsButton1;
    [SerializeField] private Button coinsButton2;
    [SerializeField] private Button coinsButton3;

    [Header("Hint prices")]
    [SerializeField] private int hintPrice1 = 50;
    [SerializeField] private int hintPrice5 = 200;
    [SerializeField] private int hintPrice10 = 350;

    private bool rewardPending;

    private void OnEnable()
    {
        Bind(hintButton1, () => BuyHints(1, hintPrice1));
        Bind(hintButton2, () => BuyHints(5, hintPrice5));
        Bind(hintButton3, () => BuyHints(10, hintPrice10));
        Bind(coinsButton1, () => BuyCoins(CoinPack.Pack1));
        Bind(coinsButton2, () => BuyCoins(CoinPack.Pack2));
        Bind(coinsButton3, () => BuyCoins(CoinPack.Pack3));
        Refresh();
    }

    private void OnDisable()
    {
        Unbind(hintButton1); Unbind(hintButton2); Unbind(hintButton3);
        Unbind(coinsButton1); Unbind(coinsButton2); Unbind(coinsButton3);
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void Unbind(Button button)
    {
        if (button != null) button.onClick.RemoveAllListeners();
    }

    private void BuyHints(int amount, int price)
    {
        if (!GameManager.TryBuyHints(amount, price))
        {
            SetStatus("NOT ENOUGH COINS");
            return;
        }
        SetStatus($"ADDED {amount} HINT{(amount == 1 ? "" : "S")}");
        Refresh();
    }

    private void BuyCoins(CoinPack pack)
    {
        if (rewardPending) return;
        FSAdsManager ads = FSAdsManager.Instance;
        if (ads == null || !ads.IsRewardedAdReady)
        {
            SetStatus("NO REWARDED AD IS AVAILABLE. TRY AGAIN LATER.");
            return;
        }

        rewardPending = true;
        SetButtonsInteractable(false);
        int amount = (int)pack;
        ads.ShowRewardedAd(() =>
        {
            rewardPending = false;
            GameManager.AddCoins(amount);
            SetStatus($"ADDED {amount} COINS");
            Refresh();
        }, () =>
        {
            rewardPending = false;
            SetButtonsInteractable(true);
            SetStatus("AD NOT COMPLETED. TRY AGAIN?");
        });
    }

    private void Refresh()
    {
        if (coinsText != null) coinsText.text = GameManager.Coins.ToString();
        SetButtonsInteractable(!rewardPending);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (hintButton1 != null) hintButton1.interactable = interactable;
        if (hintButton2 != null) hintButton2.interactable = interactable;
        if (hintButton3 != null) hintButton3.interactable = interactable;
        if (coinsButton1 != null) coinsButton1.interactable = interactable;
        if (coinsButton2 != null) coinsButton2.interactable = interactable;
        if (coinsButton3 != null) coinsButton3.interactable = interactable;
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}

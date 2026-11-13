using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using FuncrateStudio;


public enum CoinPack
{
    Pack1 = 40,
    Pack2 = 120,
    Pack3 = 400
}

public class ShopManager : MonoBehaviour
{

    [Header("Coins")]
    [SerializeField] private TMP_Text coinsText;

    [Header("Hint Buttons")]
    [SerializeField] private Button hintButton1;
    [SerializeField] private Button hintButton2;
    [SerializeField] private Button hintButton3;


    [Header("Coins Buttons")]
    [SerializeField] private Button coinsButton1;
    [SerializeField] private Button coinsButton2;
    [SerializeField] private Button coinsButton3;

    public CoinPack selectedCoinPack;

    // Start is called before the first frame update
    void Start()
    {
        coinsText.text = GameManager.Coins.ToString();
    }


    void OnEnable()
    {
        //Add listeners to hint buttons
        hintButton1.onClick.AddListener(() => AddHints(1));
        hintButton2.onClick.AddListener(() => AddHints(5));
        hintButton3.onClick.AddListener(() => AddHints(10));

        //Add listeners to coin buttons
        coinsButton1.onClick.AddListener(() => AddCoins(CoinPack.Pack1));
        coinsButton2.onClick.AddListener(() => AddCoins(CoinPack.Pack2));
        coinsButton3.onClick.AddListener(() => AddCoins(CoinPack.Pack3));
    }


    private void AddHints(int hintsToAdd)
    {
        if (GameManager.Coins >= hintsToAdd)
        {

            coinsText.text = GameManager.Coins.ToString();
        }
        else // Not enough coins
        {

        }
    }


    private void AddCoins(CoinPack coinPack)
    {
        if (coinPack == CoinPack.Pack1) // Show rewarded ad for Pack1
        {
            PlayRewardedAdForMultipleTimes(coinPack, 1);
        }
        else if (coinPack == CoinPack.Pack2) // Show two rewarded ad for Pack2
        {
            PlayRewardedAdForMultipleTimes(coinPack, 2);
        }
        else if (coinPack == CoinPack.Pack3) // Show three rewarded ad for Pack3
        {
            PlayRewardedAdForMultipleTimes(coinPack, 3);
        }

        coinsText.text = GameManager.Coins.ToString();
    }

    private void PlayRewardedAdForMultipleTimes(CoinPack coinPack, int times)
    {
        if (times <= 0)
        {
            return;
        }

        FSAdsManager.Instance.ShowRewardedAd(() =>
        {
            PlayRewardedAdForMultipleTimes(coinPack, times - 1);
            coinsText.text = GameManager.Coins.ToString();
        });
    }
}

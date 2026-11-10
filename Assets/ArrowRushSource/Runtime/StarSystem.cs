using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation helpers for the stars authored in the level scene.</summary>
public static class StarSystem
{
    public static void HideStars(Image[] stars)
    {
        if (stars == null) return;
        foreach (Image star in stars)
        {
            if (star == null) continue;
            star.transform.DOKill();
            star.gameObject.SetActive(false);
        }
    }

    public static IEnumerator PresentStars(Image[] stars, int earned, Action<string> playSfx)
    {
        if (stars == null || stars.Length == 0) yield break;
        int count = Mathf.Min(3, stars.Length, Mathf.Clamp(earned, 0, 3));
        yield return new WaitForSecondsRealtime(0.25f);
        for (int i = 0; i < count; i++)
        {
            Image star = stars[i];
            if (star == null) continue;
            Transform starTransform = star.transform;
            starTransform.DOKill();
            star.gameObject.SetActive(true);
            starTransform.localScale = Vector3.zero;
            playSfx?.Invoke("LevelStar_" + (i + 1));
            starTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }
}

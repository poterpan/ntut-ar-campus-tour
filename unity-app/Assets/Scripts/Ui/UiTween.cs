using System;
using System.Collections;
using UnityEngine;

namespace NtutAR.Ui
{
    /// <summary>共用 UI 動畫協程。用法:host.StartCoroutine(UiTween.Fade(group, 1f, 0.25f))</summary>
    public static class UiTween
    {
        public static IEnumerator Fade(CanvasGroup group, float to, float duration, Action onDone = null)
        {
            float from = group.alpha;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                group.alpha = Mathf.LerpUnclamped(from, to, UiEase.OutCubic(t));
                yield return null;
            }
            group.alpha = to;
            onDone?.Invoke();
        }

        public static IEnumerator SlideAnchored(RectTransform rect, Vector2 to, float duration, Action onDone = null)
        {
            Vector2 from = rect.anchoredPosition;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                rect.anchoredPosition = Vector2.LerpUnclamped(from, to, UiEase.OutCubic(t));
                yield return null;
            }
            rect.anchoredPosition = to;
            onDone?.Invoke();
        }

        public static IEnumerator Pop(RectTransform rect, float duration, Action onDone = null)
        {
            // 0 → 1 帶 overshoot,用於蓋章/按鈕出現
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                rect.localScale = Vector3.one * UiEase.OutBack(t);
                yield return null;
            }
            rect.localScale = Vector3.one;
            onDone?.Invoke();
        }
    }
}

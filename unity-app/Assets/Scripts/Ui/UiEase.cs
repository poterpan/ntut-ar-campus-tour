using UnityEngine;

namespace NtutAR.Ui
{
    /// <summary>緩動函數(t ∈ [0,1])。UI 動畫一律經由這裡,不引第三方 tween 套件。</summary>
    public static class UiEase
    {
        public static float OutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            return 1f - u * u * u;
        }

        public static float OutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}

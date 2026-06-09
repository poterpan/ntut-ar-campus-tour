using UnityEngine;

namespace NtutAR.Ui
{
    /// <summary>把所掛的 RectTransform 限制在 Screen.safeArea 內(iPhone 瀏海/Home bar)。掛在 Canvas 下第一層容器。</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect _applied;

        private void Awake() => Apply();
        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            if (safe == _applied || Screen.width == 0 || Screen.height == 0) return;
            _applied = safe;

            var rect = (RectTransform)transform;
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

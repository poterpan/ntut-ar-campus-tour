using System.Collections;
using TMPro;
using UnityEngine;
using NtutAR.Poi;

namespace NtutAR.Ui
{
    /// <summary>AR 主畫面 HUD 總控:接收 geo-pose(由 Geo 層 driver 餵入,本層不依賴 ARCore),
    /// 更新接近提示/小地圖/進度,並驅動集章解鎖。</summary>
    public sealed class HudController : MonoBehaviour
    {
        [Header("Refs(由 HudBuilder 接線)")]
        [SerializeField] private PoiService _poiService;
        [SerializeField] private MinimapView _minimap;
        [SerializeField] private RectTransform _poiDotTemplate;
        [SerializeField] private CanvasGroup _banner;
        [SerializeField] private TextMeshProUGUI _bannerText;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private CanvasGroup _stampToast;
        [SerializeField] private TextMeshProUGUI _stampToastText;

        [Header("參數")]
        [SerializeField] private float _bannerShowMeters = 80f;
        [SerializeField] private float _stampUnlockMeters = 30f;

        public ExplorationService Exploration { get; private set; }

        private bool _bannerVisible;
        private Coroutine _bannerTween;

        private void Awake()
        {
            Exploration = new ExplorationService(new PlayerPrefsStore());
            Exploration.StampUnlocked += OnStampUnlocked;
            _banner.alpha = 0f;
            _stampToast.alpha = 0f;
        }

        private void Start()
        {
            if (_poiService != null && _minimap != null)
                _minimap.BuildPoiDots(_poiService.All, _poiDotTemplate);
            RefreshProgress();
        }

        /// <summary>由 ArHudGeoDriver / MockGeoDriver 以約 1Hz 呼叫。</summary>
        public void UpdateGeoPose(double lat, double lng)
        {
            if (_minimap != null) _minimap.UpdatePlayer(lat, lng);
            if (_poiService == null) return;

            var nearest = _poiService.GetNearest(lat, lng);
            if (!nearest.HasValue) return;
            var poi = nearest.Value;
            double d = GeoMapProjector.DistanceMeters(lat, lng, poi.lat, poi.lng);

            bool show = d <= _bannerShowMeters;
            if (show) _bannerText.text = $"{poi.name} 在前方 {FormatDistance(d)}";
            if (show != _bannerVisible)
            {
                _bannerVisible = show;
                if (_bannerTween != null) StopCoroutine(_bannerTween);
                _bannerTween = StartCoroutine(UiTween.Fade(_banner, show ? 1f : 0f, 0.3f));
            }

            if (d <= _stampUnlockMeters && !Exploration.IsUnlocked(poi.id))
                Exploration.Unlock(poi.id);
        }

        public static string FormatDistance(double meters) =>
            meters >= 1000 ? $"{meters / 1000.0:0.0}km" : $"{meters:0}m";

        private void OnStampUnlocked(string poiId)
        {
            if (_poiService != null && _poiService.TryGetById(poiId, out var poi))
                _stampToastText.text = $"收集到「{poi.name}」紀念章!";
            if (_minimap != null) _minimap.SetUnlocked(poiId, UiPalette.AccentOrange);
            RefreshProgress();
            StartCoroutine(StampToastRoutine());
        }

        private IEnumerator StampToastRoutine()
        {
            yield return UiTween.Fade(_stampToast, 1f, 0.25f);
            yield return UiTween.Pop((RectTransform)_stampToast.transform, 0.35f);
            yield return new WaitForSeconds(2.5f);
            yield return UiTween.Fade(_stampToast, 0f, 0.4f);
        }

        private void RefreshProgress()
        {
            int total = _poiService != null ? _poiService.All.Count : 0;
            _progressText.text = $"探索 {Exploration.UnlockedCount}/{total} 個景點";
        }
    }
}

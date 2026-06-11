using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NtutAR.Ui
{
    /// <summary>圓形小地圖:風格化底圖 + 玩家點 + POI 點。座標由 GeoMapProjector 換算。</summary>
    public sealed class MinimapView : MonoBehaviour
    {
        [SerializeField] private RectTransform _dotLayer;
        [SerializeField] private RectTransform _playerDot;
        [SerializeField] private GeoRect _mapRect = new GeoRect(25.0445, 121.5330, 25.0405, 121.5375);

        private readonly List<(string id, RectTransform dot)> _poiDots = new List<(string, RectTransform)>();

        public void BuildPoiDots(IReadOnlyList<NtutAR.Poi.Poi> pois, RectTransform dotTemplate)
        {
            foreach (var (_, dot) in _poiDots) Destroy(dot.gameObject);
            _poiDots.Clear();
            foreach (var poi in pois)
            {
                var dot = Instantiate(dotTemplate, _dotLayer);
                dot.gameObject.SetActive(true);
                dot.anchorMin = dot.anchorMax = GeoMapProjector.ToUv(poi.lat, poi.lng, _mapRect);
                dot.anchoredPosition = Vector2.zero;
                _poiDots.Add((poi.id, dot));
            }
        }

        public void UpdatePlayer(double lat, double lng)
        {
            Vector2 uv = GeoMapProjector.ToUv(lat, lng, _mapRect);
            _playerDot.anchorMin = _playerDot.anchorMax = uv;
            _playerDot.anchoredPosition = Vector2.zero;
        }

        public void SetUnlocked(string poiId, Color unlockedColor)
        {
            foreach (var (id, dot) in _poiDots)
                if (id == poiId && dot.TryGetComponent<Image>(out var img))
                    img.color = unlockedColor;
        }
    }
}

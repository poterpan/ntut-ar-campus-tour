using System.Collections.Generic;
using TMPro;
using UnityEngine;
using NtutAR.Poi;

namespace NtutAR.Ui
{
    /// <summary>上拉抽屜:POI 列表(名稱/距離/探索狀態)。Open/Close 用 UiTween 滑動。</summary>
    public sealed class PoiDrawerPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private RectTransform _listRoot;
        [SerializeField] private RectTransform _itemTemplate;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private float _openY = 0f;
        [SerializeField] private float _closedY = -980f;

        private readonly List<GameObject> _items = new List<GameObject>();
        private bool _open;
        private double _lat, _lng;
        private bool _hasGeo;

        public bool IsOpen => _open;

        public void Toggle(PoiService pois, ExplorationService exploration)
        {
            _open = !_open;
            if (_open) Rebuild(pois, exploration);
            StopAllCoroutines();
            StartCoroutine(UiTween.SlideAnchored(_panel, new Vector2(0, _open ? _openY : _closedY), 0.35f));
        }

        public void UpdateGeo(double lat, double lng) { _lat = lat; _lng = lng; _hasGeo = true; }

        private void Rebuild(PoiService pois, ExplorationService exploration)
        {
            foreach (var item in _items) Destroy(item);
            _items.Clear();
            int unlocked = 0;
            foreach (var poi in pois.All)
            {
                var item = Instantiate(_itemTemplate, _listRoot);
                item.gameObject.SetActive(true);
                item.Find("Name").GetComponent<TextMeshProUGUI>().text = poi.name;
                var sub = item.Find("Sub").GetComponent<TextMeshProUGUI>();
                bool done = exploration.IsUnlocked(poi.id);
                if (done) unlocked++;
                string dist = _hasGeo
                    ? HudController.FormatDistance(GeoMapProjector.DistanceMeters(_lat, _lng, poi.lat, poi.lng))
                    : "--";
                sub.text = done ? $"{dist} · 已收藏 ✓" : $"{dist} · 未探索";
                var icon = item.Find("IconBg/Icon");
                if (icon != null)
                    icon.GetComponent<TextMeshProUGUI>().text = poi.name.Length > 0 ? poi.name.Substring(0, 1) : "?";
                _items.Add(item.gameObject);
            }
            _titleText.text = $"校園景點 ({unlocked}/{pois.All.Count})";
        }
    }
}

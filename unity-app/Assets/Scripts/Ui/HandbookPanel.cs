using System.Collections.Generic;
using TMPro;
using UnityEngine;
using NtutAR.Poi;

namespace NtutAR.Ui
{
    /// <summary>全螢幕集章冊:每個 POI 一枚圓章,未解鎖為灰圈問號;底部餵貓計數。</summary>
    public sealed class HandbookPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _root;
        [SerializeField] private RectTransform _grid;
        [SerializeField] private RectTransform _stampTemplate;
        [SerializeField] private TextMeshProUGUI _summaryText;
        [SerializeField] private TextMeshProUGUI _feedText;

        private readonly List<GameObject> _stamps = new List<GameObject>();

        public void Open(PoiService pois, ExplorationService exploration)
        {
            gameObject.SetActive(true);
            Rebuild(pois, exploration);
            _root.alpha = 0f;
            StartCoroutine(UiTween.Fade(_root, 1f, 0.25f));
        }

        public void Close() => StartCoroutine(UiTween.Fade(_root, 0f, 0.2f, () => gameObject.SetActive(false)));

        private void Rebuild(PoiService pois, ExplorationService exploration)
        {
            foreach (var s in _stamps) Destroy(s);
            _stamps.Clear();
            int unlocked = 0;
            foreach (var poi in pois.All)
            {
                var stamp = Instantiate(_stampTemplate, _grid);
                stamp.gameObject.SetActive(true);
                bool done = exploration.IsUnlocked(poi.id);
                if (done) unlocked++;
                stamp.Find("Fill").gameObject.SetActive(done);
                stamp.Find("Locked").gameObject.SetActive(!done);
                stamp.Find("Name").GetComponent<TextMeshProUGUI>().text = done ? poi.name : "???";
                var fillIcon = stamp.Find("Fill/Icon");
                if (fillIcon != null && poi.name.Length > 0)
                    fillIcon.GetComponent<TextMeshProUGUI>().text = poi.name.Substring(0, 1);
                _stamps.Add(stamp.gameObject);
            }
            _summaryText.text = $"收集 {unlocked} / {pois.All.Count} 枚紀念章";
            _feedText.text = $"已餵食校園貓 {exploration.FeedCount} 次";
        }
    }
}

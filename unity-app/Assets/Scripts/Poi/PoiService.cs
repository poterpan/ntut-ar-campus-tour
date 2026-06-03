using System.Collections.Generic;
using UnityEngine;

namespace NtutAR.Poi
{
    public sealed class PoiService : MonoBehaviour
    {
        [SerializeField] private TextAsset _poiDataJson;

        private PoiRepository _repo = new PoiRepository(new List<Poi>());

        public IReadOnlyList<Poi> All => _repo.All;

        private void Awake() => Load();

        public void Load()
        {
            if (_poiDataJson == null)
            {
                Debug.LogError("[PoiService] poi_data.json TextAsset not assigned.");
                _repo = new PoiRepository(new List<Poi>());
                return;
            }

            var result = PoiDataParser.Parse(_poiDataJson.text);
            if (result.HasError)
            {
                Debug.LogError($"[PoiService] {result.Error}");
                _repo = new PoiRepository(new List<Poi>());
                return;
            }

            foreach (var w in result.Warnings)
                Debug.LogWarning($"[PoiService] {w}");

            _repo = new PoiRepository(result.Pois);
            Debug.Log($"[PoiService] Loaded {result.Pois.Count} POIs.");
        }

        public bool TryGetById(string id, out Poi poi) => _repo.TryGetById(id, out poi);

        public Poi? GetNearest(double lat, double lng) => _repo.GetNearest(lat, lng);
    }
}

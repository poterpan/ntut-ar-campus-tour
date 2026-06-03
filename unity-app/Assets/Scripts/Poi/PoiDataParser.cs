using System;
using System.Collections.Generic;
using UnityEngine;

namespace NtutAR.Poi
{
    public sealed class PoiParseResult
    {
        public readonly List<Poi> Pois = new List<Poi>();
        public readonly List<string> Warnings = new List<string>();
        public string Error;
        public bool HasError => !string.IsNullOrEmpty(Error);
    }

    public static class PoiDataParser
    {
        [Serializable]
        private class PoiList
        {
            public List<Poi> pois = new List<Poi>();
        }

        public static PoiParseResult Parse(string json)
        {
            var result = new PoiParseResult();
            if (string.IsNullOrWhiteSpace(json))
            {
                result.Error = "POI JSON is empty.";
                return result;
            }

            PoiList list;
            try
            {
                list = JsonUtility.FromJson<PoiList>(json);
            }
            catch (Exception ex)
            {
                result.Error = $"POI JSON parse failed: {ex.Message}";
                return result;
            }

            if (list?.pois == null)
            {
                result.Error = "POI JSON has no 'pois' array.";
                return result;
            }

            var seen = new HashSet<string>();
            foreach (var poi in list.pois)
            {
                if (string.IsNullOrEmpty(poi.id))
                {
                    result.Warnings.Add("POI with empty id skipped.");
                    continue;
                }
                if (!seen.Add(poi.id))
                {
                    result.Warnings.Add($"Duplicate id '{poi.id}' ignored (first wins).");
                    continue;
                }
                if (poi.lat == 0 && poi.lng == 0)
                    result.Warnings.Add($"POI '{poi.id}' missing coordinates (lat/lng both 0).");
                if (string.IsNullOrEmpty(poi.shortDescription))
                    result.Warnings.Add($"POI '{poi.id}' missing shortDescription.");
                if (string.IsNullOrEmpty(poi.llmSystemPrompt))
                    result.Warnings.Add($"POI '{poi.id}' missing llmSystemPrompt.");

                result.Pois.Add(poi);
            }

            return result;
        }
    }
}

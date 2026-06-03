using System;
using System.Collections.Generic;
using UnityEngine;

namespace NtutAR.Geo
{
    public sealed class AnchorRegistry
    {
        private readonly IAnchorResolver _resolver;
        private readonly Dictionary<string, Transform> _resolved = new Dictionary<string, Transform>();
        private readonly HashSet<string> _inFlight = new HashSet<string>();

        public AnchorRegistry(IAnchorResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public int ResolvedCount { get { return _resolved.Count; } }

        public void ResolveAll(IReadOnlyList<NtutAR.Poi.Poi> pois)
        {
            foreach (var poi in pois)
            {
                if (string.IsNullOrEmpty(poi.id)) continue;
                if (_resolved.ContainsKey(poi.id) || _inFlight.Contains(poi.id)) continue;
                _inFlight.Add(poi.id);
                _resolver.Resolve(poi, OnResolved);
            }
        }

        private void OnResolved(AnchorResolveResult result)
        {
            if (result == null || string.IsNullOrEmpty(result.PoiId)) return;
            _inFlight.Remove(result.PoiId);
            if (result.Status == AnchorResolveStatus.Success && result.Anchor != null)
                _resolved[result.PoiId] = result.Anchor;
        }

        public Transform GetAnchor(string poiId)
        {
            Transform t;
            return _resolved.TryGetValue(poiId, out t) ? t : null;
        }
    }
}

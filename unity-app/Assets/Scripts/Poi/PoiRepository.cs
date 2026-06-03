using System.Collections.Generic;

namespace NtutAR.Poi
{
    public sealed class PoiRepository
    {
        private readonly List<Poi> _pois;

        public IReadOnlyList<Poi> All => _pois;

        public PoiRepository(IEnumerable<Poi> pois)
        {
            _pois = pois != null ? new List<Poi>(pois) : new List<Poi>();
        }

        public bool TryGetById(string id, out Poi poi)
        {
            foreach (var p in _pois)
            {
                if (p.id == id)
                {
                    poi = p;
                    return true;
                }
            }
            poi = default;
            return false;
        }

        public Poi? GetNearest(double lat, double lng)
        {
            if (_pois.Count == 0) return null;
            Poi best = _pois[0];
            double bestDist = Haversine(lat, lng, best.lat, best.lng);
            for (int i = 1; i < _pois.Count; i++)
            {
                double d = Haversine(lat, lng, _pois[i].lat, _pois[i].lng);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = _pois[i];
                }
            }
            return best;
        }

        private static double Haversine(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371000.0; // 公尺
            double dLat = Deg2Rad(lat2 - lat1);
            double dLng = Deg2Rad(lng2 - lng1);
            double a = System.Math.Sin(dLat / 2) * System.Math.Sin(dLat / 2) +
                       System.Math.Cos(Deg2Rad(lat1)) * System.Math.Cos(Deg2Rad(lat2)) *
                       System.Math.Sin(dLng / 2) * System.Math.Sin(dLng / 2);
            double c = 2 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1 - a));
            return R * c;
        }

        private static double Deg2Rad(double deg) => deg * System.Math.PI / 180.0;
    }
}

using System;
using UnityEngine;

namespace NtutAR.Ui
{
    /// <summary>小地圖底圖對應的地理範圍(西北角、東南角)。</summary>
    [Serializable]
    public struct GeoRect
    {
        public double northLat;
        public double westLng;
        public double southLat;
        public double eastLng;

        public GeoRect(double northLat, double westLng, double southLat, double eastLng)
        {
            this.northLat = northLat;
            this.westLng = westLng;
            this.southLat = southLat;
            this.eastLng = eastLng;
        }
    }

    /// <summary>GPS ↔ 小地圖 UV 的線性換算 + 距離計算。校園範圍小,線性近似誤差可忽略。</summary>
    public static class GeoMapProjector
    {
        /// <summary>回傳 UV(x: 西→東 0→1,y: 南→北 0→1),超界 clamp。</summary>
        public static Vector2 ToUv(double lat, double lng, in GeoRect rect)
        {
            double u = (lng - rect.westLng) / (rect.eastLng - rect.westLng);
            double v = (lat - rect.southLat) / (rect.northLat - rect.southLat);
            return new Vector2(Mathf.Clamp01((float)u), Mathf.Clamp01((float)v));
        }

        /// <summary>Haversine 距離(公尺)。</summary>
        public static double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371000.0;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLng = (lng2 - lng1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}

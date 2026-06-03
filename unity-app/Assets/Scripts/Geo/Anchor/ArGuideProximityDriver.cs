using UnityEngine;
using Google.XR.ARCoreExtensions;
using NtutAR.Poi;

namespace NtutAR.Geo
{
    // 在 Geo 組件,維持 NtutAR.Guide 不依賴 ARCore:讀 geo-pose → 取最近 POI → 呼叫 Guide 顯示
    public sealed class ArGuideProximityDriver : MonoBehaviour
    {
        [SerializeField] private AREarthManager _earthManager;
        [SerializeField] private PoiService _poiService;
        [SerializeField] private GeospatialAnchorManager _anchorManager;
        [SerializeField] private NtutAR.Guide.GuideInteractionController _guide;
        [SerializeField] private float _proximityMeters = 30f;
        [SerializeField] private float _checkInterval = 1f;

        private float _next;

        private void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + _checkInterval;

            if (_earthManager == null || _poiService == null || _anchorManager == null || _guide == null) return;
            if (_earthManager.EarthTrackingState != UnityEngine.XR.ARSubsystems.TrackingState.Tracking) return;

            var pose = _earthManager.CameraGeospatialPose;
            var nearest = _poiService.GetNearest(pose.Latitude, pose.Longitude);
            if (!nearest.HasValue) return;

            var poi = nearest.Value;
            if (_anchorManager.GetAnchor(poi.id) == null) return;   // anchor 尚未解析

            _guide.ShowPoiByProximity(poi);
        }
    }
}

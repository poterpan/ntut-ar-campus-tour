using UnityEngine;
using Google.XR.ARCoreExtensions;

namespace NtutAR.Geo
{
    // VPS 定位狀態機:達標後隱藏 overlay 並觸發 anchor 解析
    public sealed class ArLocalizationController : MonoBehaviour
    {
        [SerializeField] private AREarthManager _earthManager;
        [SerializeField] private GeospatialAnchorManager _anchorManager;
        [SerializeField] private GameObject _localizingOverlay;   // 「正在定位…」UI
        [SerializeField] private double _horizontalAccuracyThreshold = 20.0;
        [SerializeField] private double _yawAccuracyThreshold = 25.0;

        private bool _localized;

        private void Update()
        {
            if (_localized || _earthManager == null) return;
            // AR Session 未啟動時(Editor)getter 會拋 NRE
            if (!_earthManager.isActiveAndEnabled) return;
            if (_earthManager.EarthState != EarthState.Enabled) return;
            if (_earthManager.EarthTrackingState != UnityEngine.XR.ARSubsystems.TrackingState.Tracking) return;

            var pose = _earthManager.CameraGeospatialPose;
            if (pose.HorizontalAccuracy > _horizontalAccuracyThreshold) return;
            if (pose.OrientationYawAccuracy > _yawAccuracyThreshold) return;

            _localized = true;
            if (_localizingOverlay != null) _localizingOverlay.SetActive(false);
            if (_anchorManager != null) _anchorManager.ResolveAllPois();
            Debug.Log("[ArLocalization] Localized; resolving anchors.");
        }
    }
}

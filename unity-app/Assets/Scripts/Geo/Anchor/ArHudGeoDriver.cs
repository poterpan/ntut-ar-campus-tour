using UnityEngine;
using Google.XR.ARCoreExtensions;
using NtutAR.Ui;

namespace NtutAR.Geo
{
    // 讀 geo-pose → 餵給 HUD(維持 NtutAR.Ui 不依賴 ARCore;模式同 ArGuideProximityDriver)
    public sealed class ArHudGeoDriver : MonoBehaviour
    {
        [SerializeField] private AREarthManager _earthManager;
        [SerializeField] private HudController _hud;
        [SerializeField] private OnboardingController _onboarding;
        [SerializeField] private float _interval = 1f;

        private float _next;
        private bool _notifiedLocalized;

        private void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + _interval;
            if (_earthManager == null || _hud == null) return;
            // AR Session 未啟動時(Editor / AR 初始化前)EarthManager 的 getter 會拋 NRE,先擋掉
            if (!_earthManager.isActiveAndEnabled) return;
            if (_earthManager.EarthTrackingState != UnityEngine.XR.ARSubsystems.TrackingState.Tracking) return;

            if (!_notifiedLocalized && _onboarding != null)
            {
                _notifiedLocalized = true;
                _onboarding.NotifyLocalized();
            }

            var pose = _earthManager.CameraGeospatialPose;
            _hud.UpdateGeoPose(pose.Latitude, pose.Longitude);
        }
    }
}

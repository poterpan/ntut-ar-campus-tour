using UnityEngine;

namespace NtutAR.Ui
{
    /// <summary>Editor demo 用假 GPS:沿兩點往返走,並在啟動 2 秒後觸發 onboarding 完成。</summary>
    public sealed class MockGeoDriver : MonoBehaviour
    {
        [SerializeField] private HudController _hud;
        [SerializeField] private OnboardingController _onboarding;
        [SerializeField] private double _fromLat = 25.0430, _fromLng = 121.5345;
        [SerializeField] private double _toLat = 25.0419, _toLng = 121.5357;   // 約略走向某 POI
        [SerializeField] private float _walkSeconds = 25f;

        private float _t;
        private bool _notified;

        private void Update()
        {
            if (_hud == null) return;
            if (!_notified && Time.timeSinceLevelLoad > 2f && _onboarding != null)
            {
                _notified = true;
                _onboarding.NotifyLocalized();
            }
            _t += Time.deltaTime / _walkSeconds;
            float k = Mathf.PingPong(_t, 1f);
            _hud.UpdateGeoPose(
                _fromLat + (_toLat - _fromLat) * k,
                _fromLng + (_toLng - _fromLng) * k);
        }
    }
}

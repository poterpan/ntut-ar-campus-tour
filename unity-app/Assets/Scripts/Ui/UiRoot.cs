using UnityEngine;
using UnityEngine.UI;
using NtutAR.Poi;
using NtutAR.Cat;

namespace NtutAR.Ui
{
    /// <summary>UI 場景接線中樞:HUD 按鈕 ↔ 抽屜/手帳,餵貓事件 → ExplorationService。
    /// 掛在場景的 UiSystem 物件上,Inspector 接好引用。</summary>
    public sealed class UiRoot : MonoBehaviour
    {
        [SerializeField] private PoiService _poiService;
        [SerializeField] private HudController _hud;
        [SerializeField] private PoiDrawerPanel _drawer;
        [SerializeField] private HandbookPanel _handbook;
        [SerializeField] private Button _drawerHandle;
        [SerializeField] private Button _handbookButton;
        [SerializeField] private CatSummonController _catSummon;
        [Tooltip("可空;有開場流程時,HUD 會隱藏到開場結束才淡入")]
        [SerializeField] private OnboardingController _onboarding;
        [SerializeField] private CanvasGroup _hudGroup;
        [Header("隱藏測試開關(連點玩家狀態列 5 下召喚 NPC;皆可空)")]
        [SerializeField] private Button _statusButton;
        [SerializeField] private NtutAR.Guide.GuideInteractionController _guide;

        private int _debugTapCount;
        private float _lastDebugTapTime;

        private void Start()
        {
            if (_onboarding != null && _hudGroup != null && _onboarding.gameObject.activeInHierarchy)
            {
                _hudGroup.alpha = 0f;
                _hudGroup.blocksRaycasts = false;
                _onboarding.Finished += () =>
                {
                    _hudGroup.blocksRaycasts = true;
                    StartCoroutine(UiTween.Fade(_hudGroup, 1f, 0.5f));
                };
            }

            _drawerHandle.onClick.AddListener(() => _drawer.Toggle(_poiService, _hud.Exploration));
            _hud.GeoUpdated += _drawer.UpdateGeo;
            // 抽屜開啟時把手沒有作用(被面板蓋住),直接隱藏避免從半透明面板透出
            _drawer.OpenChanged += open => _drawerHandle.gameObject.SetActive(!open);
            _handbookButton.onClick.AddListener(() => _handbook.Open(_poiService, _hud.Exploration));
            if (_catSummon != null)
                _catSummon.CatFed += () => _hud.Exploration.IncrementFeedCount();

            if (_statusButton != null && _guide != null)
                _statusButton.onClick.AddListener(OnStatusTapped);
        }

        /// <summary>連點 5 下(間隔 < 0.6s)觸發 debug 召喚 NPC。</summary>
        private void OnStatusTapped()
        {
            _debugTapCount = (Time.unscaledTime - _lastDebugTapTime < 0.6f) ? _debugTapCount + 1 : 1;
            _lastDebugTapTime = Time.unscaledTime;
            if (_debugTapCount >= 5)
            {
                _debugTapCount = 0;
                _guide.DebugSummonNpc();
            }
        }
    }
}

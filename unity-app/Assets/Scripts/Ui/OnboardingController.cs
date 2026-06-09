using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NtutAR.Ui
{
    /// <summary>開場流程:Splash → 權限說明 → 尋找位置中 → 定位完成 → 自我關閉。
    /// AR 初始化由既有 Bootstrap 流程處理;本面板只負責視覺,
    /// 由場景接線呼叫 NotifyLocalized() 進入完成步(Editor 可由 MockGeoDriver 觸發)。</summary>
    public sealed class OnboardingController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _splash;
        [SerializeField] private CanvasGroup _permission;
        [SerializeField] private CanvasGroup _locating;
        [SerializeField] private CanvasGroup _done;
        [SerializeField] private Button _startButton;
        [SerializeField] private float _splashSeconds = 2.2f;

        public event Action Finished;

        private bool _localized;

        private void Start()
        {
            _splash.alpha = 1f;
            _permission.alpha = 0f; _permission.gameObject.SetActive(false);
            _locating.alpha = 0f; _locating.gameObject.SetActive(false);
            _done.alpha = 0f; _done.gameObject.SetActive(false);
            _startButton.onClick.AddListener(OnStartPressed);
            StartCoroutine(SplashRoutine());
        }

        private IEnumerator SplashRoutine()
        {
            yield return new WaitForSeconds(_splashSeconds);
            _permission.gameObject.SetActive(true);
            yield return UiTween.Fade(_splash, 0f, 0.4f);
            _splash.gameObject.SetActive(false);
            yield return UiTween.Fade(_permission, 1f, 0.4f);
        }

        private void OnStartPressed()
        {
            // 真正的權限請求由 AR Session 啟動時系統跳窗;這裡只推進視覺流程
            StartCoroutine(ToLocating());
        }

        private IEnumerator ToLocating()
        {
            _locating.gameObject.SetActive(true);
            yield return UiTween.Fade(_permission, 0f, 0.3f);
            _permission.gameObject.SetActive(false);
            yield return UiTween.Fade(_locating, 1f, 0.3f);
            if (_localized) yield return DoneRoutine();
        }

        /// <summary>定位完成時由外部呼叫(ArHudGeoDriver 或 MockGeoDriver)。</summary>
        public void NotifyLocalized()
        {
            if (_localized) return;
            _localized = true;
            if (_locating.gameObject.activeSelf) StartCoroutine(DoneRoutine());
        }

        private IEnumerator DoneRoutine()
        {
            _done.gameObject.SetActive(true);
            yield return UiTween.Fade(_locating, 0f, 0.3f);
            yield return UiTween.Fade(_done, 1f, 0.3f);
            yield return UiTween.Pop((RectTransform)_done.transform, 0.4f);
            yield return new WaitForSeconds(1.6f);
            yield return UiTween.Fade(_done, 0f, 0.5f);
            Finished?.Invoke();
            gameObject.SetActive(false);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace NtutAR.Guide
{
    /// <summary>
    /// 「按住說話」麥克風鈕(Issue #26 STT)。按下→GuideChatPanel.OnMicDown(開始錄音),
    /// 放開→OnMicUp(停止錄音並送辨識)。用 IPointerDown/UpHandler 直接接,
    /// 不走 EventTrigger 的 UnityEvent(MCP/版控都較穩)。物件需有 Graphic(Image)當 raycast target。
    /// 按住時改色 + 放大 + 換字,給明確的視覺回饋(否則使用者不確定有沒有按到)。
    /// </summary>
    public sealed class MicHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private GuideChatPanel _panel;
        [SerializeField] private Image _image;
        [SerializeField] private TextMeshProUGUI _label;
        [Tooltip("按住時的底色(錄音紅,跟閒置綠形成對比)")]
        [SerializeField] private Color _pressedColor = new Color(0.898f, 0.302f, 0.243f, 1f);
        [Tooltip("按住時顯示的文字")]
        [SerializeField] private string _heldText = "聆聽中…";
        [Tooltip("按住時的放大倍率")]
        [SerializeField] private float _pressedScale = 1.08f;

        private Color _normalColor = Color.white;
        private string _idleText = "按住\n說話";
        private Vector3 _baseScale = Vector3.one;
        private bool _held;

        private void Awake()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_image != null) _normalColor = _image.color;
            if (_label != null) _idleText = _label.text;
            _baseScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _held = true;
            ApplyPressedVisual(true);
            _panel?.OnMicDown();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_held) return;          // 沒按下過就放開(從外面拖進來)不觸發
            _held = false;
            ApplyPressedVisual(false);
            _panel?.OnMicUp();
        }

        private void ApplyPressedVisual(bool pressed)
        {
            if (_image != null) _image.color = pressed ? _pressedColor : _normalColor;
            if (_label != null) _label.text = pressed ? _heldText : _idleText;
            transform.localScale = pressed ? _baseScale * _pressedScale : _baseScale;
        }

        private void OnDisable()
        {
            // 面板關閉/銷毀時若還按著,還原外觀避免下次開啟殘留紅色放大態
            if (_held) { _held = false; ApplyPressedVisual(false); }
        }
    }
}

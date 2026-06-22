using UnityEngine;
using UnityEngine.EventSystems;

namespace NtutAR.Guide
{
    /// <summary>
    /// 「按住說話」麥克風鈕(Issue #26 STT)。按下→GuideChatPanel.OnMicDown(開始錄音),
    /// 放開→OnMicUp(停止錄音並送辨識)。用 IPointerDown/UpHandler 直接接,
    /// 不走 EventTrigger 的 UnityEvent(MCP/版控都較穩)。物件需有 Graphic(Image)當 raycast target。
    /// </summary>
    public sealed class MicHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private GuideChatPanel _panel;

        public void OnPointerDown(PointerEventData eventData) => _panel?.OnMicDown();
        public void OnPointerUp(PointerEventData eventData) => _panel?.OnMicUp();
    }
}

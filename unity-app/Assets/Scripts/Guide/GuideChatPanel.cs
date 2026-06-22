using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NtutAR.Guide
{
    public sealed class GuideChatPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _root;            // 整個面板(開關)
        [SerializeField] private TMP_InputField _input;
        [SerializeField] private RectTransform _messageContainer;
        [SerializeField] private GameObject _messagePrefab;   // 內含 TextMeshProUGUI
        [Tooltip("送出鈕(可留空;留空時 busy 期間仍會擋送出,只是少了視覺 disable)")]
        [SerializeField] private Button _sendButton;
        [Tooltip("等待 LLM 回覆時顯示的提示文字")]
        [SerializeField] private string _thinkingText = "老黃思考中…";

        public event Action<string> Sent;

        // Issue #26 STT:麥克風「按住說話」。把 mic 按鈕的 EventTrigger PointerDown→OnMicDown、
        // PointerUp→OnMicUp 接到下面兩個方法,controller 收到事件後啟動/停止 ISpeechInput 擷取。
        public event Action SpeechCaptureStart;
        public event Action SpeechCaptureEnd;

        /// <summary>面板目前是否開啟(Issue #20:OpenChat 用來避免重複起 session)。</summary>
        public bool IsOpen => _root != null && _root.activeSelf;

        private GameObject _thinkingBubble;
        private bool _busy;

        public void Open() => _root.SetActive(true);

        public void Close()
        {
            ClearMessages();
            _root.SetActive(false);
        }

        public void AppendMessage(string speaker, string text)
        {
            var go = Instantiate(_messagePrefab, _messageContainer);
            go.GetComponentInChildren<TextMeshProUGUI>().text = $"{speaker}: {text}";
        }

        // 綁到送出 Button.onClick(以及 InputField onSubmit)
        public void OnSendButton()
        {
            var q = _input.text != null ? _input.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(q)) return;
            _input.text = string.Empty;
            SubmitText(q);
        }

        /// <summary>送出一句話(送出鈕與 STT 共用)。查詢中(busy)直接忽略。</summary>
        public void SubmitText(string text)
        {
            if (_busy) return;   // Issue #21:查詢中不接受再次送出(即使送出鈕未接也擋得住)
            if (string.IsNullOrWhiteSpace(text)) return;
            AppendMessage("你", text.Trim());
            Sent?.Invoke(text.Trim());
        }

        // mic 按鈕 EventTrigger 接點(PointerDown / PointerUp)
        public void OnMicDown() => SpeechCaptureStart?.Invoke();
        public void OnMicUp() => SpeechCaptureEnd?.Invoke();

        /// <summary>
        /// Issue #21:切換「等待 LLM 回覆」狀態。busy 時插一顆「思考中…」泡泡並鎖住輸入,
        /// 回覆到達(SetBusy(false))時移除。回覆泡泡請在 SetBusy(false) 之後再 Append,
        /// 才不會夾在思考泡泡下面。
        /// </summary>
        public void SetBusy(bool busy)
        {
            _busy = busy;
            if (_sendButton != null) _sendButton.interactable = !busy;
            if (_input != null) _input.interactable = !busy;

            if (busy)
            {
                if (_thinkingBubble == null && _messagePrefab != null)
                {
                    _thinkingBubble = Instantiate(_messagePrefab, _messageContainer);
                    _thinkingBubble.GetComponentInChildren<TextMeshProUGUI>().text = $"導遊: {_thinkingText}";
                }
            }
            else if (_thinkingBubble != null)
            {
                Destroy(_thinkingBubble);
                _thinkingBubble = null;
            }
        }

        private void ClearMessages()
        {
            _thinkingBubble = null;   // 一併銷毀於下方迴圈,清掉殘留參考
            _busy = false;
            if (_sendButton != null) _sendButton.interactable = true;
            if (_input != null) _input.interactable = true;
            for (int i = _messageContainer.childCount - 1; i >= 0; i--)
                Destroy(_messageContainer.GetChild(i).gameObject);
        }
    }
}

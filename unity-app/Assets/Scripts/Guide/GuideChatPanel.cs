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
        [Tooltip("文字已出、語音生成中(GLM TTS ~6-10s)顯示的提示文字")]
        [SerializeField] private string _speechText = "正在生成語音…";

        public event Action<string> Sent;

        // Issue #26 STT:麥克風「按住說話」。把 mic 按鈕的 EventTrigger PointerDown→OnMicDown、
        // PointerUp→OnMicUp 接到下面兩個方法,controller 收到事件後啟動/停止 ISpeechInput 擷取。
        public event Action SpeechCaptureStart;
        public event Action SpeechCaptureEnd;

        /// <summary>面板目前是否開啟(Issue #20:OpenChat 用來避免重複起 session)。</summary>
        public bool IsOpen => _root != null && _root.activeSelf;

        // 單一「狀態泡泡」:依序顯示「老黃思考中…」(LLM 查詢)→「正在生成語音…」(TTS 生成)→ 消失。
        private GameObject _pendingBubble;
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
        /// Issue #21:LLM 查詢中。顯示「老黃思考中…」狀態泡泡並鎖住輸入。
        /// 注意:SetBusy(false) 只解鎖輸入,不收掉泡泡 —— 由後續的 SetSpeechPreparing 接手
        /// (思考→生成語音 兩段連續無縫),最後在語音開始播時才收掉。
        /// </summary>
        public void SetBusy(bool busy)
        {
            _busy = busy;
            if (_sendButton != null) _sendButton.interactable = !busy;
            if (_input != null) _input.interactable = !busy;
            if (busy) ShowPending(_thinkingText);
        }

        /// <summary>
        /// Issue #26 / 延遲提示:文字已出、語音還在生成(GLM TTS POST ~6-10s)時顯示「正在生成語音…」;
        /// 語音開始播放(SpeakingStarted)時 preparing=false → 收掉泡泡。
        /// </summary>
        public void SetSpeechPreparing(bool preparing)
        {
            if (preparing) ShowPending(_speechText);
            else HidePending();
        }

        private void ShowPending(string text)
        {
            if (_pendingBubble == null && _messagePrefab != null)
                _pendingBubble = Instantiate(_messagePrefab, _messageContainer);
            if (_pendingBubble != null)
                _pendingBubble.GetComponentInChildren<TextMeshProUGUI>().text = $"導遊: {text}";
        }

        private void HidePending()
        {
            if (_pendingBubble != null)
            {
                Destroy(_pendingBubble);
                _pendingBubble = null;
            }
        }

        private void ClearMessages()
        {
            _pendingBubble = null;   // 一併銷毀於下方迴圈,清掉殘留參考
            _busy = false;
            if (_sendButton != null) _sendButton.interactable = true;
            if (_input != null) _input.interactable = true;
            for (int i = _messageContainer.childCount - 1; i >= 0; i--)
                Destroy(_messageContainer.GetChild(i).gameObject);
        }
    }
}

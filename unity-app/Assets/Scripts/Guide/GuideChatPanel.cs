using System;
using UnityEngine;
using TMPro;

namespace NtutAR.Guide
{
    public sealed class GuideChatPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _root;            // 整個面板(開關)
        [SerializeField] private TMP_InputField _input;
        [SerializeField] private RectTransform _messageContainer;
        [SerializeField] private GameObject _messagePrefab;   // 內含 TextMeshProUGUI

        public event Action<string> Sent;

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
            AppendMessage("你", q);
            Sent?.Invoke(q);
        }

        private void ClearMessages()
        {
            for (int i = _messageContainer.childCount - 1; i >= 0; i--)
                Destroy(_messageContainer.GetChild(i).gameObject);
        }
    }
}

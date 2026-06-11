using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using NtutAR.Poi;

namespace NtutAR.Guide
{
    /// <summary>
    /// 正式 ILlmClient:OpenAI-compatible chat/completions(Bearer 認證)。
    /// 設定(含 API key)讀 StreamingAssets/llm_config.json —— 該檔已 gitignore,
    /// 範本見同目錄 llm_config.example.json。Android 的 StreamingAssets 在 jar 內,
    /// 一律用 UnityWebRequest 讀,不用 File IO(Issue #10 code review 待辦)。
    /// 失敗時 throw,由 GuideChatController 降級成 FallbackMessage。
    /// </summary>
    public sealed class OpenAiCompatLlmClient : MonoBehaviour, ILlmClient
    {
        private const string ConfigFileName = "llm_config.json";

        [Header("預設值(會被 llm_config.json 覆寫)")]
        [SerializeField] private string _apiBaseUrl = "https://testvideo.site/v1";
        [SerializeField] private string _model = "gpt-5.5";
        [SerializeField] private int _maxTokens = 500;
        [Tooltip("請求逾時(秒)")]
        [SerializeField] private int _timeoutSeconds = 30;

        private string _apiKey;
        private Task _configTask;

        [Serializable]
        private class LlmConfig
        {
            public string apiBaseUrl;
            public string model;
            public int maxTokens;
            public string apiKey;
        }

        public async Task<string> AskAsync(string question, PoiContext poi, CancellationToken ct = default)
        {
            _configTask ??= LoadConfigAsync();
            await _configTask;

            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("[Llm] 缺 API key:請建立 StreamingAssets/llm_config.json(見 llm_config.example.json)");

            string url = $"{_apiBaseUrl.TrimEnd('/')}/chat/completions";
            string json = LlmWire.BuildRequestJson(_model, LlmPromptBuilder.Build(poi), question, _maxTokens);

            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
            request.timeout = _timeoutSeconds;

            await SendAsync(request, ct);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Llm] 請求失敗 {request.responseCode} {request.error}\n{request.downloadHandler.text}");
                throw new Exception($"LLM request failed: {request.responseCode} {request.error}");
            }

            string answer = LlmWire.ExtractAnswer(request.downloadHandler.text);
            if (answer == null)
            {
                Debug.LogWarning($"[Llm] 回應無法解析:{request.downloadHandler.text}");
                throw new Exception("LLM response has no parsable content");
            }
            return answer;
        }

        /// <summary>讀 StreamingAssets 設定(僅一次,lazy)。找不到檔案不算錯——Inspector 預設值仍可用(但無 key 會在 Ask 時報錯)。</summary>
        private async Task LoadConfigAsync()
        {
            string path = Path.Combine(Application.streamingAssetsPath, ConfigFileName);
            // iOS/Editor 是一般路徑要補 file://;Android 已是 jar:file:// URL
            string uri = path.Contains("://") ? path : "file://" + path;

            using var request = UnityWebRequest.Get(uri);
            await SendAsync(request, CancellationToken.None);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Llm] 找不到 {ConfigFileName},使用 Inspector 預設值(無 API key)。");
                return;
            }

            try
            {
                var config = JsonUtility.FromJson<LlmConfig>(request.downloadHandler.text);
                if (!string.IsNullOrWhiteSpace(config.apiBaseUrl)) _apiBaseUrl = config.apiBaseUrl.Trim();
                if (!string.IsNullOrWhiteSpace(config.model)) _model = config.model.Trim();
                if (config.maxTokens > 0) _maxTokens = config.maxTokens;
                if (!string.IsNullOrWhiteSpace(config.apiKey)) _apiKey = config.apiKey.Trim();
                Debug.Log($"[Llm] 設定載入:{_apiBaseUrl} / {_model} / maxTokens={_maxTokens} / key={( _apiKey != null ? "有" : "無")}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Llm] {ConfigFileName} 解析失敗:{ex.Message}");
            }
        }

        /// <summary>UnityWebRequest → Task(completed callback 橋接;取消時 Abort)。</summary>
        private static Task SendAsync(UnityWebRequest request, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var op = request.SendWebRequest();
            op.completed += _ => tcs.TrySetResult(true);

            if (ct.CanBeCanceled)
            {
                ct.Register(() =>
                {
                    try { request.Abort(); } catch (Exception) { /* 已釋放就算了 */ }
                });
            }
            return tcs.Task;
        }
    }
}

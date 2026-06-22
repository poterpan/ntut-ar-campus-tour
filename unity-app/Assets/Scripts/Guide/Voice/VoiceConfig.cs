using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace NtutAR.Guide.Voice
{
    /// <summary>
    /// 語音服務(TTS/STT)的設定,與 LLM 共用 StreamingAssets/llm_config.json。
    /// JsonUtility 會忽略它不認得的欄位,所以同一份檔同時供 OpenAiCompatLlmClient 與語音服務讀,
    /// 互不影響。金鑰一律放這份(gitignored);CI 由 secret 生成。
    /// </summary>
    [Serializable]
    public sealed class VoiceConfig
    {
        // GLM TTS
        public string glmApiKey;
        public string glmTtsUrl = "https://open.bigmodel.cn/api/paas/v4/audio/speech";
        public string glmTtsModel = "glm-tts";
        public string glmVoice = "xiaochen";
        public float glmSpeed = 1.3f;

        // ElevenLabs STT
        public string elevenLabsApiKey;
        public string sttUrl = "https://api.elevenlabs.io/v1/speech-to-text";
        public string sttModel = "scribe_v1";

        private const string ConfigFileName = "llm_config.json";
        private static Task<VoiceConfig> _shared;

        /// <summary>載入(只讀一次,之後共用同一個 Task)。找不到檔案回預設值(無金鑰)。</summary>
        public static Task<VoiceConfig> LoadAsync()
        {
            return _shared ??= LoadInternalAsync();
        }

        private static async Task<VoiceConfig> LoadInternalAsync()
        {
            var config = new VoiceConfig();
            string path = Path.Combine(Application.streamingAssetsPath, ConfigFileName);
            string uri = path.Contains("://") ? path : "file://" + path;   // Android 已是 jar:file:// URL

            using var request = UnityWebRequest.Get(uri);
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            request.SendWebRequest().completed += _ => tcs.TrySetResult(true);
            await tcs.Task;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Voice] 找不到 {ConfigFileName},語音功能無金鑰可用(會降級為靜默)。");
                return config;
            }

            try
            {
                // 用 FromJsonOverwrite 保留預設值(JSON 沒帶的欄位不被清空)
                JsonUtility.FromJsonOverwrite(request.downloadHandler.text, config);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Voice] {ConfigFileName} 解析失敗:{ex.Message}");
            }
            return config;
        }
    }
}

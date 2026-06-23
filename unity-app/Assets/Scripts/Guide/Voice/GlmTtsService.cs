using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace NtutAR.Guide.Voice
{
    /// <summary>
    /// GLM(智譜)TTS,實作 ITtsService。模式同 OpenAiCompatLlmClient:設定/金鑰讀 llm_config.json。
    /// model glm-tts、voice 小陳(xiaochen)、speed 1.3、response_format wav(24kHz mono)。
    /// 免費帳號音檔開頭帶服務端提示音 → 用 WavUtil 能量裁剪掉。
    /// 播放期間 IsSpeaking/事件可供 GuideChatController 同步 NPC Talk/Listening 動畫時長。
    /// 失敗時降級:不拋(由呼叫端的 try/catch 也會吃掉),純文字照常顯示。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class GlmTtsService : MonoBehaviour, ITtsService
    {
        [Tooltip("請求逾時(秒);GLM TTS 約 6~10s")]
        [SerializeField] private int _timeoutSeconds = 30;
        [Tooltip("是否裁掉開頭服務端提示音(GLM 免費帳號浮水印)")]
        [SerializeField] private bool _trimPromptTone = true;

        public bool IsSpeaking { get; private set; }
        public event Action SpeakingStarted;
        public event Action SpeakingStopped;

        private AudioSource _audio;
        private Task<VoiceConfig> _configTask;

        [Serializable]
        private class TtsRequest
        {
            public string model;
            public string input;
            public string voice;
            public string response_format;
            public float speed;
        }

        private void Awake()
        {
            _audio = GetComponent<AudioSource>();
            _audio.playOnAwake = false;
        }

        public async Task SpeakAsync(string text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            _configTask ??= VoiceConfig.LoadAsync();
            var cfg = await _configTask;

            if (string.IsNullOrWhiteSpace(cfg.glmApiKey))
            {
                Debug.LogWarning("[GlmTts] 缺 glmApiKey(llm_config.json),語音降級為靜默。");
                return;
            }

            byte[] wav = await RequestWavAsync(text, cfg, ct);
            if (wav == null || wav.Length == 0) return;

            if (!WavUtil.TryDecode(wav, out float[] samples, out int channels, out int sampleRate))
            {
                Debug.LogWarning("[GlmTts] WAV 解碼失敗,語音降級為靜默。");
                return;
            }

            int startSample = _trimPromptTone
                ? WavUtil.FindSpeechStartSample(samples, channels, sampleRate)
                : 0;
            int len = samples.Length - startSample;
            if (len <= 0) return;

            float[] trimmed = new float[len];
            Array.Copy(samples, startSample, trimmed, 0, len);

            int frames = len / channels;
            var clip = AudioClip.Create("glm_tts", frames, channels, sampleRate, false);
            clip.SetData(trimmed, 0);

            await PlayAsync(clip, ct);
        }

        private async Task<byte[]> RequestWavAsync(string text, VoiceConfig cfg, CancellationToken ct)
        {
            var body = new TtsRequest
            {
                model = cfg.glmTtsModel,
                input = text,
                voice = cfg.glmVoice,
                response_format = "wav",
                speed = cfg.glmSpeed
            };
            string json = JsonUtility.ToJson(body);

            using var request = new UnityWebRequest(cfg.glmTtsUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {cfg.glmApiKey}");
            request.timeout = _timeoutSeconds;

            await SendAsync(request, ct);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[GlmTts] 請求失敗 {request.responseCode} {request.error}");
                return null;
            }
            return request.downloadHandler.data;
        }

        private async Task PlayAsync(AudioClip clip, CancellationToken ct)
        {
            _audio.clip = clip;
            _audio.Play();
            IsSpeaking = true;
            SpeakingStarted?.Invoke();
            try
            {
                await Task.Yield();   // 讓 AudioSource 起播,isPlaying 才會為 true
                while (_audio.isPlaying)
                {
                    if (ct.IsCancellationRequested)
                    {
                        _audio.Stop();
                        break;
                    }
                    await Task.Yield();
                }
            }
            finally
            {
                IsSpeaking = false;
                SpeakingStopped?.Invoke();
            }
        }

        private static Task SendAsync(UnityWebRequest request, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            request.SendWebRequest().completed += _ => tcs.TrySetResult(true);
            if (ct.CanBeCanceled)
            {
                ct.Register(() => { try { request.Abort(); } catch (Exception) { /* 已釋放 */ } });
            }
            return tcs.Task;
        }
    }
}

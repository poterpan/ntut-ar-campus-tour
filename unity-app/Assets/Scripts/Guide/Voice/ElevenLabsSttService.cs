using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace NtutAR.Guide.Voice
{
    /// <summary>
    /// ElevenLabs 語音轉文字(scribe_v1),實作 ISpeechInput。
    /// 「按住說話」語意:UI 按下時呼叫 ListenAsync(token),放開時 token.Cancel() →
    /// 停止錄音並把錄到的音檔上傳轉寫(放開不是錯誤,而是「停止錄音、開始辨識」的訊號)。
    /// scribe 輸出為簡體,預設轉繁顯示;餵 LLM 用原文即可。
    /// 金鑰讀 llm_config.json(elevenLabsApiKey)。
    /// 注意:iOS 需在 PlayerSettings 設 Microphone Usage Description;Android 需 RECORD_AUDIO 權限。
    /// </summary>
    public sealed class ElevenLabsSttService : MonoBehaviour, ISpeechInput
    {
        [Tooltip("最長錄音秒數(到時自動停止)")]
        [SerializeField] private int _maxSeconds = 15;
        [Tooltip("錄音取樣率(語音 16k 足夠,上傳檔較小)")]
        [SerializeField] private int _sampleRate = 16000;
        [Tooltip("辨識(上傳)逾時秒數")]
        [SerializeField] private int _timeoutSeconds = 20;
        [Tooltip("把辨識結果(簡體)轉繁體再回傳")]
        [SerializeField] private bool _convertToTraditional = true;

        private Task<VoiceConfig> _configTask;

        [Serializable]
        private class SttResponse { public string text; }

        public async Task<string> ListenAsync(CancellationToken ct = default)
        {
            _configTask ??= VoiceConfig.LoadAsync();
            var cfg = await _configTask;

            if (string.IsNullOrWhiteSpace(cfg.elevenLabsApiKey))
            {
                Debug.LogWarning("[Stt] 缺 elevenLabsApiKey(llm_config.json),語音輸入停用。");
                return string.Empty;
            }
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[Stt] 找不到麥克風裝置。");
                return string.Empty;
            }

            await EnsureMicPermissionAsync();

            // ── 錄音:從按下到放開(ct 取消)或達上限 ──
            var clip = Microphone.Start(null, false, _maxSeconds, _sampleRate);
            if (clip == null) return string.Empty;

            float deadline = Time.realtimeSinceStartup + _maxSeconds;
            var stop = new TaskCompletionSource<bool>();
            using (ct.Register(() => stop.TrySetResult(true)))
            {
                while (!stop.Task.IsCompleted &&
                       Time.realtimeSinceStartup < deadline &&
                       Microphone.IsRecording(null))
                {
                    await Task.Yield();
                }
            }

            int pos = Microphone.GetPosition(null);
            Microphone.End(null);
            if (pos <= 0) return string.Empty;

            var buffer = new float[clip.samples * clip.channels];
            clip.GetData(buffer, 0);
            int valid = Mathf.Min(pos * clip.channels, buffer.Length);
            var mono = new float[valid];
            Array.Copy(buffer, mono, valid);

            byte[] wav = WavUtil.EncodeWav16(mono, _sampleRate);

            // 放開只是停止錄音 → 辨識請求不可用 ct(會被立刻 Abort);改用 None,靠 timeout 收尾
            string text = await TranscribeAsync(wav, cfg);
            if (string.IsNullOrEmpty(text)) return string.Empty;

            return _convertToTraditional ? ChineseTextUtil.SimplifiedToTraditional(text) : text;
        }

        private async Task<string> TranscribeAsync(byte[] wav, VoiceConfig cfg)
        {
            var form = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("file", wav, "audio.wav", "audio/wav"),
                new MultipartFormDataSection("model_id", cfg.sttModel),
            };

            using var request = UnityWebRequest.Post(cfg.sttUrl, form);
            request.SetRequestHeader("xi-api-key", cfg.elevenLabsApiKey);
            request.timeout = _timeoutSeconds;

            await SendAsync(request, CancellationToken.None);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stt] 辨識失敗 {request.responseCode} {request.error}\n{request.downloadHandler.text}");
                return string.Empty;
            }

            try
            {
                var r = JsonUtility.FromJson<SttResponse>(request.downloadHandler.text);
                return r?.text != null ? r.text.Trim() : string.Empty;
            }
            catch (Exception)
            {
                Debug.LogWarning($"[Stt] 回應無法解析:{request.downloadHandler.text}");
                return string.Empty;
            }
        }

        private static async Task EnsureMicPermissionAsync()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                var op = Application.RequestUserAuthorization(UserAuthorization.Microphone);
                while (op != null && !op.isDone) await Task.Yield();
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
                // 等使用者回應(粗略等待;授權後下次按即可用)
                await Task.Delay(500);
            }
#else
            await Task.CompletedTask;
#endif
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

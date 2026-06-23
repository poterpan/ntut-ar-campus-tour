using System;
using System.Threading;
using System.Threading.Tasks;
using NtutAR.Poi;

namespace NtutAR.Guide
{
    public sealed class GuideChatController
    {
        public const string FallbackMessage = "抱歉,我現在連不上,稍後再試。";

        private readonly ILlmClient _llm;
        private readonly ITtsService _tts;
        private NtutAR.Poi.Poi _currentPoi;
        private bool _busy;

        public bool IsBusy => _busy;

        public event Action<NpcState> NpcStateChanged;
        public event Action<string> GuideMessageReady;
        /// <summary>文字已出、語音尚在生成(true)→ 語音開始播放(false)。供 UI 顯示「正在生成語音…」。</summary>
        public event Action<bool> SpeechPreparingChanged;

        public GuideChatController(ILlmClient llm, ITtsService tts)
        {
            _llm = llm ?? throw new ArgumentNullException(nameof(llm));
            _tts = tts ?? throw new ArgumentNullException(nameof(tts));
            // 語音真正開始播 → 收掉「正在生成語音…」指示
            _tts.SpeakingStarted += () => SpeechPreparingChanged?.Invoke(false);
        }

        public Task StartSessionAsync(NtutAR.Poi.Poi poi, CancellationToken ct = default)
        {
            _currentPoi = poi;
            // 有開場白才講(Talk→Listening);沒有就讓 Greet(揮手)靠 Animator 的 exit-time 自然播完回 Listening,不強制狀態以免打斷揮手
            if (!string.IsNullOrEmpty(poi.shortDescription))
                return SpeakAsGuideAsync(poi.shortDescription, ct);
            return Task.CompletedTask;
        }

        public async Task AskAsync(string question, CancellationToken ct = default)
        {
            if (_busy || string.IsNullOrWhiteSpace(question)) return;
            _busy = true;
            try
            {
                string answer;
                try
                {
                    answer = await _llm.AskAsync(question, _currentPoi.ToContext(), ct);
                }
                catch (Exception)
                {
                    answer = FallbackMessage;
                }
                await SpeakAsGuideAsync(answer, ct);
            }
            finally
            {
                _busy = false;
            }
        }

        private async Task SpeakAsGuideAsync(string text, CancellationToken ct)
        {
            GuideMessageReady?.Invoke(text);
            NpcStateChanged?.Invoke(NpcState.Talking);
            SpeechPreparingChanged?.Invoke(true);   // 文字已出,語音生成中(SpeakingStarted 會收掉)
            try
            {
                await _tts.SpeakAsync(text, ct);
            }
            catch (Exception)
            {
                // TTS 失敗 → 降級純文字,不中斷
            }
            finally
            {
                SpeechPreparingChanged?.Invoke(false);   // 播完/失敗都確保收掉指示
            }
            NpcStateChanged?.Invoke(NpcState.Listening);
        }
    }
}

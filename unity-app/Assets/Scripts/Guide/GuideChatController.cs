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

        public GuideChatController(ILlmClient llm, ITtsService tts)
        {
            _llm = llm ?? throw new ArgumentNullException(nameof(llm));
            _tts = tts ?? throw new ArgumentNullException(nameof(tts));
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
            try
            {
                await _tts.SpeakAsync(text, ct);
            }
            catch (Exception)
            {
                // TTS 失敗 → 降級純文字,不中斷
            }
            NpcStateChanged?.Invoke(NpcState.Listening);
        }
    }
}

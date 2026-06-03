using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NtutAR.Guide.Mocks
{
    public sealed class MockTtsService : MonoBehaviour, ITtsService
    {
        public bool IsSpeaking { get; private set; }
        public event Action SpeakingStarted;
        public event Action SpeakingStopped;

        public async Task SpeakAsync(string text, CancellationToken ct = default)
        {
            IsSpeaking = true;
            SpeakingStarted?.Invoke();
            Debug.Log($"[MockTts] 念: {text}");
            await Task.Delay(Mathf.Clamp(text.Length * 60, 500, 4000), ct);
            IsSpeaking = false;
            SpeakingStopped?.Invoke();
        }
    }
}

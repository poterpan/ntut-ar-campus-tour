using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using NtutAR.Poi;

namespace NtutAR.Guide
{
    public enum NpcState { Listening, Talking }

    public interface ILlmClient
    {
        Task<string> AskAsync(string question, PoiContext poi, CancellationToken ct = default);
    }

    public interface ITtsService
    {
        Task SpeakAsync(string text, CancellationToken ct = default);
        bool IsSpeaking { get; }
        event Action SpeakingStarted;   // 保留供未來 lip-sync
        event Action SpeakingStopped;
    }

    public interface ISpeechInput   // STT —— 本期保留,不實作
    {
        Task<string> ListenAsync(CancellationToken ct = default);
    }

    public interface IPoiAnchorProvider
    {
        Transform GetAnchor(string poiId);   // 找不到回 null
    }
}

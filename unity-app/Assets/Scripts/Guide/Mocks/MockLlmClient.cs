using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using NtutAR.Poi;

namespace NtutAR.Guide.Mocks
{
    public sealed class MockLlmClient : MonoBehaviour, ILlmClient
    {
        public async Task<string> AskAsync(string question, PoiContext poi, CancellationToken ct = default)
        {
            await Task.Delay(600, ct);
            return $"(mock)關於「{poi.Name}」,你問了:{question}。這是模擬回答。";
        }
    }
}

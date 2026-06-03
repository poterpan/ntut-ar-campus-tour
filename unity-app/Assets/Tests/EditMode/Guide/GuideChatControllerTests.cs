using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NtutAR.Poi;

namespace NtutAR.Guide.Tests
{
    public class GuideChatControllerTests
    {
        private sealed class FakeLlm : ILlmClient
        {
            public int Calls;
            public string Response = "這是導遊的回答。";
            public bool Throw;
            public TaskCompletionSource<string> Gate;   // 非 null = 卡住直到 SetResult

            public Task<string> AskAsync(string question, PoiContext poi, CancellationToken ct = default)
            {
                Calls++;
                if (Throw) throw new Exception("boom");
                return Gate != null ? Gate.Task : Task.FromResult(Response);
            }
        }

        private sealed class FakeTts : ITtsService
        {
            public readonly List<string> Spoken = new List<string>();
            public bool IsSpeaking => false;
#pragma warning disable 67
            public event Action SpeakingStarted;
            public event Action SpeakingStopped;
#pragma warning restore 67
            public Task SpeakAsync(string text, CancellationToken ct = default)
            {
                Spoken.Add(text);
                return Task.CompletedTask;
            }
        }

        private static NtutAR.Poi.Poi MakePoi(string shortDesc) => new NtutAR.Poi.Poi
        {
            id = "p01", name = "校門口", lat = 25.04, lng = 121.53,
            shortDescription = shortDesc, llmSystemPrompt = "你是校門口導遊。"
        };

        [Test]
        public void Ask_Normal_RaisesMessageAndSpeaks()
        {
            var llm = new FakeLlm { Response = "答案A" };
            var tts = new FakeTts();
            var c = new GuideChatController(llm, tts);
            string shown = null;
            c.GuideMessageReady += m => shown = m;

            c.AskAsync("這是什麼?").GetAwaiter().GetResult();

            Assert.AreEqual(1, llm.Calls);
            Assert.AreEqual("答案A", shown);
            Assert.Contains("答案A", tts.Spoken);
        }

        [Test]
        public void Ask_LlmThrows_ShowsFallbackNoThrow()
        {
            var c = new GuideChatController(new FakeLlm { Throw = true }, new FakeTts());
            string shown = null;
            c.GuideMessageReady += m => shown = m;

            Assert.DoesNotThrow(() => c.AskAsync("x").GetAwaiter().GetResult());
            Assert.AreEqual(GuideChatController.FallbackMessage, shown);
        }

        [Test]
        public void Ask_StateSequence_TalkingThenListening()
        {
            var c = new GuideChatController(new FakeLlm(), new FakeTts());
            var states = new List<NpcState>();
            c.NpcStateChanged += s => states.Add(s);

            c.AskAsync("x").GetAwaiter().GetResult();

            Assert.AreEqual(new[] { NpcState.Talking, NpcState.Listening }, states.ToArray());
        }

        [Test]
        public void Ask_WhileBusy_IgnoresSecond()
        {
            var gate = new TaskCompletionSource<string>();
            var llm = new FakeLlm { Gate = gate };
            var c = new GuideChatController(llm, new FakeTts());

            var t1 = c.AskAsync("first");                 // 卡在 gate
            c.AskAsync("second").GetAwaiter().GetResult(); // busy → 忽略
            Assert.AreEqual(1, llm.Calls);

            gate.SetResult("done");
            t1.GetAwaiter().GetResult();
        }

        [Test]
        public void StartSession_WithShortDesc_Speaks()
        {
            var tts = new FakeTts();
            var c = new GuideChatController(new FakeLlm(), tts);

            c.StartSessionAsync(MakePoi("開場白!")).GetAwaiter().GetResult();

            Assert.Contains("開場白!", tts.Spoken);
        }

        [Test]
        public void StartSession_EmptyShortDesc_NoSpeakNoStateChange()
        {
            var tts = new FakeTts();
            var c = new GuideChatController(new FakeLlm(), tts);
            int stateChanges = 0;
            c.NpcStateChanged += s => stateChanges++;

            c.StartSessionAsync(MakePoi("")).GetAwaiter().GetResult();

            Assert.AreEqual(0, tts.Spoken.Count);
            Assert.AreEqual(0, stateChanges);
        }
    }
}

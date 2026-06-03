# 導遊 NPC 體驗 — Phase 1(mock-first)實作計畫

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 用 mock seams 把「導遊 NPC 對話」在桌面 Play 跑通 —— 點 NPC → 開場白 → 問答 → NPC idle/talk 動畫,完全不依賴真的 AR anchor / LLM / TTS。

**Architecture:** 純 C# 編排核心 `GuideChatController`(EditMode 可單測)+ 四個 seam 介面(`ILlmClient`/`ITtsService`/`ISpeechInput`/`IPoiAnchorProvider`)+ MonoBehaviour 黏合(`NpcAnimator`/`GuideChatPanel`/`GuideInteractionController`)+ mock 實作。資料來源接已併入 main 的 `NtutAR.Poi.PoiService`。

**Tech Stack:** Unity 6 (C#)、async/await、TextMeshPro(中文 UI)、Unity Test Framework (NUnit, EditMode)、asmdef、Humanoid rig。

## 對 spec 的實作期細化

- `GuideChatController` 在 `SpeakAsync` 的 await 前後**自行驅動** `NpcStateChanged`(Talking→Listening),不依賴 `ITtsService` 的 `SpeakingStarted/Stopped` 事件(事件保留在介面供未來 lip-sync)。理由:可決定性單測。
- Phase 1 桌面跑通用 `_useDebugPoi` 路徑(指定一個 POI、用 mock anchor 放鏡頭前),**真實 geo-proximity**(`AREarthManager.CameraGeospatialPose` → `PoiService.GetNearest`)留待 Phase 2(需 AR 裝置)。
- 「序列化介面」用 `[SerializeField] private MonoBehaviour` + `as` 轉型(Unity 慣用法),mock 實作做成 MonoBehaviour 以便 Inspector 指定。

## 兩階段執行(同 POI 計畫,因 .meta 須 Unity 產生)

- **階段 A(Unity 關閉)**:Task 1–5 純檔案撰寫,不 commit。
- **階段 B(Unity 開啟)**:Task 6 —— 產 .meta、匯入 FBX、建 Animator/prefab/UI、接測試場景、跑 EditMode 測試 + 桌面 Play 驗證、commit。

## File Structure

```
unity-app/Assets/Scripts/Guide/
  NtutAR.Guide.asmdef           # references: NtutAR.Poi, Unity.TextMeshPro
  Seams.cs                       # NpcState + ILlmClient/ITtsService/ISpeechInput/IPoiAnchorProvider
  GuideChatController.cs         # 純 C# 編排(可單測)
  NpcAnimator.cs                 # MonoBehaviour
  GuideChatPanel.cs              # MonoBehaviour (TMP UI)
  GuideInteractionController.cs  # MonoBehaviour (場景大腦,Phase 1 debug 路徑)
  Mocks/
    MockLlmClient.cs MockTtsService.cs MockPoiAnchorProvider.cs   # MonoBehaviour mocks
unity-app/Assets/Tests/EditMode/
  NtutAR.Guide.Tests.asmdef      # references NtutAR.Guide, NtutAR.Poi
  GuideChatControllerTests.cs
unity-app/Assets/Art/NPC/        # 4 個 FBX + Animator Controller + NpcGuide.prefab(Task 6 於 Unity 建)
```

---

## Task 1: Guide 組件 + seams

**Files:**
- Create: `unity-app/Assets/Scripts/Guide/NtutAR.Guide.asmdef`
- Create: `unity-app/Assets/Scripts/Guide/Seams.cs`

- [ ] **Step 1: asmdef**

`unity-app/Assets/Scripts/Guide/NtutAR.Guide.asmdef`:
```json
{
    "name": "NtutAR.Guide",
    "rootNamespace": "NtutAR.Guide",
    "references": [
        "NtutAR.Poi",
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: seams `Seams.cs`**

`unity-app/Assets/Scripts/Guide/Seams.cs`:
```csharp
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
```

- [ ] **Step 3: 階段 A 不 commit**

---

## Task 2: GuideChatController(測試先行)

**Files:**
- Create: `unity-app/Assets/Tests/EditMode/NtutAR.Guide.Tests.asmdef`
- Create: `unity-app/Assets/Tests/EditMode/GuideChatControllerTests.cs`
- Create: `unity-app/Assets/Scripts/Guide/GuideChatController.cs`

- [ ] **Step 1: 測試 asmdef**

`unity-app/Assets/Tests/EditMode/NtutAR.Guide.Tests.asmdef`:
```json
{
    "name": "NtutAR.Guide.Tests",
    "rootNamespace": "NtutAR.Guide.Tests",
    "references": [
        "NtutAR.Guide",
        "NtutAR.Poi",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: 失敗測試 `GuideChatControllerTests.cs`**(mock 同步完成,以 `.GetAwaiter().GetResult()` 在 `[Test]` 內等待,避免 async runner 問題)

`unity-app/Assets/Tests/EditMode/GuideChatControllerTests.cs`:
```csharp
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
            public event Action SpeakingStarted;
            public event Action SpeakingStopped;
            public Task SpeakAsync(string text, CancellationToken ct = default)
            {
                Spoken.Add(text);
                return Task.CompletedTask;
            }
        }

        private static Poi MakePoi(string shortDesc) => new Poi
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
        public void StartSession_EmptyShortDesc_NoSpeak_EndsListening()
        {
            var tts = new FakeTts();
            var c = new GuideChatController(new FakeLlm(), tts);
            NpcState last = NpcState.Talking;
            c.NpcStateChanged += s => last = s;

            c.StartSessionAsync(MakePoi("")).GetAwaiter().GetResult();

            Assert.AreEqual(0, tts.Spoken.Count);
            Assert.AreEqual(NpcState.Listening, last);
        }
    }
}
```

- [ ] **Step 3: 實作 `GuideChatController.cs`**

`unity-app/Assets/Scripts/Guide/GuideChatController.cs`:
```csharp
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
        private Poi _currentPoi;
        private bool _busy;

        public bool IsBusy => _busy;

        public event Action<NpcState> NpcStateChanged;
        public event Action<string> GuideMessageReady;

        public GuideChatController(ILlmClient llm, ITtsService tts)
        {
            _llm = llm ?? throw new ArgumentNullException(nameof(llm));
            _tts = tts ?? throw new ArgumentNullException(nameof(tts));
        }

        public async Task StartSessionAsync(Poi poi, CancellationToken ct = default)
        {
            _currentPoi = poi;
            if (!string.IsNullOrEmpty(poi.shortDescription))
                await SpeakAsGuideAsync(poi.shortDescription, ct);
            else
                NpcStateChanged?.Invoke(NpcState.Listening);
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
```

- [ ] **Step 4: 階段 A 不 commit**(red/green 驗證在 Task 6)

---

## Task 3: Mock seams(桌面 Play 用,MonoBehaviour)

**Files:**
- Create: `unity-app/Assets/Scripts/Guide/Mocks/MockLlmClient.cs`
- Create: `unity-app/Assets/Scripts/Guide/Mocks/MockTtsService.cs`
- Create: `unity-app/Assets/Scripts/Guide/Mocks/MockPoiAnchorProvider.cs`

- [ ] **Step 1: `MockLlmClient.cs`**
```csharp
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
```

- [ ] **Step 2: `MockTtsService.cs`**
```csharp
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
```

- [ ] **Step 3: `MockPoiAnchorProvider.cs`**
```csharp
using UnityEngine;

namespace NtutAR.Guide.Mocks
{
    public sealed class MockPoiAnchorProvider : MonoBehaviour, IPoiAnchorProvider
    {
        [SerializeField] private Transform _anchorPoint;   // 場景放一個點(鏡頭前)

        public Transform GetAnchor(string poiId) => _anchorPoint;
    }
}
```

- [ ] **Step 4: 階段 A 不 commit**

---

## Task 4: NpcAnimator + GuideChatPanel(MonoBehaviour)

**Files:**
- Create: `unity-app/Assets/Scripts/Guide/NpcAnimator.cs`
- Create: `unity-app/Assets/Scripts/Guide/GuideChatPanel.cs`

- [ ] **Step 1: `NpcAnimator.cs`**(Animator 參數:Trigger `Greet`/`Talk`/`Listening`)
```csharp
using UnityEngine;

namespace NtutAR.Guide
{
    [RequireComponent(typeof(Animator))]
    public sealed class NpcAnimator : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
        }

        public void PlayGreet() => _animator.SetTrigger("Greet");
        public void PlayListening() => _animator.SetTrigger("Listening");
        public void PlayTalk() => _animator.SetTrigger("Talk");

        public void OnNpcState(NpcState state)
        {
            if (state == NpcState.Talking) PlayTalk();
            else PlayListening();
        }
    }
}
```

- [ ] **Step 2: `GuideChatPanel.cs`**(TMP UI;Canvas/prefab 於 Task 6 建)
```csharp
using System;
using UnityEngine;
using TMPro;

namespace NtutAR.Guide
{
    public sealed class GuideChatPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _root;            // 整個面板(開關)
        [SerializeField] private TMP_InputField _input;
        [SerializeField] private RectTransform _messageContainer;
        [SerializeField] private GameObject _messagePrefab;   // 內含 TextMeshProUGUI

        public event Action<string> Sent;

        public void Open() => _root.SetActive(true);

        public void Close()
        {
            ClearMessages();
            _root.SetActive(false);
        }

        public void AppendMessage(string speaker, string text)
        {
            var go = Instantiate(_messagePrefab, _messageContainer);
            go.GetComponentInChildren<TextMeshProUGUI>().text = $"{speaker}: {text}";
        }

        // 綁到送出 Button.onClick(以及 InputField onSubmit)
        public void OnSendButton()
        {
            var q = _input.text != null ? _input.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(q)) return;
            _input.text = string.Empty;
            AppendMessage("你", q);
            Sent?.Invoke(q);
        }

        private void ClearMessages()
        {
            for (int i = _messageContainer.childCount - 1; i >= 0; i--)
                Destroy(_messageContainer.GetChild(i).gameObject);
        }
    }
}
```

- [ ] **Step 3: 階段 A 不 commit**

---

## Task 5: GuideInteractionController(場景大腦,Phase 1 debug 路徑)

**Files:**
- Create: `unity-app/Assets/Scripts/Guide/GuideInteractionController.cs`

- [ ] **Step 1: 實作**
```csharp
using UnityEngine;
using NtutAR.Poi;

namespace NtutAR.Guide
{
    public sealed class GuideInteractionController : MonoBehaviour
    {
        [Header("資料/服務(可拖 mock 或真實實作)")]
        [SerializeField] private PoiService _poiService;
        [SerializeField] private MonoBehaviour _llmBehaviour;     // ILlmClient
        [SerializeField] private MonoBehaviour _ttsBehaviour;     // ITtsService
        [SerializeField] private MonoBehaviour _anchorBehaviour;  // IPoiAnchorProvider

        [Header("場景物件")]
        [SerializeField] private GameObject _npcPrefab;
        [SerializeField] private GuideChatPanel _panel;
        [SerializeField] private Camera _arCamera;

        [Header("Phase 1 桌面 debug")]
        [SerializeField] private bool _useDebugPoi = true;
        [SerializeField] private string _debugPoiId = "p01";

        private GuideChatController _chat;
        private NpcAnimator _npcAnimator;
        private GameObject _npcInstance;
        private Poi _activePoi;

        private void Awake()
        {
            var llm = _llmBehaviour as ILlmClient;
            var tts = _ttsBehaviour as ITtsService;
            _chat = new GuideChatController(llm, tts);
            _chat.NpcStateChanged += OnNpcState;
            _chat.GuideMessageReady += text => _panel.AppendMessage("導遊", text);
            _panel.Sent += question => _ = _chat.AskAsync(question);
            _panel.Close();
        }

        private void Start()
        {
            // Phase 1 桌面:直接顯示指定 POI 的 NPC(真實 geo-proximity 留待 Phase 2)
            if (_useDebugPoi && _poiService != null && _poiService.TryGetById(_debugPoiId, out var poi))
                ShowNpc(poi);
        }

        private void Update()
        {
            if (_npcInstance == null) return;
            if (!TryGetTapRay(out var ray)) return;
            if (Physics.Raycast(ray, out var hit) && hit.collider.transform.IsChildOf(_npcInstance.transform))
                OpenChat();
        }

        private void ShowNpc(Poi poi)
        {
            _activePoi = poi;
            var anchor = (_anchorBehaviour as IPoiAnchorProvider)?.GetAnchor(poi.id);
            if (anchor == null)
            {
                Debug.LogWarning($"[Guide] POI '{poi.id}' 無 anchor,NPC 不顯示。");
                return;
            }
            _npcInstance = Instantiate(_npcPrefab, anchor.position, anchor.rotation);
            _npcAnimator = _npcInstance.GetComponentInChildren<NpcAnimator>();
        }

        private void OpenChat()
        {
            _panel.Open();
            _npcAnimator?.PlayGreet();
            _ = _chat.StartSessionAsync(_activePoi);
        }

        private void OnNpcState(NpcState state) => _npcAnimator?.OnNpcState(state);

        private bool TryGetTapRay(out Ray ray)
        {
            ray = default;
            if (_arCamera == null) return false;
            if (Input.GetMouseButtonDown(0))
            {
                ray = _arCamera.ScreenPointToRay(Input.mousePosition);
                return true;
            }
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                ray = _arCamera.ScreenPointToRay(Input.GetTouch(0).position);
                return true;
            }
            return false;
        }
    }
}
```

- [ ] **Step 2: 階段 A 不 commit**

---

## Task 6: Unity 整合 + Play 驗證 + commit(須開啟 Unity)

**前置:通知使用者開啟 Unity。**

- [ ] **Step 1: 開 Unity,等匯入/編譯**
匯入新檔、產生 .meta、編譯 `NtutAR.Guide` / `NtutAR.Guide.Tests`。讀 `read_console` 確認**無紅字**;若 TMP 首次使用要 import TMP Essentials(Window → TextMeshPro → Import TMP Essential Resources)。

- [ ] **Step 2: 跑 EditMode 測試**
Test Runner → EditMode → 只跑 `NtutAR.Guide.Tests`,預期 **6 綠**(`GuideChatControllerTests`)。連同既有 `NtutAR.Poi.Tests` 9 綠 → 全部 15 綠。

- [ ] **Step 3: 匯入 NPC FBX(4 個,Humanoid)**
從 `/Users/poterpan/Downloads/Unity_NPC/LJR/Animation/Meshy_AI_Head_Sprout_biped_FBX/` 複製 4 個到 `unity-app/Assets/Art/NPC/`:
`...Listening_Gesture...`、`...Talk_with_Left_Hand_on_Hip...`、`...Wave_One_Hand...`、`...Walking...`。
匯入設定:挑一個(Listening)當**角色本體**(Model:匯入 mesh;Rig:**Humanoid**,Avatar 從此建)。其餘 3 個:Rig = Humanoid → **Copy From Other Avatar**(用 Listening 的 Avatar);Model 分頁取消匯 mesh/材質(只要 AnimationClip);Animation 分頁設 Loop Time(Listening/Walking 勾、Wave/Talk 視需要)。

- [ ] **Step 4: 建 Animator Controller**
`Assets/Art/NPC/NpcGuide.controller`:狀態 `Listening`(預設,Listening clip)、`Talk`(Talk clip)、`Greet`(Wave clip)。參數 Trigger:`Listening`/`Talk`/`Greet`。轉換:Any State→各狀態以對應 trigger;`Greet` 用 Has Exit Time → `Listening`(揮完回待命)。

- [ ] **Step 5: 建 NpcGuide prefab**
`Assets/Art/NPC/NpcGuide.prefab`:角色模型 + Animator(指定 NpcGuide.controller)+ `NpcAnimator`(指 Animator)+ 一個涵蓋身體的 `CapsuleCollider`(供 tap raycast)。

- [ ] **Step 6: 建測試場景 + 對話 UI**
新場景 `Assets/Scenes/GuideNpcTest.unity`(含 Main Camera + Directional Light)。建 Canvas(Screen Space - Overlay):
- 面板 `_root`(Panel):內含 `TMP_InputField`(`_input`)、ScrollView 的 Content(`_messageContainer`)、送出 Button。
- 訊息 prefab `Assets/Art/NPC/ChatMessage.prefab`:一個含 `TextMeshProUGUI` 的列(`_messagePrefab`)。
- 中文:用含 CJK 的字型建 TMP Font Asset(Window → TextMeshPro → Font Asset Creator,來源如系統 PingFang/Noto Sans CJK),套到輸入框與訊息文字。
掛 `GuideChatPanel` 到面板,指好 4 個欄位;送出 Button.onClick → `GuideChatPanel.OnSendButton`。

- [ ] **Step 7: 接 GuideInteractionController + mock**
場景建空物件 `GuideSystem`,掛:`PoiService`(指定 `poi_data.json` TextAsset)、`MockLlmClient`、`MockTtsService`、`MockPoiAnchorProvider`(其 `_anchorPoint` 指一個放在鏡頭前 ~2m 的空物件)、`GuideInteractionController`。把各欄位拖好(`_llmBehaviour`=MockLlmClient、`_ttsBehaviour`=MockTtsService、`_anchorBehaviour`=MockPoiAnchorProvider、`_npcPrefab`=NpcGuide、`_panel`=面板、`_arCamera`=Main Camera、`_poiService`=PoiService)。

- [ ] **Step 8: 桌面 Play 驗證**
進 Play:鏡頭前出現 NPC(播 Listening)。點 NPC → 面板開、NPC 揮手(Greet→Listening)、Console 印 `[MockTts] 念: <p01 的 shortDescription 或空>`。在輸入框打字送出 → 出現「你: …」與「導遊: (mock)關於「校門口」…」、NPC 播 Talk 約 1–2 秒後回 Listening。連點送出不會重複(busy 擋)。

- [ ] **Step 9: commit(程式 + 測試 + mock + 資產 + 場景 + meta)**
```bash
git add unity-app/Assets/Scripts/Guide unity-app/Assets/Tests/EditMode/NtutAR.Guide.Tests.asmdef unity-app/Assets/Tests/EditMode/NtutAR.Guide.Tests.asmdef.meta unity-app/Assets/Tests/EditMode/GuideChatControllerTests.cs unity-app/Assets/Tests/EditMode/GuideChatControllerTests.cs.meta
git commit -m "feat(npc): 導遊對話 Phase 1(mock-first)— GuideChatController + seams + mocks + UI + 互動 + EditMode 測試"
git add unity-app/Assets/Art/NPC unity-app/Assets/Scenes/GuideNpcTest.unity unity-app/Assets/Scenes/GuideNpcTest.unity.meta unity-app/Assets/Art/NPC.meta unity-app/Assets/Art.meta
git commit -m "feat(npc): NpcGuide 模型/Animator/prefab + 對話 UI + 測試場景"
git push origin feature/guide-npc
```

---

## 自我檢查(對 spec)

- 顯示=hybrid:NPC 世界(mock anchor 放鏡頭前)+ overlay 面板 ✓(Phase 1 用 debug POI,真 anchor Phase 2)
- 觸發=點 NPC ✓;頭上提示 billboard → Task 6 可加(此版先省,標註)⚠️ Phase 1 暫不做提示 billboard,Play 直接可點 NPC
- 對話=文字 + TTS(mock)✓;STT 介面保留 ✓
- 開場白=shortDescription ✓
- 動畫=Listening/Talk/Wave 對應 ✓;錯誤處理(LLM fallback、TTS 降級、busy 擋、anchor null)✓ 皆有測試或碼涵蓋
- 測試=GuideChatController 6 案 ✓

## 後續(範圍外)
Phase 2 真 `ILlmClient`(抽 PR #6 邏輯)+ 真 geo-proximity(AREarthManager→GetNearest)+ 真 `IPoiAnchorProvider`(anchor 層);Phase 3 真 TTS;Phase 4 STT。提示 billboard 可併 Phase 2。

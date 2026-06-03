# 導遊 NPC 體驗 — 設計文件

> 日期:2026-06-03
> 範圍:`unity-app/` 主程式的「虛擬導遊 NPC」功能 —— NPC 顯示 + 觸發互動 + LLM 對話 + TTS + 動畫狀態機。
> 依賴(本 spec 範圍外,以 seam 抽象):Geospatial anchor 放置層、bootstrap 場景、B 的 LLM 服務、TTS provider。

## 1. 目標

讓使用者走到校園 POI 旁,看到一個站在現場的 3D 導遊 NPC,點它即可用自然語言問該地點的問題,NPC 以文字 + 語音回答並配合說話動畫。資料層 `PoiService`(已完成)提供每個 POI 的座標與 `PoiContext(Id, Name, LlmSystemPrompt)`。

## 2. 已定案決策

| 決策 | 選擇 |
|---|---|
| 顯示範式 | **混合式**:NPC 站在 POI 真實位置(world-space,經 anchor),對話走**畫面 overlay 面板**(中文長句好讀好打字) |
| 觸發 | **點擊 NPC**(raycast collider)+ **靠近時頭上浮提示**「💬 點我導覽」(用 `PoiService.GetNearest` 判定靠近) |
| 對話形式 | **文字 + TTS**(TTS provider 未定 → `ITtsService` 抽象);**保留 STT seam**(`ISpeechInput`,最後一週有時間再做) |
| 開場白 | 用該 POI 的 `shortDescription`(C 回填,TTS 念出),非 LLM 生成 |
| 模型 | Meshy biped **FBX**、Rig = **Humanoid**;clip 對應見 §5 |
| 開發策略 | **mock-first**:`IPoiAnchorProvider`/`ILlmClient`/`ITtsService` 先用 mock,NPC 功能即可在桌面 Play 跑通,之後逐一換真的 |

## 3. 元件架構(命名空間 `NtutAR.Guide`)

```
PoiService (既有) ── GetNearest(userLat,userLng) → Poi / PoiContext
        │
GuideInteractionController (MonoBehaviour,場景大腦)
  • 從 AREarthManager 取使用者 geo-pose → PoiService.GetNearest
  • 靠近(< 閾值)→ 經 IPoiAnchorProvider 顯示 NPC@anchor + 頭上提示
  • tap NPC(raycast collider)→ 開面板、觸發 GuideChatController.StartSession(poi)
        ├── NpcGuide (prefab: 模型 + Humanoid Animator + Collider + 世界提示 billboard)
        │      └── NpcAnimator (MonoBehaviour:PlayGreet/PlayListening/PlayTalk)
        ├── GuideChatPanel (MonoBehaviour, 畫面 overlay UI:訊息列/輸入框/送出)
        └── GuideChatController (純 C# 編排:持 PoiContext、Ask→LLM→顯示→TTS→驅動動畫)
                └── seams: ILlmClient · ITtsService · ISpeechInput(保留) · IPoiAnchorProvider
```

職責切分(每個單元一個清楚責任、可獨立理解/測試):

| 元件 | 做什麼 | 依賴 |
|---|---|---|
| `GuideInteractionController` | 靠近偵測、顯示/隱藏 NPC、tap 處理、開關面板 | `PoiService`、`IPoiAnchorProvider`、`AREarthManager` |
| `NpcGuide`(prefab)+ `NpcAnimator` | 世界中的 NPC;`NpcAnimator` 把動畫狀態包成方法 | — |
| `GuideChatPanel` | overlay 對話 UI,純顯示與輸入轉發 | `GuideChatController` |
| `GuideChatController` | 對話編排與狀態;**純 C#,可單測** | `ILlmClient`、`ITtsService` |
| Mock 實作 | 桌面 Play / 測試用的假 seam | — |

## 4. Seam 介面

```csharp
namespace NtutAR.Guide
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;
    using NtutAR.Poi;   // PoiContext

    public interface ILlmClient
    {
        Task<string> AskAsync(string question, PoiContext poi, CancellationToken ct = default);
    }

    public interface ITtsService
    {
        Task SpeakAsync(string text, CancellationToken ct = default);
        bool IsSpeaking { get; }
        event Action SpeakingStarted;   // 供 NPC talk 動畫對齊
        event Action SpeakingStopped;
    }

    // STT —— 本期保留,不實作
    public interface ISpeechInput
    {
        Task<string> ListenAsync(CancellationToken ct = default);
    }

    // 由 Geospatial anchor 層(sibling spec)提供;mock 版先放鏡頭前固定位
    public interface IPoiAnchorProvider
    {
        Transform GetAnchor(string poiId);   // 找不到回 null
    }
}
```

`GuideChatController`(編排核心,純 C#、可單測):

```csharp
public enum NpcState { Listening, Talking }

public sealed class GuideChatController
{
    public GuideChatController(ILlmClient llm, ITtsService tts);

    public event Action<NpcState> NpcStateChanged;   // 驅動 NpcAnimator
    public event Action<string> GuideMessageReady;   // 給面板顯示(逐則)

    // 開場:念該 POI 的 shortDescription(非 LLM)
    public Task StartSessionAsync(Poi poi, CancellationToken ct = default);
    // 問答:Ask→顯示→TTS→動畫
    public Task AskAsync(string question, CancellationToken ct = default);
}
```

## 5. NPC 模型與動畫對應

來源:`/Users/poterpan/Downloads/Unity_NPC/LJR`(Meshy,biped withSkin FBX,各 ~21MB)。

| NPC 狀態 | clip | 何時 |
|---|---|---|
| Idle / 待命 | `Listening_Gesture` | 面板開、等使用者輸入 |
| 說話 | `Talk_with_Left_Hand_on_Hip` | TTS 念回覆期間(`SpeakingStarted`→`SpeakingStopped`) |
| 打招呼 | `Wave_One_Hand` | `StartSessionAsync` 開場,播完轉 Listening |
| (未來)帶路 | `Walking` | 帶路到下個 POI(範圍外) |

**資產管理**:用 FBX、Rig=Humanoid。每個動畫是「整隻 mesh + 單一動畫」的獨立 FBX → **只進需要的 4 個**(Listening/Talk/Wave/Walking),動畫 FBX 匯入時設定不匯 mesh/材質只取 clip(避免 runtime 重複);GLB 與未用 FBX **不進 repo**(否則 ~190MB+)。NPC 做成 prefab(模型 + Animator Controller + Collider + 世界提示 billboard),放 `Assets/Art/NPC/`。

## 6. 互動資料流

1. `GuideInteractionController` 每幀(或定時)從 `AREarthManager.CameraGeospatialPose` 取使用者 lat/lng → `PoiService.GetNearest`。
2. 最近 POI 距離 < 閾值(預設 20m,可調)→ 經 `IPoiAnchorProvider.GetAnchor(poi.id)` 取世界 Transform → 顯示 `NpcGuide` 於該處 + 頭上「💬 點我導覽」billboard 提示。
3. 使用者點 NPC(對 collider raycast)→ `GuideInteractionController` 開 `GuideChatPanel`、直接呼叫 `NpcAnimator.PlayGreet()`(播 Wave,完成後自動回 Listening)→ `GuideChatController.StartSessionAsync(poi)`:`shortDescription` 經 `ITtsService` 念出並顯示為開場訊息。
4. 使用者打字送出 → `AskAsync(question)`:`ILlmClient.AskAsync(question, poi.ToContext())` → 回覆文字進面板 + `ITtsService.SpeakAsync` → `SpeakingStarted` 觸發 NpcState=Talking,`SpeakingStopped` 回 Listening。
5. 離開範圍 / 關面板 → 隱藏 NPC、結束 session。

**動畫接線**:`GuideInteractionController` 訂閱 `GuideChatController.NpcStateChanged` → 呼叫 `NpcAnimator.PlayListening()` / `PlayTalk()`;`PlayGreet()` 由互動控制器在開場時直接呼叫(Wave 不屬 `NpcState` 列舉,是一次性過場)。

## 7. 錯誤處理(任何情況不崩潰)

| 情況 | 行為 |
|---|---|
| LLM 逾時 / 例外 | `GuideChatController` 攔截 → 顯示友善訊息「抱歉,我現在連不上,稍後再試」,NPC 回 Listening,不丟例外 |
| TTS 失敗 | 降級為純文字(訊息照顯示),記 log;動畫退回 Listening |
| `IPoiAnchorProvider.GetAnchor` 回 null | 不顯示 NPC(該 POI 尚無 anchor);記 log |
| POI 的 `llmSystemPrompt` / `shortDescription` 空(C 未填) | 仍運作(LLM 拿到空上下文 / 開場白略過);dev 警告 |
| 連點送出 / session 進行中再送 | `GuideChatController` 用 busy 旗標擋重入,避免重複請求(PR #6 的 bug 不重蹈) |

## 8. 測試

EditMode 單元測試(`GuideChatController`,mock `ILlmClient`/`ITtsService`):

1. `AskAsync` 正常 → 觸發 `GuideMessageReady`(回覆文字)、呼叫 `SpeakAsync`
2. LLM 丟例外 → 收到 fallback 訊息、不丟例外、NpcState 回 Listening
3. 狀態序列:Ask 時 Listening→Talking→Listening(對齊 TTS 事件)
4. session 進行中再 `AskAsync` → 被 busy 擋下(不重複呼叫 LLM)
5. `StartSessionAsync` → 念 `shortDescription`(空字串則略過)

`NpcAnimator` / UI / 互動:Unity 內手動 + 桌面 Play 驗證(mock seam)。

## 9. 階段(YAGNI,逐步落地)

- **Phase 1(本 spec 核心,mock-first)**:seams + `GuideChatController` + `NpcAnimator` + `GuideChatPanel` + `NpcGuide` prefab + `GuideInteractionController` + Mock(`MockLlmClient` 回固定字、`MockTtsService` 計時、`MockPoiAnchorProvider` 放鏡頭前)。桌面 Play 可跑完整流程。
- **Phase 2**:真 `ILlmClient`(從 PR #6 的 UnityWebRequest 邏輯抽成乾淨 async client)。
- **Phase 3**:真 `ITtsService`(provider 屆時定;雲端或裝置)。
- **Phase 4(stretch,最後一週)**:`ISpeechInput` / STT。

## 10. 檔案配置

```
unity-app/Assets/Scripts/Guide/
  NtutAR.Guide.asmdef            # references: NtutAR.Poi
  Seams.cs                        # ILlmClient / ITtsService / ISpeechInput / IPoiAnchorProvider
  GuideChatController.cs          # 純 C# 編排
  NpcAnimator.cs                  # MonoBehaviour
  GuideInteractionController.cs   # MonoBehaviour
  GuideChatPanel.cs               # MonoBehaviour (UI)
  Mocks/ MockLlmClient.cs, MockTtsService.cs, MockPoiAnchorProvider.cs
unity-app/Assets/Tests/EditMode/
  NtutAR.Guide.Tests.asmdef       # references NtutAR.Guide
  GuideChatControllerTests.cs
unity-app/Assets/Art/NPC/         # 選定的 4 個 FBX + Animator Controller + NpcGuide.prefab
```

## 11. 範圍外 / 依賴(各自獨立 spec)

- **Geospatial anchor 放置層 + bootstrap 場景**:提供真的 `IPoiAnchorProvider`(讀 `PoiService.All` 把 POI 放成 ARCore anchor)。本功能先用 mock。
- **B 的 LLM 服務**:Phase 2 換上;介面已用 `ILlmClient` 對齊 `分工.md`。
- **TTS provider 選型**:Phase 3。
- **校園貓 RL(D)**:無關。

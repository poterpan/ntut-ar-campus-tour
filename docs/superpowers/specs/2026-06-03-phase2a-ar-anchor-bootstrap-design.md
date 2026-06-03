# Phase 2a — AR bootstrap 場景 + Geospatial anchor 層 設計文件

> 日期:2026-06-03
> 範圍:把 Phase 1 的導遊 NPC 接進**真實 AR 世界** —— bootstrap 場景 + VPS 定位 + `GeospatialAnchorManager`(依 `PoiService` 放 ARCore anchor)+ 真 `IPoiAnchorProvider` + `GuideInteractionController` 真 proximity。
> 不含(各自獨立):真 `ILlmClient`(Phase 2b,Issue #10)、真 TTS、其他 POI 的 marker/導航、貓 RL。

## 1. 目標

使用者開 App → AR 定位(VPS)→ 校園 POI 在真實位置出現導遊 NPC → 走近最近的 POI 看到 NPC → 點擊用文字對話(LLM 暫 mock)。延續 Phase 1 的「最近單一導遊」模型,把 mock anchor 換成真 Geospatial anchor。

## 2. 已定案決策

| 決策 | 選擇 |
|---|---|
| bootstrap 場景 | **複製 `GeospatialArf4` 改造**成 `Bootstrap.unity`:保留可運作 AR rig + ARCoreExtensions Geospatial 設定,拔掉 demo UI / tap-to-anchor / StreetscapeGeometry,加上我們的系統 |
| NPC 呈現 | **最近單一導遊**(延續 Phase 1):為所有主動 POI 解析 anchor,但只在最近 POI 顯示一隻 NPC |
| anchor 類型 | **逐 POI 依 `poi.anchorType`**(目前全 Terrain → `ResolveAnchorOnTerrainAsync`);manager 同時支援 Geospatial(`AddAnchor`)/ Rooftop |
| LLM | **續用 `MockLlmClient`**(2b 再換真的) |
| 主動 POI 集合 | **`docs/POI.md` 的 5 點**(新生南路側門 / 學生餐廳入口 / 演講廳入口 / 第一教學大樓 / 化工館);其餘收集點為備用 |
| 編輯器 mock 路徑 | 保留(`GuideInteractionController` 的 debug 路徑),AR 路徑為實機用 |

## 3. 元件架構(命名空間 `NtutAR.Geo`)

```
Bootstrap.unity (從 GeospatialArf4 改造)
├── AR rig:ARSession · ARSessionOrigin · AR Camera · AREarthManager · ARAnchorManager · ARCoreExtensions(GeospatialMode=Enabled)
├── ArLocalizationController (MonoBehaviour)
│     • VPS 狀態機(沿用 sample 邏輯):等 EarthState.Enabled + EarthTrackingState.Tracking + 精度門檻(水平 20m / yaw 25°),180s timeout
│     • 狀態:Initializing / Localizing / Localized / Failed;對外事件 OnLocalized
│     • 「正在定位…」overlay,Localized 後隱藏
├── GeospatialAnchorManager (MonoBehaviour, 實作 NtutAR.Guide.IPoiAnchorProvider)
│     • OnLocalized 後,讀 PoiService.All,逐 POI 經 IAnchorResolver 解析 anchor
│     • 維護 anchorRegistry: poiId → resolved Transform
│     • GetAnchor(poiId) → 已解析回 Transform,否則 null
├── IAnchorResolver (seam) — 把 ARCore 解析呼叫抽象化
│     • ArCoreAnchorResolver(實機:ResolveAnchorOnTerrainAsync / AddAnchor / Rooftop)
│     • MockAnchorResolver(編輯器/測試:回相機前固定 Transform)
└── GuideSystem(Phase 1)
      • GuideInteractionController 新增「真 proximity」路徑(mode 旗標切換 debug/AR):
        AREarthManager.CameraGeospatialPose → (lat,lng) → PoiService.GetNearest
        → 最近且 anchor 已解析(GetAnchor != null)→ 在該 anchor 顯示 NPC
      • IPoiAnchorProvider 由 GeospatialAnchorManager 提供(取代 MockPoiAnchorProvider)
```

職責:

| 元件 | 做什麼 | 依賴 |
|---|---|---|
| `ArLocalizationController` | VPS 定位狀態機 + overlay | AREarthManager, ARCoreExtensions |
| `GeospatialAnchorManager` | POI→anchor 解析、registry、`IPoiAnchorProvider` | PoiService, IAnchorResolver, ARAnchorManager |
| `IAnchorResolver` + 實作 | 抽象 ARCore anchor 解析(真/mock) | ARCore Extensions(真) |
| `GuideInteractionController`(改) | 真 proximity 取最近 POI + 顯示 NPC | AREarthManager, PoiService, IPoiAnchorProvider |

## 4. Seam 介面

```csharp
namespace NtutAR.Geo
{
    using UnityEngine;
    using NtutAR.Poi;

    public enum AnchorResolveStatus { Pending, Success, Failed }

    public sealed class AnchorResolveResult
    {
        public string PoiId;
        public AnchorResolveStatus Status;
        public Transform Anchor;   // Success 時非 null
    }

    // 抽象 ARCore 解析;真實作走 promise/coroutine,mock 立即回固定 Transform
    public interface IAnchorResolver
    {
        // 對一個 POI 發起解析;完成時回呼(可能非同步)
        void Resolve(Poi poi, System.Action<AnchorResolveResult> onDone);
    }
}
```

`NtutAR.Guide.IPoiAnchorProvider`(已存在)由 `GeospatialAnchorManager` 實作:`Transform GetAnchor(string poiId)`。

## 5. 資料流(實機)

1. `Bootstrap.unity` 載入 → AR session 啟動 → `ArLocalizationController` 顯示「正在定位…」。
2. `EarthState.Enabled` + `EarthTrackingState.Tracking` + 精度達標 → `OnLocalized`,隱藏 overlay。
3. `GeospatialAnchorManager` 讀 `PoiService.All`,對每個 POI 呼叫 `IAnchorResolver.Resolve`;成功的寫進 registry。
4. `GuideInteractionController`(AR 模式)定時讀 `CameraGeospatialPose` → `PoiService.GetNearest(lat,lng)` → 若最近 POI 距離 < 閾值且 `GetAnchor` 已回非 null → 在該 anchor 顯示 NPC(揮手/對話流程同 Phase 1)。
5. 點 NPC → 對話面板(LLM 仍 mock)。

## 6. 錯誤處理

| 情況 | 行為 |
|---|---|
| 定位逾時(180s) | `ArLocalizationController` 進 Failed,overlay 顯示「定位失敗,請到空曠處重試」+ 重試鈕 |
| 某 POI anchor 解析失敗 | 記 log,該 POI 不顯示 NPC;其餘照常 |
| 走近的 POI anchor 尚未解析 | 暫不顯示(等解析完成);不報錯 |
| 精度持續不足 | 維持 Localizing overlay(不進 anchor 解析) |

## 7. 測試

- **單元(EditMode,mock `IAnchorResolver`)**:`GeospatialAnchorManager` 的純邏輯 —— N 個 POI → N 次 Resolve(帶正確 lat/lng/type)、成功結果寫進 registry、`GetAnchor` 命中/未命中、解析失敗不崩。把 registry + 解析調度邏輯放可單測的純類別,MonoBehaviour 為薄殼。
- **編輯器 Play**:用 `MockAnchorResolver` + `GuideInteractionController` debug 路徑跑通(同 Phase 1)。
- **實機(TestFlight)**:實地走 5 個 POI → NPC 在現場出現 → 點擊對話。AR 真 anchor 僅實機可驗。

## 8. 主動 POI 集合 / 資料前置(2a 的依賴,非 AR 程式)

主動集合 = `docs/POI.md` 5 點。`poi_data.json` 需**收斂成這 5 點**,座標取自 `poi_captures.json`:

| docs/POI.md | 對應收集點(poi_captures.json) |
|---|---|
| 學生餐廳入口 | p04 學餐入口 |
| 演講廳入口 | p07 國際演講廳入口 |
| 第一教學大樓 | p06 第一教學樓 |
| 化工館 | p08 化工館 |
| 新生南路側門(起點) | p01 校門口 |

其餘收集點(國百館 / 土木館 / 大圓球)為備用,不入主動集合。內容欄位(`shortDescription` / `llmSystemPrompt`)由 C 從 docs/POI.md 壓縮回填。此資料任務與 AR 程式並行,不互卡。

## 9. 檔案配置

```
unity-app/Assets/Scripts/Geo/
  NtutAR.Geo.asmdef            # 新:references NtutAR.Poi, NtutAR.Guide, ARCore Extensions(POICollector 一併納入此組件)
  AnchorSeams.cs                # IAnchorResolver / AnchorResolveResult / AnchorResolveStatus
  GeospatialAnchorManager.cs    # MonoBehaviour + 純 AnchorRegistry 邏輯;實作 IPoiAnchorProvider
  ArCoreAnchorResolver.cs       # 真 ARCore 解析(實機)
  MockAnchorResolver.cs         # 編輯器/測試
  ArLocalizationController.cs   # VPS 狀態機 + overlay
  (既有)POICollector.cs / Editor/*
unity-app/Assets/Scripts/Guide/
  GuideInteractionController.cs # 改:加 AR proximity 路徑
unity-app/Assets/Scenes/
  Bootstrap.unity               # 從 GeospatialArf4 改造
unity-app/Assets/Tests/EditMode/Geo/
  NtutAR.Geo.Tests.asmdef
  AnchorRegistryTests.cs
```

> 注意:新增 `NtutAR.Geo.asmdef` 會把既有 `POICollector` 納入該組件(它用 ARCore Extensions,asmdef 須引用該套件);測試組件放 `Tests/EditMode/Geo/` 子資料夾(避免同層多 asmdef,見 [[unity-asmdef-and-poi-namespace]] 教訓)。引用 POI 型別記得用 `NtutAR.Poi.Poi` 全名。

## 10. 範圍外 / 後續
- **Phase 2b**:真 `ILlmClient`(抽 PR #6,Issue #10)。
- 其他 POI 的 marker / 路徑導航(Phase 3)。
- 真 TTS(Phase 3)、STT(stretch)。
- 貓 RL(D,獨立)。

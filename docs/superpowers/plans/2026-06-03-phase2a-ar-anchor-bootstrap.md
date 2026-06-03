# Phase 2a — AR bootstrap + Geospatial anchor 層 實作計畫

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 Phase 1 的導遊 NPC 接進真實 AR 世界 —— VPS 定位後,`GeospatialAnchorManager` 依 `PoiService` 在 5 個校園 POI 放 ARCore anchor,`GuideInteractionController` 用真實 geo-pose 取最近 POI 顯示 NPC。

**Architecture:** 純邏輯 `AnchorRegistry`(POI→解析調度 + registry,可單測)+ `IAnchorResolver` seam(真 ARCore / mock)+ MonoBehaviour 殼 `GeospatialAnchorManager`(實作 `NtutAR.Guide.IPoiAnchorProvider`)+ `ArLocalizationController`(VPS 狀態機)+ `GuideInteractionController` 加 AR proximity 路徑。bootstrap 場景由 `GeospatialArf4` 改造。

**Tech Stack:** Unity 6、AR Foundation、ARCore Extensions(Geospatial/Terrain anchor)、Unity Test Framework、asmdef。

---

## 前置條件
- PR #9(NPC Phase 1)已合併進 main;本計畫分支 `feature/phase2a-ar-anchor` 從更新後的 main 開出。
- **開分支須在 Unity 關閉時做**(避免洗套件雷,見 [[unity-git-checkout-hazard]])。

## 三階段(因 AR 僅實機可驗)
- **階段 A(Unity 關閉,純檔案)**:Task 1–7 撰寫程式 + 資料,不 commit。
- **階段 B(Unity 開啟,MCP)**:Task 8 —— 產 meta、跑 EditMode 測試、改造 Bootstrap 場景、編輯器 Play(mock resolver)驗證、commit。
- **階段 C(實機 TestFlight)**:Task 9 —— build → 實地走 5 個 POI 驗證。

## File Structure
```
unity-app/Assets/Scripts/Geo/
  POICollector.cs               # (既有)留 Assembly-CSharp —— 依賴 Geospatial Sample 的 GeospatialController,不可被 asmdef 捕獲
  Anchor/                        # 新組件(不含 POICollector;否則它在 asmdef 內看不到範例 → CS0246)
    NtutAR.Geo.asmdef            # references NtutAR.Poi, NtutAR.Guide, Google.XR.ARCoreExtensions, Unity.XR.ARFoundation, Unity.XR.ARSubsystems
    AnchorSeams.cs                # IAnchorResolver / AnchorResolveResult / AnchorResolveStatus
    AnchorRegistry.cs             # 純邏輯:解析調度 + registry(可單測)
    GeospatialAnchorManager.cs    # MonoBehaviour;持 AnchorRegistry;實作 IPoiAnchorProvider
    MockAnchorResolver.cs         # 編輯器/測試:回相機前固定 Transform
    ArCoreAnchorResolver.cs       # 實機:ARCore Terrain/Geospatial 解析
    ArLocalizationController.cs   # VPS 狀態機 + overlay
    ArGuideProximityDriver.cs     # 讀 geo-pose → GetNearest → 呼叫 Guide(維持 Guide 不依賴 ARCore)
  (既有)../Editor/* postprocessor 不受影響
unity-app/Assets/Scripts/Guide/
  GuideInteractionController.cs # 改:加 AR proximity 路徑
unity-app/Assets/Data/poi_data.json   # 收斂成 5 個主動 POI
unity-app/Assets/Scenes/Bootstrap.unity   # 從 GeospatialArf4 改造
unity-app/Assets/Tests/EditMode/Geo/
  NtutAR.Geo.Tests.asmdef
  AnchorRegistryTests.cs
```

---

## Task 1: poi_data.json 收斂成 5 個主動 POI

**Files:** Modify `unity-app/Assets/Data/poi_data.json`

- [ ] **Step 1: 改寫為 5 點**(docs/POI.md 名稱 + 對應收集點座標,內容欄位仍由 C 回填)
```json
{
    "pois": [
        { "id": "p01", "name": "新生南路側門",   "lat": 25.043593212290668, "lng": 121.53319001415388, "altitude": 24.136355090886356, "anchorType": "Terrain", "shortDescription": "", "llmSystemPrompt": "" },
        { "id": "p02", "name": "學生餐廳入口",   "lat": 25.043698462660577, "lng": 121.53366466922937, "altitude": 24.333744423463940, "anchorType": "Terrain", "shortDescription": "", "llmSystemPrompt": "" },
        { "id": "p03", "name": "演講廳入口",     "lat": 25.043663622032587, "lng": 121.53398796604488, "altitude": 24.509720870293678, "anchorType": "Terrain", "shortDescription": "", "llmSystemPrompt": "" },
        { "id": "p04", "name": "第一教學大樓",   "lat": 25.043563461366735, "lng": 121.53385922552877, "altitude": 24.582515378482640, "anchorType": "Terrain", "shortDescription": "", "llmSystemPrompt": "" },
        { "id": "p05", "name": "化工館",         "lat": 25.043680003476699, "lng": 121.53439591644180, "altitude": 24.612994682043790, "anchorType": "Terrain", "shortDescription": "", "llmSystemPrompt": "" }
    ]
}
```
備用點(國百館/土木館/大圓球)從主動集合移除(保留在 `poi_captures.json` 原始檔)。

- [ ] **Step 2: 階段 A 不 commit**

---

## Task 2: NtutAR.Geo 組件 + anchor seams

**Files:** Create `unity-app/Assets/Scripts/Geo/NtutAR.Geo.asmdef`, `AnchorSeams.cs`

- [ ] **Step 1: asmdef**(納入既有 POICollector;引用 ARCore Extensions)

`NtutAR.Geo.asmdef`:
```json
{
    "name": "NtutAR.Geo",
    "rootNamespace": "NtutAR.Geo",
    "references": [
        "NtutAR.Poi",
        "NtutAR.Guide",
        "Google.XR.ARCoreExtensions",
        "Unity.XR.ARFoundation",
        "Unity.XR.ARSubsystems"
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
> 確認組件名:在 Unity 內查 ARCore Extensions / ARFoundation 的 asmdef 名(`Google.XR.ARCoreExtensions`、`Unity.XR.ARFoundation`、`Unity.XR.ARSubsystems`)。若名稱不符,Task 8 Step 1 編譯時依錯誤修正 references。

- [ ] **Step 2: `AnchorSeams.cs`**
```csharp
using System;
using UnityEngine;

namespace NtutAR.Geo
{
    public enum AnchorResolveStatus { Pending, Success, Failed }

    public sealed class AnchorResolveResult
    {
        public string PoiId;
        public AnchorResolveStatus Status;
        public Transform Anchor;   // Success 時非 null
    }

    public interface IAnchorResolver
    {
        // 對一個 POI 發起解析;完成時回呼(可能非同步)
        void Resolve(NtutAR.Poi.Poi poi, Action<AnchorResolveResult> onDone);
    }
}
```

- [ ] **Step 3: 階段 A 不 commit**

---

## Task 3: AnchorRegistry(測試先行)

**Files:** Create `unity-app/Assets/Tests/EditMode/Geo/NtutAR.Geo.Tests.asmdef`, `AnchorRegistryTests.cs`, `unity-app/Assets/Scripts/Geo/AnchorRegistry.cs`

- [ ] **Step 1: 測試 asmdef**

`unity-app/Assets/Tests/EditMode/Geo/NtutAR.Geo.Tests.asmdef`:
```json
{
    "name": "NtutAR.Geo.Tests",
    "rootNamespace": "NtutAR.Geo.Tests",
    "references": [ "NtutAR.Geo", "NtutAR.Poi", "UnityEngine.TestRunner", "UnityEditor.TestRunner" ],
    "includePlatforms": [ "Editor" ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [ "nunit.framework.dll" ],
    "autoReferenced": false,
    "defineConstraints": [ "UNITY_INCLUDE_TESTS" ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: 失敗測試 `AnchorRegistryTests.cs`**(fake resolver 記錄請求、可手動回呼)
```csharp
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NtutAR.Geo.Tests
{
    public class AnchorRegistryTests
    {
        private sealed class FakeResolver : IAnchorResolver
        {
            public readonly List<string> Requested = new List<string>();
            private Action<AnchorResolveResult> _cb;
            public void Resolve(NtutAR.Poi.Poi poi, Action<AnchorResolveResult> onDone)
            {
                Requested.Add(poi.id);
                _cb = onDone;
            }
            public void Complete(string poiId, bool success, Transform anchor)
            {
                _cb(new AnchorResolveResult { PoiId = poiId, Status = success ? AnchorResolveStatus.Success : AnchorResolveStatus.Failed, Anchor = anchor });
            }
        }

        private static List<NtutAR.Poi.Poi> Pois(params string[] ids)
        {
            var list = new List<NtutAR.Poi.Poi>();
            foreach (var id in ids) list.Add(new NtutAR.Poi.Poi { id = id, name = id, anchorType = "Terrain" });
            return list;
        }

        [Test]
        public void ResolveAll_DispatchesOncePerPoi()
        {
            var r = new FakeResolver();
            var reg = new AnchorRegistry(r);
            reg.ResolveAll(Pois("p01", "p02"));
            Assert.AreEqual(new[] { "p01", "p02" }, r.Requested.ToArray());
        }

        [Test]
        public void Success_RegistersAnchor()
        {
            var r = new FakeResolver();
            var reg = new AnchorRegistry(r);
            var go = new GameObject("a");
            try
            {
                reg.ResolveAll(Pois("p01"));
                r.Complete("p01", true, go.transform);
                Assert.AreSame(go.transform, reg.GetAnchor("p01"));
                Assert.AreEqual(1, reg.ResolvedCount);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void Failure_NoAnchor()
        {
            var r = new FakeResolver();
            var reg = new AnchorRegistry(r);
            reg.ResolveAll(Pois("p01"));
            r.Complete("p01", false, null);
            Assert.IsNull(reg.GetAnchor("p01"));
            Assert.AreEqual(0, reg.ResolvedCount);
        }

        [Test]
        public void ResolveAll_SkipsInFlightAndResolved()
        {
            var r = new FakeResolver();
            var reg = new AnchorRegistry(r);
            reg.ResolveAll(Pois("p01"));        // p01 in-flight
            reg.ResolveAll(Pois("p01"));        // 不重發
            Assert.AreEqual(1, r.Requested.Count);
        }

        [Test]
        public void GetAnchor_Unknown_ReturnsNull()
        {
            var reg = new AnchorRegistry(new FakeResolver());
            Assert.IsNull(reg.GetAnchor("nope"));
        }
    }
}
```

- [ ] **Step 3: 實作 `AnchorRegistry.cs`**
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NtutAR.Geo
{
    public sealed class AnchorRegistry
    {
        private readonly IAnchorResolver _resolver;
        private readonly Dictionary<string, Transform> _resolved = new Dictionary<string, Transform>();
        private readonly HashSet<string> _inFlight = new HashSet<string>();

        public AnchorRegistry(IAnchorResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public int ResolvedCount { get { return _resolved.Count; } }

        public void ResolveAll(IReadOnlyList<NtutAR.Poi.Poi> pois)
        {
            foreach (var poi in pois)
            {
                if (string.IsNullOrEmpty(poi.id)) continue;
                if (_resolved.ContainsKey(poi.id) || _inFlight.Contains(poi.id)) continue;
                _inFlight.Add(poi.id);
                _resolver.Resolve(poi, OnResolved);
            }
        }

        private void OnResolved(AnchorResolveResult result)
        {
            if (result == null || string.IsNullOrEmpty(result.PoiId)) return;
            _inFlight.Remove(result.PoiId);
            if (result.Status == AnchorResolveStatus.Success && result.Anchor != null)
                _resolved[result.PoiId] = result.Anchor;
        }

        public Transform GetAnchor(string poiId)
        {
            Transform t;
            return _resolved.TryGetValue(poiId, out t) ? t : null;
        }
    }
}
```

- [ ] **Step 4: 階段 A 不 commit**(red/green 在 Task 8)

---

## Task 4: Mock + 真 ARCore resolver

**Files:** Create `MockAnchorResolver.cs`, `ArCoreAnchorResolver.cs`

- [ ] **Step 1: `MockAnchorResolver.cs`**(編輯器/Play:把 anchor 放相機前、依 POI index 橫向錯開)
```csharp
using System;
using UnityEngine;

namespace NtutAR.Geo
{
    public sealed class MockAnchorResolver : MonoBehaviour, IAnchorResolver
    {
        [SerializeField] private Camera _camera;
        private int _index;

        public void Resolve(NtutAR.Poi.Poi poi, Action<AnchorResolveResult> onDone)
        {
            var cam = _camera != null ? _camera : Camera.main;
            var go = new GameObject("MockAnchor_" + poi.id);
            var basePos = cam != null ? cam.transform.position + cam.transform.forward * 3f : Vector3.forward * 3f;
            go.transform.position = basePos + new Vector3(_index * 2f, 0f, 0f);
            go.transform.rotation = Quaternion.identity;
            _index++;
            onDone(new AnchorResolveResult { PoiId = poi.id, Status = AnchorResolveStatus.Success, Anchor = go.transform });
        }
    }
}
```

- [ ] **Step 2: `ArCoreAnchorResolver.cs`**(實機:依 anchorType 走 ARCore;Terrain 用 promise coroutine)
```csharp
using System;
using System.Collections;
using UnityEngine;
using Google.XR.ARCoreExtensions;
using UnityEngine.XR.ARFoundation;

namespace NtutAR.Geo
{
    public sealed class ArCoreAnchorResolver : MonoBehaviour, IAnchorResolver
    {
        [SerializeField] private ARAnchorManager _anchorManager;

        public void Resolve(NtutAR.Poi.Poi poi, Action<AnchorResolveResult> onDone)
        {
            StartCoroutine(ResolveRoutine(poi, onDone));
        }

        private IEnumerator ResolveRoutine(NtutAR.Poi.Poi poi, Action<AnchorResolveResult> onDone)
        {
            var rot = Quaternion.identity;
            if (poi.AnchorType == NtutAR.Poi.PoiAnchorType.Geospatial)
            {
                var anchor = _anchorManager.AddAnchor(poi.lat, poi.lng, poi.altitude, rot);
                onDone(Result(poi.id, anchor != null, anchor != null ? anchor.transform : null));
                yield break;
            }
            // Terrain(預設):altitude 由地形解析
            var promise = _anchorManager.ResolveAnchorOnTerrainAsync(poi.lat, poi.lng, 0, rot);
            yield return promise;
            var res = promise.Result;
            bool ok = res.TerrainAnchorState == TerrainAnchorState.Success && res.Anchor != null;
            onDone(Result(poi.id, ok, ok ? res.Anchor.transform : null));
        }

        private static AnchorResolveResult Result(string id, bool ok, Transform t)
        {
            return new AnchorResolveResult { PoiId = id, Status = ok ? AnchorResolveStatus.Success : AnchorResolveStatus.Failed, Anchor = t };
        }
    }
}
```
> Terrain/Rooftop promise 型別與 `TerrainAnchorState` 來自 Explore 報告;Task 8 Step 1 若 API 名稱有出入,依編譯錯誤對照 ARCore Extensions 修正。

- [ ] **Step 3: 階段 A 不 commit**

---

## Task 5: GeospatialAnchorManager(MonoBehaviour,實作 IPoiAnchorProvider)

**Files:** Create `GeospatialAnchorManager.cs`

- [ ] **Step 1: 實作**
```csharp
using UnityEngine;
using NtutAR.Poi;

namespace NtutAR.Geo
{
    public sealed class GeospatialAnchorManager : MonoBehaviour, NtutAR.Guide.IPoiAnchorProvider
    {
        [SerializeField] private PoiService _poiService;
        [SerializeField] private MonoBehaviour _resolverBehaviour;   // IAnchorResolver(Mock 或 ArCore)

        private AnchorRegistry _registry;

        private void Awake()
        {
            var resolver = _resolverBehaviour as IAnchorResolver;
            _registry = new AnchorRegistry(resolver);
        }

        // 由 ArLocalizationController 在定位完成後呼叫
        public void ResolveAllPois()
        {
            if (_poiService != null) _registry.ResolveAll(_poiService.All);
        }

        public Transform GetAnchor(string poiId)
        {
            return _registry != null ? _registry.GetAnchor(poiId) : null;
        }
    }
}
```

- [ ] **Step 2: 階段 A 不 commit**

---

## Task 6: ArLocalizationController(VPS 狀態機 + overlay)

**Files:** Create `ArLocalizationController.cs`

- [ ] **Step 1: 實作**(沿用 sample 門檻;定位達標後呼叫 GeospatialAnchorManager.ResolveAllPois + 隱藏 overlay)
```csharp
using UnityEngine;
using Google.XR.ARCoreExtensions;

namespace NtutAR.Geo
{
    public sealed class ArLocalizationController : MonoBehaviour
    {
        [SerializeField] private AREarthManager _earthManager;
        [SerializeField] private GeospatialAnchorManager _anchorManager;
        [SerializeField] private GameObject _localizingOverlay;   // 「正在定位…」UI
        [SerializeField] private double _horizontalAccuracyThreshold = 20.0;
        [SerializeField] private double _yawAccuracyThreshold = 25.0;

        private bool _localized;

        private void Update()
        {
            if (_localized || _earthManager == null) return;
            if (_earthManager.EarthState != EarthState.Enabled) return;
            if (_earthManager.EarthTrackingState != UnityEngine.XR.ARSubsystems.TrackingState.Tracking) return;

            var pose = _earthManager.CameraGeospatialPose;
            if (pose.HorizontalAccuracy > _horizontalAccuracyThreshold) return;
            if (pose.OrientationYawAccuracy > _yawAccuracyThreshold) return;

            _localized = true;
            if (_localizingOverlay != null) _localizingOverlay.SetActive(false);
            if (_anchorManager != null) _anchorManager.ResolveAllPois();
            Debug.Log("[ArLocalization] Localized; resolving anchors.");
        }
    }
}
```

- [ ] **Step 2: 階段 A 不 commit**

---

## Task 7: GuideInteractionController 加 AR proximity 路徑

**Files:** Modify `unity-app/Assets/Scripts/Guide/GuideInteractionController.cs`

- [ ] **Step 1: 加公開方法 `ShowPoiByProximity`(供 AR driver 呼叫)**

相依方向:`NtutAR.Guide` **不引用** ARCore / `NtutAR.Geo`。AR 的「讀 geo-pose → GetNearest → 顯示 NPC」觸發,由 `NtutAR.Geo.ArGuideProximityDriver`(Step 2)驅動,只呼叫 Guide 的公開方法。在 `GuideInteractionController` 加:
```csharp
        // 由 AR proximity driver 呼叫:顯示指定 POI 的 NPC(_activePoi 與 ShowNpc 皆 Phase 1 已存在)
        public void ShowPoiByProximity(NtutAR.Poi.Poi poi)
        {
            if (_npcInstance != null && _activePoi.id == poi.id) return;  // 已在顯示同一 POI
            ShowNpc(poi);
        }
```
AR 場景中把 `_useDebugPoi` 設 `false`(Start 不自動 spawn,改由 driver 驅動)—— 純 Inspector 設定,不必改 `Start`。

- [ ] **Step 2: 在 `NtutAR.Geo` 新增 `ArGuideProximityDriver.cs`**
```csharp
using UnityEngine;
using Google.XR.ARCoreExtensions;
using NtutAR.Poi;

namespace NtutAR.Geo
{
    public sealed class ArGuideProximityDriver : MonoBehaviour
    {
        [SerializeField] private AREarthManager _earthManager;
        [SerializeField] private PoiService _poiService;
        [SerializeField] private GeospatialAnchorManager _anchorManager;
        [SerializeField] private NtutAR.Guide.GuideInteractionController _guide;
        [SerializeField] private float _proximityMeters = 30f;
        [SerializeField] private float _checkInterval = 1f;

        private float _next;

        private void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + _checkInterval;
            if (_earthManager == null || _earthManager.EarthTrackingState != UnityEngine.XR.ARSubsystems.TrackingState.Tracking) return;

            var pose = _earthManager.CameraGeospatialPose;
            var nearest = _poiService.GetNearest(pose.Latitude, pose.Longitude);
            if (!nearest.HasValue) return;
            var poi = nearest.Value;
            if (_anchorManager.GetAnchor(poi.id) == null) return;   // anchor 尚未解析
            _guide.ShowPoiByProximity(poi);
        }
    }
}
```
> `GuideInteractionController.ShowNpc` 用 `IPoiAnchorProvider.GetAnchor`(已注入 `GeospatialAnchorManager`)取得真 anchor 位置;`_useDebugPoi` 在 AR 場景設 false。NPC 面向使用者:在 `ShowNpc` 末加 `_npcInstance.transform.LookAt(...)`(Y 軸對相機)—— 見 Step 3。

- [ ] **Step 3: `GuideInteractionController.ShowNpc` 末尾讓 NPC 面向相機**

在 `ShowNpc` 內 `Instantiate` 之後加:
```csharp
            if (_arCamera != null)
            {
                var look = _arCamera.transform.position - _npcInstance.transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.001f) _npcInstance.transform.rotation = Quaternion.LookRotation(look);
            }
```

- [ ] **Step 4: 階段 A 不 commit**

---

## Task 8: Unity 整合 + 編輯器驗證 + commit(須開啟 Unity)

**前置:通知使用者開啟 Unity。**

- [ ] **Step 1: 確認編譯**：讀 `read_console`;若 `NtutAR.Geo.asmdef` 的 ARCore/ARFoundation 組件名不符 → 依錯誤修 references 後重編。
- [ ] **Step 2: 跑 EditMode 測試**：`NtutAR.Geo.Tests` 5 綠(+ 既有 Poi 6 / Guide 6 = 共 17 綠)。
- [ ] **Step 3: 改造 Bootstrap 場景**:複製 `Assets/Samples/.../Geospatial Sample/Scenes/GeospatialArf4.unity` → `Assets/Scenes/Bootstrap.unity`;停用/刪除 sample 的 demo UI、tap-to-anchor、StreetscapeGeometry 顯示(保留 ARSession/ARSessionOrigin/AREarthManager/ARAnchorManager/ARCoreExtensions)。
- [ ] **Step 4: 接系統**:場景加 `GuideSystem`(PoiService〔指 poi_data.json〕、MockTtsService、MockLlmClient、GeospatialAnchorManager〔resolver 先指 MockAnchorResolver 供編輯器測;實機改 ArCoreAnchorResolver〕、ArLocalizationController〔指 AREarthManager + overlay UI〕、ArGuideProximityDriver、GuideInteractionController〔`_useDebugPoi=false`、`IPoiAnchorProvider`=GeospatialAnchorManager〕)、對話 UI Canvas(可從 GuideNpcTest 複製)。
- [ ] **Step 5: 編輯器 Play(mock)**:用 MockAnchorResolver,Play → console 應印 PoiService Loaded 5、anchors 解析、走近(mock 直接顯示)→ 點 NPC → 對話流程同 Phase 1。
- [ ] **Step 6: commit**
```bash
git add unity-app/Assets/Scripts/Geo unity-app/Assets/Scripts/Geo.meta \
        unity-app/Assets/Tests/EditMode/Geo unity-app/Assets/Tests/EditMode/Geo.meta \
        unity-app/Assets/Scripts/Guide/GuideInteractionController.cs \
        unity-app/Assets/Data/poi_data.json unity-app/Assets/Scenes/Bootstrap.unity unity-app/Assets/Scenes/Bootstrap.unity.meta
git commit -m "feat(ar): Phase 2a — Geospatial anchor 層 + bootstrap 場景 + NPC 真 proximity(mock resolver 可編輯器驗)"
git push origin feature/phase2a-ar-anchor
```

---

## Task 9: 實機驗證(TestFlight)

- [ ] **Step 1**:`GeospatialAnchorManager` 的 resolver 切到 `ArCoreAnchorResolver`(指 ARAnchorManager);ArLocalizationController 指真 AREarthManager。
- [ ] **Step 2**:推分支觸發 CI(或本機 build)→ TestFlight。
- [ ] **Step 3**:實地走 5 個 POI → 等定位 overlay 消失 → 走近 POI → NPC 在現場出現、面向你 → 點擊對話(mock LLM)。記錄哪些 POI 的 Terrain anchor 解析成功/失敗(校園 VPS 覆蓋)。

---

## 自我檢查(對 spec)
- bootstrap 場景(改造 sample)→ Task 3/8 ✓;VPS 定位機 → Task 6 ✓;GeospatialAnchorManager + IAnchorResolver + IPoiAnchorProvider → Task 2/3/4/5 ✓;真 proximity → Task 7 ✓;錯誤處理(定位逾時/anchor 失敗/未解析)→ Task 6 + AnchorRegistry + Task 7 driver 檢查 ✓;主動 5 點 → Task 1 ✓;單測 → Task 3 ✓;實機 → Task 9 ✓。
- 組件相依方向:`NtutAR.Guide` 不引用 ARCore;AR 觸發由 `NtutAR.Geo.ArGuideProximityDriver` 驅動、只呼叫 Guide 的公開 `ShowPoiByProximity` ✓。
- 型別:POI 型別一律 `NtutAR.Poi.Poi`(避命名空間衝突)✓。

## 範圍外
Phase 2b 真 ILlmClient(Issue #10);marker/導航;真 TTS;素材瘦身。

# POI 資料層載入器 + 統一 Schema 實作計畫

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `unity-app/` 建立單一可信來源的 POI 資料層 —— 合併 schema 的 `poi_data.json` + 可查詢的 `PoiService`。

**Architecture:** 三層切分以利測試 —— 純解析器 `PoiDataParser`(JSON→`Poi`)、純查詢倉儲 `PoiRepository`(`TryGetById`/`GetNearest`)、Unity 黏合層 `PoiService`(MonoBehaviour,持 `TextAsset`、Awake 解析、委派查詢)。解析器與倉儲皆為純 C#,以 EditMode 單元測試覆蓋;`PoiService` 為薄黏合,於 Unity 內手動驗證。

**Tech Stack:** Unity 6 (C#)、`JsonUtility`、Unity Test Framework (NUnit, EditMode)、asmdef。

---

## 對 spec 的兩處實作期細化

1. **新增 `PoiRepository`(純 C# 類別)** 承載 `TryGetById`/`GetNearest`,使查詢邏輯可在 EditMode 直接以手造 `List<Poi>` 單測,不需 GameObject/`TextAsset`。`PoiService` 對外 API 不變(仍 `All`/`TryGetById`/`GetNearest`),只是內部委派給 repository。
2. **`PoiContext` 用 plain `readonly struct`(非 `record struct`)**:`record struct` 屬 C# 10,Unity 6 預設 C# 9 可能無法編譯;plain struct 在任何版本皆可,且 `(Id, Name, LlmSystemPrompt)` 契約形狀不變、對 B 的 `AskAsync(question, PoiContext)` 完全相容。

## 執行兩階段(因應 Unity 目前關閉)

- **階段 1(Unity 關閉)**:Task 1–5 純檔案撰寫,不 commit(Unity 尚未產生 `.meta`,依 repo 規範 `.meta` 必須與程式一起 commit)。
- **階段 2(Unity 開啟)**:Task 6 —— 通知使用者開 Unity,產生 `.meta`、跑 EditMode 測試(真正的 red/green 驗證點)、把 `PoiService` 接進場景、分階段 commit(程式+meta、資料+場景)。

> TDD 取捨說明:Unity 測試需 Test Runner(須開 Unity),而使用者保持關閉,故「先寫測試 → 看失敗 → 實作 → 看通過」的 red/green 驗證集中在 Task 6 一次跑完。Task 2/3 仍維持「測試先於實作」的撰寫順序以保留 TDD 紀律。

## File Structure

```
unity-app/Assets/
├── Scripts/Poi/
│   ├── NtutAR.Poi.asmdef     # 新 assembly(autoReferenced,Assembly-CSharp 可直接用)
│   ├── Poi.cs                # Poi struct + PoiAnchorType enum + PoiContext struct
│   ├── PoiDataParser.cs       # 純解析:JSON → PoiParseResult(Pois + Warnings + Error)
│   ├── PoiRepository.cs       # 純查詢:All / TryGetById / GetNearest(haversine)
│   └── PoiService.cs          # MonoBehaviour:TextAsset → parse → repository → 委派查詢
├── Data/
│   └── poi_data.json          # 8 筆 POI,座標取自 poi_captures.json,內容欄位留空
└── Tests/EditMode/
    ├── NtutAR.Poi.Tests.asmdef
    ├── PoiDataParserTests.cs
    └── PoiRepositoryTests.cs
```

---

## Task 1: 建立 Poi assembly 與資料模型

**Files:**
- Create: `unity-app/Assets/Scripts/Poi/NtutAR.Poi.asmdef`
- Create: `unity-app/Assets/Scripts/Poi/Poi.cs`

- [ ] **Step 1: 建立 asmdef**

`unity-app/Assets/Scripts/Poi/NtutAR.Poi.asmdef`:
```json
{
    "name": "NtutAR.Poi",
    "rootNamespace": "NtutAR.Poi",
    "references": [],
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

- [ ] **Step 2: 建立資料模型 `Poi.cs`**

`unity-app/Assets/Scripts/Poi/Poi.cs`:
```csharp
using System;

namespace NtutAR.Poi
{
    [Serializable]
    public struct Poi
    {
        public string id;
        public string name;
        public double lat;
        public double lng;
        public double altitude;
        public string anchorType;          // 字串存檔,經 AnchorType 解析
        public string shortDescription;
        public string llmSystemPrompt;

        public PoiAnchorType AnchorType =>
            Enum.TryParse(anchorType, true, out PoiAnchorType t) ? t : PoiAnchorType.Terrain;

        public PoiContext ToContext() => new PoiContext(id, name, llmSystemPrompt);
    }

    public enum PoiAnchorType { Terrain, Rooftop, Geospatial }

    public readonly struct PoiContext
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string LlmSystemPrompt;

        public PoiContext(string id, string name, string llmSystemPrompt)
        {
            Id = id;
            Name = name;
            LlmSystemPrompt = llmSystemPrompt;
        }
    }
}
```

- [ ] **Step 3: 階段 1 不 commit**（等 Task 6 Unity 產生 `.meta` 後一起 commit）

---

## Task 2: PoiDataParser(測試先行)

**Files:**
- Create: `unity-app/Assets/Tests/EditMode/NtutAR.Poi.Tests.asmdef`
- Create: `unity-app/Assets/Tests/EditMode/PoiDataParserTests.cs`
- Create: `unity-app/Assets/Scripts/Poi/PoiDataParser.cs`

- [ ] **Step 1: 建立測試 assembly**

`unity-app/Assets/Tests/EditMode/NtutAR.Poi.Tests.asmdef`:
```json
{
    "name": "NtutAR.Poi.Tests",
    "rootNamespace": "NtutAR.Poi.Tests",
    "references": [
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

- [ ] **Step 2: 寫失敗測試 `PoiDataParserTests.cs`**

`unity-app/Assets/Tests/EditMode/PoiDataParserTests.cs`(測試資料用 ASCII 避免編碼問題):
```csharp
using NUnit.Framework;

namespace NtutAR.Poi.Tests
{
    public class PoiDataParserTests
    {
        [Test]
        public void Parse_ValidJson_ReturnsPois()
        {
            string json = "{\"pois\":[{\"id\":\"p01\",\"name\":\"Gate\",\"lat\":25.04,\"lng\":121.53,\"altitude\":24.0,\"anchorType\":\"Terrain\",\"shortDescription\":\"hi\",\"llmSystemPrompt\":\"ctx\"}]}";
            var result = PoiDataParser.Parse(json);
            Assert.IsFalse(result.HasError);
            Assert.AreEqual(1, result.Pois.Count);
            Assert.AreEqual("p01", result.Pois[0].id);
            Assert.AreEqual(25.04, result.Pois[0].lat, 1e-9);
        }

        [Test]
        public void Parse_EmptyString_ReturnsError()
        {
            var result = PoiDataParser.Parse("");
            Assert.IsTrue(result.HasError);
            Assert.AreEqual(0, result.Pois.Count);
        }

        [Test]
        public void Parse_MalformedJson_ReturnsErrorNoThrow()
        {
            var result = PoiDataParser.Parse("{ not json ]");
            Assert.IsTrue(result.HasError);
            Assert.AreEqual(0, result.Pois.Count);
        }

        [Test]
        public void Parse_DuplicateId_KeepsFirstAndWarns()
        {
            string json = "{\"pois\":[" +
                "{\"id\":\"p01\",\"name\":\"A\",\"lat\":1,\"lng\":1,\"shortDescription\":\"x\",\"llmSystemPrompt\":\"y\"}," +
                "{\"id\":\"p01\",\"name\":\"B\",\"lat\":2,\"lng\":2,\"shortDescription\":\"x\",\"llmSystemPrompt\":\"y\"}]}";
            var result = PoiDataParser.Parse(json);
            Assert.AreEqual(1, result.Pois.Count);
            Assert.AreEqual("A", result.Pois[0].name);
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("Duplicate")));
        }

        [Test]
        public void Parse_MissingContent_Warns()
        {
            string json = "{\"pois\":[{\"id\":\"p01\",\"name\":\"A\",\"lat\":1,\"lng\":1,\"shortDescription\":\"\",\"llmSystemPrompt\":\"\"}]}";
            var result = PoiDataParser.Parse(json);
            Assert.AreEqual(1, result.Pois.Count);
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("shortDescription")));
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("llmSystemPrompt")));
        }
    }
}
```

- [ ] **Step 3: 實作 `PoiDataParser.cs`**

`unity-app/Assets/Scripts/Poi/PoiDataParser.cs`:
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NtutAR.Poi
{
    public sealed class PoiParseResult
    {
        public readonly List<Poi> Pois = new List<Poi>();
        public readonly List<string> Warnings = new List<string>();
        public string Error;
        public bool HasError => !string.IsNullOrEmpty(Error);
    }

    public static class PoiDataParser
    {
        [Serializable]
        private class PoiList
        {
            public List<Poi> pois = new List<Poi>();
        }

        public static PoiParseResult Parse(string json)
        {
            var result = new PoiParseResult();
            if (string.IsNullOrWhiteSpace(json))
            {
                result.Error = "POI JSON is empty.";
                return result;
            }

            PoiList list;
            try
            {
                list = JsonUtility.FromJson<PoiList>(json);
            }
            catch (Exception ex)
            {
                result.Error = $"POI JSON parse failed: {ex.Message}";
                return result;
            }

            if (list?.pois == null)
            {
                result.Error = "POI JSON has no 'pois' array.";
                return result;
            }

            var seen = new HashSet<string>();
            foreach (var poi in list.pois)
            {
                if (string.IsNullOrEmpty(poi.id))
                {
                    result.Warnings.Add("POI with empty id skipped.");
                    continue;
                }
                if (!seen.Add(poi.id))
                {
                    result.Warnings.Add($"Duplicate id '{poi.id}' ignored (first wins).");
                    continue;
                }
                if (poi.lat == 0 && poi.lng == 0)
                    result.Warnings.Add($"POI '{poi.id}' missing coordinates (lat/lng both 0).");
                if (string.IsNullOrEmpty(poi.shortDescription))
                    result.Warnings.Add($"POI '{poi.id}' missing shortDescription.");
                if (string.IsNullOrEmpty(poi.llmSystemPrompt))
                    result.Warnings.Add($"POI '{poi.id}' missing llmSystemPrompt.");

                result.Pois.Add(poi);
            }

            return result;
        }
    }
}
```

> 注意:`JsonUtility` 依賴 `UnityEngine`,故此測試須在 Unity Test Runner (EditMode) 跑(Task 6)。

- [ ] **Step 4: 階段 1 不 commit**（驗證與 commit 在 Task 6）

---

## Task 3: PoiRepository(測試先行)

**Files:**
- Create: `unity-app/Assets/Tests/EditMode/PoiRepositoryTests.cs`
- Create: `unity-app/Assets/Scripts/Poi/PoiRepository.cs`

- [ ] **Step 1: 寫失敗測試 `PoiRepositoryTests.cs`**

`unity-app/Assets/Tests/EditMode/PoiRepositoryTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;

namespace NtutAR.Poi.Tests
{
    public class PoiRepositoryTests
    {
        private static List<Poi> Sample() => new List<Poi>
        {
            new Poi { id = "p01", name = "Gate", lat = 25.0436, lng = 121.5332 },
            new Poi { id = "p08", name = "ChemBldg", lat = 25.0437, lng = 121.5344 },
        };

        [Test]
        public void TryGetById_Hit_ReturnsTrue()
        {
            var repo = new PoiRepository(Sample());
            Assert.IsTrue(repo.TryGetById("p08", out var poi));
            Assert.AreEqual("ChemBldg", poi.name);
        }

        [Test]
        public void TryGetById_Miss_ReturnsFalse()
        {
            var repo = new PoiRepository(Sample());
            Assert.IsFalse(repo.TryGetById("p99", out _));
        }

        [Test]
        public void GetNearest_ReturnsClosest()
        {
            var repo = new PoiRepository(Sample());
            var near = repo.GetNearest(25.0436, 121.5332); // 幾乎在 p01
            Assert.IsTrue(near.HasValue);
            Assert.AreEqual("p01", near.Value.id);
        }

        [Test]
        public void GetNearest_EmptyList_ReturnsNull()
        {
            var repo = new PoiRepository(new List<Poi>());
            Assert.IsFalse(repo.GetNearest(0, 0).HasValue);
        }
    }
}
```

- [ ] **Step 2: 實作 `PoiRepository.cs`**

`unity-app/Assets/Scripts/Poi/PoiRepository.cs`:
```csharp
using System.Collections.Generic;

namespace NtutAR.Poi
{
    public sealed class PoiRepository
    {
        private readonly List<Poi> _pois;

        public IReadOnlyList<Poi> All => _pois;

        public PoiRepository(IEnumerable<Poi> pois)
        {
            _pois = pois != null ? new List<Poi>(pois) : new List<Poi>();
        }

        public bool TryGetById(string id, out Poi poi)
        {
            foreach (var p in _pois)
            {
                if (p.id == id)
                {
                    poi = p;
                    return true;
                }
            }
            poi = default;
            return false;
        }

        public Poi? GetNearest(double lat, double lng)
        {
            if (_pois.Count == 0) return null;
            Poi best = _pois[0];
            double bestDist = Haversine(lat, lng, best.lat, best.lng);
            for (int i = 1; i < _pois.Count; i++)
            {
                double d = Haversine(lat, lng, _pois[i].lat, _pois[i].lng);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = _pois[i];
                }
            }
            return best;
        }

        private static double Haversine(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371000.0; // 公尺
            double dLat = Deg2Rad(lat2 - lat1);
            double dLng = Deg2Rad(lng2 - lng1);
            double a = System.Math.Sin(dLat / 2) * System.Math.Sin(dLat / 2) +
                       System.Math.Cos(Deg2Rad(lat1)) * System.Math.Cos(Deg2Rad(lat2)) *
                       System.Math.Sin(dLng / 2) * System.Math.Sin(dLng / 2);
            double c = 2 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1 - a));
            return R * c;
        }

        private static double Deg2Rad(double deg) => deg * System.Math.PI / 180.0;
    }
}
```

- [ ] **Step 3: 階段 1 不 commit**

---

## Task 4: PoiService（MonoBehaviour 黏合層）

**Files:**
- Create: `unity-app/Assets/Scripts/Poi/PoiService.cs`

- [ ] **Step 1: 實作 `PoiService.cs`**

`unity-app/Assets/Scripts/Poi/PoiService.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace NtutAR.Poi
{
    public sealed class PoiService : MonoBehaviour
    {
        [SerializeField] private TextAsset _poiDataJson;

        private PoiRepository _repo = new PoiRepository(new List<Poi>());

        public IReadOnlyList<Poi> All => _repo.All;

        private void Awake() => Load();

        public void Load()
        {
            if (_poiDataJson == null)
            {
                Debug.LogError("[PoiService] poi_data.json TextAsset not assigned.");
                _repo = new PoiRepository(new List<Poi>());
                return;
            }

            var result = PoiDataParser.Parse(_poiDataJson.text);
            if (result.HasError)
            {
                Debug.LogError($"[PoiService] {result.Error}");
                _repo = new PoiRepository(new List<Poi>());
                return;
            }

            foreach (var w in result.Warnings)
                Debug.LogWarning($"[PoiService] {w}");

            _repo = new PoiRepository(result.Pois);
            Debug.Log($"[PoiService] Loaded {result.Pois.Count} POIs.");
        }

        public bool TryGetById(string id, out Poi poi) => _repo.TryGetById(id, out poi);

        public Poi? GetNearest(double lat, double lng) => _repo.GetNearest(lat, lng);
    }
}
```

- [ ] **Step 2: 階段 1 不 commit**

---

## Task 5: 種子化 poi_data.json

**Files:**
- Create: `unity-app/Assets/Data/poi_data.json`

- [ ] **Step 1: 建立 `poi_data.json`**（座標逐筆取自 `poi_captures.json`,內容欄位留空待 C 回填)

`unity-app/Assets/Data/poi_data.json`:
```json
{
    "pois": [
        { "id": "p01", "name": "校門口",         "lat": 25.043593212290668, "lng": 121.53319001415388, "altitude": 24.136355090886356, "anchorType": "Terrain", "shortDescription": "", "llmSystemPrompt": "" },
        { "id": "p02", "name": "國百館",         "lat": 25.043633166987016, "lng": 121.53336406853296, "altitude": 24.272039216943090, "anchorType": "Terrain", "shortDescription": "", "llmSystemPrompt": "" },
        { "id": "p03", "name": "土木館",         "lat": 25.043498067812440, "lng": 121.53338813170010, "altitude": 24.209809488616885, "anchorType": "Terrain", "shortDescription": "", "llmSystemPrompt": "" },
        { "id": "p04", "name": "學餐入口",       "lat": 25.043698462660577, "lng": 121.53366466922937, "altitude": 24.333744423463940, "anchorType": "Terrain", "shortDescription": "", "llmSystemPrompt": "" },
        { "id": "p05", "name": "大圓球",         "lat": 25.043610334019914, "lng": 121.53362452832033, "altitude": 24.387642802670599, "anchorType": "Terrain", "shortDescription": "", "llmSystemPrompt": "" },
        { "id": "p06", "name": "第一教學樓",     "lat": 25.043563461366735, "lng": 121.53385922552877, "altitude": 24.582515378482640, "anchorType": "Terrain", "shortDescription": "", "llmSystemPrompt": "" },
        { "id": "p07", "name": "國際演講廳入口", "lat": 25.043663622032587, "lng": 121.53398796604488, "altitude": 24.509720870293678, "anchorType": "Terrain", "shortDescription": "", "llmSystemPrompt": "" },
        { "id": "p08", "name": "化工館",         "lat": 25.043680003476699, "lng": 121.53439591644180, "altitude": 24.612994682043790, "anchorType": "Terrain", "shortDescription": "", "llmSystemPrompt": "" }
    ]
}
```

- [ ] **Step 2: 階段 1 不 commit**

---

## Task 6: Unity 整合 — 產 meta、跑測試、接場景、commit（須開啟 Unity）

**前置：通知使用者開啟 Unity Editor。** 在此之前不嘗試連線 MCP。

**Files:**
- 由 Unity 自動產生:上述所有檔案的 `.meta`
- Modify(場景接線,使用者於 Editor 操作或經 MCP)

- [ ] **Step 1: 開啟 Unity**

請使用者開啟 `unity-app`。Unity 會匯入新檔並為每個 `.cs` / `.json` / `.asmdef` 產生 `.meta`,並編譯兩個新 assembly(`NtutAR.Poi`、`NtutAR.Poi.Tests`)。

- [ ] **Step 2: 確認編譯無誤**

Console 應無紅色錯誤。若 `NtutAR.Poi.Tests` 找不到 `nunit.framework.dll`,確認 Test Framework 套件已安裝(Window → Package Manager → Unity Registry → Test Framework)。

- [ ] **Step 3: 跑 EditMode 測試(red/green 驗證點)**

Window → General → Test Runner → EditMode → Run All。
Expected: 9 個測試全綠(`PoiDataParserTests` 5 + `PoiRepositoryTests` 4)。
若有紅燈,依訊息修正對應檔案後重跑。

- [ ] **Step 4: 把 PoiService 接進場景**

在 bootstrap 場景(或新建空場景測試)建一個空 GameObject 命名 `PoiService`,掛上 `PoiService` 元件,把 `Assets/Data/poi_data.json` 拖到 `_poiDataJson` 欄位。進 Play,Console 應印 `[PoiService] Loaded 8 POIs.` 並對每個 POI 印出 `missing shortDescription / llmSystemPrompt` 警告(預期,內容待 C 填)。

- [ ] **Step 5: commit(程式 + 測試 + asmdef + meta)**

```bash
git add unity-app/Assets/Scripts/Poi unity-app/Assets/Tests/EditMode
git commit -m "feat(poi): POI 資料層 — 統一 schema、PoiDataParser、PoiRepository、PoiService + EditMode 測試"
```

- [ ] **Step 6: commit(資料 + 場景接線)**

```bash
git add unity-app/Assets/Data
# 若有修改場景檔,一併 add 對應的 .unity 與 .meta
git commit -m "data(poi): 種子化 poi_data.json(8 點座標,內容待回填)+ 場景接上 PoiService"
```

- [ ] **Step 7: 推分支(可選,待使用者確認)**

```bash
git push -u origin feature/poi-data-loader
```

---

## 完成後銜接點(本計畫範圍外)

- C 回填 `poi_data.json` 的 `shortDescription` / `llmSystemPrompt`(`PoiService` 警告會即時提示哪些未填)。
- `GeospatialAnchorManager` 讀 `PoiService.All` 依 `lat/lng/anchorType` 放 ARCore anchor。
- B 的 `ILlmClient.AskAsync` 以 `Poi.ToContext()` 取得 `PoiContext`。

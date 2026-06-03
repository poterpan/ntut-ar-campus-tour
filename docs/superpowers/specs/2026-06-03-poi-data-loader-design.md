# POI 載入骨架 + 統一 Schema — 設計文件

> 日期:2026-06-03
> 範圍:`unity-app/` 主程式的 POI **資料層**(統一 schema + 載入器 / `PoiService`)。
> 不含:Geospatial anchor 放置、NPC 接線、LLM 串接、場景 UI(屬後續任務,本層只提供資料給它們)。

## 1. 目標與背景

主程式需要一個**單一可信來源**的 POI 資料,同時承載:

- **地理欄位**(A 實地收集,已存在 `unity-app/Assets/StreamingAssets/poi_captures.json`,8 點)
- **內容欄位**(C 撰寫的 TTS 開場白與 LLM system prompt)

Code review 已發現分裂風險:LLM 測試專案的 `poi_contexts.json`(5 點)與 canonical 的 8 點
**同一 id 對到不同地點**。本設計用「單一合併檔」根除這類 drift。

對應 `docs/分工.md` 既有約定:

```csharp
public record PoiContext(string Id, string Name, string LlmSystemPrompt);
// ILlmClient.AskAsync(string question, PoiContext poi, CancellationToken ct)
```

## 2. 已定案決策

| 決策 | 選擇 | 理由 |
|---|---|---|
| 資料結構 | **單一合併檔** `poi_data.json` | 無 join、無 drift;A 維護座標,C 在同檔填內容欄位(不同欄位幾乎不衝突) |
| 格式 | **純 JSON → C# struct**(`JsonUtility`) | 與既有 `poi_captures.json` 一致;text-mergeable;C 不開 Unity 也能編輯 |
| 載入方式 | **序列化 `TextAsset` 參考**(Inspector 指定) | 避開 `File.IO` 讀 StreamingAssets 在 Android 失敗的問題(PR #6 的 bug);iOS + Android 皆可 |

## 3. 元件與資料流

```
Assets/Data/poi_data.json          ← 合併後的單一來源(從 8 點 captures 種子化)
        │  TextAsset 參考(Inspector 指定)
        ▼
PoiDataParser   (純 C#,不依賴 Unity)   JSON 字串 → List<Poi> + 驗證警告
        ▼
PoiService      (MonoBehaviour)        Awake 時解析,對外提供查詢 API
        ▼
後續 consumer:GeospatialAnchorManager / NpcController / ILlmClient(透過 PoiContext)
```

**設計原則**:把「純解析邏輯」(`PoiDataParser`)與「Unity 黏合層」(`PoiService`)切開 ——
解析器可在 EditMode 測試中以字串輸入單測,不需 Unity runtime;`PoiService` 維持薄黏合。

## 4. 資料模型與 JSON schema

`JsonUtility` **無法解析頂層為陣列的 JSON**,故以 `{ "pois": [...] }` 包一層
(與 `poi_captures.json` 的 `"captures"` 同模式)。

```jsonc
{ "pois": [
  { "id": "p01", "name": "校門口",
    "lat": 25.043593, "lng": 121.533190, "altitude": 24.14,
    "anchorType": "Terrain",
    "shortDescription": "",   // C 填:TTS 30 秒開場白
    "llmSystemPrompt": "" },   // C 填:LLM 對話 system prompt
  // ... 共 8 筆,座標逐筆取自 poi_captures.json
] }
```

```csharp
namespace NtutAR.Poi
{
    [System.Serializable]
    public struct Poi
    {
        public string id;
        public string name;
        public double lat;
        public double lng;
        public double altitude;
        public string anchorType;          // 解析為 PoiAnchorType,預設 Terrain
        public string shortDescription;
        public string llmSystemPrompt;

        public PoiContext ToContext() => new PoiContext(id, name, llmSystemPrompt);
    }

    public enum PoiAnchorType { Terrain, Rooftop, Geospatial }

    public readonly record struct PoiContext(string Id, string Name, string LlmSystemPrompt);
}
```

- 欄位皆 `public`:`JsonUtility` (反)序列化所需,屬 DTO,非 Inspector 欄位(不違反 CLAUDE.md)。
- `anchorType` 以字串存檔、解析為 `PoiAnchorType`;無法解析時 fallback `Terrain`(校園地面 POI)。
- `poi_captures.json` 保留為 POICollector 的原始現場存檔(App 不載入);`poi_data.json` 為 App 維護來源。

## 5. 載入器 API 與錯誤處理

```csharp
public sealed class PoiService : MonoBehaviour
{
    [SerializeField] private TextAsset _poiDataJson;

    public IReadOnlyList<Poi> All { get; }                 // 解析後唯讀清單
    public bool TryGetById(string id, out Poi poi);        // 找不到回 false
    public Poi? GetNearest(double lat, double lng);        // haversine,清單空回 null
}
```

**錯誤處理(任何情況都不丟例外、不讓場景崩潰):**

| 情況 | 行為 |
|---|---|
| TextAsset 未指定 / 內容空 | 記一次 `LogError`,`All` = 空清單 |
| JSON 格式錯誤 | `try/catch` 記 `LogError`,`All` = 空清單 |
| 重複 id | `LogWarning` 列出,採第一筆 |
| 缺座標(lat/lng 皆 0) | `LogWarning` 列出該 id |
| `shortDescription` 或 `llmSystemPrompt` 為空 | `LogWarning` 列出該 id —— **即時看到 C 尚未填的 POI** |

驗證在解析時一次跑完(`PoiDataParser.Validate`),只記 log 不阻斷載入。

`GetNearest`:標準 haversine 距離,回傳最近的 `Poi`;清單為空回 `null`。供後續 AR 層判斷
「使用者目前靠近哪個 POI」。

## 6. 檔案配置

命名空間 `NtutAR.Poi`(與既有 `NtutAR.Geo` 一致)。

```
unity-app/Assets/
├── Scripts/Poi/
│   ├── Poi.cs              # Poi struct + PoiAnchorType enum + PoiContext record
│   ├── PoiDataParser.cs    # 純解析 + 驗證(static / 無 Unity 依賴)
│   └── PoiService.cs       # MonoBehaviour:持有 TextAsset、Awake 解析、查詢 API
├── Data/
│   └── poi_data.json       # 8 筆 POI(座標已填,內容欄位留空待 C)
└── Tests/EditMode/
    ├── NtutAR.Poi.Tests.asmdef
    └── PoiDataParserTests.cs
```

## 7. 種子化(seeding)

實作時把 `poi_captures.json` 的 8 筆逐筆轉成 `poi_data.json`:

- 帶入 `id` / `name` / `lat` / `lng` / `altitude`
- 捨棄收集用的精度欄位(`*Accuracy` / `capturedAt`)—— App runtime 不需要
- `anchorType` 全填 `"Terrain"`
- `shortDescription` / `llmSystemPrompt` 留空字串(交給 C 回填)

## 8. 測試

EditMode 單元測試(`PoiDataParser`,純邏輯、免 runtime):

1. 合法 JSON → 解析出正確筆數與欄位值
2. 格式錯誤 JSON → 回空清單、不丟例外
3. 重複 id → 採第一筆並產生警告
4. `TryGetById` 命中 / 未命中
5. `GetNearest` 回傳預期最近點;空清單回 `null`

## 9. Unity 介入點(需要時通知使用者開啟)

本設計的程式碼與 JSON **可在 Unity 關閉狀態下全部以檔案撰寫完成**。完成後,使用者開啟
Unity **一次**以:

1. 讓 Unity 產生新檔的 `.meta`(依 repo 規範須 commit)
2. 把 `PoiService` 掛到 bootstrap 場景的 GameObject,並指定 `poi_data.json` 這個 TextAsset
3. 透過 Test Runner 跑 EditMode 測試

屆時會明確通知使用者開啟 Unity(在此之前不嘗試連線 MCP)。

## 10. 後續 consumer(本任務範圍外,僅記錄銜接點)

- `GeospatialAnchorManager`:讀 `PoiService.All`,依 `lat/lng/anchorType` 放 ARCore anchor
- `NpcController`:在 anchor 處生成導遊 NPC
- `ILlmClient.AskAsync`:用 `Poi.ToContext()` 取得 `PoiContext` 作為對話上下文
- `GetNearest`:AR 層判斷使用者鄰近 POI

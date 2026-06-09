# UI/UX 總體設計 — 北科 AR 校園導覽

> 2026-06-10 定案。功能凍結,純外觀/體驗打磨;允許 Mock Data。
> 呈現形式:課堂簡報 + Demo 影片(黃金流程深度打磨),但維持基本完整度(無死路、狀態提示齊全)。
> Wireframes:`docs/ui-wireframes/*.html`(brainstorm 產物,簡報可引用)。

## 1. 結構決策

- **主結構 C — AR 常駐**:App 全程為 AR 相機畫面(課程要求 AR-based,不採 2D 地圖主畫面)。
- **HUD 配置 A**:罐頭按鈕置於畫面下中(Pokémon GO 寶可夢球位),狀態資訊在上、行動按鈕在下。

## 2. 畫面清單

| 畫面 | 內容 | 備註 |
|---|---|---|
| 開場流程 | Splash → 權限說明 → 「尋找位置中」 → 定位完成 | 4 步;後 2 步疊在相機上,接現有 ArLocalizationController 狀態 |
| AR 主畫面 HUD | 玩家狀態列(左上)、圓形小地圖(右上)、POI 接近提示橫幅(上中)、罐頭鈕(下中)、圖鑑鈕(罐頭右側)、抽屜把手(底部) | 接近提示接現有 ArGuideProximityDriver |
| 上拉抽屜 | POI 列表:圖示、名稱、距離、探索狀態;點項目展開簡介 | 資料來自 PoiService |
| 探索手帳(圖鑑) | 全螢幕:圓形紀念章 grid、未解鎖虛線問號、餵貓計數 | 入口為 HUD 圖鑑鈕 |
| NPC 對話 | 現有 GuideChatPanel 換皮:毛玻璃半版、NPC 名牌、圓角訊息泡泡 | 功能不動 |

## 3. 視覺系統(Design Tokens)

| Token | 值 |
|---|---|
| 毛玻璃卡片底 | `rgba(255,252,245,0.75)` + blur(行動端 URP 真 blur 成本高,實作時評估真/假 blur) |
| 主文字 | `#5D4037`(暖棕) |
| 次要文字 | `#A1887F` |
| 主按鈕 | `#AED581`(抹茶綠) |
| 貓咪/罐頭強調 | `#F57C00`(橘) |
| 暖底色(全屏頁) | `#F7F1E5 → #EDDFC8` 漸層 |
| 標題字型 | 現有 Noto Serif CJK TC |
| UI 內文字型 | 新增圓黑體(候選:jf open 粉圓,OFL) |
| 圖示 | 第一版 TMP sprite(emoji 風)+ 幾何圖形;美術可後換 |

風格定位:皮克敏 Bloom 暖色療癒為基底 + 毛玻璃疊加(與皮克敏風 NPC 模型一致)。

## 4. Mock Data 盤點

| 資料 | 真/Mock | 來源 |
|---|---|---|
| POI 列表、距離 | 真 | PoiService + GPS |
| 集章解鎖 | 真 | 進入 POI 範圍觸發,PlayerPrefs 持久化 |
| 餵貓計數 | 真 | CatQLearningAgent.TargetReached 累加,PlayerPrefs |
| 玩家暱稱、頭像 | Mock | 寫死 |
| 小地圖底圖 | 半真 | 風格化校園插畫一張;玩家/POI 以 GPS 線性換算貼點 |

## 5. 技術作法

- 全 uGUI;每面板一個 prefab + controller,新增 `Assets/Scripts/Ui/` + `NtutAR.Ui.asmdef`(引用 Poi/Guide/Cat 視需要)。
- 動畫不引第三方套件:自寫小型 coroutine tween 工具(fade / slide / scale)。
- 處理 iPhone safe area(現有 UI 未處理)。
- 既有 Bootstrap 的範例狀態 UI 整併進新 HUD,移除開發用殘留。

## 6. 黃金 Demo 流程(影片腳本骨架)

開場動畫 → 定位完成 → 接近提示 → 紅樓 NPC 對話 → 丟罐頭餵貓 → 蓋章動畫 → 打開手帳看收集進度。

## 7. 範圍外

設定頁、罐頭拋物線手感(獨立 PR)、真實地圖引擎、頭像/帳號系統。

## 8. 占位內容(實作時須由團隊定案)

- App 名稱(wireframe 暫用「北科尋貓記」)
- NPC 名字(暫用「小導」)
- 小地圖校園插畫底圖(可先用簡化色塊版)

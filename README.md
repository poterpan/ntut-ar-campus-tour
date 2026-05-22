# 北科 AR 校園導覽 App — 期末專案 Memo

> 課程:電腦圖學與擴增實境(NTUT 創新 AI 所)
> 作者:Poter(潘柏嘉)
> 組員:潘柏嘉、蔡宗育、簡妤真、張凱琳
> 最後更新:2026-05-22
> 用途:作為對話重啟時的完整 context

---

## 1. 專案概述

### 1.1 題目
**北科 AR 校園導覽:LLM 導遊與 AI 互動體驗**

副標:結合 Geospatial AR、大型語言模型與強化學習的北科校園導覽應用

### 1.2 場域
**北科新生南路側門入口路徑**,5 個 POI:
1. 新生南路側門(起點)
2. 學生餐廳入口
3. 演講廳入口
4. 第一教學大樓
5. 化工館

### 1.3 對象
- 主要:新生入學導覽
- 次要:訪客、招生說明會、校慶活動

### 1.4 課程要求對齊
- ✅ 校園相關導覽 App(場域不限)
- ✅ AR App、基於 Unity
- ✅ 包含一個「訓練出來的」NPC(原本指 ML-Agents Hummingbirds RL,確認過任何訓練出來的 model 都可,不強制對齊蜂鳥)

---

## 2. 技術架構

### 2.1 三層架構

**Client 端(iOS / Android)**
- Unity 6.3 LTS (6000.3.10f1)
- AR Foundation 6.x
- ARCore Extensions for AR Foundation 1.54.0(支援 iOS/Android 雙平台)
- ARKit XR Plugin(iOS provider)
- ML-Agents 推論(Sentis,Unity 6 取代 Barracuda)
- TTS 播放(iOS AVSpeechSynthesizer 或 server-side TTS)

**雲端服務**
- ARCore Geospatial API(Google VPS,負責高精度定位)
- LLM API(候選:OpenAI / Anthropic Claude / Google Gemini)
- (可選)RAG 知識庫,存放校園資料

**訓練端**
- ML-Agents 訓練 pipeline
- 跑在 Poter 的 Ubuntu workstation(poterpan-ntut-3090, RTX 3090)
- 訓練校園貓 RL agent

### 2.2 互動流程

```
1. 使用者站到 POI 附近(GPS 粗判,半徑 20–30m)
2. Geospatial VPS lock 上精確位置(< 1m 精度)
3. AR NPC(導遊角色)在使用者前方出現
4. NPC 用 LLM 講解該建築 + 接受提問
5. 講解結束後,NPC 引導至下個 POI
6. (彩蛋)使用者觸發圖鑑模式 → AR 貓出現,RL 餵食互動
```

---

## 3. 定位技術原理(Geospatial API)

### 3.1 三層疊加

**第一層:GPS + IMU(粗定位)**
- GPS: 5–15m 精度
- 氣壓計:估海拔
- 地磁:粗估 yaw,但市區誤差大
- 作用:給 VPS 一個搜尋的起點

**第二層:VPS(視覺定位,核心魔法)**
- Google 用 15+ 年 Street View 照片建立全球 3D 點雲
- Deep neural network 抽取「長期穩定的視覺特徵點」
- 手機抓當下相機畫面的特徵點 → 上傳雲端(不是原圖)
- 雲端在 30m 半徑內 3D 點雲做匹配 → PnP 反推 6DoF pose
- Round-trip ~100–500ms,實際 1–2Hz 修正

**第三層:ARKit/ARCore 本機 SLAM**
- 60Hz frame-to-frame 連續追蹤
- 短時間內公分級精度,但會 drift
- API 維護一個 T_world 變換矩陣對齊本地 SLAM 和全球座標
- VPS 每 1–2 秒重新校準 T_world

### 3.2 為什麼 Yaw Accuracy 是 lock 品質的關鍵指標
- 位置、Roll/Pitch 都有額外參考(GPS / IMU 重力)
- 只有 Yaw 完全靠 VPS 從畫面推斷
- 所以 Yaw Acc 從 14° 掉到 < 1° 那一刻,就是 VPS 真正 lock 上

### 3.3 Anchor 類型
- **WGS84 Anchor**:絕對 lat/lng/altitude(altitude 難精準)
- **Terrain Anchor**:lat/lng + 相對地面高度,自動算 altitude(推薦)
- **Rooftop Anchor**:lat/lng + 相對屋頂高度,自動貼到建物頂

---

## 4. VPS 可行性驗證結果(已實測)

### 4.1 校內路徑
- Horizontal Accuracy: < 1m
- Vertical Accuracy: < 1m
- Yaw Accuracy: ~1°

### 4.2 校外路面(新生南路、忠孝東路側)
- Horizontal Accuracy: < 0.5m
- Vertical Accuracy: < 0.5m
- Yaw Accuracy: ~0.5°

### 4.3 Anchor 漂移實測
- 放 anchor 後繞一圈回來:1–2m 偏差(在 VPS 理論範圍內,正常)
- 對建築物導覽用途完全夠用

### 4.4 結論
北科校園 VPS 涵蓋良好,Geospatial API 完全可行,**期末作業可以基於這個技術繼續開發**。

---

## 5. NPC 設計

### 5.1 主導覽 NPC(LLM 驅動)
- **形象**:可視化角色(具體形象待定)
- **功能**:
  - 走到 POI 時出現
  - 講解該建築物 / 設施
  - 接受使用者提問,LLM 即時回答
  - 講解結束後引導至下一個 POI
- **TTS**:語音輸出(候選方案:iOS AVSpeechSynthesizer / edge-tts server-side)
- **架構待定**:雲端 LLM API,可能加 RAG(校史、設施資料、餐廳資訊等)

### 5.2 彩蛋 NPC(ML-Agents RL)
- **形象**:校園貓
- **遊戲**:AR 餵食小遊戲
  - 使用者點 AR 地面放飼料
  - 貓 RL agent 判斷該不該去吃、最短路徑、避障
- **RL 設定**:
  - 觀察空間:貓位置、最近飼料位置、障礙物、飢餓值
  - 動作空間:連續(方向 + 速度)或離散(前後左右停)
  - Reward:吃到飼料 +10、移動 -0.01、撞牆 -1
- **訓練環境**:Unity Editor 簡化 3D 平面場景,隨機生成飼料和障礙
- **推論**:訓練完 export ONNX,iOS 端用 Sentis 推論
- **與主線解耦**:不強行整合到導覽流程,當獨立彩蛋
- **與主線輕度連結**:導覽中可能會提到「附近有校貓出沒,可開圖鑑模式互動」

---

## 6. 環境設定(已完成)

### 6.1 Unity 專案
- 專案名:ARCore_test(初版,正式專案再開新的)
- Unity 版本:6.3 LTS (6000.3.10f1)
- Bundle ID(iOS):`me.panspace.ntutar.poc`
- Render Pipeline:URP(Mobile_RPAsset + Mobile_Renderer)

### 6.2 已裝套件
- AR Foundation
- Apple ARKit XR Plugin
- Google ARCore Extensions for AR Foundation 1.54.0
- ML-Agents(待補裝)

### 6.3 Project Settings 重點
- Active Input Handling: **Both**(舊版 Input Manager + 新版 Input System)
- IL2CPP Code Generation: **Faster (smaller) builds**(開發階段)
- Camera Usage Description: 已填
- Location Usage Description: 已填
- iOS Support Enabled: ✅
- Geospatial Optional Feature: ✅
- Android / iOS Authentication Strategy: API Key
- ARKit Plug-in Provider(iOS): ✅
- Mobile_Renderer 已加 **AR Background Renderer Feature**(URP 必備)

### 6.4 Google Cloud
- 已建立 GCP project
- 已啟用 ARCore API
- 已建立 API Key 並填入 Unity
- 計費:免費 quota 對 demo 用量綽綽有餘

### 6.5 環境踩過的坑(留作備忘)
1. **Active Input Handling 卡住**:Geospatial sample 用 `Input.location`(舊 API),預設新 Input System 會編譯失敗。改 Both 解決
2. **CocoaPods 環境**:macOS 系統 Ruby + RVM Ruby 3.0.0 + brew Ruby 4.0.4 衝突。最後關掉 ~/.zlogin、~/.profile 裡的 RVM source 解決
3. **AR Foundation 子系統未啟用**:iOS 分頁的 XR Plug-in Management 沒勾 ARKit,build 出去 ARCore session 會回 `UnavailableDeviceNotCompatible`(誤導性錯誤,實際是 ARKit provider 缺失)
4. **URP 黃色畫面**:Mobile_Renderer 沒加 AR Background Renderer Feature → AR 相機 feed 渲染不出來,只看到 clear color
5. **Build 速度**:URP + IL2CPP + Sample 第一次約 3.5 分鐘。改 Development Build + Faster (smaller) builds 後 incremental 1–2 分鐘

---

## 7. 開發時程規劃(開發期 1 個月 / 4 週)

> 提案週結束後進入 4 週開發期。分工與模組介面見 `docs/分工.md`。

| 週次 | 主要工作 |
|---|---|
| W1 骨架 | 建 Unity 專案推 GitHub、全員 clone;定 Assets 結構與場景/prefab 策略、模組介面;實地收集 5 個 POI 座標;LLM API 串接起步、NPC 模型蒐集、RL 訓練場景搭建 |
| W2 平行開發 | Geospatial + POI 部署可運作;LLM 對話 standalone 跑通;NPC 出現在 POI + 動畫;RL 校園貓訓練中 |
| W3 整合 | LLM 對話接進 NPC;RL 貓 ONNX 接進 AR;整合進主場景;填入 POI 解說文本 |
| W4 收尾 | 實機整合測試、bug fix、UI 美化、Demo 影片、結案報告 |

---

## 8. 評分項目對應(老師簡報內容)

| 評分項目 | 占比 | 對應作法 |
|---|---|---|
| Level of Difficulty | 20% | Geospatial + LLM + ML-Agents 三者整合 |
| Completion | 20% | Milestone 清楚、按時 deliver |
| Functionality | 20% | 主導覽 + 彩蛋雙功能完整可跑 |
| Proposal Presentation | 10% | 下週提案 |
| Final Presentation | 15% | 期末展示 |
| Report / Video | 15% | 結案報告 + Demo 影片 |

---

## 9. 提案簡報結構(草稿)

1. **封面**:題目 + 組員 + 視覺意象圖
2. **動機**:新生入學導覽痛點 + 訪客導覽情境
3. **題目 & 場域**:新生南路側門路徑地圖 + POI 標記
4. **技術架構**:三層架構圖 / sequence 流程
5. **技術亮點**:Geospatial + LLM + ML-Agents 整合
6. **可行性驗證**:VPS 實測數據截圖(校外 0.5m、校內 1m)
7. **預期成果**:demo 想像描述 + UI mock
8. **時程**:Gantt 或表格
9. **(可選)風險與備案**:VPS 不穩備援(Image Tracking)

---

## 10. 已驗證的工具與資源

### 10.1 必用
- ARCore Extensions for Unity: https://github.com/google-ar/arcore-unity-extensions
- ARCore Geospatial Sample(已成功 build 並實機驗證)

### 10.2 可參考
- **Pocket Garden**(Google I/O 2022,完整生產級程式碼)
  https://github.com/buck-co/PocketGarden
  `Assets/_GeoAR Framework` 可抄架構
- **GeospatialAPI-Unity-StarterKit**(輕量版,僅 v1.37.0)
  https://github.com/TakashiYoshinaga/GeospatialAPI-Unity-StarterKit

### 10.3 Unity 官方
- ARCore Geospatial 開發者指南:
  https://developers.google.com/ar/develop/geospatial
- Unity Sentis 文件(ML-Agents 推論):
  Unity 6 內建 Package Manager 可找

---

## 11. 待決定 / 待補事項

- [ ] 主導覽 NPC 具體形象(角色設計、3D model 來源)
- [ ] LLM 選擇(OpenAI / Anthropic / Gemini)
- [ ] TTS 方案選擇(本地 / server-side)
- [ ] 5 個 POI 的實地座標(走訪新生南路側門路徑收集 lat/lng)
- [ ] POI 內容文本(每個建築物要講的故事 + 學餐店家清單)
- [ ] RAG 知識庫架構(是否需要 vector DB)
- [ ] RL 訓練的具體 scene 設計
- [ ] 校園貓 3D model 來源
- [ ] iOS 還是 Android 為主要 demo 平台(目前已 build iOS,Bundle ID 是 iOS 慣例)
- [ ] App 名稱(目前 ARCore_test 是 placeholder)

---

## 12. 個人技術背景(影響選擇)

- 主要開發環境:Mac(iOS 開發必備)
- 訓練機:Ubuntu workstation,RTX 3090(可跑 ML-Agents)
- 熟悉:Next.js、Python、Swift/iOS、Cloudflare 生態、YOLO11(spine 專案)
- 對 ML-Agents:課堂教過 Hummingbirds(待確認熟悉度)
- 之前的相關專案:NTUTBox(iOS app)、QR code 點名系統、Spine X-ray AI(學術合作)

---

## 13. 對話歷史摘要(本次討論結果)

1. 一開始討論題目方向,從 5 個 idea 中選定「校史時光機/校園導覽」方向
2. 釐清 tracking 技術:GPS / Image Tracking / Geospatial API / 自建 3D 模型
3. 選擇 Geospatial API(精度高、適合戶外、不用建模)
4. 驗證 VPS 在北科校園涵蓋(實測通過,校外 <0.5m / 校內 <1m / Yaw <1°)
5. 跑通 ARCore Geospatial Sample(踩了一堆環境坑)
6. 確認 API 費用問題(免費 quota 對 demo 用量綽綽有餘)
7. 重新理解老師對「AI trained NPC」的要求(原意是 ML-Agents 而非 LLM)
8. 確定方案:**主導覽 LLM + 彩蛋 RL 餵食小遊戲**(雙線解耦)
9. 場域定為**新生南路側門入口路徑**,POI 確定:側門 → 學餐入口 → 演講廳入口 → 第一教學大樓 → 化工館
10. 完成提案簡報 5 頁(HTML 格式,可轉 PDF)
11. 題目從「智慧導覽」改為「校園導覽」(因課程要求本來就是導覽 App,差異化在場域)

---

## 14. 下一步行動

**本週(W1)**:
1. 實地走一次新生南路側門路徑,收集 5 個 POI 的精確 lat/lng
2. 替換提案簡報的 3 個圖片 placeholder(側門實景、VPS 測試截圖)
3. 準備提案口頭報告
4. 想 NPC 角色設定(名字、性格、視覺風格)

**下週(W2)**:
- 提案報告
- 收老師回饋,視回饋調整方向
- 開始 Geospatial 串接和 LLM 接通的 MVP

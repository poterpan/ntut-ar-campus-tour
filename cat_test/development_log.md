# 貓咪強化學習尋找罐頭專案 - 開發紀錄

## 2026-06-05

### 目前進度
1. **專案結構檢查**：
   - 確認此專案為 Unity 專案。
   - 專案目錄下有 `Assets`, `Packages`, `ProjectSettings` 等目錄。
   - `Packages/manifest.json` 中尚未安裝 Unity 官方的 `com.unity.ml-agents` 插件。
2. **現有程式碼分析**：
   - 發現貓咪控制程式碼為 `Assets/Ladymito/Free_cat/Models/CatManualControl.cs`。
   - 該程式碼目前為「自動巡航」設定，貓咪會以恆定速度前進 (moveInput = 1.0) 並往右旋轉 (turnInput = 0.5) 繞圈圈，並會撥放貓咪行走的動畫（修改 Animator 中的 `Speed` 參數）。
3. **實作 Q-Learning 演算法與 Agent 控制腳本**：
   - 新增了 [CatQLearningAgent.cs](file:///f:/Un/cat_test/Assets/CatQLearningAgent.cs)：實作 Q-table (4種距離 * 8種角度共 32 個離散狀態，3 種移動旋轉動作)、Epsilon-Greedy 探索策略、Shaping Reward 獎勵塑造與時間懲罰機制、自動保存與載入 Q-table (JSON 格式)，並包含 Heuristic 手動控制模式（可使用鍵盤 W/A/D 控制貓咪移動）。
   - 新增了 `CanTrigger` 元件，當貓咪碰撞到罐頭時，會觸發 `ReachTarget()` 給予 +15 獎勵並重置回合。
4. **一鍵式場景自動配置工具**：
   - 新增了 [CatTrainingSetup.cs](file:///f:/Un/cat_test/Assets/Editor/CatTrainingSetup.cs) 及選單項 `Cat Training -> Setup RL Environment`，點擊即可自動移除舊的控制腳本、掛載 `CatQLearningAgent`、自動設定 Rigidbody 為 Kinematic、並自動為貓咪生成 Collider。
5. **新增 Scene 視窗即時訓練視覺化導引 (Gizmos)**：
   - 在 [CatQLearningAgent.cs](file:///f:/Un/cat_test/Assets/CatQLearningAgent.cs) 中新增了 `OnDrawGizmos()`。在 Scene 視窗繪製活動邊界（半徑 8.0f）與貓咪指向罐頭的「紅綠漸層引導線」。
6. **支援背景持續運行 (Run in Background)**：
   - 在 [CatQLearningAgent.cs](file:///f:/Un/cat_test/Assets/CatQLearningAgent.cs) 的 `Start()` 中加入了 `Application.runInBackground = true;`。當用戶點擊其他視窗或最小化 Unity 時，遊戲與訓練仍會持續運行，方便用戶掛網訓練。
7. **修正 JSON 序列化存檔 Bug**：
   - **發現問題**：Unity 內建的 `JsonUtility` 無法直接序列化二維或巢狀陣列（如原本的 `List<float[]>` 動作 Q 值），導致一開始儲存的 JSON 只有 keys，而沒有 values，使訓練進度無法真正保存。
   - **修復方案**：新增 `QRow` 類別用來封裝 `float[]` 陣列，並將 `QTableData` 的 `values` 改為 `List<QRow>`。此改動能確保 JsonUtility 正確儲存和載入 Q-table 的所有狀態數值。
8. **新增內建模型支援 (Resources.Load)**：
   - 在 [CatQLearningAgent.cs](file:///f:/Un/cat_test/Assets/CatQLearningAgent.cs) 的 `LoadQTable()` 中加入了備用載入機制。如果找不到本地的最新訓練存檔，會自動讀取 `Assets/Resources/cat_qtable.json`，方便將訓練好的模型打包含入遊戲發布。
9. **支援手動擺放與運行時自由拖曳罐頭**：
   - 在 [CatQLearningAgent.cs](file:///f:/Un/cat_test/Assets/CatQLearningAgent.cs) 中新增了 `randomizeTargetPosition` 屬性。當取消勾選時，重置回合將不會強制打亂罐頭位置。這允許用戶手動在編輯器中擺放自己設計的罐頭，並可在遊戲運行中於 Scene 視窗即時拖曳罐頭位置，貓咪會跟隨移動。
10. **實作自訂罐頭的「一鍵式防呆自動配置」**：
    - 在 [CatQLearningAgent.cs](file:///f:/Un/cat_test/Assets/CatQLearningAgent.cs) 中新增了 `SetupUserCan(GameObject canObj)`。自動配置碰撞器與偵測腳本。
11. **編輯器自動同步寫入專案 Assets/Resources 資源夾**：
    - 在 [CatQLearningAgent.cs](file:///f:/Un/cat_test/Assets/CatQLearningAgent.cs) 的 `SaveQTable()` 裡加入了 `#if UNITY_EDITOR` 自動同步機制。每當訓練存檔時，會自動在專案中建立 `Assets/Resources` 並寫入 `cat_qtable.json`。
12. **手動將 LocalLow 最新模型拷貝至專案**：
    - 由於用戶在加入自動同步機制前，就已經跑完 1300 次訓練，因此模型最初只存在於 AppData/LocalLow 中。我們已主動建立 `Assets/Resources` 目錄，並將 1300 次最新模型 `cat_qtable.json` 複製進去，使 Unity Project 視窗可以正常刷出資源。
13. **新增 isTraining 模式開關（保護訓練成果並自動切換測試模式）**：
    - 在 [CatQLearningAgent.cs](file:///f:/Un/cat_test/Assets/CatQLearningAgent.cs) 中新增了 `isTraining` 布林開關。
14. **新增 qTableAsset 拖曳載入方框**：
    - 在 [CatQLearningAgent.cs](file:///f:/Un/cat_test/Assets/CatQLearningAgent.cs) 中新增了 `public TextAsset qTableAsset;` 欄位。這在 Inspector 視窗中提供了一個「模型檔案拖曳方框」，用戶可以直接將專案視窗中的任意 JSON 模型檔案（如 `cat_qtable.json`）拖入其中，載入時會自動以該欄位的模型為最高優先級讀取。
15. **支援連續尋路（貓咪留在原地繼續尋找下一顆罐頭）**：
    - 在 [CatQLearningAgent.cs](file:///f:/Un/cat_test/Assets/CatQLearningAgent.cs) 中新增了 `resetAgentOnSuccess` 變數（預設為 `false`）。
16. **建立 Unity 標準 .gitignore 檔案**：
    - 在專案根目錄下建立了 [.gitignore](file:///f:/Un/cat_test/.gitignore) 檔案，以確保用戶將專案上傳至 GitHub 時，不會上傳多餘的本地快取目錄（如 `Library`、`Temp`），加速上傳過程並保持 Git 庫乾淨。

### 下一步規劃
- 解答用戶關於將專案上傳至 GitHub 時設定是否會保留的問題，並提醒 Git 忽略規則與模型同步細節。

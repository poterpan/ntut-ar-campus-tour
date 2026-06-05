using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 貓咪 Q-Learning 強化學習 Agent
/// </summary>
public class CatQLearningAgent : MonoBehaviour
{
    [Header("訓練/測試模式")]
    [Tooltip("勾選後可使用鍵盤控制：W 鍵直走，A 鍵左轉，D 鍵右轉")]
    public bool heuristicMode = false;
    [Tooltip("是否處於訓練模式。若取消勾選，將自動關閉探索與更新，進入純成果展示模式")]
    public bool isTraining = true;
    [Tooltip("訓練速度縮放，調高可加速訓練（建議 1 到 100）")]
    [Range(1, 100)]
    public float trainingSpeedScale = 1.0f;

    [Header("模型檔案 (Q-Table JSON)")]
    [Tooltip("您可以將訓練好的 JSON 模型檔案拖曳至此方框中。若保留空白，將優先載入本地最新存檔")]
    public TextAsset qTableAsset;

    [Header("目標物 (罐頭)")]
    public Transform targetCan;
    [Tooltip("若無指定罐頭，是否自動生成一個簡易罐頭")]
    public bool autoCreateCan = true;
    [Tooltip("重置時是否隨機化罐頭位置 (訓練時建議勾選；若想手動擺放或拖曳罐頭，請取消勾選)")]
    public bool randomizeTargetPosition = true;
    [Tooltip("成功吃到罐頭後，是否將貓咪重置傳送回起點 (預設為 false，貓咪將留在原地從當前位置繼續找下一個罐頭)")]
    public bool resetAgentOnSuccess = false;

    [Header("移動與旋轉參數")]
    public float moveSpeed = 3.0f;
    public float turnSpeed = 150f;

    [Header("Q-Learning 超參數")]
    public float learningRate = 0.1f;       // Alpha
    public float discountFactor = 0.9f;     // Gamma
    public float epsilon = 1.0f;            // 初始探索率
    public float epsilonDecay = 0.995f;     // 每個回合的衰減率
    public float minEpsilon = 0.05f;        // 最小探索率

    [Header("決策間隔")]
    [Tooltip("每隔多少秒進行一次動作決策 (秒)")]
    public float decisionInterval = 0.1f;

    [Header("訓練區域邊界")]
    [Tooltip("貓咪能活動的方形區域半徑（中心點為 0, 0, 0）")]
    public float boundaryRadius = 8.0f;

    [Header("即時狀態監控 (唯讀)")]
    public int currentEpisode = 0;
    public float episodeReward = 0f;
    public float cumulativeReward = 0f;
    public int successCount = 0;
    public int failCount = 0;
    public int currentDistanceState = 0;
    public int currentAngleState = 0;
    public int currentState = 0;
    public float currentEpsilon = 1.0f;

    // Q-table 儲存結構: State ID -> Action Q-values
    // 狀態數 = 4 (距離) * 8 (角度) = 32
    // 動作數 = 3 (0:直走, 1:左轉, 2:右轉)
    private Dictionary<int, float[]> qTable = new Dictionary<int, float[]>();
    private const int ACTION_COUNT = 3;

    private Vector3 spawnPosition;
    private Animator catAnimator;
    private float decisionTimer = 0f;
    
    // 儲存上一步的狀態與動作，用於 Q-value 更新
    private int lastState = -1;
    private int lastAction = -1;
    private float lastDistanceToTarget = 0f;
    private bool hasLastStep = false;

    // 存檔路徑
    private string qTableSavePath;

    void Start()
    {
        // 強制開啟背景運行，這樣當您切換到其他視窗時，貓咪依然會繼續訓練
        Application.runInBackground = true;

        spawnPosition = transform.position;
        catAnimator = GetComponentInChildren<Animator>();
        qTableSavePath = Path.Combine(Application.persistentDataPath, "cat_qtable.json");
        currentEpsilon = isTraining ? epsilon : 0f;

        // 初始化 Q-table
        InitializeQTable();

        // 罐頭配置
        if (targetCan == null && autoCreateCan)
        {
            CreateDefaultCan();
        }
        else if (targetCan != null)
        {
            // 防呆：自動幫用戶手動指定的罐頭進行碰撞與偵測元件的配置
            SetupUserCan(targetCan.gameObject);
        }

        // 重置場景開始第一回合 (第一次強制重置貓咪)
        ResetEpisode(true);
    }

    void Update()
    {
        // 即時調整 Unity 遊戲運行速度，以便快速訓練
        Time.timeScale = trainingSpeedScale;

        // 檢查是否超出邊界（失敗條件）
        CheckBoundary();

        // 決策計時器
        decisionTimer += Time.deltaTime;
        if (decisionTimer >= decisionInterval)
        {
            decisionTimer = 0f;
            MakeDecision();
        }
    }

    /// <summary>
    /// 初始化 Q-table
    /// </summary>
    private void InitializeQTable()
    {
        qTable.Clear();
        // 4 種距離 * 8 種角度 = 32 個狀態
        for (int state = 0; state < 32; state++)
        {
            qTable[state] = new float[ACTION_COUNT]; // 預設值為 0
        }
        
        // 嘗試載入已有的 Q-table
        LoadQTable();
    }

    /// <summary>
    /// 自動生成一個簡易的罐頭 (Can) 物件
    /// </summary>
    private void CreateDefaultCan()
    {
        GameObject canObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        canObj.name = "AutoCan";
        canObj.transform.localScale = new Vector3(0.5f, 0.25f, 0.5f);
        
        // 給罐頭塗上亮眼的黃色/橘色
        Renderer renderer = canObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1.0f, 0.6f, 0.0f); // 橘黃色罐頭
            renderer.material = mat;
        }

        // 設定 Collider 為 Trigger，方便偵測
        Collider col = canObj.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // 掛載簡單的 Tag 或碰撞偵測輔助元件
        CanTrigger trigger = canObj.AddComponent<CanTrigger>();
        trigger.agent = this;

        targetCan = canObj.transform;
    }

    /// <summary>
    /// 自動幫用戶手動指定的罐頭物件進行防呆配置（設定碰撞器為 Trigger，並自動掛載 CanTrigger 偵測腳本）
    /// </summary>
    private void SetupUserCan(GameObject canObj)
    {
        // 1. 確保罐頭上有 Collider (碰撞體)
        Collider col = canObj.GetComponent<Collider>();
        if (col == null)
        {
            // 預設加上一個 BoxCollider
            col = canObj.AddComponent<BoxCollider>();
            Debug.Log($"[CatQLearning] 自動為您的自訂罐頭 {canObj.name} 新增了 BoxCollider。");
        }
        
        // 2. 強制設定為 Trigger 觸發器模式，避免實體碰撞物理把貓咪卡住
        col.isTrigger = true;

        // 3. 確保罐頭上有 CanTrigger 元件
        CanTrigger trigger = canObj.GetComponent<CanTrigger>();
        if (trigger == null)
        {
            trigger = canObj.AddComponent<CanTrigger>();
            Debug.Log($"[CatQLearning] 自動為您的自訂罐頭 {canObj.name} 掛載了 CanTrigger 碰撞偵測腳本。");
        }
        
        // 4. 自動關聯貓咪自己 (this)
        trigger.agent = this;
    }

    /// <summary>
    /// 重置回合 (Episode)
    /// </summary>
    /// <param name="resetAgent">是否將貓咪重置回起點</param>
    public void ResetEpisode(bool resetAgent)
    {
        currentEpisode++;
        episodeReward = 0f;
        hasLastStep = false;
        lastState = -1;
        lastAction = -1;

        // 1. 重置貓咪位置到地圖中心附近，朝向隨機 (若 resetAgent 為 true)
        if (resetAgent)
        {
            transform.position = new Vector3(spawnPosition.x, spawnPosition.y, spawnPosition.z);
            transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
        }

        // 2. 隨機重置罐頭位置 (如果開啟了 randomizeTargetPosition 且有指定罐頭)
        if (targetCan != null && randomizeTargetPosition)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(3.0f, boundaryRadius - 1.5f);
            targetCan.position = new Vector3(spawnPosition.x + randomCircle.x, spawnPosition.y, spawnPosition.z + randomCircle.y);
        }

        lastDistanceToTarget = Vector3.Distance(transform.position, targetCan.position);

        // 3. 衰減 Epsilon (僅在訓練模式下)
        if (isTraining && currentEpsilon > minEpsilon)
        {
            currentEpsilon *= epsilonDecay;
        }
        else if (!isTraining)
        {
            currentEpsilon = 0f; // 測試模式下確保為 0
        }
    }

    /// <summary>
    /// 檢查貓咪是否跑出訓練邊界
    /// </summary>
    private void CheckBoundary()
    {
        float distanceFromCenter = Vector3.Distance(transform.position, spawnPosition);
        if (distanceFromCenter > boundaryRadius)
        {
            // 給予強烈的懲罰並重置
            AddReward(-10.0f);
            failCount++;
            
            // 執行一次最後的更新 (僅在訓練模式下)
            if (hasLastStep && isTraining)
            {
                UpdateQValue(lastState, lastAction, -10.0f, GetCurrentState());
            }

            // 出界了，必須將貓咪重置回起點
            ResetEpisode(true);
        }
    }

    /// <summary>
    /// 計算並取得目前離散化的狀態 (State ID: 0 ~ 31)
    /// </summary>
    private int GetCurrentState()
    {
        if (targetCan == null) return 0;

        Vector3 toTarget = targetCan.position - transform.position;
        float distance = toTarget.magnitude;

        // 1. 距離離散化 (4 個區間)
        int distState;
        if (distance < 2.0f) distState = 0;      // 極近
        else if (distance < 4.5f) distState = 1; // 近
        else if (distance < 7.0f) distState = 2; // 中
        else distState = 3;                     // 遠

        currentDistanceState = distState;

        // 2. 角度離散化 (8 個區間，以貓咪 Forward 朝向為基準)
        // SignedAngle 返回 -180 ~ 180 度
        float angle = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);
        
        // 將其轉換為 0 ~ 360 度
        float angle360 = angle;
        if (angle360 < 0) angle360 += 360f;

        // 切成 8 等分，每等分 45 度
        // 0: 前(337.5 ~ 22.5), 1: 右前, 2: 右, 3: 右後, 4: 後, 5: 左後, 6: 左, 7: 左前
        // 我們將角度往後偏移 22.5 度，這樣「正前方」就會落在 0 的區間內
        float shiftedAngle = angle360 + 22.5f;
        if (shiftedAngle >= 360f) shiftedAngle -= 360f;
        
        int angleState = Mathf.FloorToInt(shiftedAngle / 45f);
        angleState = Mathf.Clamp(angleState, 0, 7);
        currentAngleState = angleState;

        // 合併為 State ID (0 ~ 31)
        currentState = angleState * 4 + distState;
        return currentState;
    }

    /// <summary>
    /// 進行動作決策
    /// </summary>
    private void MakeDecision()
    {
        if (targetCan == null) return;

        int currentState = GetCurrentState();
        int action = 0;

        if (heuristicMode)
        {
            // 手動模式
            action = GetHeuristicAction();
        }
        else
        {
            // Q-Learning Epsilon-Greedy 探索策略
            if (UnityEngine.Random.value < currentEpsilon)
            {
                // 探索：隨機選擇動作
                action = UnityEngine.Random.Range(0, ACTION_COUNT);
            }
            else
            {
                // 利用：選擇 Q 值最大的動作
                action = GetBestAction(currentState);
            }
        }

        // 執行動作物理位移與動畫
        ExecuteAction(action);

        // 計算此動作的獎勵 (Reward)
        float reward = CalculateReward();
        AddReward(reward);

        // 更新 Q-table (僅在訓練模式下)
        if (hasLastStep && !heuristicMode && isTraining)
        {
            UpdateQValue(lastState, lastAction, reward, currentState);
        }

        // 記錄當前步為「下一步」的「上一步」
        lastState = currentState;
        lastAction = action;
        hasLastStep = true;
    }

    /// <summary>
    /// 取得手動控制動作
    /// </summary>
    private int GetHeuristicAction()
    {
        // 預設為停止 (無動作) 但通常是往前走或旋轉
        // W/上方向鍵 -> 直走 (0)
        // A/左方向鍵 -> 左轉 (1)
        // D/右方向鍵 -> 右轉 (2)
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            return 0;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            return 1;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            return 2;
        }
        
        // 預設直走，讓貓咪自己動
        return 0; 
    }

    /// <summary>
    /// 尋找當前狀態下 Q 值最高的動作
    /// </summary>
    private int GetBestAction(int state)
    {
        float[] qValues = qTable[state];
        float maxQ = qValues[0];
        int bestAction = 0;

        for (int i = 1; i < qValues.Length; i++)
        {
            if (qValues[i] > maxQ)
            {
                maxQ = qValues[i];
                bestAction = i;
            }
        }
        return bestAction;
    }

    /// <summary>
    /// 執行動作對應的物理移動與動畫
    /// </summary>
    private void ExecuteAction(int action)
    {
        float moveInput = 0f;
        float turnInput = 0f;

        switch (action)
        {
            case 0: // 直走
                moveInput = 1.0f;
                turnInput = 0.0f;
                break;
            case 1: // 左轉
                moveInput = 0.2f; // 轉向時仍有微小前進，避免原地自旋死鎖
                turnInput = -1.0f;
                break;
            case 2: // 右轉
                moveInput = 0.2f;
                turnInput = 1.0f;
                break;
        }

        // 物理移動
        transform.Translate(Vector3.forward * moveInput * moveSpeed * decisionInterval);
        transform.Rotate(Vector3.up * turnInput * turnSpeed * decisionInterval);

        // 更新動畫參數
        if (catAnimator != null)
        {
            catAnimator.SetFloat("Speed", moveInput);
        }
    }

    /// <summary>
    /// 計算當前步驟的 Reward
    /// </summary>
    private float CalculateReward()
    {
        float currentDistance = Vector3.Distance(transform.position, targetCan.position);
        float reward = 0f;

        // 1. 距離改變獎勵 (Shaping Reward)
        float deltaDistance = lastDistanceToTarget - currentDistance;
        reward += deltaDistance * 2.0f; // 接近罐頭有正獎勵，遠離有負值

        // 2. 時間懲罰 (Step Penalty)
        // 驅使貓咪走捷徑，越快到達越好
        reward -= 0.05f;

        // 3. 面向罐頭的方向獎勵 (鼓勵朝著正確的方向走)
        Vector3 toTarget = (targetCan.position - transform.position).normalized;
        float alignment = Vector3.Dot(transform.forward, toTarget); // -1 (背對) 到 1 (正對)
        if (alignment > 0.8f)
        {
            reward += 0.02f; // 朝著罐頭給予微小額外獎勵
        }
        else if (alignment < -0.5f)
        {
            reward -= 0.02f; // 背對扣分
        }

        lastDistanceToTarget = currentDistance;
        return reward;
    }

    /// <summary>
    /// 更新 Q-value 公式
    /// </summary>
    private void UpdateQValue(int state, int action, float reward, int nextState)
    {
        float oldQ = qTable[state][action];
        
        // 取得 nextState 下的最大 Q 值
        float maxNextQ = qTable[nextState][0];
        for (int i = 1; i < ACTION_COUNT; i++)
        {
            if (qTable[nextState][i] > maxNextQ)
            {
                maxNextQ = qTable[nextState][i];
            }
        }

        // Q-Learning 更新公式
        float newQ = oldQ + learningRate * (reward + discountFactor * maxNextQ - oldQ);
        qTable[state][action] = newQ;
    }

    /// <summary>
    /// 累加 Reward 統計
    /// </summary>
    private void AddReward(float amount)
    {
        episodeReward += amount;
        cumulativeReward += amount;
    }

    /// <summary>
    /// 成功吃到罐頭（由 CanTrigger 觸發）
    /// </summary>
    public void ReachTarget()
    {
        // 給予高額正向獎勵
        AddReward(15.0f);
        successCount++;

        // 更新最後一步的 Q 值 (吃到罐頭的狀態，僅在訓練模式下)
        if (hasLastStep && !heuristicMode && isTraining)
        {
            UpdateQValue(lastState, lastAction, 15.0f, GetCurrentState());
        }

        // 儲存 Q-table (僅在訓練模式下，每成功 10 次存檔一次)
        if (successCount % 10 == 0 && isTraining)
        {
            SaveQTable();
        }

        // 重置回合，是否傳送貓咪回原點取決於 resetAgentOnSuccess 變數
        ResetEpisode(resetAgentOnSuccess);
    }

    #region Q-Table 存檔與讀取

    /// <summary>
    /// 包裹動作 Q 值的類別，用於解決 Unity JsonUtility 無法序列化巢狀陣列的問題
    /// </summary>
    [System.Serializable]
    private class QRow
    {
        public float[] actions;

        public QRow(float[] vals)
        {
            actions = vals;
        }
    }

    /// <summary>
    /// 將 Q-table 儲存成 JSON
    /// </summary>
    [ContextMenu("Save Q-Table")]
    public void SaveQTable()
    {
        try
        {
            QTableData data = new QTableData();
            data.keys = new List<int>();
            data.values = new List<QRow>();

            foreach (var kvp in qTable)
            {
                data.keys.Add(kvp.Key);
                data.values.Add(new QRow(kvp.Value));
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(qTableSavePath, json);
            Debug.Log($"[CatQLearning] Q-table 成功存檔至: {qTableSavePath}");

#if UNITY_EDITOR
            // 編輯器防呆：自動同步寫入到專案 Assets/Resources 資料夾中，省去用戶翻找 AppData 檔案的麻煩
            try
            {
                string resourcesDir = Path.Combine(Application.dataPath, "Resources");
                if (!Directory.Exists(resourcesDir))
                {
                    Directory.CreateDirectory(resourcesDir);
                }
                string resourcesPath = Path.Combine(resourcesDir, "cat_qtable.json");
                File.WriteAllText(resourcesPath, json);
                Debug.Log($"[CatQLearning] 編輯器自動同步：Q-table 已自動寫入專案資源目錄: {resourcesPath}");
                UnityEditor.AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CatQLearning] 編輯器自動同步失敗(不影響本地存檔): {ex.Message}");
            }
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[CatQLearning] 存檔失敗: {e.Message}");
        }
    }

    /// <summary>
    /// 從 JSON 載入 Q-table (優先讀取本地最新訓練存檔，若無則讀取 Resources 內建模型)
    /// </summary>
    [ContextMenu("Load Q-Table")]
    public void LoadQTable()
    {
        try
        {
            string json = "";

            // 1. 優先使用用戶手動拖曳至 qTableAsset 欄位的模型
            if (qTableAsset != null)
            {
                json = qTableAsset.text;
                Debug.Log($"[CatQLearning] 成功載入您手動拖入方框的模型: {qTableAsset.name}");
            }
            // 2. 次之載入本地最新訓練存檔
            else if (File.Exists(qTableSavePath))
            {
                json = File.ReadAllText(qTableSavePath);
                Debug.Log($"[CatQLearning] 偵測到本地最新訓練存檔，已成功載入: {qTableSavePath}");
            }
            // 3. 最後嘗試從 Resources 載入內建模型
            else
            {
                TextAsset builtinModel = Resources.Load<TextAsset>("cat_qtable");
                if (builtinModel != null)
                {
                    json = builtinModel.text;
                    Debug.Log("[CatQLearning] 找不到本地存檔，已成功載入內建的 Resources/cat_qtable 模型！");
                }
            }

            if (!string.IsNullOrEmpty(json))
            {
                QTableData data = JsonUtility.FromJson<QTableData>(json);

                if (data != null && data.keys != null && data.values != null)
                {
                    for (int i = 0; i < data.keys.Count; i++)
                    {
                        // 確保讀取的資料維度正確
                        if (data.values[i] != null && data.values[i].actions != null)
                        {
                            qTable[data.keys[i]] = data.values[i].actions;
                        }
                    }
                    Debug.Log($"[CatQLearning] Q-table 載入成功，共讀取 {data.keys.Count} 個狀態。");
                }
            }
            else
            {
                Debug.LogWarning("[CatQLearning] 找不到任何本地或內建的 Q-table 模型，將以預設值(0)開始訓練。");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CatQLearning] 讀取失敗: {e.Message}");
        }
    }

    [ContextMenu("Clear Q-Table & Reset Data")]
    public void ClearQTable()
    {
        InitializeQTable();
        if (File.Exists(qTableSavePath))
        {
            File.Delete(qTableSavePath);
        }
        successCount = 0;
        failCount = 0;
        currentEpisode = 0;
        cumulativeReward = 0;
        currentEpsilon = epsilon;
        Debug.Log("[CatQLearning] 已清除 Q-table 與統計數據。");
    }

    [System.Serializable]
    private class QTableData
    {
        public List<int> keys;
        public List<QRow> values;
    }

    #endregion

    /// <summary>
    /// 在 Unity 編輯器 Scene 視窗中繪製視覺化訓練資訊
    /// </summary>
    void OnDrawGizmos()
    {
        // 1. 繪製訓練區域邊界 (黃色圓圈)
        Gizmos.color = new Color(1.0f, 0.92f, 0.016f, 0.5f); // 半透明黃色
        Vector3 center = spawnPosition;
        if (!Application.isPlaying)
        {
            center = transform.position; // 編輯器未播放時，以當前貓咪位置為中心展示活動半徑
        }
        
        // 繪製圓形邊界
        int segments = 60;
        float angleStep = 360f / segments;
        Vector3 lastPoint = center + new Vector3(boundaryRadius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float rad = Mathf.Deg2Rad * (i * angleStep);
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(rad) * boundaryRadius, 0, Mathf.Sin(rad) * boundaryRadius);
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }

        // 2. 繪製貓咪指向目標罐頭的引導線
        if (Application.isPlaying && targetCan != null)
        {
            Vector3 toTarget = (targetCan.position - transform.position).normalized;
            float alignment = Vector3.Dot(transform.forward, toTarget); // 點積，-1 到 1

            // 正對目標為綠色，偏離為紅色，轉向過程中呈現過渡色
            Gizmos.color = Color.Lerp(Color.red, Color.green, (alignment + 1.0f) / 2.0f);
            
            // 繪製從貓咪上方 (高度 0.2) 到罐頭上方的引導線
            Gizmos.DrawLine(transform.position + Vector3.up * 0.2f, targetCan.position + Vector3.up * 0.2f);
            
            // 在貓咪頭頂繪製一個小球標示目標方向
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.7f, 0.1f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.7f, transform.position + Vector3.up * 0.7f + toTarget * 0.5f);

            // 繪製貓咪自身的朝向箭頭 (藍色線，代表 Forward)
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.2f, transform.forward * 1.2f);
        }
    }
}

/// <summary>
/// 罐頭碰撞觸發器
/// </summary>
public class CanTrigger : MonoBehaviour
{
    public CatQLearningAgent agent;

    private void OnTriggerEnter(Collider other)
    {
        // 判斷碰撞對象是否為貓咪 (判斷是否掛載了 Agent，或者判斷 tag)
        CatQLearningAgent catAgent = other.GetComponent<CatQLearningAgent>();
        if (catAgent == null)
        {
            // 如果貓咪的碰撞器在子物件上，則往上層尋找
            catAgent = other.GetComponentInParent<CatQLearningAgent>();
        }

        if (catAgent != null && catAgent == agent)
        {
            agent.ReachTarget();
        }
    }
}

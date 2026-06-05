using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class CatTrainingSetup : EditorWindow
{
    [MenuItem("Cat Training/Setup RL Environment")]
    public static void SetupEnvironment()
    {
        // 1. 尋找場景中的貓咪物件
        // 先尋找是否掛載了原本的 CatManualControl 腳本
        CatManualControl oldControl = Object.FindFirstObjectByType<CatManualControl>();
        GameObject catObj = null;

        if (oldControl != null)
        {
            catObj = oldControl.gameObject;
            Debug.Log($"[CatTrainingSetup] 找到掛載了 CatManualControl 的貓咪物件: {catObj.name}");
            
            // 移除舊的控制腳本
            DestroyImmediate(oldControl);
            Debug.Log("[CatTrainingSetup] 已移除舊的 CatManualControl 腳本。");
        }
        else
        {
            // 如果沒找到，嘗試用名字搜尋貓咪
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.name.ToLower().Contains("cat") || obj.name.ToLower().Contains("ladymito"))
                {
                    // 確保不是 Prefab
                    if (obj.scene.IsValid())
                    {
                        catObj = obj;
                        Debug.Log($"[CatTrainingSetup] 通過名稱找到貓咪物件: {catObj.name}");
                        break;
                    }
                }
            }
        }

        if (catObj == null)
        {
            EditorUtility.DisplayDialog("提示", "找不到場景中的貓咪物件。\n請確保場景中已有貓咪模型，且名字中包含 'cat'。", "確定");
            return;
        }

        // 2. 檢查或新增 CatQLearningAgent
        CatQLearningAgent agent = catObj.GetComponent<CatQLearningAgent>();
        if (agent == null)
        {
            agent = catObj.AddComponent<CatQLearningAgent>();
            Debug.Log("[CatTrainingSetup] 已為貓咪成功掛載 CatQLearningAgent 腳本。");
        }
        else
        {
            Debug.Log("[CatTrainingSetup] 貓咪物件上已掛載 CatQLearningAgent。");
        }

        // 3. 設定參數
        agent.heuristicMode = false;
        agent.trainingSpeedScale = 1.0f;
        agent.autoCreateCan = true;
        agent.boundaryRadius = 8.0f;

        // 4. 設定貓咪 Rigidbody
        // 貓咪如果是用 transform 移動，Rigidbody 最好設為 Is Kinematic，或者如果需要碰撞，請加上 Collider
        Rigidbody rb = catObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            Debug.Log("[CatTrainingSetup] 已將貓咪的 Rigidbody 設定為 Is Kinematic，避免重力與物理衝突影響 Q-Learning 移動。");
        }

        // 確保貓咪身上有 CharacterController 或是 Collider
        Collider catCollider = catObj.GetComponent<Collider>();
        if (catCollider == null)
        {
            catCollider = catObj.GetComponentInChildren<Collider>();
        }

        if (catCollider == null)
        {
            // 如果貓咪子物件或本體都沒有 Collider，幫它在本體加上一個 CapsuleCollider
            CapsuleCollider capCol = catObj.AddComponent<CapsuleCollider>();
            capCol.center = new Vector3(0, 0.25f, 0);
            capCol.radius = 0.25f;
            capCol.height = 0.7f;
            capCol.direction = 2; // Z-axis 朝向
            Debug.Log("[CatTrainingSetup] 貓咪身上找不到 Collider，已自動在本體新增 CapsuleCollider。");
        }

        // 5. 標記場景已修改
#if UNITY_EDITOR
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
#endif

        EditorUtility.DisplayDialog("成功", "強化學習環境已自動配置完成！\n\n1. 已將控制腳本替換為 CatQLearningAgent\n2. 貓咪已設定為 Kinematic 物理模式\n3. 已確保貓咪具有 Collider 碰撞體\n4. 點擊 Play 即可自動生成罐頭並開始訓練！", "太棒了");
    }
}

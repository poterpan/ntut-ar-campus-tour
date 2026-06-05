using UnityEngine;

public class CatManualControl : MonoBehaviour
{
    [Header("自動巡航設定")]
    public float moveSpeed = 0.5f;   // 前進速度
    public float turnSpeed = 100f; // 旋轉速度（數值越大，圈圈繞得越小越急）

    private Animator catAnimator;

    void Start()
    {
        // 自動抓取子物件（貓咪）身上的動畫控制器
        catAnimator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // ====== 【核心改動：由程式碼強制定時輸出】 ======
        
        // 1. 強制讓前進輸入永遠為 1 (代表一直全速前進)
        float moveInput = 1.0f; 
        
        // 2. 強制讓旋轉輸入永遠為 0.5 (代表方向盤一直往右打死，維持固定弧度)
        float turnInput = 0.5f; 

        // 3. 執行前進物理位移
        transform.Translate(Vector3.forward * moveInput * moveSpeed * Time.deltaTime);

        // 4. 執行旋轉物理位移
        transform.Rotate(Vector3.up * turnInput * turnSpeed * Time.deltaTime);

        // 5. 將恆定的前進數值 (1.0) 傳給動畫控制器，讓腳絕對不會停下來
        if (catAnimator != null)
        {
            catAnimator.SetFloat("Speed", moveInput);
        }
    }
}
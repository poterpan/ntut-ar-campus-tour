using UnityEngine;

namespace NtutAR.Cat
{
    /// <summary>
    /// 罐頭碰撞觸發器:貓咪(CatQLearningAgent)碰到罐頭時回報成功。
    /// 由 CatQLearningAgent 在生成/配置罐頭時自動掛載並初始化。
    /// </summary>
    public class CanTrigger : MonoBehaviour
    {
        private CatQLearningAgent _agent;

        public void Initialize(CatQLearningAgent agent)
        {
            _agent = agent;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_agent == null) return;

            // 判斷碰撞對象是否為貓咪;若貓咪的碰撞器在子物件上,則往上層尋找
            CatQLearningAgent catAgent = other.GetComponent<CatQLearningAgent>();
            if (catAgent == null)
            {
                catAgent = other.GetComponentInParent<CatQLearningAgent>();
            }

            if (catAgent != null && catAgent == _agent)
            {
                _agent.ReachTarget();
            }
        }
    }
}

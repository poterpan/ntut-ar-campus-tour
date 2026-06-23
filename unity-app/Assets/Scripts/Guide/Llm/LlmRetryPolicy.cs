using UnityEngine;
using UnityEngine.Networking;

namespace NtutAR.Guide
{
    /// <summary>
    /// LLM 請求的重試策略(純函式,方便單元測試)。
    /// 只重試「可能會自己恢復」的暫時性錯誤:限流(429)、逾時(408)、伺服器錯(5xx)、
    /// 連線層級錯誤(無 HTTP code)。401/400 這類設定錯誤不重試(重試也沒用)。
    /// </summary>
    public static class LlmRetryPolicy
    {
        public const int DefaultMaxRetries = 2;
        private const int MaxBackoffMs = 8000;

        public static bool ShouldRetry(UnityWebRequest.Result result, long responseCode)
        {
            if (result == UnityWebRequest.Result.ConnectionError ||
                result == UnityWebRequest.Result.ProtocolError)
            {
                if (responseCode == 429 || responseCode == 408) return true;   // 限流 / 逾時
                if (responseCode >= 500 && responseCode <= 599) return true;    // 伺服器錯
                if (responseCode == 0) return true;                            // 連線層級錯誤,沒有 HTTP code
                return false;                                                  // 其餘 4xx:不重試
            }
            return false;
        }

        /// <summary>指數退避:attempt 0→1s、1→2s、2→4s…,上限 8s。</summary>
        public static int BackoffMs(int attempt)
        {
            if (attempt < 0) attempt = 0;
            long ms = 1000L << attempt;
            return (int)Mathf.Min(ms, MaxBackoffMs);
        }
    }
}

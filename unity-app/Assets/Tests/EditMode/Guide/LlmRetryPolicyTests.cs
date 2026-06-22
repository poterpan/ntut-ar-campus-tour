using NUnit.Framework;
using UnityEngine.Networking;

namespace NtutAR.Guide.Tests
{
    public class LlmRetryPolicyTests
    {
        [Test]
        public void ShouldRetry_429_True()
        {
            Assert.IsTrue(LlmRetryPolicy.ShouldRetry(UnityWebRequest.Result.ProtocolError, 429));
        }

        [Test]
        public void ShouldRetry_408_True()
        {
            Assert.IsTrue(LlmRetryPolicy.ShouldRetry(UnityWebRequest.Result.ProtocolError, 408));
        }

        [Test]
        public void ShouldRetry_5xx_True()
        {
            Assert.IsTrue(LlmRetryPolicy.ShouldRetry(UnityWebRequest.Result.ProtocolError, 503));
        }

        [Test]
        public void ShouldRetry_ConnectionError_NoCode_True()
        {
            Assert.IsTrue(LlmRetryPolicy.ShouldRetry(UnityWebRequest.Result.ConnectionError, 0));
        }

        [Test]
        public void ShouldRetry_401_False()
        {
            // 認證錯誤是設定問題,重試沒用
            Assert.IsFalse(LlmRetryPolicy.ShouldRetry(UnityWebRequest.Result.ProtocolError, 401));
        }

        [Test]
        public void ShouldRetry_400_False()
        {
            Assert.IsFalse(LlmRetryPolicy.ShouldRetry(UnityWebRequest.Result.ProtocolError, 400));
        }

        [Test]
        public void ShouldRetry_Success_False()
        {
            Assert.IsFalse(LlmRetryPolicy.ShouldRetry(UnityWebRequest.Result.Success, 200));
        }

        [Test]
        public void Backoff_IsExponential_CappedAt8s()
        {
            Assert.AreEqual(1000, LlmRetryPolicy.BackoffMs(0));
            Assert.AreEqual(2000, LlmRetryPolicy.BackoffMs(1));
            Assert.AreEqual(4000, LlmRetryPolicy.BackoffMs(2));
            Assert.AreEqual(8000, LlmRetryPolicy.BackoffMs(3));
            Assert.AreEqual(8000, LlmRetryPolicy.BackoffMs(10));   // 上限
        }
    }
}

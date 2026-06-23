using NUnit.Framework;
using NtutAR.Guide.Voice;

namespace NtutAR.Guide.Tests
{
    public class ChineseTextUtilTests
    {
        [Test]
        public void Converts_CommonSimplified_ToTraditional()
        {
            Assert.AreEqual("這個學生餐廳", ChineseTextUtil.SimplifiedToTraditional("这个学生餐厅"));
            Assert.AreEqual("請問圖書館", ChineseTextUtil.SimplifiedToTraditional("请问图书馆"));
        }

        [Test]
        public void LeavesUnmappedChars_Unchanged()
        {
            // 「裡」未收錄(歧義字 里→裡/里 跳過),原樣保留;英數標點不動
            Assert.AreEqual("北科大 NTUT!", ChineseTextUtil.SimplifiedToTraditional("北科大 NTUT!"));
        }

        [Test]
        public void NullOrEmpty_ReturnsAsIs()
        {
            Assert.IsNull(ChineseTextUtil.SimplifiedToTraditional(null));
            Assert.AreEqual("", ChineseTextUtil.SimplifiedToTraditional(""));
        }
    }
}

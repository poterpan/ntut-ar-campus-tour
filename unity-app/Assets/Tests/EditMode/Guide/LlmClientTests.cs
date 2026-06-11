using NUnit.Framework;
using NtutAR.Guide;
using NtutAR.Poi;

public class LlmClientTests
{
    [Test]
    public void PromptBuilder_ContainsPoiIdNameAndContext()
    {
        var poi = new PoiContext("p01", "新生南路側門", "側門旁有腳踏車棚與警衛室。");
        string prompt = LlmPromptBuilder.Build(poi);
        StringAssert.Contains("CURRENT_POI_ID:p01", prompt);
        StringAssert.Contains("CURRENT_POI_NAME:新生南路側門", prompt);
        StringAssert.Contains("側門旁有腳踏車棚與警衛室。", prompt);
        StringAssert.Contains("繁體中文", prompt);
    }

    [Test]
    public void PromptBuilder_EmptyContext_UsesPlaceholder()
    {
        var poi = new PoiContext("p09", "未知點", "");
        string prompt = LlmPromptBuilder.Build(poi);
        StringAssert.Contains("尚無詳細資料", prompt);
    }

    [Test]
    public void RequestJson_HasModelMessagesAndMaxTokens()
    {
        string json = LlmWire.BuildRequestJson("gpt-5.5", "SYS", "你好", 500);
        StringAssert.Contains("\"model\":\"gpt-5.5\"", json);
        StringAssert.Contains("\"role\":\"system\"", json);
        StringAssert.Contains("\"content\":\"SYS\"", json);
        StringAssert.Contains("\"role\":\"user\"", json);
        StringAssert.Contains("\"max_tokens\":500", json);
    }

    [Test]
    public void ExtractAnswer_ValidResponse_ReturnsTrimmedContent()
    {
        string json = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\" 歡迎來到北科! \"}}]}";
        Assert.AreEqual("歡迎來到北科!", LlmWire.ExtractAnswer(json));
    }

    [Test]
    public void ExtractAnswer_MalformedOrEmpty_ReturnsNull()
    {
        Assert.IsNull(LlmWire.ExtractAnswer(null));
        Assert.IsNull(LlmWire.ExtractAnswer(""));
        Assert.IsNull(LlmWire.ExtractAnswer("not json"));
        Assert.IsNull(LlmWire.ExtractAnswer("{\"choices\":[]}"));
        Assert.IsNull(LlmWire.ExtractAnswer("{\"choices\":[{\"message\":{\"content\":\"\"}}]}"));
    }
}

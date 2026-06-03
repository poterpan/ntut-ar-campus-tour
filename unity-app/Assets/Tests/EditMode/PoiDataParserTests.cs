using NUnit.Framework;

namespace NtutAR.Poi.Tests
{
    public class PoiDataParserTests
    {
        [Test]
        public void Parse_ValidJson_ReturnsPois()
        {
            string json = "{\"pois\":[{\"id\":\"p01\",\"name\":\"Gate\",\"lat\":25.04,\"lng\":121.53,\"altitude\":24.0,\"anchorType\":\"Terrain\",\"shortDescription\":\"hi\",\"llmSystemPrompt\":\"ctx\"}]}";
            var result = PoiDataParser.Parse(json);
            Assert.IsFalse(result.HasError);
            Assert.AreEqual(1, result.Pois.Count);
            Assert.AreEqual("p01", result.Pois[0].id);
            Assert.AreEqual(25.04, result.Pois[0].lat, 1e-9);
        }

        [Test]
        public void Parse_EmptyString_ReturnsError()
        {
            var result = PoiDataParser.Parse("");
            Assert.IsTrue(result.HasError);
            Assert.AreEqual(0, result.Pois.Count);
        }

        [Test]
        public void Parse_MalformedJson_ReturnsErrorNoThrow()
        {
            var result = PoiDataParser.Parse("{ not json ]");
            Assert.IsTrue(result.HasError);
            Assert.AreEqual(0, result.Pois.Count);
        }

        [Test]
        public void Parse_DuplicateId_KeepsFirstAndWarns()
        {
            string json = "{\"pois\":[" +
                "{\"id\":\"p01\",\"name\":\"A\",\"lat\":1,\"lng\":1,\"shortDescription\":\"x\",\"llmSystemPrompt\":\"y\"}," +
                "{\"id\":\"p01\",\"name\":\"B\",\"lat\":2,\"lng\":2,\"shortDescription\":\"x\",\"llmSystemPrompt\":\"y\"}]}";
            var result = PoiDataParser.Parse(json);
            Assert.AreEqual(1, result.Pois.Count);
            Assert.AreEqual("A", result.Pois[0].name);
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("Duplicate")));
        }

        [Test]
        public void Parse_MissingContent_Warns()
        {
            string json = "{\"pois\":[{\"id\":\"p01\",\"name\":\"A\",\"lat\":1,\"lng\":1,\"shortDescription\":\"\",\"llmSystemPrompt\":\"\"}]}";
            var result = PoiDataParser.Parse(json);
            Assert.AreEqual(1, result.Pois.Count);
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("shortDescription")));
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("llmSystemPrompt")));
        }
    }
}

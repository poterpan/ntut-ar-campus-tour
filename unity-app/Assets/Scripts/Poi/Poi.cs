using System;

namespace NtutAR.Poi
{
    [Serializable]
    public struct Poi
    {
        public string id;
        public string name;
        public double lat;
        public double lng;
        public double altitude;
        public string anchorType;          // 字串存檔,經 AnchorType 解析
        public string shortDescription;
        public string llmSystemPrompt;

        public PoiAnchorType AnchorType =>
            Enum.TryParse(anchorType, true, out PoiAnchorType t) ? t : PoiAnchorType.Terrain;

        public PoiContext ToContext() => new PoiContext(id, name, llmSystemPrompt);
    }

    public enum PoiAnchorType { Terrain, Rooftop, Geospatial }

    public readonly struct PoiContext
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string LlmSystemPrompt;

        public PoiContext(string id, string name, string llmSystemPrompt)
        {
            Id = id;
            Name = name;
            LlmSystemPrompt = llmSystemPrompt;
        }
    }
}

using NtutAR.Poi;

namespace NtutAR.Guide
{
    /// <summary>導覽員 system prompt 組裝(純函數,可測)。規則沿用 llm-unity-test 驗證過的版本。</summary>
    public static class LlmPromptBuilder
    {
        public static string Build(PoiContext poi)
        {
            string context = string.IsNullOrWhiteSpace(poi.LlmSystemPrompt)
                ? "(目前這個景點尚無詳細資料)"
                : poi.LlmSystemPrompt;

            return
                "你是北科 AR 校園導覽助手。使用者現在站在目前 POI 附近,正在透過 AR 導覽員介面提問。\n" +
                "資料來源規則:你只能根據 CURRENT_POI_CONTEXT 回答;這份內容來自專案的 docs/POI.md。不要自行補不存在的店家、樓層、歷史或營業時間。\n" +
                "回答規則:先直接回答使用者問題。不要把整段資料照抄給使用者。若使用者問「有哪些、列出、店家、餐廳、營業時間、怎麼走」,請挑重點用最多 6 個項目整理。若 CURRENT_POI_CONTEXT 沒有資料,才說目前資料沒有寫到。\n" +
                "格式規則:使用純文字,不要使用 Markdown,不要使用 **粗體符號**。回答保持適合手機 AR 介面閱讀,通常 3 到 6 句即可。\n" +
                "語氣:繁體中文,親切自然,像校園導覽員。若問題和目前 POI 無關,可以簡短回答後提醒目前位置。\n\n" +
                $"CURRENT_POI_ID:{poi.Id}\n" +
                $"CURRENT_POI_NAME:{poi.Name}\n" +
                "CURRENT_POI_CONTEXT:\n" +
                context;
        }
    }
}

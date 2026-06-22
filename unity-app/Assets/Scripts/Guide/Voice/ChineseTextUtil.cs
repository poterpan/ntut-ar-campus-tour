using System.Collections.Generic;
using System.Text;

namespace NtutAR.Guide.Voice
{
    /// <summary>
    /// 簡→繁轉換(最佳努力)。STT(ElevenLabs scribe / GLM asr)輸出皆為簡體,
    /// 餵 LLM 無妨,但顯示在對話 UI 上希望是繁體。
    /// 為避免一簡對多繁的歧義字(发→發/髮、里→裡/里、后→後/后、面→麵/面、几→幾/几…)誤轉,
    /// 這裡只收「無歧義」的高頻常用字;沒收錄的字原樣保留(仍可讀)。
    /// 需要完整轉換可改接 OpenCC 詞庫。
    /// </summary>
    public static class ChineseTextUtil
    {
        // 每筆 = "簡繁";只收無歧義的高頻字
        private static readonly string[] Pairs =
        {
            // 結構 / 虛詞
            "这這", "个個", "们們", "么麼", "没沒", "为為", "与與", "给給", "来來", "两兩",
            "从從", "会會", "对對", "错錯", "让讓", "还還", "过過", "边邊", "内內", "点點",
            // 對話 / 動作
            "问問", "题題", "说說", "话話", "请請", "谢謝", "觉覺", "现現", "见見", "观觀",
            "视視", "听聽", "声聲", "响響", "读讀", "写寫", "语語", "课課", "学學", "习習",
            // 校園 / 導覽
            "师師", "馆館", "厅廳", "园園", "区區", "门門", "开開", "关關", "实實", "业業",
            "专專", "务務", "员員", "导導", "览覽", "团團", "图圖", "书書", "网網", "络絡",
            "电電", "脑腦", "机機", "统統", "计計", "设設", "备備", "试試", "验驗", "项項",
            "单單", "双雙", "号號", "码碼", "数數", "库庫", "类類", "选選", "择擇", "确確",
            "认認", "输輸", "转轉", "换換", "时時", "间間", "长長", "应應", "该該", "离離",
            "难難", "热熱", "爱愛", "乐樂", "车車", "东東",
            // 動物 / 名詞(校園貓彩蛋)
            "龙龍", "凤鳳", "风風", "飞飛", "马馬", "鸟鳥", "鱼魚", "贝貝", "猫貓", "饿餓", "头頭",
            // 常見姓氏(組員 / NPC)
            "杨楊", "张張", "陈陳", "刘劉", "简簡",
        };

        private static readonly Dictionary<char, char> S2T = BuildMap();

        private static Dictionary<char, char> BuildMap()
        {
            var map = new Dictionary<char, char>(Pairs.Length);
            foreach (var p in Pairs)
            {
                if (p.Length == 2 && p[0] != p[1]) map[p[0]] = p[1];
            }
            return map;
        }

        public static string SimplifiedToTraditional(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
                sb.Append(S2T.TryGetValue(c, out char t) ? t : c);
            return sb.ToString();
        }
    }
}

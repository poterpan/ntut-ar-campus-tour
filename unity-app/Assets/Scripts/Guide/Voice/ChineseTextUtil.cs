using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NtutAR.Guide.Voice
{
    /// <summary>
    /// 簡→繁(台灣正體)轉換,使用 OpenCC 對照表(Apache-2.0,內嵌於 Assets/Resources/OpenCC/)。
    /// 流程同 OpenCC s2tw:① 詞組優先最長匹配(STPhrases,解一簡多繁的語境歧義,如 头发→頭髮、
    /// 后面→後面)② 單字 fallback(STCharacters)③ 台灣字形修正(TWVariants,裏→裡、着→著)。
    /// 資料 lazy 載入一次並快取;載入失敗則原樣回傳(不影響 STT 主流程)。
    /// STT(scribe)輸出簡體,餵 LLM 用原文即可,只有顯示在 UI 上時轉繁。
    /// </summary>
    public static class ChineseTextUtil
    {
        private static Dictionary<string, string> _phrases;
        private static Dictionary<char, char> _chars;
        private static Dictionary<char, char> _tw;
        private static int _maxPhraseLen = 1;
        private static bool _loaded;

        public static string SimplifiedToTraditional(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            EnsureLoaded();
            if ((_chars == null || _chars.Count == 0) && (_phrases == null || _phrases.Count == 0))
                return text;   // 對照表載入失敗 → 不轉,原樣回傳

            // ① 詞組最長匹配 + ② 單字 fallback(簡 → 繁)
            var sb = new StringBuilder(text.Length);
            int i = 0, n = text.Length;
            while (i < n)
            {
                bool matched = false;
                int maxLen = System.Math.Min(_maxPhraseLen, n - i);
                for (int len = maxLen; len >= 2; len--)
                {
                    if (_phrases.TryGetValue(text.Substring(i, len), out var ph))
                    {
                        sb.Append(ph);
                        i += len;
                        matched = true;
                        break;
                    }
                }
                if (!matched)
                {
                    char c = text[i++];
                    sb.Append(_chars.TryGetValue(c, out var tc) ? tc : c);
                }
            }

            // ③ 台灣字形修正(generic 繁 → 台灣正體)
            if (_tw != null && _tw.Count > 0)
            {
                for (int k = 0; k < sb.Length; k++)
                    if (_tw.TryGetValue(sb[k], out var twc)) sb[k] = twc;
            }
            return sb.ToString();
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _phrases = new Dictionary<string, string>();
            _chars = new Dictionary<char, char>();
            _tw = new Dictionary<char, char>();

            LoadChars("OpenCC/STCharacters", _chars);
            LoadPhrases("OpenCC/STPhrases", _phrases);
            LoadChars("OpenCC/TWVariants", _tw);
        }

        private static string[] ReadLines(string resPath)
        {
            var ta = Resources.Load<TextAsset>(resPath);
            if (ta == null)
            {
                Debug.LogWarning($"[S2T] 找不到 OpenCC 資料:Resources/{resPath}.txt");
                return null;
            }
            string[] lines = ta.text.Split('\n');
            Resources.UnloadAsset(ta);   // 解析後釋放原始文字(STPhrases ~1MB)
            return lines;
        }

        // key 為單字的對照(STCharacters / TWVariants);value 取第一個候選
        private static void LoadChars(string resPath, Dictionary<char, char> map)
        {
            var lines = ReadLines(resPath);
            if (lines == null) return;
            foreach (var raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                int tab = line.IndexOf('\t');
                if (tab != 1) continue;                       // key 必須是單一字元
                string val = line.Substring(tab + 1);
                if (val.Length == 0) continue;
                int sp = val.IndexOf(' ');
                map[line[0]] = (sp > 0 ? val.Substring(0, sp) : val)[0];
            }
        }

        // key 為詞組(STPhrases,2+ 字);value 取第一個候選
        private static void LoadPhrases(string resPath, Dictionary<string, string> map)
        {
            var lines = ReadLines(resPath);
            if (lines == null) return;
            foreach (var raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                int tab = line.IndexOf('\t');
                if (tab < 2) continue;                        // key 至少 2 字
                string key = line.Substring(0, tab);
                string val = line.Substring(tab + 1);
                if (val.Length == 0) continue;
                int sp = val.IndexOf(' ');
                map[key] = sp > 0 ? val.Substring(0, sp) : val;
                if (key.Length > _maxPhraseLen) _maxPhraseLen = key.Length;
            }
        }
    }
}

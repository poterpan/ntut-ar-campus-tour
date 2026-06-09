using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using NtutAR.Ui;

namespace NtutAR.UiBuilder
{
    /// <summary>把 Bootstrap 場景中 ChatCanvas 下的對話面板改成暖白毛玻璃風(就地修改場景物件)。
    /// 用法:開 Bootstrap 場景後執行 menu item,然後存場景。</summary>
    internal static class ChatReskin
    {
        [MenuItem("NtutAR/Build UI/Reskin ChatCanvas (open Bootstrap first)")]
        public static void Reskin()
        {
            var chatCanvas = GameObject.Find("ChatCanvas");
            if (chatCanvas == null) { Debug.LogError("[ChatReskin] 找不到 ChatCanvas,請先開 Bootstrap"); return; }

            foreach (var img in chatCanvas.GetComponentsInChildren<Image>(true))
            {
                img.sprite = UiBuilderKit.RoundedSprite;
                img.type = Image.Type.Sliced;
                // 按鈕維持綠色、面板用玻璃白
                img.color = img.GetComponent<Button>() != null ? UiPalette.ButtonGreen : UiPalette.GlassFill;
            }
            foreach (var text in chatCanvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.font = UiBuilderKit.UiFont;
                text.color = UiPalette.TextMain;
            }
            EditorUtility.SetDirty(chatCanvas);
            Debug.Log("[ChatReskin] done — 請檢視後存檔場景");
        }
    }
}

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using NtutAR.Guide;
using NtutAR.Ui;

namespace NtutAR.UiBuilder
{
    /// <summary>對話 UI(wireframe 版):毛玻璃半版面板 + NPC 名牌 + 白底訊息泡泡 + 大字輸入列。
    /// 生成 GuideChat.prefab 與 ChatMessage.prefab,取代 PR #6 的舊 ChatCanvas。</summary>
    internal static class ChatBuilder
    {
        [MenuItem("NtutAR/Build UI/GuideChat.prefab")]
        public static void Build()
        {
            var messagePrefab = BuildMessagePrefab();

            var canvas = UiBuilderKit.MakeCanvas("GuideChat", 25);   // 高於 HUD/抽屜/手帳,低於開場(30)

            // ---- 半版面板(下半,下緣出血平底) ----
            var panel = UiBuilderKit.MakeGlassPanel(canvas.transform, "Panel");
            panel.color = new Color(1f, 0.988f, 0.961f, 0.92f);   // 比 HUD 玻璃更不透明,顧及文字可讀性
            panel.raycastTarget = true;
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0);
            panelRect.pivot = new Vector2(.5f, 0);
            panelRect.sizeDelta = new Vector2(0, 980);            // 視覺高 880 + 出血 100
            panelRect.anchoredPosition = new Vector2(0, -100);

            // ---- NPC 名牌(左上) ----
            var badge = UiBuilderKit.MakeGlassPanel(panel.transform, "NpcBadge");
            badge.sprite = UiBuilderKit.CircleSprite; badge.type = Image.Type.Simple;
            badge.color = UiPalette.ButtonGreen;
            UiBuilderKit.Place(badge, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -24), new Vector2(72, 72));
            var badgeChar = UiBuilderKit.MakeText(badge.transform, "Char", "黃", 34, Color.white);
            UiBuilderKit.Stretch((RectTransform)badgeChar.transform);
            var npcName = UiBuilderKit.MakeText(panel.transform, "NpcName", "老黃 · 校園導遊", 30, UiPalette.TextMain);
            npcName.alignment = TextAlignmentOptions.Left;
            UiBuilderKit.Place(npcName, new Vector2(0, 1), new Vector2(0, 1), new Vector2(118, -38), new Vector2(500, 44));

            // ---- 關閉鈕(右上) ----
            var closeBtn = UiBuilderKit.MakeCloseButton(panel.transform, "CloseButton", 84);
            UiBuilderKit.Place(closeBtn.GetComponent<Image>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -18), new Vector2(84, 84));

            // ---- 訊息捲動區 ----
            var scrollGo = new GameObject("Scroll", typeof(RectTransform));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRect = (RectTransform)scrollGo.transform;
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(30, 400);          // 底部讓給輸入列(含出血 100 + Home Bar 安全邊距 140)
            scrollRect.offsetMax = new Vector2(-30, -120);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scrollGo.AddComponent<RectMask2D>();
            var content = new GameObject("Messages", typeof(RectTransform));
            content.transform.SetParent(scrollGo.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 0);
            contentRect.pivot = new Vector2(.5f, 0);              // 釘在底:新訊息永遠看得到
            contentRect.sizeDelta = Vector2.zero;                  // 新建 RectTransform 預設 100x100,不歸零會比視口寬
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 16;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;       // 泡泡撐滿容器寬(否則保持 prefab 預設 100px 變細條)
            layout.childForceExpandWidth = true;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRect;
            scroll.horizontal = false;

            // ---- 輸入列 ----
            var inputBg = UiBuilderKit.MakeGlassPanel(panel.transform, "InputField", 2f);
            inputBg.color = Color.white;
            inputBg.raycastTarget = true;
            UiBuilderKit.Place(inputBg, new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 130), new Vector2(0, 100));
            var inputRect = (RectTransform)inputBg.transform;
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(1, 0);
            inputRect.offsetMin = new Vector2(30, 270);     // +140 抬離 Home Bar(避免誤觸喚醒 Siri)
            inputRect.offsetMax = new Vector2(-390, 370);   // 右側讓給 麥克風 + 送出 兩顆鈕

            var textArea = new GameObject("Text Area", typeof(RectTransform));
            textArea.transform.SetParent(inputBg.transform, false);
            var taRect = (RectTransform)textArea.transform;
            UiBuilderKit.Stretch(taRect);
            taRect.offsetMin = new Vector2(24, 8);
            taRect.offsetMax = new Vector2(-24, -8);
            textArea.AddComponent<RectMask2D>();

            var placeholder = UiBuilderKit.MakeText(textArea.transform, "Placeholder", "想問什麼呢…", 34, UiPalette.TextSub);
            placeholder.alignment = TextAlignmentOptions.Left;
            placeholder.fontStyle = FontStyles.Italic;
            UiBuilderKit.Stretch((RectTransform)placeholder.transform);

            var inputText = UiBuilderKit.MakeText(textArea.transform, "Text", "", 34, UiPalette.TextMain);
            inputText.alignment = TextAlignmentOptions.Left;
            UiBuilderKit.Stretch((RectTransform)inputText.transform);

            var input = inputBg.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = taRect;
            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.pointSize = 34;
            input.lineType = TMP_InputField.LineType.SingleLine;

            var sendBtnImg = UiBuilderKit.MakeGlassPanel(panel.transform, "SendButton", 1.9f);
            sendBtnImg.color = UiPalette.ButtonGreen;
            sendBtnImg.raycastTarget = true;
            UiBuilderKit.Place(sendBtnImg, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 270), new Vector2(200, 100));
            var sendBtn = sendBtnImg.gameObject.AddComponent<Button>();
            var sendLabel = UiBuilderKit.MakeText(sendBtnImg.transform, "Label", "送出", 34, Color.white);
            UiBuilderKit.Stretch((RectTransform)sendLabel.transform);

            // ---- 麥克風鈕(按住說話,Issue #26 STT)----
            var micBtnImg = UiBuilderKit.MakeGlassPanel(panel.transform, "MicButton", 1.9f);
            micBtnImg.color = UiPalette.ButtonGreen;
            micBtnImg.raycastTarget = true;
            UiBuilderKit.Place(micBtnImg, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-250, 270), new Vector2(130, 100));
            var micLabel = UiBuilderKit.MakeText(micBtnImg.transform, "Label", "按住\n說話", 26, Color.white);
            UiBuilderKit.Stretch((RectTransform)micLabel.transform);
            var micHold = micBtnImg.gameObject.AddComponent<MicHoldButton>();   // 按住=錄音,放開=辨識

            // ---- GuideChatPanel 接線 ----
            var chatPanel = canvas.AddComponent<GuideChatPanel>();
            UiBuilderKit.SetPrivate(chatPanel, "_root", panel.gameObject);
            UiBuilderKit.SetPrivate(chatPanel, "_input", input);
            UiBuilderKit.SetPrivate(chatPanel, "_messageContainer", contentRect);
            UiBuilderKit.SetPrivate(chatPanel, "_messagePrefab", messagePrefab);
            UiBuilderKit.SetPrivate(chatPanel, "_sendButton", sendBtn);   // Issue #21:busy 時 disable
            UiBuilderKit.SetPrivate(micHold, "_panel", chatPanel);        // Issue #26:麥克風鈕回呼面板
            UiBuilderKit.SetPrivate(micHold, "_image", micBtnImg);        // 按住回饋:改色
            UiBuilderKit.SetPrivate(micHold, "_label", micLabel);         // 按住回饋:換字
            UnityEditor.Events.UnityEventTools.AddPersistentListener(sendBtn.onClick, chatPanel.OnSendButton);
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(input.onSubmit, chatPanel.OnSendButton);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(closeBtn.onClick, chatPanel.Close);

            panel.gameObject.SetActive(false);   // 預設關閉,由 GuideInteractionController 開
            UiBuilderKit.SavePrefab(canvas.gameObject, "Assets/Prefabs/Ui/GuideChat.prefab");
        }

        /// <summary>訊息泡泡:白底圓角 + 深色文字,高度隨內容。
        /// 結構:根(空 RectTransform + VLG)/ Bg(Image,ignoreLayout)/ Text。
        /// Image 與 LayoutGroup 同物件時,Image 會用 sprite 原始尺寸(256px)參與
        /// preferred 計算,泡泡被撐成固定高;Bg 設 ignoreLayout 即可讓文字主導高度。</summary>
        private static GameObject BuildMessagePrefab()
        {
            var root = new GameObject("ChatMessage", typeof(RectTransform));
            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 16, 16);
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            var bg = UiBuilderKit.MakeGlassPanel(root.transform, "Bg", 2.4f);
            bg.color = new Color(1f, 1f, 1f, 0.95f);
            UiBuilderKit.Stretch((RectTransform)bg.transform);
            bg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var text = UiBuilderKit.MakeText(root.transform, "Text", "訊息", 30, UiPalette.TextMain);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = true;

            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Ui/ChatMessage.prefab");
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/ChatMessage.prefab");
        }
    }
}

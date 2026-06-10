using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using NtutAR.Ui;

namespace NtutAR.UiBuilder
{
    internal static class HandbookBuilder
    {
        [MenuItem("NtutAR/Build UI/Handbook.prefab")]
        public static void Build()
        {
            var canvas = UiBuilderKit.MakeCanvas("Handbook", 20);
            var group = canvas.AddComponent<CanvasGroup>();
            var safe = UiBuilderKit.MakeSafeArea(canvas);

            // 暖色全屏底
            var bg = UiBuilderKit.MakeGlassPanel(safe, "Background");
            bg.color = UiPalette.WarmBgTop;
            bg.raycastTarget = true;   // 全屏頁要擋住背後
            UiBuilderKit.Stretch((RectTransform)bg.transform);

            var title = UiBuilderKit.MakeText(bg.transform, "Title", "探索手帳", 52, UiPalette.TextMain, title: true);
            UiBuilderKit.Place(title, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -90), new Vector2(700, 70));
            var summary = UiBuilderKit.MakeText(bg.transform, "Summary", "收集 0 / 8 枚紀念章", 28, UiPalette.TextSub);
            UiBuilderKit.Place(summary, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -160), new Vector2(700, 40));

            // 章格 grid
            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(bg.transform, false);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            UiBuilderKit.Place(grid, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -220), new Vector2(960, 1100));
            grid.cellSize = new Vector2(280, 300);
            grid.spacing = new Vector2(40, 30);
            grid.childAlignment = TextAnchor.UpperCenter;

            // 章 template
            var stamp = new GameObject("StampTemplate", typeof(RectTransform));
            stamp.transform.SetParent(gridGo.transform, false);
            var fill = UiBuilderKit.MakeGlassPanel(stamp.transform, "Fill");
            fill.sprite = UiBuilderKit.Icon("StampBadge"); fill.type = Image.Type.Simple;
            fill.color = Color.white;
            fill.preserveAspect = true;
            UiBuilderKit.Place(fill, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, 0), new Vector2(220, 220));
            var locked = UiBuilderKit.MakeGlassPanel(stamp.transform, "Locked");
            locked.sprite = UiBuilderKit.CircleSprite; locked.type = Image.Type.Simple;
            locked.color = new Color(0.74f, 0.67f, 0.64f, 0.4f);
            UiBuilderKit.Place(locked, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, 0), new Vector2(220, 220));
            var lockedIcon = UiBuilderKit.MakeText(locked.transform, "Icon", "?", 90, UiPalette.TextSub);
            UiBuilderKit.Stretch((RectTransform)lockedIcon.transform);
            var stampName = UiBuilderKit.MakeText(stamp.transform, "Name", "???", 26, UiPalette.TextMain);
            UiBuilderKit.Place(stampName, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 10), new Vector2(260, 40));
            stamp.SetActive(false);

            // 餵貓計數 + 關閉鈕
            var feed = UiBuilderKit.MakeText(bg.transform, "FeedCount", "已餵食校園貓 0 次", 28, UiPalette.TextSub);
            UiBuilderKit.Place(feed, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 160), new Vector2(700, 40));
            var closeBtn = UiBuilderKit.MakeCloseButton(bg.transform, "CloseButton", 110);
            UiBuilderKit.Place(closeBtn.GetComponent<Image>(), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 40), new Vector2(110, 110));

            var panel = canvas.AddComponent<HandbookPanel>();
            UiBuilderKit.SetPrivate(panel, "_root", group);
            UiBuilderKit.SetPrivate(panel, "_grid", (RectTransform)gridGo.transform);
            UiBuilderKit.SetPrivate(panel, "_stampTemplate", (RectTransform)stamp.transform);
            UiBuilderKit.SetPrivate(panel, "_summaryText", summary);
            UiBuilderKit.SetPrivate(panel, "_feedText", feed);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(closeBtn.onClick, panel.Close);

            canvas.SetActive(false); // 預設關閉,由 UiRoot 開啟
            UiBuilderKit.SavePrefab(canvas, "Assets/Prefabs/Ui/Handbook.prefab");
        }
    }
}

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

            // 半透明暗背景(全幅出血,點擊外圍可關閉)
            var backdrop = new GameObject("Backdrop", typeof(RectTransform));
            backdrop.transform.SetParent(canvas.transform, false);
            UiBuilderKit.Stretch((RectTransform)backdrop.transform);
            var backdropImg = backdrop.AddComponent<Image>();
            backdropImg.color = new Color(0.2f, 0.14f, 0.1f, 0.45f);
            backdropImg.raycastTarget = true;
            var backdropBtn = backdrop.AddComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;

            // 置中彈出卡片(5 枚徽章不需要全螢幕 sheet)
            var bg = UiBuilderKit.MakeGlassPanel(canvas.transform, "Card");
            bg.color = UiPalette.WarmBgTop;
            bg.raycastTarget = true;
            UiBuilderKit.Place(bg, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(960, 1240));

            var title = UiBuilderKit.MakeText(bg.transform, "Title", "探索手帳", 48, UiPalette.TextMain, title: true);
            UiBuilderKit.Place(title, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -56), new Vector2(700, 64));
            var summary = UiBuilderKit.MakeText(bg.transform, "Summary", "收集 0 / 8 枚紀念章", 28, UiPalette.TextSub);
            UiBuilderKit.Place(summary, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -120), new Vector2(700, 40));

            // 章格 grid(3x2)
            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(bg.transform, false);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            UiBuilderKit.Place(grid, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -170), new Vector2(880, 700));
            grid.cellSize = new Vector2(270, 300);
            grid.spacing = new Vector2(30, 30);
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
            UiBuilderKit.Place(feed, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 150), new Vector2(700, 40));
            var closeBtn = UiBuilderKit.MakeCloseButton(bg.transform, "CloseButton", 100);
            UiBuilderKit.Place(closeBtn.GetComponent<Image>(), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 36), new Vector2(100, 100));

            var panel = canvas.AddComponent<HandbookPanel>();
            UiBuilderKit.SetPrivate(panel, "_root", group);
            UiBuilderKit.SetPrivate(panel, "_grid", (RectTransform)gridGo.transform);
            UiBuilderKit.SetPrivate(panel, "_stampTemplate", (RectTransform)stamp.transform);
            UiBuilderKit.SetPrivate(panel, "_summaryText", summary);
            UiBuilderKit.SetPrivate(panel, "_feedText", feed);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(closeBtn.onClick, panel.Close);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(backdropBtn.onClick, panel.Close);

            canvas.SetActive(false); // 預設關閉,由 UiRoot 開啟
            UiBuilderKit.SavePrefab(canvas, "Assets/Prefabs/Ui/Handbook.prefab");
        }
    }
}

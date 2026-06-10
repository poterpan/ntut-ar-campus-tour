using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using NtutAR.Ui;

namespace NtutAR.UiBuilder
{
    internal static class DrawerBuilder
    {
        [MenuItem("NtutAR/Build UI/PoiDrawer.prefab")]
        public static void Build()
        {
            var canvas = UiBuilderKit.MakeCanvas("PoiDrawer", 12);

            // 面板掛 canvas 根並橫向撐滿(出血到螢幕左右與底邊;固定寬 1080 在窄機型會超出螢幕)
            var panel = UiBuilderKit.MakeGlassPanel(canvas.transform, "Panel");
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0);
            panelRect.pivot = new Vector2(.5f, 0);
            panelRect.sizeDelta = new Vector2(0, 1040);
            panelRect.anchoredPosition = new Vector2(0, -980); // 初始收合(closedY)

            var grabber = UiBuilderKit.MakeGlassPanel(panel.transform, "Grabber");
            grabber.color = UiPalette.TextSub;
            UiBuilderKit.Place(grabber, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -16), new Vector2(96, 10));

            // 頂部整條為可點擊的收合區(打開後 HUD 把手被蓋住,收合靠這裡)
            var grabZone = new GameObject("GrabZone", typeof(RectTransform));
            grabZone.transform.SetParent(panel.transform, false);
            var grabRect = (RectTransform)grabZone.transform;
            grabRect.anchorMin = new Vector2(0, 1);
            grabRect.anchorMax = new Vector2(1, 1);
            grabRect.pivot = new Vector2(.5f, 1);
            grabRect.sizeDelta = new Vector2(0, 110);
            grabRect.anchoredPosition = Vector2.zero;
            var grabImg = grabZone.AddComponent<UnityEngine.UI.Image>();
            grabImg.color = Color.clear; // 透明點擊區
            var grabBtn = grabZone.AddComponent<UnityEngine.UI.Button>();
            var title = UiBuilderKit.MakeText(panel.transform, "Title", "校園景點", 36, UiPalette.TextMain, title: true);
            UiBuilderKit.Place(title, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -64), new Vector2(600, 50));

            // Scroll list
            var scrollGo = new GameObject("Scroll", typeof(RectTransform));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRect = (RectTransform)scrollGo.transform;
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(30, 30);
            scrollRect.offsetMax = new Vector2(-30, -120);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scrollGo.AddComponent<RectMask2D>();
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(scrollGo.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(.5f, 1);
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRect;
            scroll.horizontal = false;

            // Item template
            var item = UiBuilderKit.MakeGlassPanel(content.transform, "ItemTemplate");
            item.color = UiPalette.CardWhite;
            ((RectTransform)item.transform).sizeDelta = new Vector2(0, 120);
            item.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;
            var iconBg = UiBuilderKit.MakeGlassPanel(item.transform, "IconBg");
            iconBg.sprite = UiBuilderKit.CircleSprite; iconBg.type = Image.Type.Simple;
            iconBg.color = UiPalette.ButtonGreen;
            UiBuilderKit.Place(iconBg, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(24, 0), new Vector2(80, 80));
            var icon = UiBuilderKit.MakeText(iconBg.transform, "Icon", "館", 38, Color.white);
            UiBuilderKit.Stretch((RectTransform)icon.transform);
            var nameT = UiBuilderKit.MakeText(item.transform, "Name", "景點名", 32, UiPalette.TextMain);
            nameT.alignment = TextAlignmentOptions.Left;
            UiBuilderKit.Place(nameT, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(124, 22), new Vector2(700, 44));
            var subT = UiBuilderKit.MakeText(item.transform, "Sub", "-- · 未探索", 24, UiPalette.TextSub);
            subT.alignment = TextAlignmentOptions.Left;
            UiBuilderKit.Place(subT, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(124, -26), new Vector2(700, 36));
            item.gameObject.SetActive(false);

            var drawer = canvas.AddComponent<PoiDrawerPanel>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(grabBtn.onClick, drawer.ToggleCached);
            UiBuilderKit.SetPrivate(drawer, "_panel", panelRect);
            UiBuilderKit.SetPrivate(drawer, "_listRoot", contentRect);
            UiBuilderKit.SetPrivate(drawer, "_itemTemplate", (RectTransform)item.transform);
            UiBuilderKit.SetPrivate(drawer, "_titleText", title);

            UiBuilderKit.SavePrefab(canvas, "Assets/Prefabs/Ui/PoiDrawer.prefab");
        }
    }
}

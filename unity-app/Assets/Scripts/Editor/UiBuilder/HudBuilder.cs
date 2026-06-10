using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using NtutAR.Ui;

namespace NtutAR.UiBuilder
{
    internal static class HudBuilder
    {
        [MenuItem("NtutAR/Build UI/ArHud.prefab")]
        public static void Build()
        {
            var canvas = UiBuilderKit.MakeCanvas("ArHud", 10);
            var hudGroup = canvas.AddComponent<CanvasGroup>(); // 開場期間由 UiRoot 隱藏
            var safe = UiBuilderKit.MakeSafeArea(canvas);
            var hud = canvas.AddComponent<HudController>();

            // ---- 玩家狀態列(左上) ----
            var status = UiBuilderKit.MakeGlassPanel(safe, "PlayerStatus", 2f);
            UiBuilderKit.Place(status, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(340, 110));
            var avatar = UiBuilderKit.MakeCircularPhoto(status.transform, "Avatar", UiBuilderKit.Icon("AvatarCat"), 86);
            UiBuilderKit.Place(avatar, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(86, 86));
            var nick = UiBuilderKit.MakeText(status.transform, "Nickname", "探索者", 34, UiPalette.TextMain);
            nick.alignment = TMPro.TextAlignmentOptions.Left;
            UiBuilderKit.Place(nick, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(112, 18), new Vector2(220, 44));
            var progress = UiBuilderKit.MakeText(status.transform, "Progress", "探索 0/8 個景點", 24, UiPalette.TextSub);
            progress.alignment = TMPro.TextAlignmentOptions.Left;
            UiBuilderKit.Place(progress, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(112, -22), new Vector2(220, 34));

            // ---- 小地圖(右上,圓形) ----
            var mapBg = UiBuilderKit.MakeGlassPanel(safe, "Minimap");
            mapBg.sprite = UiBuilderKit.CircleSprite; mapBg.type = Image.Type.Simple;
            UiBuilderKit.Place(mapBg, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(220, 220));
            var mapInner = UiBuilderKit.MakeGlassPanel(mapBg.transform, "MapImage");
            mapInner.sprite = UiBuilderKit.CircleSprite; mapInner.type = Image.Type.Simple;
            mapInner.color = new Color32(0xCF, 0xE3, 0xB8, 0xFF); // 草地綠佔位底圖,之後換校園插畫
            UiBuilderKit.Stretch((RectTransform)mapInner.transform);
            ((RectTransform)mapInner.transform).offsetMin = new Vector2(10, 10);
            ((RectTransform)mapInner.transform).offsetMax = new Vector2(-10, -10);
            var dotLayer = new GameObject("Dots", typeof(RectTransform));
            dotLayer.transform.SetParent(mapInner.transform, false);
            UiBuilderKit.Stretch((RectTransform)dotLayer.transform);
            var playerDot = UiBuilderKit.MakeGlassPanel(dotLayer.transform, "PlayerDot");
            playerDot.sprite = UiBuilderKit.CircleSprite; playerDot.type = Image.Type.Simple;
            playerDot.color = Color.white;
            UiBuilderKit.Place(playerDot, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(26, 26));
            var poiDot = UiBuilderKit.MakeGlassPanel(dotLayer.transform, "PoiDotTemplate");
            poiDot.sprite = UiBuilderKit.CircleSprite; poiDot.type = Image.Type.Simple;
            poiDot.color = new Color32(0xE5, 0x73, 0x73, 0xFF);
            UiBuilderKit.Place(poiDot, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(20, 20));
            poiDot.gameObject.SetActive(false);
            var minimap = mapBg.gameObject.AddComponent<MinimapView>();
            UiBuilderKit.SetPrivate(minimap, "_dotLayer", (RectTransform)dotLayer.transform);
            UiBuilderKit.SetPrivate(minimap, "_playerDot", (RectTransform)playerDot.transform);

            // ---- 接近提示橫幅(上中) ----
            var banner = UiBuilderKit.MakeGlassPanel(safe, "ProximityBanner", 2.6f);
            UiBuilderKit.Place(banner, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -160), new Vector2(560, 76));
            var bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            var bannerText = UiBuilderKit.MakeText(banner.transform, "Text", "紅樓在前方 35m", 30, UiPalette.TextMain);
            UiBuilderKit.Stretch((RectTransform)bannerText.transform);

            // ---- 蓋章 Toast(中央偏上) ----
            var toast = UiBuilderKit.MakeGlassPanel(safe, "StampToast", 2.1f);
            UiBuilderKit.Place(toast, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 200), new Vector2(640, 96));
            var toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            var toastText = UiBuilderKit.MakeText(toast.transform, "Text", "收集到紀念章!", 32, UiPalette.AccentOrange);
            UiBuilderKit.Stretch((RectTransform)toastText.transform);

            // ---- 罐頭按鈕(下中)+ 圖鑑按鈕(右側),全彩 icon + 玻璃圓底 ----
            var canBtn = UiBuilderKit.MakeIconButton(safe, "CanButton", UiBuilderKit.Icon("CatCanIcon"), 190, UiPalette.GlassFill);
            UiBuilderKit.Place(canBtn.GetComponent<Image>(), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 210), new Vector2(190, 190));
            var bookBtn = UiBuilderKit.MakeIconButton(safe, "HandbookButton", UiBuilderKit.Icon("HandbookIcon"), 130, UiPalette.GlassFill);
            UiBuilderKit.Place(bookBtn.GetComponent<Image>(), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(255, 245), new Vector2(130, 130));

            // ---- 放置提示(罐頭模式用,接 CatSummonController 的 hint 欄位) ----
            var hint = UiBuilderKit.MakeGlassPanel(safe, "PlacementHint", 2.8f);
            UiBuilderKit.Place(hint, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -260), new Vector2(520, 70));
            var hintText = UiBuilderKit.MakeText(hint.transform, "Text", "點擊地面放置罐頭", 30, UiPalette.TextMain);
            UiBuilderKit.Stretch((RectTransform)hintText.transform);
            hint.gameObject.SetActive(false);

            // ---- 抽屜把手(底部,橫向撐滿;掛 canvas 根全幅出血,下緣圓角推出螢幕外 = 平底,與抽屜一致) ----
            var handle = UiBuilderKit.MakeGlassPanel(canvas.transform, "DrawerHandle", 1.5f);
            var handleRect = (RectTransform)handle.transform;
            handleRect.anchorMin = new Vector2(0, 0);
            handleRect.anchorMax = new Vector2(1, 0);
            handleRect.pivot = new Vector2(.5f, 0);
            handleRect.sizeDelta = new Vector2(0, 240);          // 下方 100 推出螢幕外
            handleRect.anchoredPosition = new Vector2(0, -100);
            handle.gameObject.AddComponent<Button>();
            var handleText = UiBuilderKit.MakeText(handle.transform, "Text", "︿ 校園景點", 28, UiPalette.TextSub);
            var handleTextRect = (RectTransform)handleText.transform;
            UiBuilderKit.Stretch(handleTextRect);
            handleTextRect.offsetMin = new Vector2(0, 100);      // 文字置中於可見的上半段

            // ---- 貓召喚 controller 掛同物件,接 HUD 的按鈕/提示 ----
            var summon = canvas.AddComponent<NtutAR.Cat.CatSummonController>();
            UiBuilderKit.SetPrivate(summon, "_catPrefab",
                AssetDatabase.LoadAssetAtPath<NtutAR.Cat.CatQLearningAgent>("Assets/Prefabs/CatAgent.prefab"));
            UiBuilderKit.SetPrivate(summon, "_canPrefab",
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CatCan.prefab"));
            UiBuilderKit.SetPrivate(summon, "_summonButton", canBtn);
            UiBuilderKit.SetPrivate(summon, "_placementHint", hint.gameObject);

            // ---- HudController 接線(_poiService 為場景物件,prefab 留空,場景整合時接) ----
            UiBuilderKit.SetPrivate(hud, "_minimap", minimap);
            UiBuilderKit.SetPrivate(hud, "_poiDotTemplate", (RectTransform)poiDot.transform);
            UiBuilderKit.SetPrivate(hud, "_banner", bannerGroup);
            UiBuilderKit.SetPrivate(hud, "_bannerText", bannerText);
            UiBuilderKit.SetPrivate(hud, "_progressText", progress);
            UiBuilderKit.SetPrivate(hud, "_stampToast", toastGroup);
            UiBuilderKit.SetPrivate(hud, "_stampToastText", toastText);

            bannerGroup.alpha = 0f;
            toastGroup.alpha = 0f;

            UiBuilderKit.SavePrefab(canvas, "Assets/Prefabs/Ui/ArHud.prefab");
        }
    }
}

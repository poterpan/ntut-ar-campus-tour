using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using NtutAR.Ui;

namespace NtutAR.UiBuilder
{
    internal static class OnboardingBuilder
    {
        [MenuItem("NtutAR/Build UI/Onboarding.prefab")]
        public static void Build()
        {
            var canvas = UiBuilderKit.MakeCanvas("Onboarding", 30);
            // 開場各步直接掛 canvas 根(全幅出血,蓋住瀏海與 Home bar 區);內容皆置中,不需 SafeArea
            var safe = (RectTransform)canvas.transform;

            // Step 1: Splash(暖色全屏)
            var splash = MakeStep(safe, "Splash", true);
            var logo = UiBuilderKit.MakeCircularPhoto(splash.transform, "Logo", UiBuilderKit.Icon("AvatarCat"), 240);
            UiBuilderKit.Place(logo, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 200), new Vector2(240, 240));
            var appName = UiBuilderKit.MakeText(splash.transform, "AppName", "北科 AR 校園導覽", 56, UiPalette.TextMain, title: true);
            UiBuilderKit.Place(appName, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 10), new Vector2(800, 80));
            var tagline = UiBuilderKit.MakeText(splash.transform, "Tagline", "邊逛邊聊,還有貓", 30, UiPalette.TextSub);
            UiBuilderKit.Place(tagline, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -60), new Vector2(600, 44));

            // Step 2: 權限說明
            var permission = MakeStep(safe, "Permission", true);
            var permTitle = UiBuilderKit.MakeText(permission.transform, "Title", "需要相機與定位權限", 44, UiPalette.TextMain, title: true);
            UiBuilderKit.Place(permTitle, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 160), new Vector2(800, 60));
            var permBody = UiBuilderKit.MakeText(permission.transform, "Body",
                "AR 導覽需要透過相機辨識周遭環境,\n並用 GPS 找到你在校園的位置", 30, UiPalette.TextSub);
            UiBuilderKit.Place(permBody, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 40), new Vector2(800, 100));
            var startBtnImg = UiBuilderKit.MakeGlassPanel(permission.transform, "StartButton", 1.8f);
            startBtnImg.color = UiPalette.ButtonGreen;
            UiBuilderKit.Place(startBtnImg, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -140), new Vector2(460, 110));
            var startBtn = startBtnImg.gameObject.AddComponent<Button>();
            var startLabel = UiBuilderKit.MakeText(startBtnImg.transform, "Label", "好,開始吧", 36, Color.white);
            UiBuilderKit.Stretch((RectTransform)startLabel.transform);

            // Step 3: 尋找位置中(半透明卡,底下露出相機)
            var locating = MakeStep(safe, "Locating", false);
            var locCard = UiBuilderKit.MakeGlassPanel(locating.transform, "Card");
            UiBuilderKit.Place(locCard, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 0), new Vector2(720, 260));
            var locTitle = UiBuilderKit.MakeText(locCard.transform, "Title", "正在尋找你的位置…", 36, UiPalette.TextMain);
            UiBuilderKit.Place(locTitle, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 24), new Vector2(640, 50));
            var locHint = UiBuilderKit.MakeText(locCard.transform, "Hint", "對著周圍建築物緩慢環視", 26, UiPalette.TextSub);
            UiBuilderKit.Place(locHint, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -36), new Vector2(640, 40));

            // Step 4: 完成
            var done = MakeStep(safe, "Done", false);
            var doneCard = UiBuilderKit.MakeGlassPanel(done.transform, "Card");
            UiBuilderKit.Place(doneCard, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 0), new Vector2(640, 200));
            var doneTitle = UiBuilderKit.MakeText(doneCard.transform, "Title", "定位完成!", 40, UiPalette.TextMain);
            UiBuilderKit.Place(doneTitle, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 24), new Vector2(600, 56));
            var doneSub = UiBuilderKit.MakeText(doneCard.transform, "Sub", "歡迎來到北科大", 28, UiPalette.TextSub);
            UiBuilderKit.Place(doneSub, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -34), new Vector2(600, 40));

            var ctrl = canvas.AddComponent<OnboardingController>();
            UiBuilderKit.SetPrivate(ctrl, "_splash", splash.GetComponent<CanvasGroup>());
            UiBuilderKit.SetPrivate(ctrl, "_permission", permission.GetComponent<CanvasGroup>());
            UiBuilderKit.SetPrivate(ctrl, "_locating", locating.GetComponent<CanvasGroup>());
            UiBuilderKit.SetPrivate(ctrl, "_done", done.GetComponent<CanvasGroup>());
            UiBuilderKit.SetPrivate(ctrl, "_startButton", startBtn);

            UiBuilderKit.SavePrefab(canvas, "Assets/Prefabs/Ui/Onboarding.prefab");
        }

        /// <summary>fullBg=true 時鋪滿暖色背景(Splash/Permission);false 為透明層(疊相機)。</summary>
        private static GameObject MakeStep(RectTransform safe, string name, bool fullBg)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(safe, false);
            UiBuilderKit.Stretch((RectTransform)go.transform);
            go.AddComponent<CanvasGroup>();
            if (fullBg)
            {
                var bg = UiBuilderKit.MakeGlassPanel(go.transform, "Bg");
                bg.color = UiPalette.WarmBgTop;
                UiBuilderKit.Stretch((RectTransform)bg.transform);
                bg.transform.SetAsFirstSibling();
            }
            return go;
        }
    }
}

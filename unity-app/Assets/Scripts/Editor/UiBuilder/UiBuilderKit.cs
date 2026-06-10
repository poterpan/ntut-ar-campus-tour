using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using NtutAR.Ui;

namespace NtutAR.UiBuilder
{
    /// <summary>UI prefab 程序化生成的共用工廠。佔位視覺:內建 UISprite 圓角九宮格 + 暖白半透明(假毛玻璃)。</summary>
    internal static class UiBuilderKit
    {
        public const string UiFontPath = "Assets/Art/Fonts/JfOpenHuninn SDF.asset";
        public const string TitleFontPath = "Assets/Art/Fonts/NotoSerifCJKtc SDF.asset";

        public static TMP_FontAsset UiFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontPath);
        public static TMP_FontAsset TitleFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath);

        // 程序化高解析 sprite(內建 UISprite/Knob 只有 ~64px,放大會糊且圓角太小)
        public static Sprite RoundedSprite =>
            GetOrCreateSprite("Assets/Art/Ui/RoundedRect256.png", GenRoundedRect, new Vector4(96, 96, 96, 96));
        public static Sprite CircleSprite =>
            GetOrCreateSprite("Assets/Art/Ui/Circle512.png", GenCircle, Vector4.zero);

        private static Sprite GetOrCreateSprite(string path, System.Func<Texture2D> gen, Vector4 border)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;

            if (AssetImporter.GetAtPath(path) == null)
            {
                string dir = System.IO.Path.GetDirectoryName(path);
                if (!AssetDatabase.IsValidFolder(dir))
                    AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(dir), System.IO.Path.GetFileName(dir));

                var tex = gen();
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            // 已存在但載不到 Sprite = importer 設定不對(曾發生 spriteMode 落在 Multiple),一律矯正
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spriteBorder = border;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();

            sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null) Debug.LogError($"[UiBuilder] sprite 仍載入失敗: {path}");
            return sp;
        }

        /// <summary>256x256 圓角矩形(半徑 96,SDF 反鋸齒),9-slice border 96。</summary>
        private static Texture2D GenRoundedRect()
        {
            const int size = 256;
            const float radius = 96f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var half = Vector2.one * (size / 2f);
                var d = new Vector2(Mathf.Abs(p.x - half.x), Mathf.Abs(p.y - half.y)) - (half - Vector2.one * radius);
                float dist = new Vector2(Mathf.Max(d.x, 0), Mathf.Max(d.y, 0)).magnitude
                             + Mathf.Min(Mathf.Max(d.x, d.y), 0) - radius;
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - dist)));
            }
            tex.Apply();
            return tex;
        }

        /// <summary>載入 Assets/Art/Ui/Icons/ 下的 icon,必要時矯正 importer 為 Single Sprite。</summary>
        public static Sprite Icon(string name)
        {
            string path = $"Assets/Art/Ui/Icons/{name}.png";
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;

            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti == null) { Debug.LogError($"[UiBuilder] 缺 icon: {path}"); return null; }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>圓形玻璃底 + 透明 icon 的按鈕。</summary>
        public static Button MakeIconButton(Transform parent, string name, Sprite icon, float diameter, Color bgFill)
        {
            var img = MakeGlassPanel(parent, name);
            img.sprite = CircleSprite;
            img.type = Image.Type.Simple;
            img.color = bgFill;
            img.raycastTarget = true;    // 按鈕需要接收點擊
            ((RectTransform)img.transform).sizeDelta = new Vector2(diameter, diameter);
            var btn = img.gameObject.AddComponent<Button>();

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(img.transform, false);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            var rect = (RectTransform)iconGo.transform;
            Stretch(rect);
            float pad = diameter * 0.14f;
            rect.offsetMin = new Vector2(pad, pad);
            rect.offsetMax = new Vector2(-pad, -pad);
            return btn;
        }

        /// <summary>關閉鈕:綠圓 + 白色叉叉(兩條旋轉細槓,不依賴字型字形)。</summary>
        public static Button MakeCloseButton(Transform parent, string name, float diameter)
        {
            var img = MakeGlassPanel(parent, name);
            img.sprite = CircleSprite;
            img.type = Image.Type.Simple;
            img.color = UiPalette.ButtonGreen;
            img.raycastTarget = true;
            ((RectTransform)img.transform).sizeDelta = new Vector2(diameter, diameter);
            var btn = img.gameObject.AddComponent<Button>();

            for (int i = 0; i < 2; i++)
            {
                var bar = new GameObject(i == 0 ? "BarA" : "BarB", typeof(RectTransform));
                bar.transform.SetParent(img.transform, false);
                var barImg = bar.AddComponent<Image>();
                barImg.color = Color.white;
                barImg.raycastTarget = false;
                var rect = (RectTransform)bar.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(diameter * 0.46f, diameter * 0.07f);
                rect.localRotation = Quaternion.Euler(0, 0, i == 0 ? 45 : -45);
            }
            return btn;
        }

        /// <summary>圓形遮罩照片(頭像/Logo):圓 sprite 當 Mask,子物件放方形照片。</summary>
        public static Image MakeCircularPhoto(Transform parent, string name, Sprite photo, float diameter)
        {
            var circle = MakeGlassPanel(parent, name);
            circle.sprite = CircleSprite;
            circle.type = Image.Type.Simple;
            circle.color = Color.white;
            ((RectTransform)circle.transform).sizeDelta = new Vector2(diameter, diameter);
            var mask = circle.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            var photoGo = new GameObject("Photo", typeof(RectTransform));
            photoGo.transform.SetParent(circle.transform, false);
            var photoImg = photoGo.AddComponent<Image>();
            photoImg.sprite = photo;
            photoImg.raycastTarget = false;
            Stretch((RectTransform)photoGo.transform);
            return circle;
        }

        /// <summary>512x512 反鋸齒實心圓。</summary>
        private static Texture2D GenCircle()
        {
            const int size = 512;
            float radius = size / 2f - 1.5f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var c = Vector2.one * (size / 2f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) - radius;
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - dist)));
            }
            tex.Apply();
            return tex;
        }

        public static GameObject MakeCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        /// <summary>Canvas 下的 SafeArea 容器,所有 HUD 元件掛這層之下。</summary>
        public static RectTransform MakeSafeArea(GameObject canvas)
        {
            var go = new GameObject("SafeArea", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SafeAreaFitter>();
            return rect;
        }

        /// <param name="cornerScale">圓角縮放:1 = 96px 大圓角(大面板),2 = 48px(小元件;corner 大於半高會被擠壓變尖)</param>
        public static Image MakeGlassPanel(Transform parent, string name, float cornerScale = 1f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = cornerScale;
            img.color = UiPalette.GlassFill;
            // 預設不擋點擊(裝飾性面板;隱形的橫幅/toast 曾擋住 AR 點擊)。
            // 互動元件(按鈕/需要擋住背後的全屏面板)由呼叫端顯式開回 true。
            img.raycastTarget = false;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.365f, 0.251f, 0.216f, 0.18f); // #5D4037 @18%
            shadow.effectDistance = new Vector2(0, -3);
            return img;
        }

        public static TextMeshProUGUI MakeText(Transform parent, string name, string text,
            float size, Color color, bool title = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = title ? TitleFont : UiFont;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;   // 文字一律不擋點擊
            return tmp;
        }

        public static Button MakeRoundButton(Transform parent, string name, string label,
            float diameter, Color fill)
        {
            var img = MakeGlassPanel(parent, name);
            img.sprite = CircleSprite;
            img.type = Image.Type.Simple;
            img.color = fill;
            img.raycastTarget = true;    // 按鈕需要接收點擊
            var rect = (RectTransform)img.transform;
            rect.sizeDelta = new Vector2(diameter, diameter);
            var btn = img.gameObject.AddComponent<Button>();
            var text = MakeText(img.transform, "Label", label, diameter * 0.36f, Color.white);
            Stretch((RectTransform)text.transform);
            return btn;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static RectTransform Place(Component c, Vector2 anchor, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            var rect = (RectTransform)c.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            return rect;
        }

        public static void SetPrivate(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetPrivateFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SavePrefab(GameObject root, string path)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Ui"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Ui");
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UiBuilder] saved {path}");
        }
    }
}

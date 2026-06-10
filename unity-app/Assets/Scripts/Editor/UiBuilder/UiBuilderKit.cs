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

        public static Image MakeGlassPanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
            img.color = UiPalette.GlassFill;
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
            return tmp;
        }

        public static Button MakeRoundButton(Transform parent, string name, string label,
            float diameter, Color fill)
        {
            var img = MakeGlassPanel(parent, name);
            img.sprite = CircleSprite;
            img.type = Image.Type.Simple;
            img.color = fill;
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

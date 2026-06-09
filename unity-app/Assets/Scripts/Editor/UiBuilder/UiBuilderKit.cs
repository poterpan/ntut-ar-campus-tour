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

        public static Sprite RoundedSprite =>
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        public static Sprite CircleSprite =>
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

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

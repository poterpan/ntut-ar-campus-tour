using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ScreenSpaceGuidePresenter : MonoBehaviour
{
    private const string GuidePrefabName = "FreeSample_male_1";
    private const int GuideLayer = 31;

    [SerializeField] private GameObject _guidePrefab;
    [SerializeField] private Vector2 _anchoredPosition = new(-70f, 280f);
    [SerializeField] private Vector2 _displaySize = new(300f, 500f);
    [SerializeField] private float _modelScale = 1f;
    [SerializeField] private Vector3 _modelLocalPosition = new(0f, 0f, 2.4f);
    [SerializeField] private Vector3 _modelLocalRotation = new(0f, 180f, 0f);
    [SerializeField] private float _cameraPadding = 0.96f;
    [SerializeField] private Color _transparentBackground = new(0f, 0f, 0f, 0f);

    private Canvas _parentCanvas;
    private Camera _overlayCamera;
    private GameObject _guideInstance;
    private RawImage _guideImage;
    private RenderTexture _renderTexture;
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    public static ScreenSpaceGuidePresenter EnsurePresenter(Canvas parentCanvas)
    {
        ScreenSpaceGuidePresenter presenter = FindAnyObjectByType<ScreenSpaceGuidePresenter>();
        if (presenter == null)
        {
            GameObject presenterObject = new("Screen Space Guide Presenter");
            presenter = presenterObject.AddComponent<ScreenSpaceGuidePresenter>();
            DontDestroyOnLoad(presenterObject);
        }

        presenter.SetParentCanvas(parentCanvas);
        return presenter;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreatePresenterOnPlay()
    {
        if (FindAnyObjectByType<ScreenSpaceGuidePresenter>() != null)
        {
            return;
        }

        GameObject presenter = new("Screen Space Guide Presenter");
        presenter.AddComponent<ScreenSpaceGuidePresenter>();
        DontDestroyOnLoad(presenter);
    }

    private void Awake()
    {
        CreateRenderTexture();
        CreateOverlayImage();
        CreateOverlayCamera();
        ResolveGuideInstance();
        PositionGuide();
    }

    private void SetParentCanvas(Canvas parentCanvas)
    {
        if (parentCanvas == null)
        {
            return;
        }

        _parentCanvas = parentCanvas;
        _parentCanvas.sortingOrder = Mathf.Max(_parentCanvas.sortingOrder, 10);

        if (_guideImage == null)
        {
            CreateOverlayImage();
            return;
        }

        _guideImage.transform.SetParent(_parentCanvas.transform, false);
        _guideImage.transform.SetAsLastSibling();
        ApplyImageLayout();
    }

    private void LateUpdate()
    {
        if (_guideImage == null)
        {
            CreateOverlayImage();
        }

        ApplyResponsiveLayoutIfNeeded();

        if (_overlayCamera == null)
        {
            CreateOverlayCamera();
        }

        ExcludeGuideLayerFromSceneCameras();

        if (_guideInstance == null)
        {
            ResolveGuideInstance();
        }

        PositionGuide();
    }

    private void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
        }
    }

    private void CreateRenderTexture()
    {
        if (_renderTexture != null)
        {
            return;
        }

        _renderTexture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32)
        {
            name = "Guide Overlay Render Texture"
        };
        _renderTexture.Create();
    }

    private void CreateOverlayImage()
    {
        Canvas canvas = _parentCanvas != null ? _parentCanvas : FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new("Guide Overlay Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject imageObject = new("Guide Overlay Image");
        imageObject.transform.SetParent(canvas.transform, false);
        imageObject.transform.SetAsLastSibling();

        _guideImage = imageObject.AddComponent<RawImage>();
        _guideImage.texture = _renderTexture;
        _guideImage.raycastTarget = false;
        _guideImage.color = Color.white;

        ApplyImageLayout();
    }

    private void ApplyImageLayout()
    {
        if (_guideImage == null)
        {
            return;
        }

        RectTransform rectTransform = _guideImage.rectTransform;
        rectTransform.anchorMin = new Vector2(1f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        ApplyResponsiveLayout(force: true);
    }

    private void ApplyResponsiveLayoutIfNeeded()
    {
        if (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight)
        {
            return;
        }

        ApplyResponsiveLayout(force: false);
    }

    private void ApplyResponsiveLayout(bool force)
    {
        if (_guideImage == null)
        {
            return;
        }

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        RectTransform rectTransform = _guideImage.rectTransform;
        rectTransform.anchoredPosition = _anchoredPosition;
        rectTransform.sizeDelta = _displaySize;
    }

    private void CreateOverlayCamera()
    {
        GameObject cameraObject = new("Guide Overlay Camera");
        DontDestroyOnLoad(cameraObject);

        _overlayCamera = cameraObject.AddComponent<Camera>();
        _overlayCamera.clearFlags = CameraClearFlags.SolidColor;
        _overlayCamera.backgroundColor = _transparentBackground;
        _overlayCamera.cullingMask = 1 << GuideLayer;
        _overlayCamera.orthographic = true;
        _overlayCamera.orthographicSize = 1.05f;
        _overlayCamera.nearClipPlane = 0.01f;
        _overlayCamera.farClipPlane = 20f;
        _overlayCamera.targetTexture = _renderTexture;
        _overlayCamera.enabled = true;
    }

    private void ResolveGuideInstance()
    {
        if (IsAttachedToGuide())
        {
            _guideInstance = gameObject;
        }

        if (_guideInstance == null)
        {
            _guideInstance = FindSceneGuide();
        }

        if (_guideInstance == null)
        {
            _guideInstance = SpawnGuide();
        }

        if (_guideInstance == null)
        {
            Debug.LogWarning($"{nameof(ScreenSpaceGuidePresenter)} could not find {GuidePrefabName} in the scene or project.");
            return;
        }

        DontDestroyOnLoad(_guideInstance);
        SetLayerRecursively(_guideInstance, GuideLayer);
        ExcludeGuideLayerFromSceneCameras();
        FixMaterials(_guideInstance);
        SetOverlayFriendly(_guideInstance);
    }

    private void PositionGuide()
    {
        if (_guideInstance == null || _overlayCamera == null)
        {
            return;
        }

        Transform guideTransform = _guideInstance.transform;
        guideTransform.SetParent(_overlayCamera.transform, false);
        guideTransform.localPosition = _modelLocalPosition;
        guideTransform.localRotation = Quaternion.Euler(_modelLocalRotation);
        guideTransform.localScale = Vector3.one * _modelScale;

        FrameGuideInCamera(guideTransform);
    }

    private static GameObject FindSceneGuide()
    {
        GameObject exactMatch = GameObject.Find(GuidePrefabName);
        if (exactMatch != null)
        {
            return exactMatch;
        }

        foreach (Transform transform in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (transform.name.StartsWith(GuidePrefabName))
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private bool IsAttachedToGuide()
    {
        return gameObject.name.StartsWith(GuidePrefabName);
    }

    private GameObject SpawnGuide()
    {
        if (_guidePrefab == null)
        {
            _guidePrefab = LoadGuidePrefab();
        }

        if (_guidePrefab == null)
        {
            return null;
        }

        GameObject instance = Instantiate(_guidePrefab);
        instance.name = GuidePrefabName;
        return instance;
    }

    private static GameObject LoadGuidePrefab()
    {
        GameObject prefab = Resources.Load<GameObject>(GuidePrefabName);
        if (prefab != null)
        {
            return prefab;
        }

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets($"{GuidePrefabName} t:Prefab");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
#endif

        return null;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void SetOverlayFriendly(GameObject guide)
    {
        foreach (Collider collider in guide.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

    }

    private void FrameGuideInCamera(Transform guideTransform)
    {
        Renderer[] renderers = guideTransform.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 cameraLocalCenter = _overlayCamera.transform.InverseTransformPoint(bounds.center);
        guideTransform.localPosition += new Vector3(-cameraLocalCenter.x, -cameraLocalCenter.y, 0f);

        float aspect = _displaySize.x / Mathf.Max(1f, _displaySize.y);
        float widthSize = bounds.extents.x / Mathf.Max(0.01f, aspect);
        _overlayCamera.orthographicSize = Mathf.Max(bounds.extents.y, widthSize, 0.5f) * _cameraPadding;
    }

    private void ExcludeGuideLayerFromSceneCameras()
    {
        int guideMask = 1 << GuideLayer;
        foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (camera == _overlayCamera)
            {
                continue;
            }

            camera.cullingMask &= ~guideMask;
        }
    }

    private static void FixMaterials(GameObject guide)
    {
        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            return;
        }

        foreach (Renderer renderer in guide.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = CreateCompatibleMaterial(materials[i], shader, renderer.name);
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static Material CreateCompatibleMaterial(Material source, Shader shader, string rendererName)
    {
        Material material = new(shader)
        {
            name = source != null ? $"{source.name}_Overlay" : $"{rendererName}_Overlay"
        };

        Texture texture = null;
        if (source != null)
        {
            if (source.HasProperty("_MainTex"))
            {
                texture = source.GetTexture("_MainTex");
            }

            if (texture == null && source.HasProperty("_BaseMap"))
            {
                texture = source.GetTexture("_BaseMap");
            }
        }

        if (texture != null)
        {
            material.mainTexture = texture;
        }

        Color color = PickFallbackColor(source, rendererName);
        if (material.HasProperty("_Color"))
        {
            material.color = color;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        return material;
    }

    private static Color PickFallbackColor(Material source, string rendererName)
    {
        if (source != null)
        {
            if (source.HasProperty("_Color"))
            {
                return source.GetColor("_Color");
            }

            if (source.HasProperty("_BaseColor"))
            {
                return source.GetColor("_BaseColor");
            }
        }

        string name = rendererName.ToLowerInvariant();
        if (name.Contains("hair"))
        {
            return new Color(0.16f, 0.10f, 0.07f);
        }

        if (name.Contains("body") || name.Contains("face"))
        {
            return new Color(1.0f, 0.76f, 0.62f);
        }

        if (name.Contains("bottom"))
        {
            return new Color(0.12f, 0.25f, 0.45f);
        }

        return new Color(0.98f, 0.72f, 0.32f);
    }
}

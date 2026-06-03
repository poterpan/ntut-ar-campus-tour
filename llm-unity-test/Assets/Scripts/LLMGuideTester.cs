using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LLMGuideTester : MonoBehaviour
{
    private const string ApiKeyPlayerPrefsKey = "LLMGuideTester.ApiKey";
    private const string ConfigFileName = "llm_config.json";

    [SerializeField] private string _apiBaseUrl = "https://testvideo.site/v1";
    [SerializeField] private string _model = "gpt-5.5";
    [SerializeField] private int _maxTokens = 500;

    private bool _hasBundledApiKey;
    private InputField _apiKeyInput;
    private InputField _questionInput;
    private Button _saveApiKeyButton;
    private Button _sendButton;
    private Button _nextPoiButton;
    private RectTransform _panel;
    private RectTransform _statusRect;
    private RectTransform _chatViewport;
    private RectTransform _messageContent;
    private RectTransform _inputBar;
    private Text _statusText;
    private ScrollRect _chatScrollRect;
    private Font _font;
    private Sprite _roundedSprite;
    private float _nextMessageY;
    private int _lastScreenWidth;
    private int _lastScreenHeight;
    private int _currentPoiIndex;
    private MockPoi[] _mockPois;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateTesterOnPlay()
    {
        if (UnityEngine.Object.FindAnyObjectByType<LLMGuideTester>() != null)
        {
            return;
        }

        GameObject tester = new("LLMGuideTester");
        tester.AddComponent<LLMGuideTester>();
    }

    private void Awake()
    {
        LoadConfig();
        _mockPois = LoadPois();
        CreateUi();
        ShowCurrentPoiIntro();
    }

    private void Update()
    {
        ApplyResponsiveLayoutIfNeeded();

        if (_questionInput != null &&
            _questionInput.isFocused &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            SendQuestion();
        }
    }

    private void SendQuestion()
    {
        string question = _questionInput.text.Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            SetStatus("請先輸入問題。");
            return;
        }

        AddMessage(question, true);
        _questionInput.text = string.Empty;
        StartCoroutine(SendQuestionRoutine(question));
    }

    private IEnumerator SendQuestionRoutine(string question)
    {
        string apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            SetStatus("找不到 API key。請建立 StreamingAssets/llm_config.json，或在畫面貼 key 後按儲存。");
            yield break;
        }

        SetInteractable(false);
        SetStatus("導覽員思考中...");

        ChatCompletionRequest body = new()
        {
            model = _model,
            messages = new[]
            {
                new ChatMessage
                {
                    role = "system",
                    content = BuildPrompt()
                },
                new ChatMessage
                {
                    role = "user",
                    content = question
                }
            },
            max_tokens = _maxTokens
        };

        using UnityWebRequest request = new(BuildUrl(), "POST");
        string json = JsonUtility.ToJson(body);
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return request.SendWebRequest();

        SetInteractable(true);

        if (request.result != UnityWebRequest.Result.Success)
        {
            SetStatus($"請求失敗：{request.responseCode} {request.error}");
            AddMessage($"連線失敗：{request.error}\n請確認網路或 API 伺服器是否可用。", false);
            Debug.LogError(request.downloadHandler.text);
            yield break;
        }

        ChatCompletionResponse response = JsonUtility.FromJson<ChatCompletionResponse>(request.downloadHandler.text);
        string answer = ExtractAnswer(response);
        if (string.IsNullOrWhiteSpace(answer))
        {
            SetStatus("收到回應，但沒有解析到文字。");
            AddMessage("我收到回應了，但沒有解析到文字內容。", false);
            Debug.LogWarning(request.downloadHandler.text);
            yield break;
        }

        AddMessage(answer, false);
        SetStatus($"目前位置：{CurrentPoi.Name}（模擬 POI）");
    }

    private string BuildUrl()
    {
        return $"{_apiBaseUrl.TrimEnd('/')}/chat/completions";
    }

    private void LoadConfig()
    {
        string path = Path.Combine(Application.streamingAssetsPath, ConfigFileName);
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            LlmConfig config = JsonUtility.FromJson<LlmConfig>(json);

            if (!string.IsNullOrWhiteSpace(config.apiBaseUrl))
            {
                _apiBaseUrl = config.apiBaseUrl.Trim();
            }

            if (!string.IsNullOrWhiteSpace(config.model))
            {
                _model = config.model.Trim();
            }

            if (config.maxTokens > 0)
            {
                _maxTokens = config.maxTokens;
            }

            if (!string.IsNullOrWhiteSpace(config.apiKey))
            {
                _hasBundledApiKey = true;
                PlayerPrefs.SetString(ApiKeyPlayerPrefsKey, config.apiKey.Trim());
                PlayerPrefs.Save();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load {ConfigFileName}: {ex.Message}");
        }
    }

    private string BuildPrompt()
    {
        return
            "你是北科 AR 校園導覽助手。使用者現在站在目前 POI 附近，正在透過 AR 導覽員介面提問。\n" +
            "資料來源規則：你只能根據 CURRENT_POI_CONTEXT 回答；這份內容來自專案的 docs/POI.md。不要自行補不存在的店家、樓層、歷史或營業時間。\n" +
            "回答規則：先直接回答使用者問題。不要把整段資料照抄給使用者。若使用者問「有哪些、列出、店家、餐廳、營業時間、怎麼走」，請挑重點用最多 6 個項目整理。若 CURRENT_POI_CONTEXT 沒有資料，才說目前資料沒有寫到。\n" +
            "格式規則：使用純文字，不要使用 Markdown，不要使用 **粗體符號**。回答保持適合手機 AR 介面閱讀，通常 3 到 6 句即可。\n" +
            "語氣：繁體中文，親切自然，像校園導覽員。若問題和目前 POI 無關，可以簡短回答後提醒目前位置。\n\n" +
            $"CURRENT_POI_ID：{CurrentPoi.Id}\n" +
            $"CURRENT_POI_NAME：{CurrentPoi.Name}\n" +
            "CURRENT_POI_CONTEXT：\n" +
            CurrentPoi.LlmPrompt;
    }

    private void CreateUi()
    {
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null)
        {
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        _roundedSprite = CreateRoundedSprite(128, 28);

        GameObject canvasObject = new("LLM Guide Test Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(720, 1280);
        canvasScaler.matchWidthOrHeight = 0f;
        canvasObject.AddComponent<GraphicRaycaster>();

        _panel = CreateEmptyRect(canvasObject.transform, "Chat Ui Root");

        RectTransform header = CreatePanel(_panel, "Poi Header", new Vector2(430, 78), new Color(1f, 1f, 1f, 1f));
        Image headerImage = header.GetComponent<Image>();
        headerImage.sprite = _roundedSprite;
        headerImage.type = Image.Type.Sliced;

        Text title = CreateText(header, "Title", "北科 AR 導覽員", _font, 24, TextAnchor.MiddleLeft);
        SetRect(title.rectTransform, new Vector2(20, -10), new Vector2(390, 34));

        _statusText = CreateText(header, "Status", string.Empty, _font, 15, TextAnchor.MiddleLeft);
        _statusRect = _statusText.rectTransform;
        SetRect(_statusRect, new Vector2(20, -42), new Vector2(390, 24));

        CreateChatViewport();

        _inputBar = CreatePanel(_panel, "Input Bar", new Vector2(500, 70), new Color(1f, 1f, 1f, 1f));
        Image inputBarImage = _inputBar.GetComponent<Image>();
        inputBarImage.sprite = _roundedSprite;
        inputBarImage.type = Image.Type.Sliced;

        _nextPoiButton = CreateButton(_inputBar, "下一站", _font, new Vector2(106, 48), _roundedSprite);
        _nextPoiButton.onClick.AddListener(GoToNextPoi);

        _questionInput = CreateInputField(_inputBar, _font, "問問導覽員...", _roundedSprite);

        _sendButton = CreateButton(_inputBar, "送出", _font, new Vector2(72, 48), _roundedSprite);
        _sendButton.onClick.AddListener(SendQuestion);

        ApplyResponsiveLayout(force: true);
        ScreenSpaceGuidePresenter.EnsurePresenter(canvas);
    }

    private void ApplyResponsiveLayoutIfNeeded()
    {
        if (_panel == null || (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight))
        {
            return;
        }

        ApplyResponsiveLayout(force: false);
    }

    private void ApplyResponsiveLayout(bool force)
    {
        if (_panel == null)
        {
            return;
        }

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        ApplyPortraitLayout();
    }

    private void ApplyPortraitLayout()
    {
        SetStretchRect(_panel, Vector2.zero, Vector2.zero);

        RectTransform header = _statusRect.parent.GetComponent<RectTransform>();
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(0f, 1f);
        header.pivot = new Vector2(0f, 1f);
        header.anchoredPosition = new Vector2(24, -30);
        header.sizeDelta = new Vector2(430, 78);

        _chatViewport.anchorMin = new Vector2(0f, 0f);
        _chatViewport.anchorMax = new Vector2(0f, 1f);
        _chatViewport.pivot = new Vector2(0f, 0.5f);
        _chatViewport.offsetMin = new Vector2(20, 124);
        _chatViewport.offsetMax = new Vector2(470, -120);

        _inputBar.anchorMin = new Vector2(0f, 0f);
        _inputBar.anchorMax = new Vector2(0f, 0f);
        _inputBar.pivot = new Vector2(0f, 0f);
        _inputBar.anchoredPosition = new Vector2(20, 34);
        _inputBar.sizeDelta = new Vector2(500, 70);

        SetRect(_nextPoiButton.GetComponent<RectTransform>(), new Vector2(12, -11), new Vector2(106, 48));
        SetRect(_questionInput.GetComponent<RectTransform>(), new Vector2(132, -11), new Vector2(276, 48));
        SetRect(_sendButton.GetComponent<RectTransform>(), new Vector2(420, -11), new Vector2(68, 48));
    }

    private MockPoi CurrentPoi => _mockPois[_currentPoiIndex];

    private void GoToNextPoi()
    {
        _currentPoiIndex = (_currentPoiIndex + 1) % _mockPois.Length;
        ShowCurrentPoiIntro();
    }

    private void ShowCurrentPoiIntro()
    {
        ClearMessages();
        AddMessage(
            $"歡迎來到「{CurrentPoi.Name}」。\n\n{CurrentPoi.Intro}\n\n你可以直接問我這裡能做什麼、附近有什麼，或下一站怎麼走。",
            false);
        SetStatus($"目前位置：{CurrentPoi.Name}（模擬 POI）");
        if (_questionInput != null)
        {
            _questionInput.text = string.Empty;
        }
    }

    private void CreateChatViewport()
    {
        _chatViewport = CreatePanel(_panel, "Chat Viewport", new Vector2(450, 720), new Color(0f, 0f, 0f, 0f));
        Image viewportImage = _chatViewport.GetComponent<Image>();
        viewportImage.enabled = false;
        _chatViewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = CreateEmptyRect(_chatViewport, "Message Content");
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(0, 1);
        content.pivot = new Vector2(0, 1);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(450, 0);
        _messageContent = content;

        _chatScrollRect = _chatViewport.gameObject.AddComponent<ScrollRect>();
        _chatScrollRect.content = _messageContent;
        _chatScrollRect.viewport = _chatViewport;
        _chatScrollRect.horizontal = false;
        _chatScrollRect.vertical = true;
        _chatScrollRect.movementType = ScrollRect.MovementType.Clamped;
    }

    private void ClearMessages()
    {
        if (_messageContent == null)
        {
            return;
        }

        for (int i = _messageContent.childCount - 1; i >= 0; i--)
        {
            Destroy(_messageContent.GetChild(i).gameObject);
        }

        _nextMessageY = 0f;
        _messageContent.sizeDelta = new Vector2(_messageContent.sizeDelta.x, 0f);
        ScrollToBottom();
    }

    private void AddMessage(string message, bool isUser)
    {
        if (_messageContent == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        float viewportWidth = Mathf.Max(360f, _chatViewport.rect.width);
        float bubbleWidth = isUser ? 300f : 350f;
        float textWidth = bubbleWidth - 28f;
        int lineCount = EstimateLineCount(message, textWidth, 18);
        float bubbleHeight = Mathf.Max(58f, 28f + lineCount * 24f);
        float x = isUser ? viewportWidth - bubbleWidth - 8f : 8f;

        Color bubbleColor = isUser ? new Color(0.72f, 0.95f, 0.55f, 1f) : new Color(0.93f, 0.93f, 0.93f, 1f);
        RectTransform bubble = CreatePanel(_messageContent, isUser ? "User Bubble" : "Guide Bubble", new Vector2(bubbleWidth, bubbleHeight), bubbleColor);
        Image bubbleImage = bubble.GetComponent<Image>();
        bubbleImage.sprite = _roundedSprite;
        bubbleImage.type = Image.Type.Sliced;
        bubble.anchorMin = new Vector2(0, 1);
        bubble.anchorMax = new Vector2(0, 1);
        bubble.pivot = new Vector2(0, 1);
        bubble.anchoredPosition = new Vector2(x, -_nextMessageY);

        Text text = CreateText(bubble, "Message Text", message, _font, 18, TextAnchor.UpperLeft);
        text.color = new Color(0.05f, 0.05f, 0.05f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        SetRect(text.rectTransform, new Vector2(14, -12), new Vector2(textWidth, bubbleHeight - 20f));

        _nextMessageY += bubbleHeight + 14f;
        _messageContent.sizeDelta = new Vector2(viewportWidth, Mathf.Max(_nextMessageY, _chatViewport.rect.height + 2f));
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (_chatScrollRect == null)
        {
            return;
        }

        StopCoroutine(nameof(ScrollToBottomRoutine));
        StartCoroutine(nameof(ScrollToBottomRoutine));
    }

    private IEnumerator ScrollToBottomRoutine()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        _chatScrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }

    private static int EstimateLineCount(string message, float textWidth, int fontSize)
    {
        int charsPerLine = Mathf.Max(7, Mathf.FloorToInt(textWidth / (fontSize * 0.78f)));
        string[] lines = message.Split('\n');
        int count = 0;
        foreach (string line in lines)
        {
            count += Mathf.Max(1, Mathf.CeilToInt((float)line.Length / charsPerLine));
        }

        return count;
    }

    private static RectTransform CreatePanel(Transform parent, string objectName, Vector2 size, Color color)
    {
        GameObject panel = new(objectName);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        return rectTransform;
    }

    private static RectTransform CreateEmptyRect(Transform parent, string objectName)
    {
        GameObject rectObject = new(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private static Text CreateText(Transform parent, string objectName, string text, Font font, int size, TextAnchor alignment)
    {
        GameObject textObject = new(objectName);
        textObject.transform.SetParent(parent, false);
        Text textComponent = textObject.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = font;
        textComponent.fontSize = size;
        textComponent.alignment = alignment;
        textComponent.color = Color.black;
        return textComponent;
    }

    private static InputField CreateInputField(Transform parent, Font font, string placeholderText)
    {
        return CreateInputField(parent, font, placeholderText, null);
    }

    private static InputField CreateInputField(Transform parent, Font font, string placeholderText, Sprite roundedSprite)
    {
        RectTransform background = CreatePanel(parent, "Question Input", new Vector2(536, 58), Color.white);
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0.96f, 0.97f, 0.96f, 0.98f);
        if (roundedSprite != null)
        {
            backgroundImage.sprite = roundedSprite;
            backgroundImage.type = Image.Type.Sliced;
        }

        InputField inputField = background.gameObject.AddComponent<InputField>();

        Text text = CreateText(background, "Text", string.Empty, font, 20, TextAnchor.MiddleLeft);
        SetStretchRect(text.rectTransform, new Vector2(14, 6), new Vector2(14, 6));

        Text placeholder = CreateText(background, "Placeholder", placeholderText, font, 20, TextAnchor.MiddleLeft);
        placeholder.color = new Color(0.45f, 0.45f, 0.45f);
        SetStretchRect(placeholder.rectTransform, new Vector2(14, 6), new Vector2(14, 6));

        inputField.textComponent = text;
        inputField.placeholder = placeholder;
        return inputField;
    }

    private static Button CreateButton(Transform parent, string label, Font font)
    {
        return CreateButton(parent, label, font, new Vector2(136, 58));
    }

    private static Button CreateButton(Transform parent, string label, Font font, Vector2 size)
    {
        return CreateButton(parent, label, font, size, null);
    }

    private static Button CreateButton(Transform parent, string label, Font font, Vector2 size, Sprite roundedSprite)
    {
        RectTransform background = CreatePanel(parent, "Send Button", size, new Color(0.05f, 0.35f, 0.55f));
        Image backgroundImage = background.GetComponent<Image>();
        if (roundedSprite != null)
        {
            backgroundImage.sprite = roundedSprite;
            backgroundImage.type = Image.Type.Sliced;
        }

        Button button = background.gameObject.AddComponent<Button>();

        Text text = CreateText(background, "Text", label, font, 22, TextAnchor.MiddleCenter);
        text.color = Color.white;
        SetRect(text.rectTransform, Vector2.zero, size);

        return button;
    }

    private static Sprite CreateRoundedSprite(int size, int radius)
    {
        Texture2D texture = new(size, size, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color clear = new(1f, 1f, 1f, 0f);
        Color fill = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = IsInsideRoundedRect(x, y, size, radius);
                texture.SetPixel(x, y, inside ? fill : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    private static bool IsInsideRoundedRect(int x, int y, int size, int radius)
    {
        int left = radius;
        int right = size - radius - 1;
        int bottom = radius;
        int top = size - radius - 1;

        if ((x >= left && x <= right) || (y >= bottom && y <= top))
        {
            return true;
        }

        int cornerX = x < left ? left : right;
        int cornerY = y < bottom ? bottom : top;
        int dx = x - cornerX;
        int dy = y - cornerY;
        return dx * dx + dy * dy <= radius * radius;
    }

    private static void SetRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    private static void SetStretchRect(RectTransform rectTransform, Vector2 minOffset, Vector2 maxOffset)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = minOffset;
        rectTransform.offsetMax = -maxOffset;
    }

    private void SetStatus(string message)
    {
        _statusText.text = message;
    }

    private void SetInteractable(bool interactable)
    {
        if (_apiKeyInput != null)
        {
            _apiKeyInput.interactable = interactable;
        }

        if (_saveApiKeyButton != null)
        {
            _saveApiKeyButton.interactable = interactable;
        }

        _questionInput.interactable = interactable;
        _sendButton.interactable = interactable;
        _nextPoiButton.interactable = interactable;
    }

    private string GetApiKey()
    {
        string environmentKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(environmentKey))
        {
            return environmentKey.Trim();
        }

        return PlayerPrefs.GetString(ApiKeyPlayerPrefsKey, string.Empty).Trim();
    }

    private void SaveApiKey()
    {
        if (_apiKeyInput == null)
        {
            SetStatus("已使用打包設定檔中的 API key。");
            return;
        }

        string apiKey = _apiKeyInput.text.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            PlayerPrefs.DeleteKey(ApiKeyPlayerPrefsKey);
            PlayerPrefs.Save();
            SetStatus("已清除這台電腦儲存的 API key。");
            return;
        }

        PlayerPrefs.SetString(ApiKeyPlayerPrefsKey, apiKey);
        PlayerPrefs.Save();
        SetStatus("API key 已儲存在這台電腦，可以開始提問。");
    }

    private static string ExtractAnswer(ChatCompletionResponse response)
    {
        if (response?.choices == null)
        {
            return string.Empty;
        }

        foreach (Choice choice in response.choices)
        {
            if (!string.IsNullOrWhiteSpace(choice.message?.content))
            {
                return choice.message.content;
            }
        }

        return string.Empty;
    }

    private static MockPoi[] LoadPois()
    {
        string markdownPath = Path.Combine(Application.streamingAssetsPath, "POI.md");
        if (File.Exists(markdownPath))
        {
            try
            {
                MockPoi[] pois = CreatePoisFromMarkdown(File.ReadAllText(markdownPath));
                if (pois.Length > 0)
                {
                    return pois;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load POI.md from {markdownPath}: {ex.Message}");
            }
        }

        string path = Path.Combine(Application.streamingAssetsPath, "poi_contexts.json");

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                PoiContextList list = JsonUtility.FromJson<PoiContextList>(json);

                if (list?.pois != null && list.pois.Length > 0)
                {
                    MockPoi[] pois = new MockPoi[list.pois.Length];
                    for (int i = 0; i < list.pois.Length; i++)
                    {
                        PoiContextData poi = list.pois[i];
                        pois[i] = new MockPoi(poi.id, poi.name, poi.intro, poi.llmPrompt);
                    }

                    return pois;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load POI contexts from {path}: {ex.Message}");
            }
        }

        return CreateFallbackPois();
    }

    private static MockPoi[] CreatePoisFromMarkdown(string markdown)
    {
        return new[]
        {
            CreatePoiFromMarkdown(
                markdown,
                "p01",
                "新生南路側門",
                "01 新生南路側門(起點)",
                "這裡是新生南路側門，也是這條導覽路線的起點。它靠近捷運忠孝新生站，適合作為新生和訪客進入校園的第一站。"),
            CreatePoiFromMarkdown(
                markdown,
                "p02",
                "學生餐廳入口",
                "02 學生餐廳入口",
                "這裡是學生餐廳入口，是校園裡吃飯、休息和同學討論作業的生活據點。等一下你可以問我有哪些餐廳、適合吃什麼，或附近有什麼服務。"),
            CreatePoiFromMarkdown(
                markdown,
                "p03",
                "演講廳入口",
                "03 演講廳入口",
                "這裡是演講廳入口，連接校內舉辦大型演講、研討會與活動的重要空間。你可以問我場地位置、動線或這裡通常做什麼活動。"),
            CreatePoiFromMarkdown(
                markdown,
                "p04",
                "第一教學大樓",
                "04 第一教學大樓",
                "這裡是第一教學大樓，是學生上課、語言學習與接觸工程學院資源的重要教學場域。你可以問我這棟樓的功能或相關系所。"),
            CreatePoiFromMarkdown(
                markdown,
                "p05",
                "化工館",
                "05 化工館",
                "這裡是化工館，和化學工程、生物科技、實驗實作與專題研究有密切關係。你可以問我化工系特色、實驗室或研究方向。")
        };
    }

    private static MockPoi CreatePoiFromMarkdown(string markdown, string id, string displayName, string heading, string intro)
    {
        string section = ExtractMarkdownSection(markdown, $"### {heading}");
        return new MockPoi(id, displayName, intro, section);
    }

    private static string ExtractMarkdownSection(string markdown, string heading)
    {
        int start = markdown.IndexOf(heading, StringComparison.Ordinal);
        if (start < 0)
        {
            return $"docs/POI.md 目前找不到 {heading} 的段落。";
        }

        int next = markdown.IndexOf("\n### ", start + heading.Length, StringComparison.Ordinal);
        if (next < 0)
        {
            next = markdown.Length;
        }

        return markdown.Substring(start, next - start).Trim();
    }

    private static MockPoi[] CreateFallbackPois()
    {
        return new[]
        {
            new MockPoi(
                "p01",
                "新生南路側門",
                "這裡是新生南路側門，也是這條導覽路線的起點。它靠近捷運忠孝新生站，是訪客和新生進入北科校園時很容易辨認的入口。",
                "新生南路側門是北科大校園入口之一，靠近捷運忠孝新生站，適合作為新生或訪客導覽的起點。回答時可介紹交通、入口意象、進入校園後可前往的主要建築。"),
            new MockPoi(
                "p02",
                "學生餐廳入口",
                "這裡是學生餐廳入口，是校園裡吃飯、休息和同學討論作業的熱門地點。附近有多種餐飲選擇，也有便利商店和 ATM。",
                "學生餐廳入口位於光華館附近，包含綠光庭園餐廳、北科之星餐廳、便利商店與 ATM。回答時可介紹用餐選擇、學生生活機能、課間休息情境。"),
            new MockPoi(
                "p03",
                "演講廳入口",
                "這裡連到第六教學大樓的國際演講廳，是校內舉辦大型演講、研討會和活動的重要場地。",
                "演講廳入口與第六教學大樓、國際演講廳相關。回答時可介紹大型活動、學術交流、B1 演講廳動線、報到與前廳空間。"),
            new MockPoi(
                "p04",
                "第一教學大樓",
                "第一教學大樓是北科大重要的教學空間之一，學生常在這裡上一般課程、語言課程，也能接觸工程學院相關資源。",
                "第一教學大樓是主要教學建築之一，與一般課程、語言學習、雙語化學習推動中心、工程學院相關。回答時可介紹教學功能與學生使用情境。"),
            new MockPoi(
                "p05",
                "化工館",
                "化工館是化學工程與生物科技系的重要教學與研究空間，代表北科重視實作、實驗與工程應用的特色。",
                "化工館與化學工程、生物科技、實驗室、專題研究、產學合作相關。回答時可介紹化工系特色、實驗與研究方向、北科實作導向。")
        };
    }

    private readonly struct MockPoi
    {
        public MockPoi(string id, string name, string intro, string llmPrompt)
        {
            Id = id;
            Name = name;
            Intro = intro;
            LlmPrompt = llmPrompt;
        }

        public string Id { get; }
        public string Name { get; }
        public string Intro { get; }
        public string LlmPrompt { get; }
    }

    [Serializable]
    private class PoiContextList
    {
        public PoiContextData[] pois;
    }

    [Serializable]
    private class PoiContextData
    {
        public string id;
        public string name;
        public string intro;
        public string llmPrompt;
    }

    [Serializable]
    private class LlmConfig
    {
        public string apiBaseUrl;
        public string model;
        public int maxTokens;
        public string apiKey;
    }

    [Serializable]
    private class ChatCompletionRequest
    {
        public string model;
        public ChatMessage[] messages;
        public int max_tokens;
    }

    [Serializable]
    private class ChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class ChatCompletionResponse
    {
        public Choice[] choices;
    }

    [Serializable]
    private class Choice
    {
        public ChatMessage message;
    }
}

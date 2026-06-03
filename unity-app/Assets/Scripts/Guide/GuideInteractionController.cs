using UnityEngine;
using NtutAR.Poi;

namespace NtutAR.Guide
{
    public sealed class GuideInteractionController : MonoBehaviour
    {
        [Header("資料/服務(可拖 mock 或真實實作)")]
        [SerializeField] private PoiService _poiService;
        [SerializeField] private MonoBehaviour _llmBehaviour;     // ILlmClient
        [SerializeField] private MonoBehaviour _ttsBehaviour;     // ITtsService
        [SerializeField] private MonoBehaviour _anchorBehaviour;  // IPoiAnchorProvider

        [Header("場景物件")]
        [SerializeField] private GameObject _npcPrefab;
        [SerializeField] private GuideChatPanel _panel;
        [SerializeField] private Camera _arCamera;

        [Header("Phase 1 桌面 debug")]
        [SerializeField] private bool _useDebugPoi = true;
        [SerializeField] private string _debugPoiId = "p01";

        private GuideChatController _chat;
        private NpcAnimator _npcAnimator;
        private GameObject _npcInstance;
        private NtutAR.Poi.Poi _activePoi;

        private void Awake()
        {
            var llm = _llmBehaviour as ILlmClient;
            var tts = _ttsBehaviour as ITtsService;
            _chat = new GuideChatController(llm, tts);
            _chat.NpcStateChanged += OnNpcState;
            _chat.GuideMessageReady += text => _panel.AppendMessage("導遊", text);
            _panel.Sent += question => _ = _chat.AskAsync(question);
            _panel.Close();
        }

        private void Start()
        {
            // Phase 1 桌面:直接顯示指定 POI 的 NPC(真實 geo-proximity 留待 Phase 2)
            if (_useDebugPoi && _poiService != null && _poiService.TryGetById(_debugPoiId, out var poi))
                ShowNpc(poi);
        }

        private void Update()
        {
            if (_npcInstance == null) return;
            if (!TryGetTapRay(out var ray)) return;
            if (Physics.Raycast(ray, out var hit) && hit.collider.transform.IsChildOf(_npcInstance.transform))
                OpenChat();
        }

        private void ShowNpc(NtutAR.Poi.Poi poi)
        {
            _activePoi = poi;
            var anchor = (_anchorBehaviour as IPoiAnchorProvider)?.GetAnchor(poi.id);
            if (anchor == null)
            {
                Debug.LogWarning($"[Guide] POI '{poi.id}' 無 anchor,NPC 不顯示。");
                return;
            }
            _npcInstance = Instantiate(_npcPrefab, anchor.position, anchor.rotation);
            _npcAnimator = _npcInstance.GetComponentInChildren<NpcAnimator>();
        }

        private void OpenChat()
        {
            _panel.Open();
            _npcAnimator?.PlayGreet();
            _ = _chat.StartSessionAsync(_activePoi);
        }

        private void OnNpcState(NpcState state) => _npcAnimator?.OnNpcState(state);

        private bool TryGetTapRay(out Ray ray)
        {
            ray = default;
            if (_arCamera == null) return false;
            if (Input.GetMouseButtonDown(0))
            {
                ray = _arCamera.ScreenPointToRay(Input.mousePosition);
                return true;
            }
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                ray = _arCamera.ScreenPointToRay(Input.GetTouch(0).position);
                return true;
            }
            return false;
        }
    }
}

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
            // RaycastAll:室內測試時 AR 平面(桌面等)的 collider 會擋在 NPC 前面,
            // 單發 Raycast 打到平面就停;只要路徑上有 NPC 就算點到
            foreach (var hit in Physics.RaycastAll(ray, 100f))
            {
                if (hit.collider.transform.IsChildOf(_npcInstance.transform))
                {
                    OpenChat();
                    break;
                }
            }
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
            FaceCamera();
        }

        // Phase 2a:由 AR proximity driver 呼叫,顯示指定 POI 的 NPC(同一 POI 不重生)
        public void ShowPoiByProximity(NtutAR.Poi.Poi poi)
        {
            if (_npcInstance != null && _activePoi.id == poi.id) return;
            ShowNpc(poi);
        }

        /// <summary>隱藏測試開關用(連點玩家狀態列觸發):不經 anchor,直接把 NPC 召喚到鏡頭前方,
        /// 讓不在 POI 現場時也能測對話流程。POI 內容取 debugPoiId(預設 p01)。</summary>
        public void DebugSummonNpc()
        {
            if (_poiService == null || _arCamera == null || _npcPrefab == null) return;
            if (!_poiService.TryGetById(_debugPoiId, out var poi))
            {
                if (_poiService.All.Count == 0) return;
                poi = _poiService.All[0];
            }
            _activePoi = poi;

            if (_npcInstance != null) Destroy(_npcInstance);
            var cam = _arCamera.transform;
            var forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            var pos = cam.position + forward * 2f;
            pos.y = cam.position.y - 1.3f;   // 概略地面高度(鏡頭下方 1.3m)
            _npcInstance = Instantiate(_npcPrefab, pos, Quaternion.identity);
            _npcAnimator = _npcInstance.GetComponentInChildren<NpcAnimator>();
            FaceCamera();
            Debug.Log($"[Guide] Debug 召喚 NPC(poi={poi.id},不經 anchor)");
        }

        private void FaceCamera()
        {
            if (_arCamera == null || _npcInstance == null) return;
            var look = _arCamera.transform.position - _npcInstance.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
                _npcInstance.transform.rotation = Quaternion.LookRotation(look);
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

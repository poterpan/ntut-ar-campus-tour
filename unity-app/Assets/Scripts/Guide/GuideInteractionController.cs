using System.Threading;
using System.Threading.Tasks;
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
        [SerializeField] private MonoBehaviour _speechBehaviour;  // ISpeechInput(可留空 = 不開語音輸入)

        [Header("場景物件")]
        [SerializeField] private GameObject _npcPrefab;
        [SerializeField] private GuideChatPanel _panel;
        [SerializeField] private Camera _arCamera;

        [Tooltip("Issue #22:NPC 持續面向相機的轉身速度(越大越快;瞬轉會詭異)")]
        [SerializeField] private float _faceTurnSpeed = 5f;

        [Header("Phase 1 桌面 debug")]
        [SerializeField] private bool _useDebugPoi = true;
        [SerializeField] private string _debugPoiId = "p01";

        private GuideChatController _chat;
        private NpcAnimator _npcAnimator;
        private GameObject _npcInstance;
        private NtutAR.Poi.Poi _activePoi;
        private ISpeechInput _speech;
        private CancellationTokenSource _sttCts;

        private void Awake()
        {
            var llm = _llmBehaviour as ILlmClient;
            var tts = _ttsBehaviour as ITtsService;
            _chat = new GuideChatController(llm, tts);
            _chat.NpcStateChanged += OnNpcState;
            // Issue #21:回覆到達先解除 busy(移除思考泡泡)再 Append,泡泡才不會夾在底下
            _chat.GuideMessageReady += text =>
            {
                _panel.SetBusy(false);
                _panel.AppendMessage("導遊", text);
            };
            _panel.Sent += question =>
            {
                _panel.SetBusy(true);
                _ = _chat.AskAsync(question);
            };

            // Issue #26 STT:麥克風按住/放開 → 啟動/停止語音擷取(留空則不開)
            _speech = _speechBehaviour as ISpeechInput;
            if (_speech != null)
            {
                _panel.SpeechCaptureStart += BeginSpeechCapture;
                _panel.SpeechCaptureEnd += EndSpeechCapture;
            }

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
            TickFaceCamera();   // Issue #22:NPC 存活期間每幀平滑面向相機

            if (_npcInstance == null) return;
            if (!TryGetTapRay(out var ray)) return;
            // RaycastAll:室內測試時 AR 平面(桌面等)的 collider 會擋在 NPC 前面,
            // 單發 Raycast 打到平面就停;只要路徑上有 NPC 就算點到
            var hits = Physics.RaycastAll(ray, 100f);
            bool hitNpc = false;
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(_npcInstance.transform))
                {
                    hitNpc = true;
                    OpenChat();
                    break;
                }
            }
            // 實機診斷用(Xcode console 可見):記錄每次點擊打到了什麼
            Debug.Log($"[Guide] tap hits={hits.Length} npc={hitNpc} " +
                      string.Join(",", System.Array.ConvertAll(hits, h => h.collider.name)));
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
            // Issue #20:換 POI 時銷毀舊 NPC + 關掉屬於舊 POI 的對話,避免殘留兩隻 / 舊對話
            if (_npcInstance != null) Destroy(_npcInstance);
            _panel.Close();

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
            _panel.Close();   // 與 ShowNpc 對齊:重召喚前關掉舊對話,讓 OpenChat guard 能重開 session
            var cam = _arCamera.transform;
            var forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            var pos = cam.position + forward * 2f;
            pos.y = cam.position.y - 1.3f;   // 概略地面高度(鏡頭下方 1.3m)
            _npcInstance = Instantiate(_npcPrefab, pos, Quaternion.identity);
            _npcAnimator = _npcInstance.GetComponentInChildren<NpcAnimator>();
            FaceCamera();
            Debug.Log($"[Guide] Debug 召喚 NPC(poi={poi.id},不經 anchor)");
            // 1.5 秒後自動開對話:把「對話面板/層級」與「點擊判定」兩個問題切開,
            // 點擊判定若有問題,至少對話流程先可測
            StartCoroutine(DebugAutoOpenChat());
        }

        private System.Collections.IEnumerator DebugAutoOpenChat()
        {
            yield return new WaitForSeconds(1.5f);
            if (_npcInstance != null) OpenChat();
        }

        // 生成瞬間用瞬轉對齊朝向(避免第一幀從預設朝向才開始 Slerp);之後交給 TickFaceCamera 平滑維持
        private void FaceCamera()
        {
            if (_arCamera == null || _npcInstance == null) return;
            var look = _arCamera.transform.position - _npcInstance.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
                _npcInstance.transform.rotation = Quaternion.LookRotation(look);
        }

        // Issue #22:NPC 存活期間持續平滑面向相機(僅繞 Y 軸)
        private void TickFaceCamera()
        {
            if (_arCamera == null || _npcInstance == null) return;
            var look = _arCamera.transform.position - _npcInstance.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude < 0.0001f) return;
            var target = Quaternion.LookRotation(look);
            _npcInstance.transform.rotation = Quaternion.Slerp(
                _npcInstance.transform.rotation, target, _faceTurnSpeed * Time.deltaTime);
        }

        private void OpenChat()
        {
            // Issue #20:面板已開啟就不重跑 session(避免重複 Append 開場白 / 重複 Greet)
            if (_panel.IsOpen) return;
            _panel.Open();
            _npcAnimator?.PlayGreet();
            _ = _chat.StartSessionAsync(_activePoi);
        }

        private void OnNpcState(NpcState state) => _npcAnimator?.OnNpcState(state);

        // ── Issue #26 STT:按住說話 ──
        private void BeginSpeechCapture()
        {
            if (_speech == null || _sttCts != null) return;   // 已在擷取中就忽略
            _sttCts = new CancellationTokenSource();
            _ = RunSpeechCaptureAsync(_sttCts.Token);
        }

        private void EndSpeechCapture()
        {
            _sttCts?.Cancel();   // 停止錄音;ListenAsync 內部會繼續去辨識(不是取消辨識)
        }

        private async Task RunSpeechCaptureAsync(CancellationToken token)
        {
            try
            {
                string text = await _speech.ListenAsync(token);
                if (!string.IsNullOrWhiteSpace(text))
                    _panel.SubmitText(text);   // 走與打字相同的送出管線(Sent → SetBusy → AskAsync)
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Guide] 語音輸入失敗:{ex.Message}");
            }
            finally
            {
                _sttCts?.Dispose();
                _sttCts = null;
            }
        }

        private void OnDestroy()
        {
            _sttCts?.Cancel();
            _sttCts?.Dispose();
            _sttCts = null;
        }

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

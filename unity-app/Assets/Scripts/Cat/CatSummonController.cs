using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace NtutAR.Cat
{
    /// <summary>
    /// 全域召喚貓咪流程(Pokemon Go 式):
    /// 點擊畫面下方的罐頭按鈕進入放置模式 → 點擊畫面任一處,
    /// 對 AR 平面 raycast 放置罐頭 → 貓咪從罐頭後方生成、跑去吃 →
    /// 吃完停留一段時間後消失。
    /// 在 Editor / 無 AR 環境會自動 fallback 到 Physics.Raycast(打場景 Collider)。
    /// </summary>
    public class CatSummonController : MonoBehaviour
    {
        /// <summary>貓咪吃到罐頭時觸發(供 UI 統計餵食次數)</summary>
        public event System.Action CatFed;

        [Header("Prefabs")]
        [SerializeField] private CatQLearningAgent _catPrefab;
        [SerializeField] private GameObject _canPrefab;

        [Header("UI")]
        [SerializeField] private Button _summonButton;
        [Tooltip("放置模式提示(進入放置模式時顯示)")]
        [SerializeField] private GameObject _placementHint;

        [Header("AR(可留空,會自動尋找;無 AR 時 fallback 到 Physics.Raycast)")]
        [SerializeField] private ARRaycastManager _raycastManager;

        [Header("參數")]
        [Tooltip("貓咪生成位置與罐頭的距離(公尺)")]
        [SerializeField] private float _catSpawnDistance = 2.5f;
        [Tooltip("吃完罐頭後貓咪停留幾秒才消失")]
        [SerializeField] private float _despawnDelay = 6f;

        private bool _placementArmed;
        private CatQLearningAgent _activeCat;
        private GameObject _activeCan;
        private Coroutine _despawnRoutine;

        private static readonly List<ARRaycastHit> _arHits = new List<ARRaycastHit>();

        private void Start()
        {
            if (_summonButton != null)
            {
                _summonButton.onClick.AddListener(TogglePlacementMode);
            }
            if (_placementHint != null)
            {
                _placementHint.SetActive(false);
            }
            if (_raycastManager == null)
            {
                _raycastManager = FindFirstObjectByType<ARRaycastManager>();
            }
        }

        private void Update()
        {
            if (!_placementArmed) return;
            if (!TryGetTap(out Vector2 screenPos)) return;
            if (IsPointerOverUi()) return;

            PlaceCanAtScreenPoint(screenPos);
        }

        /// <summary>切換放置模式(罐頭按鈕觸發)</summary>
        public void TogglePlacementMode()
        {
            SetPlacementArmed(!_placementArmed);
        }

        /// <summary>
        /// 對畫面座標做 raycast 放置罐頭並召喚貓咪。
        /// 公開供測試與之後的「拋出」模式重用(拋出只需換落點計算,落地後走同一條路)。
        /// </summary>
        public bool PlaceCanAtScreenPoint(Vector2 screenPos)
        {
            if (!TryRaycast(screenPos, out Vector3 hitPoint))
            {
                return false;
            }

            SpawnOrMoveCan(hitPoint);
            SetPlacementArmed(false);
            return true;
        }

        private void SetPlacementArmed(bool armed)
        {
            _placementArmed = armed;
            if (_placementHint != null)
            {
                _placementHint.SetActive(armed);
            }
        }

        private bool TryGetTap(out Vector2 screenPos)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                screenPos = Input.GetTouch(0).position;
                return true;
            }
            if (Input.GetMouseButtonDown(0))
            {
                screenPos = Input.mousePosition;
                return true;
            }
#endif
            screenPos = default;
            return false;
        }

        private bool IsPointerOverUi()
        {
            if (EventSystem.current == null) return false;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }
#endif
            return EventSystem.current.IsPointerOverGameObject();
        }

        private bool TryRaycast(Vector2 screenPos, out Vector3 point)
        {
            // 1. 優先打 AR 偵測到的平面
            if (_raycastManager != null && _raycastManager.isActiveAndEnabled &&
                _raycastManager.Raycast(screenPos, _arHits, TrackableType.PlaneWithinPolygon))
            {
                point = _arHits[0].pose.position;
                return true;
            }

            // 2. Fallback:打場景中的 Collider(Editor 測試 / AR 尚未偵測到平面)
            Camera cam = Camera.main;
            if (cam != null && Physics.Raycast(cam.ScreenPointToRay(screenPos), out RaycastHit hit, 100f))
            {
                point = hit.point;
                return true;
            }

            point = default;
            return false;
        }

        private void SpawnOrMoveCan(Vector3 position)
        {
            // 又丟了一個罐頭:取消進行中的消失倒數
            if (_despawnRoutine != null)
            {
                StopCoroutine(_despawnRoutine);
                _despawnRoutine = null;
            }

            if (_activeCan == null)
            {
                _activeCan = Instantiate(_canPrefab, position, Quaternion.identity);
            }
            else
            {
                _activeCan.transform.position = position;
            }

            if (_activeCat == null)
            {
                _activeCat = Instantiate(_catPrefab, GetCatSpawnPosition(position), Quaternion.identity);
                _activeCat.TargetReached += OnCatReachedCan;
            }

            _activeCat.SetTarget(_activeCan.transform);
        }

        private Vector3 GetCatSpawnPosition(Vector3 canPosition)
        {
            // 沿「鏡頭 → 罐頭」方向再往外退,讓貓跑向罐頭時是面對玩家的
            Camera cam = Camera.main;
            Vector3 dir = cam != null ? canPosition - cam.transform.position : Vector3.forward;
            dir.y = 0;
            dir = dir.sqrMagnitude < 0.01f ? Vector3.forward : dir.normalized;

            Vector3 spawnPos = canPosition + dir * _catSpawnDistance;
            spawnPos.y = canPosition.y;
            return spawnPos;
        }

        private void OnCatReachedCan()
        {
            CatFed?.Invoke();
            if (_activeCan != null)
            {
                Destroy(_activeCan);
                _activeCan = null;
            }
            if (_despawnRoutine != null)
            {
                StopCoroutine(_despawnRoutine);
            }
            _despawnRoutine = StartCoroutine(DespawnAfterDelay());
        }

        private IEnumerator DespawnAfterDelay()
        {
            yield return new WaitForSeconds(_despawnDelay);

            if (_activeCat != null)
            {
                _activeCat.TargetReached -= OnCatReachedCan;
                Destroy(_activeCat.gameObject);
                _activeCat = null;
            }
            _despawnRoutine = null;
        }
    }
}

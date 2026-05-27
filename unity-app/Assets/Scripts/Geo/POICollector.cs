using System;
using System.Collections.Generic;
using System.IO;
using Google.XR.ARCoreExtensions;
using Google.XR.ARCoreExtensions.Samples.Geospatial;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace NtutAR.Geo
{
    /// <summary>
    /// W1 工具:走到 POI 旁邊、等 VPS lock 後按 Save,自動把 lat/lng/精度寫進 JSON。
    /// 檔案存在 Application.persistentDataPath/poi_captures.json,
    /// 開啟 File Sharing 後(見 POIBuildPostProcess.cs)可在 Files App 內找到並 AirDrop。
    /// 另外「📋 複製 JSON」按鈕會把 JSON 同步到剪貼簿。
    ///
    /// 接管 sample 端:
    /// - GeospatialController.DisableAnchorCreation = true(取消 tap-to-anchor)
    /// - 隱藏 "TapScreenMessage" / "SnackBar"(底部 tutorial 提示)
    /// </summary>
    public class POICollector : MonoBehaviour
    {
        public AREarthManager EarthManager;
        public GeospatialController Controller;

        private readonly List<POIRecord> _records = new List<POIRecord>();
        private string _savePath;
        private string _nameInput = "POI 01";
        private string _statusText = "等 VPS lock...";
        private Vector2 _listScroll;
        private string _jsonForCopy = string.Empty;

        // 互動狀態
        private bool _clearArmed;
        private float _clearArmedUntil;
        private float _copyFeedbackUntil;

        [Serializable]
        public struct POIRecord
        {
            public string id;
            public string name;
            public double lat;
            public double lng;
            public double altitude;
            public double horizontalAccuracy;
            public double verticalAccuracy;
            public double headingAccuracy;
            public string capturedAt;
        }

        [Serializable]
        private class POIRecordList
        {
            public List<POIRecord> captures = new List<POIRecord>();
        }

        private void Awake()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "poi_captures.json");
            if (EarthManager == null)
            {
                EarthManager = FindFirstObjectByType<AREarthManager>();
            }
            if (Controller == null)
            {
                Controller = FindFirstObjectByType<GeospatialController>();
            }
            if (Controller != null)
            {
                Controller.DisableAnchorCreation = true;
            }

            DisableGameObjectByName("TapScreenMessage");
            DisableGameObjectByName("SnackBar");

            LoadRecords();
        }

        private void DisableGameObjectByName(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                go.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"[POICollector] Could not find GameObject '{name}'");
            }
        }

        private bool TryGetPose(out GeospatialPose pose)
        {
            pose = default;
            if (EarthManager == null) return false;
            if (EarthManager.EarthState != EarthState.Enabled) return false;
            if (EarthManager.EarthTrackingState != TrackingState.Tracking) return false;
            pose = EarthManager.CameraGeospatialPose;
            return true;
        }

        private void Update()
        {
            if (TryGetPose(out var pose))
            {
                _statusText = $"✅ VPS LOCKED   H {pose.HorizontalAccuracy:F2}m / Yaw {pose.OrientationYawAccuracy:F2}°";
            }
            else
            {
                var em = EarthManager == null ? "no manager" : EarthManager.EarthState.ToString();
                _statusText = $"⏳ 等 VPS lock... ({em})";
            }

            if (_clearArmed && Time.time > _clearArmedUntil)
            {
                _clearArmed = false;
            }
        }

        private void OnGUI()
        {
            var safe = Screen.safeArea;
            const float pad = 16f;
            var width = safe.width - 2 * pad;
            var height = Mathf.Min(900f, safe.height - 60f);
            var x = safe.x + pad;
            var y = Screen.height - safe.y - height - pad;

            GUI.skin.label.fontSize = 22;
            GUI.skin.button.fontSize = 28;
            GUI.skin.textField.fontSize = 26;
            GUI.skin.textArea.fontSize = 16;
            GUI.skin.box.fontSize = 22;
            // 放大 scrollbar 觸控好按
            GUI.skin.verticalScrollbar.fixedWidth = 36;
            GUI.skin.verticalScrollbarThumb.fixedWidth = 36;

            var origColor = GUI.color;

            GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);
            GUILayout.Label("POI Collector", GUI.skin.box);
            GUILayout.Label(_statusText);
            GUILayout.Space(6);

            GUILayout.Label("POI 名稱:");
            _nameInput = GUILayout.TextField(_nameInput, GUILayout.Height(56));

            var canSave = TryGetPose(out _);
            GUI.color = canSave ? Color.green : Color.gray;
            if (GUILayout.Button(canSave ? "Save POI" : "等待 VPS lock...", GUILayout.Height(88)))
            {
                if (canSave) DoSave();
            }
            GUI.color = origColor;

            GUILayout.Space(6);
            GUILayout.Label($"已存 {_records.Count} 個 POI");

            _listScroll = GUILayout.BeginScrollView(_listScroll, GUILayout.Height(280));
            int? deleteIdx = null;
            for (int i = 0; i < _records.Count; i++)
            {
                var r = _records[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    $"{r.id} {r.name}\n  {r.lat:F6}, {r.lng:F6}  (H {r.horizontalAccuracy:F2}m / Y {r.headingAccuracy:F2}°)");
                if (GUILayout.Button("✕", GUILayout.Width(72), GUILayout.Height(72)))
                {
                    deleteIdx = i;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
            GUILayout.EndScrollView();

            if (deleteIdx.HasValue)
            {
                _records.RemoveAt(deleteIdx.Value);
                SaveRecords();
            }

            // Clear All(2-tap 確認)
            if (_records.Count > 0)
            {
                GUI.color = _clearArmed ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 0.85f, 0.7f);
                var clearText = _clearArmed
                    ? $"⚠ 再按一次清空 {_records.Count} 個!"
                    : $"🗑 清空全部 ({_records.Count})";
                if (GUILayout.Button(clearText, GUILayout.Height(56)))
                {
                    if (_clearArmed)
                    {
                        _records.Clear();
                        SaveRecords();
                        _clearArmed = false;
                    }
                    else
                    {
                        _clearArmed = true;
                        _clearArmedUntil = Time.time + 3f;
                    }
                }
                GUI.color = origColor;
            }

            // 複製 JSON 到剪貼簿
            GUI.color = new Color(0.7f, 0.85f, 1f);
            if (GUILayout.Button("📋 複製 JSON 到剪貼簿", GUILayout.Height(64)))
            {
                CopyJsonToClipboard();
            }
            GUI.color = origColor;

            if (Time.time < _copyFeedbackUntil)
            {
                var ok = new GUIStyle(GUI.skin.label);
                ok.normal.textColor = new Color(0.2f, 0.9f, 0.3f);
                ok.wordWrap = true;
                GUILayout.Label($"✓ 已複製 {_jsonForCopy.Length} 字元!可貼到 Notes / Mail", ok);
            }

            // 提示:檔案存哪 + Files App 路徑
            var hint = new GUIStyle(GUI.skin.label);
            hint.fontSize = 14;
            hint.wordWrap = true;
            GUILayout.Label("Files App → 瀏覽 → 我的 iPhone → unity-app → poi_captures.json (可長按 AirDrop)", hint);

            GUILayout.EndArea();
        }

        private void DoSave()
        {
            if (!TryGetPose(out var pose)) return;
            var idx = _records.Count + 1;
            var rec = new POIRecord
            {
                id = $"p{idx:00}",
                name = string.IsNullOrEmpty(_nameInput) ? $"POI {idx:00}" : _nameInput,
                lat = pose.Latitude,
                lng = pose.Longitude,
                altitude = pose.Altitude,
                horizontalAccuracy = pose.HorizontalAccuracy,
                verticalAccuracy = pose.VerticalAccuracy,
                headingAccuracy = pose.OrientationYawAccuracy,
                capturedAt = DateTime.Now.ToString("o"),
            };
            _records.Add(rec);
            SaveRecords();
            _nameInput = $"POI {(idx + 1):00}";
            Debug.Log($"[POICollector] Saved {rec.id} {rec.name}: {rec.lat:F6}, {rec.lng:F6}");
        }

        private void CopyJsonToClipboard()
        {
            var list = new POIRecordList { captures = _records };
            _jsonForCopy = JsonUtility.ToJson(list, true);
            GUIUtility.systemCopyBuffer = _jsonForCopy;
            _copyFeedbackUntil = Time.time + 3f;
            Debug.Log($"[POICollector] JSON copied ({_jsonForCopy.Length} chars)");
        }

        private void SaveRecords()
        {
            var list = new POIRecordList { captures = _records };
            File.WriteAllText(_savePath, JsonUtility.ToJson(list, true));
        }

        private void LoadRecords()
        {
            if (!File.Exists(_savePath)) return;
            try
            {
                var json = File.ReadAllText(_savePath);
                var list = JsonUtility.FromJson<POIRecordList>(json);
                if (list?.captures != null)
                {
                    _records.Clear();
                    _records.AddRange(list.captures);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[POICollector] Failed to load existing records: {ex.Message}");
            }
        }
    }
}

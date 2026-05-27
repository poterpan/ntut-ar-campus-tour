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
    /// 用 Xcode Devices 視窗或 iOS Files App 拉回 Mac;Show JSON 也會複製到剪貼簿。
    ///
    /// 會在 Awake 接管 sample 端:
    /// - GeospatialController.DisableAnchorCreation = true(取消 tap-to-anchor)
    /// - 隱藏 "TapScreenMessage" 與 "SnackBar" GameObjects(底部的提示文字)
    /// - UI 自己貼在螢幕底部 Safe Area,避開動態島跟 sample 頂部資訊欄
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
        private bool _showJson;
        private string _jsonForCopy = string.Empty;

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

            // 隱藏 sample 內建的提示文字(POI Collector 模式不需要)
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
        }

        private void OnGUI()
        {
            var safe = Screen.safeArea;
            const float pad = 16f;
            var width = safe.width - 2 * pad;
            var height = Mathf.Min(660f, safe.height - 60f);
            var x = safe.x + pad;
            // OnGUI 用 top-left origin;safe.y 是 bottom-left origin 的下緣
            var y = Screen.height - safe.y - height - pad;

            // 觸控友善的字級(配合手指大小放大)
            GUI.skin.label.fontSize = 22;
            GUI.skin.button.fontSize = 28;
            GUI.skin.textField.fontSize = 26;
            GUI.skin.textArea.fontSize = 14;
            GUI.skin.box.fontSize = 22;

            GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);
            GUILayout.Label("POI Collector", GUI.skin.box);
            GUILayout.Label(_statusText);
            GUILayout.Space(6);

            GUILayout.Label("POI 名稱:");
            _nameInput = GUILayout.TextField(_nameInput, GUILayout.Height(56));

            var canSave = TryGetPose(out _);
            var origColor = GUI.color;
            GUI.color = canSave ? Color.green : Color.gray;
            if (GUILayout.Button(canSave ? "Save POI" : "等待 VPS lock...", GUILayout.Height(88)))
            {
                if (canSave) DoSave();
            }
            GUI.color = origColor;

            GUILayout.Space(6);
            GUILayout.Label($"已存 {_records.Count} 個 POI");

            _listScroll = GUILayout.BeginScrollView(_listScroll, GUILayout.Height(180));
            foreach (var r in _records)
            {
                GUILayout.Label(
                    $"{r.id} {r.name}\n  {r.lat:F6}, {r.lng:F6}  (H {r.horizontalAccuracy:F2}m / Y {r.headingAccuracy:F2}°)");
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Show JSON", GUILayout.Height(64)))
            {
                ShowJson();
            }
            if (GUILayout.Button("關閉", GUILayout.Height(64), GUILayout.Width(140)))
            {
                _showJson = false;
            }
            GUILayout.EndHorizontal();

            if (_showJson)
            {
                GUILayout.TextArea(_jsonForCopy, GUILayout.Height(100));
            }

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

        private void ShowJson()
        {
            var list = new POIRecordList { captures = _records };
            _jsonForCopy = JsonUtility.ToJson(list, true);
            _showJson = true;
            GUIUtility.systemCopyBuffer = _jsonForCopy;
            Debug.Log($"[POICollector] JSON copied to clipboard ({_jsonForCopy.Length} chars)");
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

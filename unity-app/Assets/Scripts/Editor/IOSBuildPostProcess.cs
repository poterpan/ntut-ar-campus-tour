#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace NtutAR.EditorTools
{
    /// <summary>
    /// iOS build 後針對「整個 App」設定 Info.plist。
    /// POI Collector 專用的設定請寫在 POIBuildPostProcess.cs。
    ///
    /// 目前處理:
    /// - ITSAppUsesNonExemptEncryption=false:宣告未使用未豁免加密,
    ///   避免每次 TestFlight 上傳都要手動回答出口合規問題。
    /// </summary>
    public static class IOSBuildPostProcess
    {
        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            if (!File.Exists(plistPath))
            {
                Debug.LogWarning($"[IOSBuildPostProcess] Info.plist not found at {plistPath}");
                return;
            }

            var plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);

            File.WriteAllText(plistPath, plist.WriteToString());
            Debug.Log("[IOSBuildPostProcess] Info.plist patched:" +
                      " ITSAppUsesNonExemptEncryption = false");
        }
    }
}
#endif

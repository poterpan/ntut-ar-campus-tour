#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace NtutAR.EditorTools
{
    /// <summary>
    /// iOS build 後修改 Info.plist:
    /// - UIFileSharingEnabled:iTunes / Finder 看得到 App Documents 資料夾
    /// - LSSupportsOpeningDocumentsInPlace:iOS 內建 Files App 整合
    ///
    /// 兩個一起開,Application.persistentDataPath 底下的檔案
    /// (例:poi_captures.json)就能在 Files App → On My iPhone → 本 App
    /// 看到並 AirDrop 給 Mac。
    /// </summary>
    public static class POIBuildPostProcess
    {
        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            if (!File.Exists(plistPath))
            {
                Debug.LogWarning($"[POIBuildPostProcess] Info.plist not found at {plistPath}");
                return;
            }

            var plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            plist.root.SetBoolean("UIFileSharingEnabled", true);
            plist.root.SetBoolean("LSSupportsOpeningDocumentsInPlace", true);

            File.WriteAllText(plistPath, plist.WriteToString());
            Debug.Log("[POIBuildPostProcess] Info.plist patched:" +
                      " UIFileSharingEnabled + LSSupportsOpeningDocumentsInPlace = true");
        }
    }
}
#endif

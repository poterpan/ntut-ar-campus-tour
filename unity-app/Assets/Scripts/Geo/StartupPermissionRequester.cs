using System.Collections;
using UnityEngine;

namespace NtutAR.Geo
{
    /// <summary>
    /// 啟動時明確請求相機(+ Android 定位)權限。
    ///
    /// 原本相機權限是由 Google Geospatial 範例的「隱私同意」流程觸發(範例在那步請求相機/定位)。
    /// 改用自訂 UI 後隱私窗不再出現 → 乾淨安裝**不會請求相機** → AR 相機背景全黑、卡在定位。
    /// 這裡用 RuntimeInitializeOnLoadMethod 在 App 啟動時自動生成一個請求器把權限補回來,
    /// 不必掛到任何場景(避免場景接線、也保證一定執行)。
    /// </summary>
    public sealed class StartupPermissionRequester : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            var go = new GameObject(nameof(StartupPermissionRequester));
            go.AddComponent<StartupPermissionRequester>();
            DontDestroyOnLoad(go);
        }

        private IEnumerator Start()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                // 顯示相機權限窗(用 Info.plist 的 NSCameraUsageDescription),等使用者回應
                yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            // Android 執行期權限:相機 + 精確定位(對齊原 Geospatial 範例請求的權限)
            RequestAndroid(UnityEngine.Android.Permission.Camera);
            RequestAndroid(UnityEngine.Android.Permission.FineLocation);
            yield return null;
#endif
            yield break;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void RequestAndroid(string permission)
        {
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
            {
                UnityEngine.Android.Permission.RequestUserPermission(permission);
            }
        }
#endif
    }
}

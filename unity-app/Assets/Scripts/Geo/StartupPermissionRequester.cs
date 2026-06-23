using System.Collections;
using UnityEngine;

namespace NtutAR.Geo
{
    /// <summary>
    /// 啟動時的 AR 開場補丁。改用自訂 UI 後,Google Geospatial 範例的「隱私同意」流程被繞過,
    /// 而該流程原本負責兩件事 —— 兩件都漏掉了,導致乾淨安裝黑畫面、卡定位:
    ///   1. 請求相機(+定位)權限
    ///   2. 呼叫 SwitchToARView(true) 啟用 AR Session / Origin / ARCoreExtensions(預設停用)
    ///
    /// 這支在 App 啟動時把兩件事補回來(RuntimeInitializeOnLoadMethod,免場景接線):
    ///   • 設好 GeospatialController 檢查的 PlayerPrefs key,讓其 OnEnable 直接 SwitchToARView(true)
    ///   • 明確請求相機(iOS)/ 相機+定位(Android)權限
    ///
    /// 注意:這等於跳過 Google 的隱私提示窗。若要符合 Geospatial 使用條款的隱私告知,
    /// 請確保自訂 onboarding 有相機/定位資料會傳給 Google 的說明。
    /// </summary>
    public sealed class StartupPermissionRequester : MonoBehaviour
    {
        // GeospatialController.cs:236 const _hasDisplayedPrivacyPromptKey 的值
        private const string GeospatialPrivacyPromptKey = "HasDisplayedGeospatialPrivacyPrompt";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            // 在 GeospatialController.OnEnable 之前設 key → OnEnable 會 SwitchToARView(true) 啟用 AR session
            if (!PlayerPrefs.HasKey(GeospatialPrivacyPromptKey))
            {
                PlayerPrefs.SetInt(GeospatialPrivacyPromptKey, 1);
                PlayerPrefs.Save();
            }

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

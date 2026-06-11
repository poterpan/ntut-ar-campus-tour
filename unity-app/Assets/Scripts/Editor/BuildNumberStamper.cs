using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// iOS build number 自動蓋章:時間戳 yyMMdd.HHmm(台北本地時間,如 260611.1430)。
/// 一眼可讀出 build 時間;CFBundleVersion 接受句點分段整數,日期段逐日遞增、
/// 時分段當日遞增 → 單調遞增、本地與 CI 同一來源、永不撞號、免手動 bump。
/// 版本字串(0.1.0)仍由 Player Settings 管理。
/// 注意:CI 的 runner 時區是 UTC,統一用 +8 換算成台北時間,本地/CI 才一致。
/// </summary>
public sealed class BuildNumberStamper : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;   // 趕在其他 build 處理之前

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS) return;

        DateTime taipei = DateTime.UtcNow.AddHours(8);
        string stamp = taipei.ToString("yyMMdd") + "." + taipei.ToString("HHmm");
        PlayerSettings.iOS.buildNumber = stamp;
        Debug.Log($"[BuildNumberStamper] iOS build number = {stamp}(台北時間 yyMMdd.HHmm)");
    }
}

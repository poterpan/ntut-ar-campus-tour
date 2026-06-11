using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// iOS build number 自動蓋章:自 2026-01-01 UTC 起的「分鐘數」。
/// 單調遞增 + 本地與 CI 同一來源 → 永不撞號、TestFlight 遞增規則永遠滿足、
/// 不需要任何人手動 bump。版本字串(0.1.0)仍由 Player Settings 管理。
/// </summary>
public sealed class BuildNumberStamper : IPreprocessBuildWithReport
{
    private static readonly DateTime Epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public int callbackOrder => -100;   // 趕在其他 build 處理之前

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS) return;

        long minutes = (long)(DateTime.UtcNow - Epoch).TotalMinutes;
        PlayerSettings.iOS.buildNumber = minutes.ToString();
        Debug.Log($"[BuildNumberStamper] iOS build number = {minutes}(自 2026-01-01 起的分鐘數)");
    }
}

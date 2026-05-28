# AR 校園導覽 — 第一週進度報告

**Team:** 第四組
**Members:** 潘柏嘉、蔡宗育、簡妤真、張凱琳
**Date:** 2026-05-28

## Demo Video

[https://youtu.be/tm-Avpb0vYg](https://youtu.be/tm-Avpb0vYg)

影片內容:Unity Editor 介面 → POI Collector 快速瀏覽 → Game View 預覽 → iPhone 實機 POI Collector 螢幕錄影 → 校園貓 3D 模型於 Unity 場景走位測試。

---

## 本週進度

- Unity 6 專案建立,整合 AR Foundation 與 ARCore Extensions(Geospatial)
- 自製 POI Collector 工具:走到 POI 後即時取得 lat / lng,自動匯出 JSON
- iOS 實機驗證:VPS 定位鎖定、POI 座標存檔皆正常
- 校園貓 3D 模型導入 Unity 場景,完成基礎走位測試
- 建立 GitHub Actions CI/CD:Unity → Xcode Archive → 簽署 → TestFlight 自動上傳
- iOS 版本 0.1.0 已成功上架 TestFlight,組員皆可安裝測試

![TestFlight 上架成功(版本 0.1.0)](images/testflight.png)
![GitHub Actions 簽署 + 上傳 TestFlight 成功](images/ci-success.png)

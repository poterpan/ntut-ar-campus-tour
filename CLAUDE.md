# CLAUDE.md — 北科 AR 校園導覽 開發規範

> 此檔供 AI 協作工具讀取:Claude Code 讀 `CLAUDE.md`,Codex 讀 `AGENTS.md`。
> `AGENTS.md` 是本檔的 symlink,**只需維護這一份**。
> 人類向的專案介紹請見 `README.md`;開發規範細節集中在此檔。

## 專案概述

北科 AR 校園導覽 —— 結合 Geospatial AR、大型語言模型(LLM)與強化學習(ML-Agents)
的北科校園導覽手機應用。期末專題,開發期程 4 週。完整提案見 `presentation/提案/`。

四項核心功能:AR 定位導覽、虛擬導遊 NPC、LLM 即時對話、AR 校園貓彩蛋(RL)。

## Workspace 結構

```
NTUT_Unity_FinalProject/
├── CLAUDE.md          # 本檔 — AI 協作開發規範
├── AGENTS.md          # → symlink 至 CLAUDE.md(供 Codex 等工具)
├── README.md          # 人類向專案介紹
├── .gitignore
├── 會議記錄/           # 會議記錄(每次會議一檔,命名 YYYY-MM-DD_主題.md)
├── presentation/      # 簡報(每份報告一個子資料夾,如 提案/)
└── unity-app/         # Unity 專案本體(尚未建立)
```

## Unity 專案規範(unity-app/)

- **引擎 / 套件**:Unity 6、AR Foundation、ARCore Extensions(Geospatial)、
  ML-Agents(Sentis 端側推論)。
- **Editor 設定**(建專案後務必設定,確保版控可 merge):
  - `Project Settings → Editor → Asset Serialization → Force Text`
  - `Project Settings → Editor → Version Control Mode → Visible Meta Files`
- **Assets 資料夾組織**:
  - `Assets/Scenes/` — 場景
  - `Assets/Scripts/` — C# 腳本(依功能分子資料夾)
  - `Assets/Prefabs/` — 預製物件
  - `Assets/Art/` — 模型、貼圖、材質
  - `Assets/Audio/` — 音效
  - `Assets/ML/` — ML-Agents 訓練設定與 ONNX 模型
- **命名**:資料夾 / 資產檔 PascalCase;場景檔用語意命名(如 `MainTour.unity`)。

## Git 協作規範

- **`.meta` 檔必須 commit** —— Unity 靠它記錄 asset GUID,漏掉會導致引用斷裂。
- **不要 commit** `Library/`、`Temp/`、建置產物 —— `.gitignore` 已排除。
- **場景編輯紀律**:同一個 `.unity` 場景避免兩人同時編輯(合併衝突極難解);
  盡量把功能拆成 prefab,各自負責不同 prefab / 場景。
- **分支**:`main` 為穩定分支;功能在 `feature/<名稱>` 分支開發,完成後開 PR 合併。
- **Commit message**:簡潔祈使句,建議加前綴(`feat:` `fix:` `docs:` `chore:`)。
- **Git LFS**:目前**暫不啟用**。若推送失敗(GitHub 單檔上限 100MB)或 repo 過大,
  再啟用 LFS 管理二進位素材(`*.png *.psd *.fbx *.wav *.mp3` 等)。

## C# 程式風格

- 類別 / 方法 PascalCase;區域變數 / 參數 camelCase;私有欄位 `_camelCase`。
- 一個檔案一個主要類別,檔名與類別名一致。
- MonoBehaviour 生命週期方法(`Awake`/`Start`/`Update`)放類別開頭。
- 僅供 Inspector 設定的欄位優先用 `[SerializeField] private`,不要公開 public 欄位。

## AI 協作注意事項

- 改動 Unity 場景 / prefab 前,先確認沒有其他人正在編輯該檔。
- 不要手動編輯 `.meta` 檔內的 GUID。
- 產生的程式碼遵循上述 C# 風格,並與既有檔案保持一致。
- 修改規範時只改 `CLAUDE.md`,`AGENTS.md` 為 symlink 會自動同步。

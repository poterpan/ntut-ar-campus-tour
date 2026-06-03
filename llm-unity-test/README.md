# LLM Unity Test

這個資料夾是獨立於 `unity-app/` 的 LLM 測試用程式碼，避免被原本 AR 專案的編譯錯誤卡住。

## 使用方式

1. 用 Unity Hub 開啟這個資料夾:

   ```text
   D:\Github\ntut-ar-campus-tour\llm-unity-test
   ```

2. 如果 Unity Hub 無法直接開啟，改用 Unity Hub 建立新的 2D 或 3D Core 專案，再把本資料夾的 `Assets/Scripts/LLMGuideTester.cs` 複製到新專案的 `Assets/Scripts/`。
3. 設定 API key。若要 build 到手機後不用手動輸入，請用打包設定檔:

   先複製:

   ```text
   Assets/StreamingAssets/llm_config.example.json
   ```

   另存為:

   ```text
   Assets/StreamingAssets/llm_config.json
   ```

   然後把 `apiKey` 改成你的 key。`llm_config.json` 已被 `.gitignore` 忽略，不要 commit。

   只在 Editor 測試時，也可以用環境變數:

   ```powershell
   setx OPENAI_API_KEY "你的 key"
   ```

   或是在 Play 後直接把 API key 貼到畫面上的 `API key` 欄位，按 `儲存`。這個方式會存在該台電腦 / 裝置自己的 PlayerPrefs 裡，不會寫進專案檔，但第一次使用者仍要貼 key。

4. 按 Play，畫面會自動出現 API key 欄位、問題輸入框與送出按鈕。

預設 API base URL 是:

```text
https://testvideo.site/v1
```

實際呼叫 endpoint:

```text
https://testvideo.site/v1/chat/completions
```

目前 POI 位置使用模擬切換，但 POI 知識來源會先讀:

```text
Assets/StreamingAssets/POI.md
```

這份檔案是從主 repo 的 `docs/POI.md` 複製過來的。它只會被放進 LLM prompt 當知識來源，不會直接把整段內容顯示在 UI 上。之後正式 POI 座標完成後，只要把位置觸發接上，LLM 對話仍可使用同一份 POI 內容。

預設 model name 是:

```text
gpt-5.5
```

## Build 注意

環境變數只適合 Editor 測試。Build 給手機使用且不想讓使用者輸入 key 時，build 前請先建立 `Assets/StreamingAssets/llm_config.json`。

正式發布時不應該把 API key 寫死在 Unity App 裡，因為使用者可以反編譯或抓封包拿到 key。正式版建議改成:

```text
Unity App -> 你們自己的後端 API -> LLM 服務
```

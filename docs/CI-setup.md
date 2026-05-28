# CI/CD 設置指南

> 目標:`git push origin main` → GitHub Actions 自動 build iOS IPA 上 TestFlight、Android APK 上 GitHub Release
> 設置時間:~4-6 小時(分階段做)
> 之後維護:幾乎為零(只要憑證沒過期)

## 額度規劃

GitHub Pro 私有 repo = **3000 min / 月**,macOS runner 10× → 等效 **300 macOS 分鐘 / 月**。

| 觸發路徑 | 何時跑 | 預估月用量 |
|---|---|---|
| `unity-app/**` 變動 push 到 main | 主要觸發 | 5-10 次 iOS build / 月 |
| `workflow_dispatch` 手動 | debug / hotfix | 1-3 次 / 月 |
| commit message 帶 `[skip ci]` | 永遠不跑 | — |

`presentation/`、`docs/`、`會議記錄/`、`README.md`、`CLAUDE.md` 任何變動都**不會觸發** CI(`paths:` filter 已設好)。

驗證跑通之前 workflow 都先設成 **只手動觸發**(`workflow_dispatch:`),避免亂跑燒額度。

---

## Phase 1:Apple 端準備(你做,~1.5 小時)

### 1.1 Apple Developer Portal → Identifiers
建立 App ID:

- developer.apple.com → Certificates, IDs & Profiles → **Identifiers** → **+**
- Type: App IDs → App
- Description:`NTUT AR Campus Tour`
- Bundle ID(Explicit):**`me.panspace.ntut-ar-campus-tour`**(跟 Unity 端設的一樣)
- Capabilities:**不用勾**(我們只用 NSCameraUsageDescription / NSLocationUsageDescription,不是正式 capability)
- Continue → Register

順手到 **Account → Membership** 抄一下 **Team ID**(10 字元),等下要用。

### 1.2 建立 Distribution 憑證(.p12)
在 Mac 上做:

1. **產生 CSR**:Mac → `應用程式 → 工具程式 → Keychain Access` → 選單列 `Keychain Access → 憑證輔助程式 → 從憑證授權要求憑證`
   - Email:你的 Apple Dev 信箱
   - Name:你的名字
   - 「儲存到磁碟」+ 「我自己指定金鑰對資訊」
   - 存成 `CertificateSigningRequest.certSigningRequest`
2. **上傳到 Apple Dev**:developer.apple.com → Certificates → **+** → **Apple Distribution**(別選 Apple Development)→ 上傳 CSR → Continue
3. **下載 .cer**,雙擊匯入 Keychain
4. **匯出 .p12**:Keychain Access → 找到剛剛安裝的「Apple Distribution: 你名字」憑證 → 右鍵 → 匯出 → 存成 `cert.p12` → **設一個密碼**(等下要當 secret)
5. **轉 base64**:終端機跑

```bash
base64 -i cert.p12 -o cert.p12.b64
pbcopy < cert.p12.b64  # 複製到剪貼簿,等下貼成 secret
```

### 1.3 App Store Provisioning Profile
1. developer.apple.com → Profiles → **+** → **App Store Connect**(Distribution 區段裡)
2. App ID:選剛剛建的 `me.panspace.ntut-ar-campus-tour`
3. Certificate:選剛剛建的 Apple Distribution
4. Provisioning Profile Name:`NTUT AR Campus Tour App Store`(自己取,記住)
5. Generate → Download `.mobileprovision`
6. **轉 base64**:

```bash
base64 -i NTUT_AR_Campus_Tour_App_Store.mobileprovision -o profile.b64
pbcopy < profile.b64
```

### 1.4 App Store Connect → 註冊 App
1. appstoreconnect.apple.com → **My Apps** → **+** → New App
2. Platforms:勾 **iOS**
3. Name:`北科 AR 校園導覽`(或英文,可改)
4. Primary Language:`Traditional Chinese`
5. Bundle ID:選剛剛建的
6. SKU:`ntut-ar-campus-tour`(隨意,只是內部識別)
7. User Access:Full Access
8. Create
9. **等 5 分鐘** 讓 app 變成可用狀態(才能上傳 build)

### 1.5 App Store Connect API Key(取代 Apple ID 密碼,避免 2FA 麻煩)
1. App Store Connect → **Users and Access** → **Integrations** → **App Store Connect API** → **Team Keys** → **+**
2. Name:`unity-app-ci`
3. Access:**App Manager**(夠用)
4. Generate
5. **下載 `.p8` 檔**(⚠️ 只能下載一次!備份好,別丟到 git)
6. 記下 **Key ID** 跟 **Issuer ID**(在 Keys 列表頂部)

### 1.6 TestFlight 加組員為 Internal Tester
1. App Store Connect → 剛剛的 App → **TestFlight** → **Internal Testing**
2. **+** 新增 Group:`NTUT Team`
3. 把組員(蔡宗育、簡妤真、張凱琳)的 Apple ID 信箱加進去
4. 他們會收到 email + iPhone TestFlight app 通知
5. **(每個組員一次性操作)** 隊友 iPhone 裝 TestFlight app → 接受邀請 → 之後每次 push 上的新版自動跳更新通知

---

## Phase 2:Unity License(~10 分鐘)

game-ci 要 `.ulf` 檔當 secret。流程:

### 選項 A(推薦):透過 game-ci 的 activation workflow

1. 我會推 `.github/workflows/unity-activation.yml`(這個 workflow 跑一次後可以刪)
2. 你到 repo → Actions → "Unity Activation" → Run workflow
3. 下載 Artifact `Unity_v6.x.alf`
4. 上 https://license.unity3d.com/manual,登入 Unity 帳號 → 上傳 `.alf` → 拿到 `.ulf` 下載
5. 把 `.ulf` 檔的**純文字內容**貼為 GitHub Secret `UNITY_LICENSE`

### 選項 B:從本機 Mac 複製
如果你 Mac 上已經有 Unity Personal 啟用,可能在這:

```bash
ls /Library/Application\ Support/Unity/
# 找 Unity_v6.x.ulf 或 Unity_lic.ulf
cat /Library/Application\ Support/Unity/Unity_v6.x.ulf | pbcopy
```

直接複製內容貼成 secret。**通常選項 A 比較穩**(CI 環境跟本機環境的指紋對得起來)。

---

## Phase 3:GitHub Secrets(~10 分鐘)

repo → **Settings → Secrets and variables → Actions → New repository secret**,逐項加:

| Secret 名稱 | 內容怎麼填 | 來源 |
|---|---|---|
| `UNITY_LICENSE` | `.ulf` 檔的**純文字內容**(不要 base64) | Phase 2 |
| `UNITY_EMAIL` | 你 Unity 帳號 email | Unity 帳號 |
| `UNITY_PASSWORD` | 你 Unity 帳號密碼 | Unity 帳號 |
| `IOS_TEAM_ID` | 10 字元 Team ID(例:`ABCD123456`) | Phase 1.1 |
| `IOS_DIST_SIGNING_KEY` | `cert.p12.b64` 的內容(`pbcopy` 過的那串) | Phase 1.2 |
| `IOS_DIST_SIGNING_KEY_PASSWORD` | 你匯出 .p12 時設的密碼 | Phase 1.2 |
| `IOS_PROVISION_PROFILE_BASE64` | `profile.b64` 內容 | Phase 1.3 |
| `IOS_PROVISION_PROFILE_NAME` | Profile 名稱(例:`NTUT AR Campus Tour App Store`) | Phase 1.3 |
| `APPSTORE_ISSUER_ID` | API Issuer ID | Phase 1.5 |
| `APPSTORE_KEY_ID` | API Key ID | Phase 1.5 |
| `APPSTORE_PRIVATE_KEY` | `.p8` 檔的**內容**(從 `-----BEGIN PRIVATE KEY-----` 到 `-----END PRIVATE KEY-----`,含這兩行) | Phase 1.5 |

---

## Phase 4:第一次跑 + Debug(我陪你)

1. Secrets 設完跟我說
2. repo → Actions → **Build iOS → TestFlight** → 右上 **Run workflow** → 選 main → 跑
3. 第一次大概率會踩坑(license、signing、ExportOptions),我看 log 跟你說怎麼修
4. 跑通後我把 workflow 改成 `paths:` 自動觸發

---

## 故障排除速查

| 症狀 | 可能原因 | 怎麼修 |
|---|---|---|
| `Unity license is not active` | `UNITY_LICENSE` secret 不對或過期 | 重跑 Phase 2 |
| `code signing error` | provisioning profile 跟 cert 對不起來 | 確認 1.2 跟 1.3 用同一個 Apple Dev 帳號 |
| `No profile matching 'XXX' found` | `IOS_PROVISION_PROFILE_NAME` 跟 plist 對不起來 | 用 keychain 看 profile 名稱對照 |
| TestFlight: `version already used` | CFBundleVersion 重複 | 用 game-ci 的 `versioning: Semantic` 或下次 commit 補一個 build number bump |
| Android build 過了但 Geospatial 不動 | 預期內 | 需要先做 Android-restricted API key + release keystore,W3/W4 再處理 |

---

## 額度防護機制(都在 workflow 內設好)

| 機制 | 效果 |
|---|---|
| `paths:` filter | `presentation/` / `docs/` / `會議記錄/` 改完不會觸發 |
| `concurrency.cancel-in-progress` | 連推兩次,舊 build 立刻取消、只跑最新 |
| `workflow_dispatch:` | 想跑時手動按一下,完全可控 |
| commit message 帶 `[skip ci]` | 即使改 `unity-app/` 也跳過 CI |

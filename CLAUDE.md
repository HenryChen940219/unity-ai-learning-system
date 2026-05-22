# ARAssistant Henry — 專案說明

## 專案簡介
這是一個 Unity AR 互動教學系統，主角是一個叫「Kelly」的 AI 助教。
目的是協助國小/大專生學習 Webduino 與 Arduino 物聯網概念，透過 AR 影像辨識、語音互動、測驗與學習單批改來進行引導。

目標平台：**Android (arm64)**

> AR 影像追蹤使用 **ARFoundation**（ARCore/ARKit），已移除 Vuforia。

---

## 技術架構

| 類別 | 套件 / 服務 |
|------|------------|
| Unity 版本 | Unity 6（URP 17.0.3） |
| AR 框架 | ARFoundation 6.0.6 + ARCore/ARKit |
| AI 對話 | Google Gemini API（HTTP 直接呼叫，非 SDK） |
| 語音辨識 | Google Cloud Speech-to-Text API |
| 語音合成 | Google Cloud Text-to-Speech API |
| 後端資料庫 | Firebase Realtime Database + Firebase Auth |
| 角色模型 | Ready Player Me + VRM（UniVRM / UniGLTF） |
| UI | TextMeshPro (TMP_Text)、Unity Input System |

---

## API 金鑰設定

金鑰存放在專案根目錄的 `secrets.json`（已列入 `.gitignore`，不會上傳 Git）：

```json
{
  "gemini_api_key": "YOUR_GEMINI_KEY",
  "google_cloud_api_key": "YOUR_GOOGLE_CLOUD_KEY"
}
```

`SecretLoader.cs` 會在 Editor 模式從 `專案根目錄/secrets.json` 讀取；
Build 後從 `.exe` 同層目錄讀取。

Firebase 設定在 `Assets/google-services.json`（同樣 gitignored）。

---

## 核心腳本說明

### Assets/GeminiManager/（AI 核心）

| 檔案 | 功能 |
|------|------|
| `UnityAndGeminiV3.cs` | Kelly 助教主腦。管理對話歷史、發送 Gemini API 請求、防作弊測驗鎖定、呼叫 TTS 播放回覆 |
| `SecretLoader.cs` | 從 `secrets.json` 讀取 API 金鑰，靜態屬性 `SecretLoader.GeminiApiKey` / `SecretLoader.GoogleCloudApiKey` |
| `SecretsBootstrapper.cs` | 場景啟動時在 Awake 自動呼叫 `SecretLoader.LoadAsync()`，掛在任意永遠存活的 GameObject 上即可 |
| `SpeechToTextManager.cs` | 控制麥克風錄音（長按空白鍵或按鈕）、送出 Google STT、將辨識文字轉發給 Gemini |
| `TextToSpeechManager.cs` | 包裝 Google TTS，管理 AudioSource 播放與強制中斷（`StopSpeaking()`） |
| `GoogleCloudSpeechToText.cs` | Google Cloud STT API 的 HTTP 請求封裝（Singleton 模式） |

### Assets/scripts/（場景與 UI）

| 檔案 | 功能 |
|------|------|
| `MainScene.cs` | 主場景總控制器。管理 Login UI、投影片翻頁、AR 相機切換、對話框、主題切換（Webduino/Arduino） |
| `FirebaseManager.cs` | Firebase Auth 登入/登出/註冊；Realtime Database 寫入學習紀錄（閱讀時間、測驗分數、對話記錄等） |
| `QuizManager.cs` | 測驗系統。動態載入 Webduino 或 Arduino 題庫（各 10 題），管理答題流程與成績上傳 Firebase |
| `WorksheetGrader.cs` | 用相機拍攝概念構圖學習單，以 Gemini Vision API 批改，三次後強制過關 |
| `ARImageTracker.cs` | AR Foundation 圖像追蹤主腦。監聽 `ARTrackedImageManager`，偵測到 `MyTopicCard` 後把 3 個互動點（ARInteractiveDot）附著到追蹤到的圖片位置上 |
| `ARInteractiveDot.cs` | AR 互動點。帶有 SphereCollider，使用者 tap 後切換對應的動畫物件與 UI Panel |
| `HintController.cs` | 逐步提示框控制器，顯示 3 頁提示內容 |
| `ARCharacterLoop.cs` | AR 角色在場景中來回走動，靠近壁燈時自動點亮 Light 元件（Stage1_LampDemo 使用） |
| `SpeechToTextData.cs` | Google STT API 回應的資料結構（`SpeechToTextResponse`、`Result`、`Alternative`、`Error`） |
| `SlideVideoController.cs` | 投影片與影片播放控制 |
| `VoiceButton.cs` | 語音輸入按鈕的 UI 事件處理 |
| `VRMLipSync.cs` | VRM 角色嘴型同步 |
| `SimpleARHandler.cs` | 舊版 Vuforia 事件處理（已全部 comment 掉，保留作參考） |
| `SetupAvatarAnimation.cs` | Ready Player Me 角色動畫初始化 |

---

## 主要資料流

```
學生說話
  → SpeechToTextManager（錄音）
  → GoogleCloudSpeechToText（Google STT API）
  → UnityAndGeminiV3（Gemini 生成回覆）
  → TextToSpeechManager（Google TTS API）
  → AudioSource 播放
  → MainScene（顯示對話記錄 UI）
  → FirebaseManager（寫入 Firebase）
```

---

## 重要開發注意事項

1. **C# 檔案編碼必須使用 UTF-8**（非 Big5/CP950）。曾發生 `HintController.cs`、`ARCharacterLoop.cs`、`SpeechToTextData.cs` 以 Big5 儲存導致中文亂碼，已修復。

2. **不要修改 `.claudeignore`**。已設定略過二進位檔（貼圖、模型、聲音、prefab、scene），Claude 只讀 C# 程式碼。

3. **`secrets.json` 絕對不能 commit**。金鑰已列入 `.gitignore`，請勿繞過。

4. **Claude 無法直接執行或建置 Unity**。修改 `.cs` 後，請在 Unity Editor 內編譯測試。

5. **UI 文字元件統一使用 TMP_Text**（TextMeshPro），不使用舊版 `UnityEngine.UI.Text`。

6. **測驗防作弊機制**：`UnityAndGeminiV3.isQuizActive = true` 時，Kelly 會拒絕所有對話。由 `MainScene` 控制開關。

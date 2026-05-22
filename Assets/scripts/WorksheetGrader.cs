using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;
using TMPro;

public class WorksheetGrader : MonoBehaviour
{
    [Header("UI 設定 (拍照介面)")]
    public GameObject panelGrader;
    public RawImage photoPreview;
    public TMP_Text textInstruction;
    public Button btnCapture;
    public Button btnClose;

    [Header("批改結果面板")]
    public GameObject panelResult;
    public TMP_Text textFeedback;
    public Button btnCloseResult;

    [Header("Gemini 設定")]
    public string modelName = "gemini-2.5-flash";

    [Header("主場景參考")]
    public MainScene mainScene;

    [Header("Firebase 紀錄")]
    public FirebaseManager firebaseManager;
    private int retryCount = 0;

    private WebCamTexture webcamTexture;
    private Texture2D capturedPhoto;

    void Start()
    {
        if (btnCapture) btnCapture.onClick.AddListener(OnCaptureClick);
        if (btnClose) btnClose.onClick.AddListener(CloseGrader);

        if (btnCloseResult) btnCloseResult.onClick.AddListener(CloseResultPanel);

        if (panelGrader) panelGrader.SetActive(false);
        if (panelResult) panelResult.SetActive(false);
    }

    public void ResetRetryCount()
    {
        retryCount = 0;
    }

    // --- 1. 開啟相機模式 ---
    public void OpenGrader()
    {
        if (panelGrader) panelGrader.SetActive(true);
        if (panelResult) panelResult.SetActive(false);

        string currentTopic = mainScene != null ? mainScene.GetCurrentTopic() : "Webduino";
        if (textInstruction) textInstruction.text = $"請將「{currentTopic} 概念構圖」學習單對準鏡頭，按下拍照。";

        if (btnCapture) btnCapture.interactable = true;

        if (webcamTexture == null)
        {
            webcamTexture = new WebCamTexture(1920, 1080, 30);
        }

        if (photoPreview) photoPreview.texture = webcamTexture;
        webcamTexture.Play();
    }

    // --- 2. 關閉模式 ---
    public void CloseGrader()
    {
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
            Destroy(webcamTexture);
            webcamTexture = null;
        }

        if (panelResult) panelResult.SetActive(false);
        if (panelGrader) panelGrader.SetActive(false);
        this.gameObject.SetActive(false);

        if (mainScene != null) mainScene.ResumeARCameraWithDelay();
    }

    // 關閉結果面板並恢復相機
    public void CloseResultPanel()
    {
        if (panelResult) panelResult.SetActive(false);

        if (webcamTexture != null && !webcamTexture.isPlaying)
        {
            webcamTexture.Play();
        }

        string currentTopic = mainScene != null ? mainScene.GetCurrentTopic() : "Webduino";
        if (textInstruction) textInstruction.text = $"請將「{currentTopic} 概念構圖」學習單對準鏡頭，重新拍照。";

        if (btnCapture) btnCapture.interactable = true;
    }

    // --- 3. 拍照與上傳 ---
    void OnCaptureClick()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            retryCount++;
            StartCoroutine(CaptureAndAnalyze());
        }
        else
        {
            if (textInstruction) textInstruction.text = "相機尚未準備好，請稍後...";
        }
    }

    IEnumerator CaptureAndAnalyze()
    {
        if (textInstruction) textInstruction.text = $"正在分析學習單邏輯 (第 {retryCount} 次嘗試)，請稍候...";
        if (btnCapture) btnCapture.interactable = false;

        yield return new WaitForEndOfFrame();

        capturedPhoto = new Texture2D(webcamTexture.width, webcamTexture.height);
        capturedPhoto.SetPixels(webcamTexture.GetPixels());
        capturedPhoto.Apply();

        if (photoPreview) photoPreview.texture = capturedPhoto;
        webcamTexture.Pause();

        byte[] imageBytes = capturedPhoto.EncodeToJPG();
        string base64Image = System.Convert.ToBase64String(imageBytes);

        string currentTopic = mainScene != null ? mainScene.GetCurrentTopic() : "Webduino";
        string promptText = "";

        // 🔥🔥🔥 判斷是否已經修改兩次了 (第3次拍照) 🔥🔥🔥
        bool isLimitReached = (retryCount >= 3);

        if (currentTopic == "Arduino")
        {
            if (isLimitReached)
            {
                // ✨ 第3次以上：強制過關並給予解答建議
                promptText =
                    "你現在是一位溫暖且包容的 Arduino 課程 AI 助教。學生已經努力嘗試並修改到第 " + retryCount + " 次了。" +
                    "【特別通關指令】：只要畫面中是一張「Arduino 概念構圖」學習單(就算有嚴重塗改、留白或寫錯)，請『務必』在第一行寫上 [PASS] 直接讓他過關！" +
                    "除非他拍的根本不是學習單(例如拍到地板或人臉)，才寫 [FAIL] 要求重拍。\n" +
                    "如果判定過關，請在 [PASS] 之後，用極度鼓勵的語氣，溫柔地告訴他哪裡寫錯了或被塗改了，並給予正確的建議。字數 100 字內。";
            }
            else
            {
                // 🛑 第1、2次：嚴格把關模式
                promptText =
                    "你現在是一位嚴格把關的 Arduino 課程 AI 助教。請檢視這張「Arduino 概念構圖」學習單。" +
                    "【批改原則，違反任一即判定不合格 [FAIL]】：" +
                    "1. 🚫 嚴禁塗鴉與作廢：四個區塊都必須有明確的文字。只要畫面中有任何一個區塊出現「被原子筆嚴重塗黑、畫圈圈作廢、明顯亂畫線條」的狀況，絕對不能給過！" +
                    "2. 🚫 邏輯必須連貫：專案主題、拆解問題(輸入/輸出)、樣式辨識(感測器/作動器種類) 必須互相對應。不能前面寫音量，後面選光線感測。" +
                    "3. 🚫 腳位必須明確：第三區塊必須寫出具體的腳位代號(例如A0, D5等)，不能只打勾或留白。" +
                    "4. 🚫 演算法須合理：第四區塊必須有合理的 If...Then... 條件判斷，不能留白或寫無意義的字。" +
                    "【輸出格式要求】：" +
                    "如果『完美符合』上述所有條件，請在回覆的第一行寫上 [PASS]，並給予 50 字以內的簡短鼓勵。\n" +
                    "如果『有任何一項不符合』（例如有區塊被塗黑、留白、或是邏輯錯誤），請務必在第一行寫上 [FAIL]，並用嚴格但具體的方式，直接點出「哪一個區塊寫錯了或被塗改了」，要求學生修正後再拍。字數請控制在 100 字以內。";
            }
        }
        else
        {
            if (isLimitReached)
            {
                // ✨ 第3次以上：強制過關並給予解答建議
                promptText =
                    "你現在是一位溫暖且包容的 Webduino 物聯網課程助教。學生已經努力嘗試並修改到第 " + retryCount + " 次了。" +
                    "【特別通關指令】：只要畫面中是一張「Webduino 概念構圖」學習單(就算有嚴重塗改、留白或寫錯)，請『務必』在第一行寫上 [PASS] 直接讓他過關！" +
                    "除非他拍的根本不是學習單(例如拍到地板或人臉)，才寫 [FAIL] 要求重拍。\n" +
                    "如果判定過關，請在 [PASS] 之後，用極度鼓勵的語氣，溫柔地告訴他哪裡寫錯了或被塗改了，並給予正確的建議。字數 100 字內。";
            }
            else
            {
                // 🛑 第1、2次：嚴格把關模式
                promptText =
                    "你現在是一位嚴格把關的 Webduino 物聯網課程助教。請檢視這張「Webduino 概念構圖」學習單。" +
                    "【批改原則，違反任一即判定不合格 [FAIL]】：" +
                    "1. 🚫 嚴禁塗鴉與作廢：四個區塊都必須有明確的文字。只要畫面中有任何一個區塊出現「被原子筆嚴重塗黑、畫圈圈作廢、明顯亂畫線條」的狀況，絕對不能給過！" +
                    "2. 🚫 邏輯必須連貫：專案主題、拆解問題(輸入/輸出)、樣式辨識(感測器/作動器種類) 必須互相對應。不能前面寫音量，後面選光線感測。" +
                    "3. 🚫 腳位必須明確：第三區塊必須寫出具體的接線腳位(例如A0, D5等)，不能只打勾或留白。" +
                    "4. 🚫 演算法須合理：第四區塊必須填寫明確的判斷條件與動作，不能留白、不能亂畫。" +
                    "【輸出格式要求】：" +
                    "如果『完美符合』上述所有條件，請在回覆的第一行寫上 [PASS]，並給予 50 字以內的簡短鼓勵。\n" +
                    "如果『有任何一項不符合』（例如有區塊被塗黑、留白、或是邏輯錯誤），請務必在第一行寫上 [FAIL]，並用嚴格但具體的方式，直接點出「哪一個區塊寫錯了或被塗改了」，要求學生修正後再拍。字數請控制在 100 字以內。";
            }
        }

        promptText = promptText.Replace("\"", "\\\"").Replace("\n", "\\n");
        string json = "{ \"contents\": [ { \"parts\": [ { \"text\": \"" + promptText + "\" }, { \"inline_data\": { \"mime_type\": \"image/jpeg\", \"data\": \"" + base64Image + "\" } } ] } ] }";
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={SecretLoader.GeminiApiKey}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
                    if (response != null && response.candidates != null && response.candidates.Length > 0)
                    {
                        string aiReply = response.candidates[0].content.parts[0].text;

                        if (aiReply.Contains("[PASS]"))
                        {
                            if (mainScene != null) mainScene.MarkWorksheetPassed();
                        }

                        string cleanReply = aiReply.Replace("[PASS]", "").Replace("[FAIL]", "").Trim();

                        if (textInstruction) textInstruction.text = "批改完成，請看結果面板！";

                        if (panelResult) panelResult.SetActive(true);
                        if (textFeedback) textFeedback.text = cleanReply;

                        if (firebaseManager != null)
                        {
                            firebaseManager.LogWorksheetStats(retryCount, cleanReply, "");
                        }
                    }
                    else
                    {
                        if (panelResult) panelResult.SetActive(true);
                        if (textFeedback) textFeedback.text = "批改失敗：AI 沒有回傳內容。";
                    }
                }
                catch (System.Exception ex)
                {
                    if (panelResult) panelResult.SetActive(true);
                    if (textFeedback) textFeedback.text = "解析錯誤：" + ex.Message;
                }
            }
            else
            {
                if (panelResult) panelResult.SetActive(true);
                if (textFeedback) textFeedback.text = "❌ 上傳失敗 (請檢查網路或 API Key)：\n" + request.error;
            }
        }
        if (btnCapture) btnCapture.interactable = true;
    }

    [System.Serializable] public class GeminiResponse { public Candidate[] candidates; }
    [System.Serializable] public class Candidate { public Content content; }
    [System.Serializable] public class Content { public Part[] parts; }
    [System.Serializable] public class Part { public string text; }
}
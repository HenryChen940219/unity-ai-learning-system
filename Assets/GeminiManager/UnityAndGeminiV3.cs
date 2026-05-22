using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using GoogleTextToSpeech.Scripts;

public class UnityAndGeminiV3 : MonoBehaviour
{
    [Header("Gemini API 設定")]
    public string modelName = "gemini-2.5-flash";

    [Header("外部組件連結")]
    public TextToSpeechManager textToSpeechManager;

    [Header("Firebase 紀錄")]
    public FirebaseManager firebaseManager;

    [Header("防作弊狀態")]
    public bool isQuizActive = false;

    [Header("🤖 AI 人設與教學範圍")]
    [TextArea(15, 40)]
    public string systemPrompt =
            "你現在是一個親切的 Webduino 與 Arduino 物聯網程式設計助教，名字叫「Kelly」。\n" +
            "你的學生剛讀完基礎教學教材，現在準備開始進行專案規劃。\n" +
            "你的任務有兩個：\n" +
            "1. 【基礎解惑】：當學生忘記教材內容時（例如問接線、訊號種類、元件原理），請告訴他們對應答案。\n" +
            "2. 【專題引導】：引導學生針對他們選擇的主題，依照四個運算思維步驟：拆解問題、樣式辨識、抽象化及演算法一步步完成「概念構圖」學習單。\n\n" +
            "【基礎教學知識庫 (當學生問這些時，請參考回答)】：\n" +
            "- 平台差異：Webduino 透過 Wi-Fi 寫網頁積木；Arduino 透過 USB 線上傳程式(分Setup與Loop)。\n" +
            "- 硬體接線：正極(給電用) Webduino標示為VCC，Arduino標示為5V；負極(接地) 皆標示為GND。\n" +
            "- 訊號種類 (必考題)：\n" +
            "  * 數位訊號 (Digital)：非黑即白，只有開與關。(例如：按鈕、紅外線、LED)。\n" +
            "  * 類比訊號 (Analog)：連續變化的數值。(例如：光敏電阻、超音波距離、溫濕度)。\n" +
            "- 常見元件原理：\n" +
            "  * 超音波傳感器：像蝙蝠一樣發出聲波測量距離。\n" +
            "  * 光敏電阻：環境越亮，電阻值或讀取到的數值會跟著改變。\n" +
            "  * 直流馬達與驅動板：用來控制輪子轉動方向與速度，是車子的雙腳。\n\n" +
            "【專題引導四步驟 (當學生決定好專題主題時，請依序引導)】：\n" +
            "如果學生是做 Webduino，已知主題為：(1)長輩安全-感應燈 (2)教室環境-悶熱警示 (3)防盜安全-抽屜防盜鈴。\n" +
            "如果學生是做 Arduino，已知主題為：\n" +
            "   (1) 健康護眼：自動調光檯燈 (關鍵字：光敏電阻、PWM 亮度控制)。\n" +
            "   (2) 行車安全：汽車倒車雷達 (關鍵字：超音波距離、蜂鳴器、三色 LED)。\n" +
            "   (3) 智慧交通：自走車 (關鍵字：超音波或紅外線感測器、直流馬達)。\n" +
            "引導流程 ：\n" +
            "Step 1. 拆解問題：確認主題後，假設學生問輸入(Input)、輸出(Output)或運作/條件規則怎麼寫，要針對他們問的問題進行回答。\n" +
            "Step 2. 樣式辨識：假設學生不知道怎麼挑選輸入感測器、輸出作動器或訊號類別等等，針對他們問的問題進行回答。\n" +
            "Step 3. 抽象化：假設學生詢問如何規劃接線腳位，確認完專案主題、感測器及作動器後，針對他們問的問題進行回答，要清楚說明接在哪個腳位。\n" +
            "Step 4. 演算法：假設學生不知道怎麼填寫勾選setup初始設定、邏輯規則、變數或設計流程圖，針對他們問的問題進行回答，並清楚說明。\n\n" +
            "【回答規則 (最重要的部分)】：\n" +
            "1. 極度精簡：每次回答請控制在 30到50 字以內 (約 2 句話)。用語音念出來太長學生會不想聽。\n" +
            "2. 直接講重點：不要有冗長的開場白，直接回答核心答案。\n" +
            "3. 嚴格圍繞在 Webduino、Arduino 與物聯網邏輯。\n" +
            "4. 遇到無關問題，請簡短地拒絕。\n" +
            "5. 使用繁體中文 (台灣用語)，語氣親切活潑。\n" +
            "6. 絕對不要使用任何 emoji 表情符號。\n" +
            "7. 是物聯網，不要聽成互聯網。";

    private List<ChatMessage> chatHistory = new List<ChatMessage>();
    private string ApiURL => $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={SecretLoader.GeminiApiKey}";
    private int maxRetries = 3;

    void Start()
    {
        chatHistory.Clear();

        ChatMessage systemSetting = new ChatMessage();
        systemSetting.role = "user";
        systemSetting.parts = new List<Part> { new Part { text = systemPrompt } };
        chatHistory.Add(systemSetting);

        string openingMessage =
            "嗨！我是物聯網助教 Kelly！\n" +
            "聽說大家剛看完基礎教材，有沒有哪裡看不懂呀？\n" +
            "(例如：搞混 Webduino 和 Arduino 的差別？還是分不清數位跟類比訊號？都可以問我喔！)\n\n" +
            "如果你都學會了，那我們就來挑戰專題吧！\n" +
            "請告訴我你是要做 Webduino 還是 Arduino，以及你想挑戰什麼主題呢？";

        ChatMessage systemConfirm = new ChatMessage();
        systemConfirm.role = "model";
        systemConfirm.parts = new List<Part> { new Part { text = openingMessage } };
        chatHistory.Add(systemConfirm);

        Debug.Log("✅ Kelly 準備好回答教材問題與專題引導了！");

        StartCoroutine(PlayOpeningAfterSecretsReady());
    }

    private IEnumerator PlayOpeningAfterSecretsReady()
    {
        yield return new WaitUntil(() => SecretLoader.IsReady);
        if (textToSpeechManager != null)
        {
            textToSpeechManager.SendTextToGoogle("哈囉，我是助教 Kelly！有什麼看不懂的都可以問我呦！");
        }
    }

    public void SetQuizMode(bool isActive)
    {
        isQuizActive = isActive;
    }

    public void SpeakDirectly(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (textToSpeechManager != null)
        {
            textToSpeechManager.SendTextToGoogle(text);
        }
    }

    public void SubmitTextQuery(string userText)
    {
        if (string.IsNullOrEmpty(userText)) return;
        SendChat(userText);
    }

    public void SendChat(string text)
    {
        if (firebaseManager != null)
        {
            firebaseManager.LogChatMessage("student", text);
        }

        if (isQuizActive)
        {
            string rejectMsg = "現在是測驗時間，Kelly助教要保持安靜喔！請相信自己的實力，祝你考試順利！";

            if (firebaseManager != null) firebaseManager.LogChatMessage("ai", rejectMsg);
            if (textToSpeechManager != null) textToSpeechManager.SendTextToGoogle(rejectMsg);

            MainScene mainScene = FindObjectOfType<MainScene>();
            if (mainScene != null)
            {
                mainScene.AddChatHistory("Kelly", rejectMsg); // 直接印在歷史紀錄框
                mainScene.SetWaitingForClear();               // 把按鈕改成清除狀態
            }
            return;
        }

        string prompt = text + " (請用繁體中文回答，限50字以內，直接講重點)";
        StartCoroutine(SendChatRequestToGemini(prompt));
    }

    private IEnumerator SendChatRequestToGemini(string newText)
    {
        if (chatHistory.Count > 20)
        {
            chatHistory.RemoveAt(1);
            chatHistory.RemoveAt(1);
        }

        ChatMessage newUserMessage = new ChatMessage();
        newUserMessage.role = "user";
        newUserMessage.parts = new List<Part> { new Part { text = newText } };
        chatHistory.Add(newUserMessage);

        GeminiRequest request = new GeminiRequest();
        request.contents = chatHistory;

        string json = JsonUtility.ToJson(request);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        int currentRetry = 0;
        bool success = false;

        while (currentRetry <= maxRetries && !success)
        {
            using (UnityWebRequest webRequest = new UnityWebRequest(ApiURL, "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return webRequest.SendWebRequest();

                if (webRequest.responseCode == 429)
                {
                    currentRetry++;
                    float waitTime = Mathf.Pow(2, currentRetry);
                    yield return new WaitForSeconds(waitTime);
                    continue;
                }

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Gemini Error: " + webRequest.error);
                    break;
                }
                else
                {
                    success = true;
                    string responseJson = webRequest.downloadHandler.text;
                    GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(responseJson);

                    if (response != null && response.candidates != null && response.candidates.Count > 0)
                    {
                        string replyText = response.candidates[0].content.parts[0].text;

                        ChatMessage newModelMessage = new ChatMessage();
                        newModelMessage.role = "model";
                        newModelMessage.parts = new List<Part> { new Part { text = replyText } };
                        chatHistory.Add(newModelMessage);

                        if (firebaseManager != null) firebaseManager.LogChatMessage("ai", replyText);

                        if (textToSpeechManager != null)
                        {
                            textToSpeechManager.SendTextToGoogle(replyText);
                        }

                        MainScene mainScene = FindObjectOfType<MainScene>();
                        if (mainScene != null)
                        {
                            mainScene.AddChatHistory("Kelly", replyText); // 直接印在歷史紀錄框
                            mainScene.SetWaitingForClear();               // 把按鈕改成清除狀態
                        }
                    }
                }
            }
        }

        if (!success) Debug.LogError("❌ 重試多次後仍然失敗，請檢查網路或 API 配額。");
    }

    [System.Serializable]
    public class GeminiRequest { public List<ChatMessage> contents; }
    [System.Serializable]
    public class ChatMessage { public string role; public List<Part> parts; }
    [System.Serializable]
    public class Part { public string text; }
    [System.Serializable]
    public class GeminiResponse { public List<Candidate> candidates; }
    [System.Serializable]
    public class Candidate { public Content content; }
    [System.Serializable]
    public class Content { public List<Part> parts; }
}
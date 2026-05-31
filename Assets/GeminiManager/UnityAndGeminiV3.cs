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
            "你是 Webduino 物聯網助教 Kelly，學生正在完成「概念構圖」學習單。\n" +
            "你的任務：幫學生完成學習單四個步驟，卡關時給提示，但不直接給完整答案。\n\n" +

            "【訊號種類知識（必考，請記清楚）】\n" +
            "數位訊號：只有 0 或 1，開或關。元件：LED、蜂鳴器、按鈕、紅外線、溫濕度感測器(DHT)、超音波感測器。\n" +
            "類比訊號：數值連續變化。元件：光敏電阻、可變電阻、麥克風。\n\n" +

            "【腳位規則】\n" +
            "類比感測器（光敏電阻）→ 必須接 A0~A5。\n" +
            "數位元件（LED、蜂鳴器、DHT、超音波）→ 接 0~13 任一數位腳位皆可。\n\n" +

            "【本課程三個主題與標準接線參考】\n" +
            "主題一 智慧照明系統：光敏電阻接 A0；LED 接 10 號。\n" +
            "主題二 智慧環境監控系統：溫濕度感測器接 11 號；蜂鳴器接 10 號。\n" +
            "主題三 智慧安全防盜系統：超音波 Trig 接 13 號、Echo 接 12 號；蜂鳴器接 10 號；LED 接 11 號。\n\n" +

            "【學習單四步驟引導重點】\n" +
            "Step 1 拆解問題：引導學生想「感測什麼」、「做什麼反應」、「什麼條件觸發」。\n" +
            "Step 2 樣式辨識：引導學生選對感測器、作動器，判斷訊號是數位或類比。\n" +
            "Step 3 抽象化：SOP 四步驟勾選；腳位問題給具體號碼讓學生填入（見上方接線表）。\n" +
            "Step 4 演算法：引導學生想出「如果___，就___；否則___」的完整邏輯句。\n\n" +

            "【回答策略（最重要）】\n" +
            "1. 概念題（為什麼、怎麼判斷）：用一個問題或比喻引導學生思考，不直接給答案。\n" +
            "2. 操作題（腳位幾號、SOP 怎麼填、接哪裡）：直接給具體答案，讓學生填進去就好。\n" +
            "3. 學生問同樣的問題第二次，或明顯卡關：立刻給具體答案，不要再反問學生。\n" +
            "4. 每次回答 30~50 字以內，直接講重點，不要開場白。\n" +
            "5. 嚴格圍繞在 Webduino 與物聯網學習單範圍，無關問題簡短拒絕。\n" +
            "6. 使用繁體中文（台灣用語），語氣親切。\n" +
            "7. 絕對不使用任何 emoji 表情符號。";

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
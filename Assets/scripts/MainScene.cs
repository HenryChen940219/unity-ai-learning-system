using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Linq;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class MainScene : MonoBehaviour
{
    [SerializeField] FirebaseManager firebaseManager;

    [Header("Login UI")]
    [SerializeField] InputField inputEmail;
    [SerializeField] InputField inputPassword;
    [SerializeField] GameObject panelLogin;
    [SerializeField] TMP_Text textRemindError;

    [Header("Info UI")]
    [SerializeField] GameObject panelInfo;
    [SerializeField] TMP_Text textEmail;

    [Header("Records UI")]
    [SerializeField] GameObject panelRecords;
    [SerializeField] TMP_Text textInfoPanelSeconds;
    [SerializeField] TMP_Text textCounts;
    [SerializeField] TMP_Text textEvents;
    [SerializeField] TMP_Text textLastLogin;

    [Header("Detail Stats UI (舊版可留空)")]
    [SerializeField] GameObject panelDetailStats;
    [SerializeField] TMP_Text textDetailContent;

    [Header("Webduino UI")]
    [SerializeField] GameObject panelWebduino;
    [SerializeField] Image slideImageDisplay;

    [Header("AI Virtual Assistant")]
    public UnityAndGeminiV3 aiAssistant;

    [Header("Webduino Slides & Voices")]
    [SerializeField] Sprite[] webduinoSlides;
    public string[] slideIntroductions;

    [Header("Webduino Concept Map Hints")]
    [SerializeField] Sprite[] webduinoHintSprites;
    public string[] webduinoHintIntroductions;

    [Header("Arduino Slides & Voices")]
    [SerializeField] Sprite[] arduinoSlides;
    public string[] arduinoIntroductions;

    [Header("Arduino Concept Map Hints")]
    [SerializeField] Sprite[] arduinoHintSprites;
    public string[] arduinoHintIntroductions;

    [Header("學習紀錄分頁呈現 UI")]
    public GameObject[] statPages;
    public Button[] btnStatPrevs;
    public Button[] btnStatNexts;
    public TMP_Text[] textStatPageNums;

    public TMP_Text textStatOverview;
    public TMP_Text textStatAnalytics;
    public TMP_Text textStatChatLogs;

    private int currentStatPageIndex = 0;

    public SlideVideoController videoController;

    [Header("Worksheet Grader")]
    public WorksheetGrader worksheetGrader;
    public Button btnOpenGrader;

    public TMP_Text btnOpenGraderText;
    public bool isWorksheetPassed = false;

    public QuizManager quizManager;

    [SerializeField] Sprite[] nextSectionSlides;

    [SerializeField] Button buttonNext;
    [SerializeField] Button buttonPrev;
    [SerializeField] TMP_Text textPageNumber;

    [Header("Concept Map UI")]
    [SerializeField] Button buttonOpenConceptMap;

    [SerializeField] Button buttonConceptMapBack;
    [SerializeField] Button buttonConceptMapNext;

    private int currentConceptIndex = 0;

    [Header("Webduino Level 1 UI System")]
    [SerializeField] GameObject panelConfirmHint_Level1;
    [SerializeField] GameObject feedbackTextObject_Level1;

    [Header("Webduino Level 2 UI System")]
    [SerializeField] GameObject panelConfirmHint_Level2;
    [SerializeField] GameObject feedbackTextObject_Level2;

    [Header("Webduino Level 3 UI System")]
    [SerializeField] GameObject panelConfirmHint_Level3;
    [SerializeField] GameObject feedbackTextObject_Level3;

    [Header("Arduino Level 1 UI System")]
    [SerializeField] GameObject arduinoPanelConfirm_Level1;
    [SerializeField] GameObject arduinoFeedback_Level1;

    [Header("Arduino Level 2 UI System")]
    [SerializeField] GameObject arduinoPanelConfirm_Level2;
    [SerializeField] GameObject arduinoFeedback_Level2;

    [Header("Arduino Level 3 UI System")]
    [SerializeField] GameObject arduinoPanelConfirm_Level3;
    [SerializeField] GameObject arduinoFeedback_Level3;

    [Header("Shared Hint Panels (Level 1 Yes)")]
    [SerializeField] GameObject hintPanelObject;
    [SerializeField] GameObject arduinoHintPanelObject;

    [SerializeField] Button slideClickButton;

    [Header("AR Interactive Button")]
    [SerializeField] GameObject buttonTriggerHint;

    [Header("Voice Assistant UI")]
    public GameObject micButtonObject;

    [Header("Text Chat UI (文字對話)")]
    public GameObject btnToggleChat;
    public GameObject chatInputPanel;
    public TMP_Text textChatHistory;
    public TMP_InputField chatInputField;
    public Button btnSendChat;
    public TMP_Text btnSendChatText;    // 🔥 新增：用來改變按鈕文字 (送出 / 清除)
    public Button btnCloseChat;

    private string currentDraft = "";
    private bool isWaitingForClear = false; // 🔥 新增：狀態切換判斷

    private bool hasShownConfirmHint = false;
    private Coroutine feedbackCoroutine;
    private Coroutine remindErrorCoroutine;

    [Header("Pre-Scan Target Hint")]
    [SerializeField] GameObject webduinoTargetHintPanel;

    private bool isShowingTargetHint = false;

    [Header("AR System")]
    public GameObject arCameraObject;
    [SerializeField] GameObject mainCamera2D;

    [Header("AR Hologram System")]
    [SerializeField] Canvas arWorldCanvas;
    [SerializeField] Transform[] webduinoTargets;
    [SerializeField] Transform[] arduinoTargets;

    [Header("Avatar Settings")]
    [SerializeField] public GameObject avatarObject;

    [Header("Avatar AR Transform Adjust")]
    [SerializeField] Vector3 avatarPosAR = new Vector3(-0.332f, -0.65f, 1.2f);
    [SerializeField] Vector3 avatarRotAR = new Vector3(5f, 155f, 0f);
    [SerializeField] Vector3 avatarScaleAR = new Vector3(0.4f, 0.4f, 0.4f);

    private Vector3 initialWorldPos;
    private Quaternion initialWorldRot;
    private Vector3 initialScale;

    private string currentTrackingPanelName = "";
    private float panelStartTime = 0f;

    private float readingStartTime = 0f;
    private bool isReading = false;
    private bool hasCompletedReading = false;

    private string currentTopic = "";

    void Update()
    {
        if (chatInputPanel != null && chatInputPanel.activeSelf && chatInputField != null && chatInputField.isFocused)
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            {
                ClickSendChat();
            }
        }
    }

    void TrackPanelTime(string newPanelName)
    {
        if (!string.IsNullOrEmpty(currentTrackingPanelName))
        {
            double duration = Time.time - panelStartTime;
            if (duration > 0.5f)
            {
                Debug.Log($"[Time] {currentTrackingPanelName}: {duration:F1}s");
                firebaseManager.LogPanelDuration(currentTrackingPanelName, duration);
            }
        }
        currentTrackingPanelName = newPanelName;
        panelStartTime = Time.time;
    }

    private void StopAndLogReadingTimer()
    {
        if (isReading)
        {
            double duration = Time.time - readingStartTime;
            if (firebaseManager != null)
            {
                firebaseManager.LogReadingStats(duration, hasCompletedReading);
                Debug.Log($"📖 [紀錄] 閱讀教材時間: {duration:F1}秒, 完讀狀態: {hasCompletedReading}");
            }
            isReading = false;
        }
    }

    [Header("Interaction: Decompose (Webduino)")]
    [SerializeField] Button btnDecomposeInvisible;
    [SerializeField] GameObject panelDecomposeInfo;
    [SerializeField] GameObject panelHint1_Content;
    [SerializeField] GameObject panelHint1_Rule;
    [SerializeField] GameObject panelHint2_Content;
    [SerializeField] GameObject panelHint2_Rule;
    [SerializeField] GameObject panelHint3_Content;
    [SerializeField] GameObject panelHint3_Rule;

    [Header("Interaction: Decompose (Arduino)")]
    [SerializeField] GameObject arduinoPanelDecomposeInfo;
    [SerializeField] GameObject arduinoPanelHint1_Content;
    [SerializeField] GameObject arduinoPanelHint1_Rule;
    [SerializeField] GameObject arduinoPanelHint2_Content;
    [SerializeField] GameObject arduinoPanelHint2_Rule;
    [SerializeField] GameObject arduinoPanelHint3_Content;
    [SerializeField] GameObject arduinoPanelHint3_Rule;

    [Header("Interaction: Pattern Recognition (Webduino)")]
    [SerializeField] Button btnPatternInvisible;
    [SerializeField] GameObject panelPatternInfo;
    [SerializeField] GameObject panelPatternHint1;
    [SerializeField] GameObject panelPatternHint2;
    [SerializeField] GameObject panelPatternHint3;

    [Header("Interaction: Pattern Recognition (Arduino)")]
    [SerializeField] GameObject arduinoPanelPatternInfo;
    [SerializeField] GameObject arduinoPanelPatternHint1;
    [SerializeField] GameObject arduinoPanelPatternHint2;
    [SerializeField] GameObject arduinoPanelPatternHint3;

    [Header("Interaction: Abstraction (Webduino)")]
    [SerializeField] Button btnAbstractInvisible;
    [SerializeField] GameObject panelAbstractInfo;
    [SerializeField] GameObject panelAbstractHint1_Step1;
    [SerializeField] GameObject panelAbstractHint1_Step2;
    [SerializeField] GameObject panelAbstractHint1_Step3;
    [SerializeField] GameObject panelAbstractHint2_Step1;
    [SerializeField] GameObject panelAbstractHint2_Step2;
    [SerializeField] GameObject panelAbstractHint2_Step3;
    [SerializeField] GameObject panelAbstractHint3_Step1;
    [SerializeField] GameObject panelAbstractHint3_Step2;
    [SerializeField] GameObject panelAbstractHint3_Step3;

    [Header("Interaction: Abstraction (Arduino)")]
    [SerializeField] GameObject arduinoPanelAbstractInfo;
    [SerializeField] GameObject arduinoPanelAbstractHint1_Step1;
    [SerializeField] GameObject arduinoPanelAbstractHint1_Step2;
    [SerializeField] GameObject arduinoPanelAbstractHint1_Step3;
    [SerializeField] GameObject arduinoPanelAbstractHint2_Step1;
    [SerializeField] GameObject arduinoPanelAbstractHint2_Step2;
    [SerializeField] GameObject arduinoPanelAbstractHint2_Step3;
    [SerializeField] GameObject arduinoPanelAbstractHint3_Step1;
    [SerializeField] GameObject arduinoPanelAbstractHint3_Step2;
    [SerializeField] GameObject arduinoPanelAbstractHint3_Step3;

    [Header("Interaction: Algorithm (Webduino)")]
    [SerializeField] Button btnAlgorithmInvisible;
    [SerializeField] GameObject panelAlgorithmInfo;
    [SerializeField] GameObject panelAlgoHint1_Step1;
    [SerializeField] GameObject panelAlgoHint1_Step2;
    [SerializeField] GameObject panelAlgoHint1_Step3;
    [SerializeField] GameObject panelAlgoHint2_Step1;
    [SerializeField] GameObject panelAlgoHint2_Step2;
    [SerializeField] GameObject panelAlgoHint2_Step3;
    [SerializeField] GameObject panelAlgoHint3_Step1;
    [SerializeField] GameObject panelAlgoHint3_Step2;
    [SerializeField] GameObject panelAlgoHint3_Step3;

    [Header("Interaction: Algorithm (Arduino)")]
    [SerializeField] GameObject arduinoPanelAlgorithmInfo;
    [SerializeField] GameObject arduinoPanelAlgoHint1_Step1;
    [SerializeField] GameObject arduinoPanelAlgoHint1_Step2;
    [SerializeField] GameObject arduinoPanelAlgoHint1_Step3;
    [SerializeField] GameObject arduinoPanelAlgoHint2_Step1;
    [SerializeField] GameObject arduinoPanelAlgoHint2_Step2;
    [SerializeField] GameObject arduinoPanelAlgoHint2_Step3;
    [SerializeField] GameObject arduinoPanelAlgoHint3_Step1;
    [SerializeField] GameObject arduinoPanelAlgoHint3_Step2;
    [SerializeField] GameObject arduinoPanelAlgoHint3_Step3;

    private Sprite[] currentActiveSlides;
    private int currentSlideIndex = 0;

    void Start()
    {
        if (avatarObject != null)
        {
            avatarObject.transform.SetParent(null);
            initialWorldPos = avatarObject.transform.position;
            initialWorldRot = avatarObject.transform.rotation;
            initialScale = avatarObject.transform.localScale;

            Animator anim = avatarObject.GetComponent<Animator>();
            if (anim != null) anim.keepAnimatorStateOnDisable = true;
        }

        if (arCameraObject != null) arCameraObject.SetActive(false);
        if (mainCamera2D != null) mainCamera2D.SetActive(true);

        panelLogin.SetActive(true);
        panelInfo.SetActive(false);
        if (panelRecords != null) panelRecords.SetActive(false);
        if (panelWebduino != null) panelWebduino.SetActive(false);
        if (panelDetailStats != null) panelDetailStats.SetActive(false);

        if (slideImageDisplay != null) slideImageDisplay.gameObject.SetActive(true);

        if (buttonOpenConceptMap != null) buttonOpenConceptMap.gameObject.SetActive(false);
        if (buttonConceptMapBack != null) buttonConceptMapBack.gameObject.SetActive(false);
        if (buttonConceptMapNext != null) buttonConceptMapNext.gameObject.SetActive(false);

        if (hintPanelObject != null) hintPanelObject.SetActive(false);
        if (arduinoHintPanelObject != null) arduinoHintPanelObject.SetActive(false);
        if (webduinoTargetHintPanel != null) webduinoTargetHintPanel.SetActive(false);

        if (buttonTriggerHint != null) buttonTriggerHint.SetActive(false);

        if (slideClickButton != null) slideClickButton.gameObject.SetActive(false);

        if (btnOpenGrader != null) btnOpenGrader.gameObject.SetActive(false);

        if (chatInputField != null)
        {
            chatInputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
            chatInputField.onSubmit.RemoveAllListeners();
            chatInputField.onSubmit.AddListener(delegate { ClickSendChat(); });

            chatInputField.onValueChanged.AddListener((val) => {
                if (!string.IsNullOrWhiteSpace(val)) currentDraft = val;
            });
        }

        if (btnSendChat != null)
        {
            btnSendChat.onClick.RemoveAllListeners();
            btnSendChat.onClick.AddListener(ClickSendChat);
        }

        if (btnCloseChat != null) btnCloseChat.onClick.AddListener(CloseChatPanel);

        if (btnToggleChat != null)
        {
            Button btn = btnToggleChat.GetComponent<Button>();
            if (btn == null) btn = btnToggleChat.AddComponent<Button>();
            btn.onClick.AddListener(OpenChatPanel);
        }

        if (chatInputPanel != null) chatInputPanel.SetActive(false);

        CloseAllInteractionPanels();

        if (textEmail != null) textEmail.text = "";
        if (textRemindError) textRemindError.text = "";

        if (firebaseManager != null) StartCoroutine(WaitForAuthReady());
    }

    public void OpenChatPanel()
    {
        if (chatInputPanel != null) chatInputPanel.SetActive(true);
        if (btnToggleChat != null) btnToggleChat.SetActive(false);
    }

    public void CloseChatPanel()
    {
        if (chatInputPanel != null) chatInputPanel.SetActive(false);
        if (btnToggleChat != null) btnToggleChat.SetActive(true);

        // 關閉時重置狀態
        isWaitingForClear = false;
        if (btnSendChatText != null) btnSendChatText.text = "送出";
        if (textChatHistory != null) textChatHistory.text = "";
        if (chatInputField != null) chatInputField.text = "";
    }

    // 🔥 第一個被呼叫的函式：把字寫入大對話框
    public void AddChatHistory(string speaker, string msg)
    {
        if (textChatHistory == null) return;

        string color = (speaker == "Kelly") ? "#FF5D00" : "#1E58FF";
        string name = (speaker == "Kelly") ? "Kelly助教" : "你";

        textChatHistory.text += $"<color={color}><b>[{name}]</b></color> {msg}\n\n";
    }

    // 🔥 第二個被呼叫的函式：把按鈕切換成清除模式
    public void SetWaitingForClear()
    {
        isWaitingForClear = true;
        if (btnSendChatText != null)
        {
            btnSendChatText.text = "清除";
        }
    }

    public void ClickSendChat()
    {
        // 🔥 狀態 1：如果是「清除」狀態
        if (isWaitingForClear)
        {
            if (textChatHistory != null) textChatHistory.text = "";
            if (btnSendChatText != null) btnSendChatText.text = "送出";
            isWaitingForClear = false;

            if (chatInputField != null)
            {
                chatInputField.text = "";
                chatInputField.ActivateInputField(); // 清除後直接喚回打字游標
            }
            return;
        }

        // 🔥 狀態 2：正常的送出邏輯
        if (chatInputField == null) return;

        string question = chatInputField.text;

        if (string.IsNullOrWhiteSpace(question) && !string.IsNullOrWhiteSpace(currentDraft))
        {
            question = currentDraft;
        }

        question = question.Replace("\n", "").Replace("\r", "").Trim();

        if (string.IsNullOrWhiteSpace(question))
        {
            chatInputField.ActivateInputField();
            return;
        }

        chatInputField.text = "";
        currentDraft = "";
        chatInputField.DeactivateInputField(); // 送出問題後，暫時收起鍵盤等答案

        AddChatHistory("Student", question);

        if (aiAssistant != null)
        {
            aiAssistant.SubmitTextQuery(question);
        }
    }

    private void ResetAvatarToOriginalState(bool hide = false)
    {
        if (avatarObject != null)
        {
            avatarObject.transform.SetParent(null, true);
            avatarObject.transform.position = initialWorldPos;
            avatarObject.transform.rotation = initialWorldRot;
            avatarObject.transform.localScale = initialScale;
            avatarObject.SetActive(!hide);
        }
    }

    public void ClickConceptMapNext()
    {
        if (arCameraObject != null && arCameraObject.activeSelf)
        {
            bool isLastStage = (currentConceptIndex == 2);
            if (isLastStage && !isWorksheetPassed)
            {
                Debug.Log("🚫 學習單未通關，阻擋跳轉");
                HandleAnyMessage("請先點擊「批改學習單」完成任務喔！");
                return;
            }

            ExitARMode(true);
            return;
        }

        if (isShowingTargetHint)
        {
            EnterARMode();
            return;
        }

        ShowTargetHintUI();
    }

    public void ClickConceptMapBack()
    {
        if (arCameraObject != null && arCameraObject.activeSelf)
        {
            if (arCameraObject != null) arCameraObject.SetActive(false);
            if (mainCamera2D != null) mainCamera2D.SetActive(true);

            if (panelWebduino != null)
            {
                Image panelBg = panelWebduino.GetComponent<Image>();
                if (panelBg != null) panelBg.enabled = true;
            }

            if (slideImageDisplay != null) slideImageDisplay.gameObject.SetActive(true);

            CloseAllConfirmAndFeedbackPanels();
            if (buttonTriggerHint != null) buttonTriggerHint.SetActive(false);

            ShowTargetHintUI();
            return;
        }

        if (isShowingTargetHint)
        {
            if (currentConceptIndex > 0)
            {
                currentConceptIndex--;
                ShowTargetHintUI();
            }
            else
            {
                CloseWebduinoSlide();
            }
            return;
        }

        CloseWebduinoSlide();
    }

    private void ShowTargetHintUI()
    {
        Debug.Log($"🖼️ 顯示第 {currentConceptIndex + 1} 關的提示");

        if (slideImageDisplay != null) slideImageDisplay.gameObject.SetActive(false);
        if (buttonOpenConceptMap != null) buttonOpenConceptMap.gameObject.SetActive(false);

        Sprite[] currentHintSprites = (currentTopic == "Arduino") ? arduinoHintSprites : webduinoHintSprites;
        string[] currentHintIntros = (currentTopic == "Arduino") ? arduinoHintIntroductions : webduinoHintIntroductions;

        if (webduinoTargetHintPanel != null)
        {
            webduinoTargetHintPanel.SetActive(true);

            Image panelImg = webduinoTargetHintPanel.GetComponent<Image>();

            if (panelImg != null && currentHintSprites != null && currentConceptIndex < currentHintSprites.Length)
            {
                if (currentHintSprites[currentConceptIndex] != null)
                {
                    panelImg.sprite = currentHintSprites[currentConceptIndex];
                    var c = panelImg.color; c.a = 1f; panelImg.color = c;
                }
            }
        }
        else EnterARMode();

        if (aiAssistant != null && currentHintIntros != null && currentConceptIndex < currentHintIntros.Length)
        {
            string hintText = currentHintIntros[currentConceptIndex];
            if (!string.IsNullOrEmpty(hintText))
            {
                aiAssistant.SpeakDirectly(hintText);
            }
        }

        if (btnOpenGrader != null)
        {
            bool isWorksheetLevel = (currentConceptIndex == 2);
            btnOpenGrader.gameObject.SetActive(isWorksheetLevel);
        }

        isShowingTargetHint = true;
        ResetAvatarToOriginalState();

        if (buttonConceptMapNext != null) buttonConceptMapNext.gameObject.SetActive(true);
    }

    private void EnterARMode()
    {
        Debug.Log("🚀 進入 AR 模式...");

        if (arCameraObject == null) { Debug.LogError("❌ 錯誤：未綁定 AR Camera"); return; }

        hasShownConfirmHint = false;
        isShowingTargetHint = false;

        if (webduinoTargetHintPanel != null) webduinoTargetHintPanel.SetActive(false);
        CloseAllConfirmAndFeedbackPanels();

        if (btnOpenGrader != null)
        {
            bool isWorksheetLevel = (currentConceptIndex == 2);
            btnOpenGrader.gameObject.SetActive(isWorksheetLevel);
        }

        if (buttonTriggerHint != null) buttonTriggerHint.SetActive(false);

        if (mainCamera2D != null) mainCamera2D.SetActive(false);
        arCameraObject.SetActive(true);

        if (panelWebduino != null)
        {
            Image panelBg = panelWebduino.GetComponent<Image>();
            if (panelBg != null) panelBg.enabled = false;
        }

        if (slideImageDisplay != null) slideImageDisplay.gameObject.SetActive(false);

        if (avatarObject != null)
        {
            avatarObject.transform.SetParent(arCameraObject.transform, false);
            avatarObject.transform.localPosition = avatarPosAR;
            avatarObject.transform.localRotation = Quaternion.Euler(avatarRotAR);
            avatarObject.transform.localScale = avatarScaleAR;
            avatarObject.SetActive(true);
        }

        if (buttonConceptMapNext != null)
        {
            bool isLastStage = (currentConceptIndex == 2);
            buttonConceptMapNext.gameObject.SetActive(!isLastStage);
        }
    }

    public void ExitARMode(bool autoAdvance = true)
    {
        Debug.Log($"🔙 退出 AR (進下一關: {autoAdvance})");

        TrackPanelTime(null);

        hasShownConfirmHint = false;
        isShowingTargetHint = false;

        if (arCameraObject != null) arCameraObject.SetActive(false);
        if (mainCamera2D != null) mainCamera2D.SetActive(true);

        if (panelWebduino != null)
        {
            Image panelBg = panelWebduino.GetComponent<Image>();
            if (panelBg != null) panelBg.enabled = true;
        }

        if (slideImageDisplay != null) slideImageDisplay.gameObject.SetActive(true);

        ResetAvatarToOriginalState();

        if (hintPanelObject != null) hintPanelObject.SetActive(false);
        if (arduinoHintPanelObject != null) arduinoHintPanelObject.SetActive(false);
        if (webduinoTargetHintPanel != null) webduinoTargetHintPanel.SetActive(false);

        CloseAllConfirmAndFeedbackPanels();

        if (buttonTriggerHint != null) buttonTriggerHint.SetActive(false);

        if (autoAdvance)
        {
            Sprite[] currentHintSprites = (currentTopic == "Arduino") ? arduinoHintSprites : webduinoHintSprites;

            if (currentHintSprites != null && currentConceptIndex < currentHintSprites.Length - 1)
            {
                currentConceptIndex++;
                ShowTargetHintUI();
            }
            else
            {
                Debug.Log("🎉 全部關卡完成！準備進入測驗...");

                if (slideImageDisplay != null)
                {
                    slideImageDisplay.sprite = null;
                    var c = slideImageDisplay.color;
                    c.a = 0f;
                    slideImageDisplay.color = c;
                }

                if (buttonConceptMapNext != null) buttonConceptMapNext.gameObject.SetActive(false);
                if (buttonConceptMapBack != null) buttonConceptMapBack.gameObject.SetActive(false);
                if (textPageNumber != null) textPageNumber.text = "";
                if (btnOpenGrader != null) btnOpenGrader.gameObject.SetActive(false);

                if (quizManager != null)
                {
                    if (aiAssistant != null) aiAssistant.SetQuizMode(true);
                    HideMicButton();
                    if (avatarObject != null) avatarObject.SetActive(false);
                    quizManager.gameObject.SetActive(true);
                    StartCoroutine(DelayStartQuiz());
                }
                else
                {
                    CloseWebduinoSlide();
                }

                return;
            }
        }

        if (buttonConceptMapNext != null) buttonConceptMapNext.gameObject.SetActive(true);
    }

    private IEnumerator DelayStartQuiz()
    {
        yield return null;

        if (quizManager != null)
        {
            quizManager.StartQuiz();
        }
    }

    public void ExitARMode()
    {
        ExitARMode(true);
    }

    public void OnARImageFound(int scannedIndex)
    {
        if (scannedIndex != currentConceptIndex) return;

        if (arWorldCanvas != null)
        {
            arWorldCanvas.gameObject.SetActive(true);

            Transform[] currentTargets = (currentTopic == "Arduino") ? arduinoTargets : webduinoTargets;

            if (currentTargets != null && scannedIndex < currentTargets.Length)
            {
                Transform targetImg = currentTargets[scannedIndex];
                if (targetImg != null)
                {
                    arWorldCanvas.transform.SetParent(targetImg);
                    arWorldCanvas.transform.localPosition = new Vector3(0f, 0f, 0f);
                    arWorldCanvas.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    arWorldCanvas.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
                }
            }
        }

        if (webduinoTargetHintPanel != null && webduinoTargetHintPanel.activeSelf) webduinoTargetHintPanel.SetActive(false);

        if (hasShownConfirmHint) return;

        if (buttonTriggerHint != null)
        {
            buttonTriggerHint.SetActive(true);
            Debug.Log($"✅ 第 {scannedIndex + 1} 關掃描成功！顯示浮空互動按鈕");
        }
        else
        {
            ClickOpenConfirmPanel();
        }

        hasShownConfirmHint = true;
    }

    public void OnARImageLost(int scannedIndex)
    {
        if (scannedIndex != currentConceptIndex) return;

        Debug.Log($"❌ 第 {scannedIndex + 1} 關圖片移開，隱藏 AR 畫布");

        if (arWorldCanvas != null) arWorldCanvas.gameObject.SetActive(false);
        if (buttonTriggerHint != null) buttonTriggerHint.SetActive(false);
    }

    public void ClickOpenConfirmPanel()
    {
        if (currentTopic == "Arduino")
        {
            if (currentConceptIndex == 0) { if (arduinoPanelConfirm_Level1 != null) arduinoPanelConfirm_Level1.SetActive(true); }
            else if (currentConceptIndex == 1) { if (arduinoPanelConfirm_Level2 != null) arduinoPanelConfirm_Level2.SetActive(true); }
            else if (currentConceptIndex == 2) { if (arduinoPanelConfirm_Level3 != null) arduinoPanelConfirm_Level3.SetActive(true); }
        }
        else
        {
            if (currentConceptIndex == 0) { if (panelConfirmHint_Level1 != null) panelConfirmHint_Level1.SetActive(true); }
            else if (currentConceptIndex == 1) { if (panelConfirmHint_Level2 != null) panelConfirmHint_Level2.SetActive(true); }
            else if (currentConceptIndex == 2) { if (panelConfirmHint_Level3 != null) panelConfirmHint_Level3.SetActive(true); }
        }
    }

    public void ClickConfirmHintYes()
    {
        CloseAllConfirmAndFeedbackPanels();

        if (currentTopic == "Arduino")
        {
            if (arduinoHintPanelObject != null) arduinoHintPanelObject.SetActive(true);
        }
        else
        {
            if (hintPanelObject != null) hintPanelObject.SetActive(true);
        }
    }

    public void ClickConfirmHintNo()
    {
        if (panelConfirmHint_Level1 != null) panelConfirmHint_Level1.SetActive(false);
        if (arduinoPanelConfirm_Level1 != null) arduinoPanelConfirm_Level1.SetActive(false);

        GameObject targetFeedback = (currentTopic == "Arduino") ? arduinoFeedback_Level1 : feedbackTextObject_Level1;

        if (targetFeedback != null)
        {
            if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = StartCoroutine(ShowFeedbackRoutine(targetFeedback));
        }
    }

    private void HideOnlyInfoPanels()
    {
        if (panelDecomposeInfo) panelDecomposeInfo.SetActive(false);
        if (panelPatternInfo) panelPatternInfo.SetActive(false);
        if (panelAbstractInfo) panelAbstractInfo.SetActive(false);
        if (panelAlgorithmInfo) panelAlgorithmInfo.SetActive(false);

        if (arduinoPanelDecomposeInfo) arduinoPanelDecomposeInfo.SetActive(false);
        if (arduinoPanelPatternInfo) arduinoPanelPatternInfo.SetActive(false);
        if (arduinoPanelAbstractInfo) arduinoPanelAbstractInfo.SetActive(false);
        if (arduinoPanelAlgorithmInfo) arduinoPanelAlgorithmInfo.SetActive(false);
    }

    public void ClickLevel2_OpenDecompose()
    {
        if (panelConfirmHint_Level2 != null) panelConfirmHint_Level2.SetActive(false); HideOnlyInfoPanels();
        if (panelDecomposeInfo != null) panelDecomposeInfo.SetActive(true);
        firebaseManager.LogLearningProgress("step1_decompose"); TrackPanelTime("decompose_panel");
    }

    public void ClickArduinoLevel2_OpenDecompose()
    {
        if (arduinoPanelConfirm_Level2 != null) arduinoPanelConfirm_Level2.SetActive(false); HideOnlyInfoPanels();
        if (arduinoPanelDecomposeInfo != null) arduinoPanelDecomposeInfo.SetActive(true);
        firebaseManager.LogLearningProgress("step1_decompose"); TrackPanelTime("decompose_panel");
    }

    public void ClickArduinoLevel2_OpenPattern()
    {
        if (arduinoPanelConfirm_Level2 != null) arduinoPanelConfirm_Level2.SetActive(false); HideOnlyInfoPanels();
        if (arduinoPanelPatternInfo != null) arduinoPanelPatternInfo.SetActive(true);
        firebaseManager.LogLearningProgress("step2_pattern"); TrackPanelTime("pattern_panel");
    }

    public void ClickArduinoLevel2_Close()
    {
        if (arduinoPanelConfirm_Level2 != null) arduinoPanelConfirm_Level2.SetActive(false);
        if (arduinoFeedback_Level2 != null) { if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine); feedbackCoroutine = StartCoroutine(ShowFeedbackRoutine(arduinoFeedback_Level2)); }
    }

    public void ClickLevel2_OpenPattern()
    {
        if (panelConfirmHint_Level2 != null) panelConfirmHint_Level2.SetActive(false); HideOnlyInfoPanels();
        if (panelPatternInfo != null) panelPatternInfo.SetActive(true);
        firebaseManager.LogLearningProgress("step2_pattern"); TrackPanelTime("pattern_panel");
    }

    public void ClickLevel2_Close()
    {
        if (panelConfirmHint_Level2 != null) panelConfirmHint_Level2.SetActive(false);
        if (feedbackTextObject_Level2 != null) { if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine); feedbackCoroutine = StartCoroutine(ShowFeedbackRoutine(feedbackTextObject_Level2)); }
    }

    public void ClickLevel3_OpenAbstract()
    {
        if (panelConfirmHint_Level3 != null) panelConfirmHint_Level3.SetActive(false); HideOnlyInfoPanels();
        if (panelAbstractInfo != null) panelAbstractInfo.SetActive(true);
        firebaseManager.LogLearningProgress("step3_abstract"); TrackPanelTime("abstract_panel");
    }

    public void ClickArduinoLevel3_OpenAbstract()
    {
        if (arduinoPanelConfirm_Level3 != null) arduinoPanelConfirm_Level3.SetActive(false); HideOnlyInfoPanels();
        if (arduinoPanelAbstractInfo != null) arduinoPanelAbstractInfo.SetActive(true);
        firebaseManager.LogLearningProgress("step3_abstract"); TrackPanelTime("abstract_panel");
    }

    public void ClickArduinoLevel3_OpenAlgorithm()
    {
        if (arduinoPanelConfirm_Level3 != null) arduinoPanelConfirm_Level3.SetActive(false); HideOnlyInfoPanels();
        if (arduinoPanelAlgorithmInfo != null) arduinoPanelAlgorithmInfo.SetActive(true);
        firebaseManager.LogLearningProgress("step4_algorithm"); TrackPanelTime("algorithm_panel");
    }

    public void ClickArduinoLevel3_Close()
    {
        if (arduinoPanelConfirm_Level3 != null) arduinoPanelConfirm_Level3.SetActive(false);
        if (arduinoFeedback_Level3 != null) { if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine); feedbackCoroutine = StartCoroutine(ShowFeedbackRoutine(arduinoFeedback_Level3)); }
    }

    public void ClickLevel3_OpenAlgorithm()
    {
        if (panelConfirmHint_Level3 != null) panelConfirmHint_Level3.SetActive(false); HideOnlyInfoPanels();
        if (panelAlgorithmInfo != null) panelAlgorithmInfo.SetActive(true);
        firebaseManager.LogLearningProgress("step4_algorithm"); TrackPanelTime("algorithm_panel");
    }

    public void ClickLevel3_Close()
    {
        if (panelConfirmHint_Level3 != null) panelConfirmHint_Level3.SetActive(false);
        if (feedbackTextObject_Level3 != null) { if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine); feedbackCoroutine = StartCoroutine(ShowFeedbackRoutine(feedbackTextObject_Level3)); }
    }

    // ====================================================
    // 🔥🔥🔥 Webduino 互動邏輯 🔥🔥🔥
    // ====================================================

    public void ClickCloseDecomposeInfo() { if (panelDecomposeInfo != null) panelDecomposeInfo.SetActive(false); }
    public void ClickBtnHint1() { if (panelHint1_Content) panelHint1_Content.SetActive(true); if (panelHint1_Rule) panelHint1_Rule.SetActive(false); }
    public void ClickBtnHint2() { if (panelHint2_Content) panelHint2_Content.SetActive(true); if (panelHint2_Rule) panelHint2_Rule.SetActive(false); }
    public void ClickBtnHint3() { if (panelHint3_Content) panelHint3_Content.SetActive(true); if (panelHint3_Rule) panelHint3_Rule.SetActive(false); }
    public void ClickNextFromHint1_Content() { if (panelHint1_Content) panelHint1_Content.SetActive(false); if (panelHint1_Rule) panelHint1_Rule.SetActive(true); }
    public void ClickNextFromHint2_Content() { if (panelHint2_Content) panelHint2_Content.SetActive(false); if (panelHint2_Rule) panelHint2_Rule.SetActive(true); }
    public void ClickNextFromHint3_Content() { if (panelHint3_Content) panelHint3_Content.SetActive(false); if (panelHint3_Rule) panelHint3_Rule.SetActive(true); }
    public void ClickOKFromHint1_Rule() { if (panelHint1_Rule) panelHint1_Rule.SetActive(false); if (panelDecomposeInfo) panelDecomposeInfo.SetActive(true); }
    public void ClickOKFromHint2_Rule() { if (panelHint2_Rule) panelHint2_Rule.SetActive(false); if (panelDecomposeInfo) panelDecomposeInfo.SetActive(true); }
    public void ClickOKFromHint3_Rule() { if (panelHint3_Rule) panelHint3_Rule.SetActive(false); if (panelDecomposeInfo) panelDecomposeInfo.SetActive(true); }

    public void ClickClosePatternInfo() { if (panelPatternInfo != null) panelPatternInfo.SetActive(false); }
    public void ClickBtnPatternHint1() { if (panelPatternHint1) panelPatternHint1.SetActive(true); if (panelPatternHint2) panelPatternHint2.SetActive(false); if (panelPatternHint3) panelPatternHint3.SetActive(false); }
    public void ClickBtnPatternHint2() { if (panelPatternHint2) panelPatternHint2.SetActive(true); if (panelPatternHint1) panelPatternHint1.SetActive(false); if (panelPatternHint3) panelPatternHint3.SetActive(false); }
    public void ClickBtnPatternHint3() { if (panelPatternHint3) panelPatternHint3.SetActive(true); if (panelPatternHint1) panelPatternHint1.SetActive(false); if (panelPatternHint2) panelPatternHint2.SetActive(false); }
    public void ClickNextFromPatternHint1() { if (panelPatternHint1) panelPatternHint1.SetActive(false); if (panelPatternHint2) panelPatternHint2.SetActive(true); }
    public void ClickNextFromPatternHint2() { if (panelPatternHint2) panelPatternHint2.SetActive(false); if (panelPatternHint3) panelPatternHint3.SetActive(true); }
    public void ClickClosePatternHint3() { if (panelPatternHint3) panelPatternHint3.SetActive(false); }

    public void ClickCloseAbstractInfo() { if (panelAbstractInfo != null) panelAbstractInfo.SetActive(false); }
    public void ClickBtnAbstractHint1() { firebaseManager.LogThemePreference("theme_elderly"); if (panelAbstractHint1_Step1) panelAbstractHint1_Step1.SetActive(true); }
    public void ClickNextFromAbstractHint1_Step1() { if (panelAbstractHint1_Step1) panelAbstractHint1_Step1.SetActive(false); if (panelAbstractHint1_Step2) panelAbstractHint1_Step2.SetActive(true); }
    public void ClickNextFromAbstractHint1_Step2() { if (panelAbstractHint1_Step2) panelAbstractHint1_Step2.SetActive(false); if (panelAbstractHint1_Step3) panelAbstractHint1_Step3.SetActive(true); }
    public void ClickOKFromAbstractHint1_Step3() { if (panelAbstractHint1_Step3) panelAbstractHint1_Step3.SetActive(false); if (panelAbstractInfo) panelAbstractInfo.SetActive(true); }

    public void ClickBtnAbstractHint2() { firebaseManager.LogThemePreference("theme_classroom"); if (panelAbstractHint2_Step1) panelAbstractHint2_Step1.SetActive(true); }
    public void ClickNextFromAbstractHint2_Step1() { if (panelAbstractHint2_Step1) panelAbstractHint2_Step1.SetActive(false); if (panelAbstractHint2_Step2) panelAbstractHint2_Step2.SetActive(true); }
    public void ClickNextFromAbstractHint2_Step2() { if (panelAbstractHint2_Step2) panelAbstractHint2_Step2.SetActive(false); if (panelAbstractHint2_Step3) panelAbstractHint2_Step3.SetActive(true); }
    public void ClickOKFromAbstractHint2_Step3() { if (panelAbstractHint2_Step3) panelAbstractHint2_Step3.SetActive(false); if (panelAbstractInfo) panelAbstractInfo.SetActive(true); }

    public void ClickBtnAbstractHint3() { firebaseManager.LogThemePreference("theme_security"); if (panelAbstractHint3_Step1) panelAbstractHint3_Step1.SetActive(true); }
    public void ClickNextFromAbstractHint3_Step1() { if (panelAbstractHint3_Step1) panelAbstractHint3_Step1.SetActive(false); if (panelAbstractHint3_Step2) panelAbstractHint3_Step2.SetActive(true); }
    public void ClickNextFromAbstractHint3_Step2() { if (panelAbstractHint3_Step2) panelAbstractHint3_Step2.SetActive(false); if (panelAbstractHint3_Step3) panelAbstractHint3_Step3.SetActive(true); }
    public void ClickOKFromAbstractHint3_Step3() { if (panelAbstractHint3_Step3) panelAbstractHint3_Step3.SetActive(false); if (panelAbstractInfo) panelAbstractInfo.SetActive(true); }

    public void ClickCloseAlgorithmInfo() { if (panelAlgorithmInfo != null) panelAlgorithmInfo.SetActive(false); }
    public void ClickBtnAlgoHint1() { firebaseManager.LogThemePreference("theme_elderly"); if (panelAlgoHint1_Step1) panelAlgoHint1_Step1.SetActive(true); }
    public void ClickNextFromAlgoHint1_Step1() { if (panelAlgoHint1_Step1) panelAlgoHint1_Step1.SetActive(false); if (panelAlgoHint1_Step2) panelAlgoHint1_Step2.SetActive(true); }
    public void ClickNextFromAlgoHint1_Step2() { if (panelAlgoHint1_Step2) panelAlgoHint1_Step2.SetActive(false); if (panelAlgoHint1_Step3) panelAlgoHint1_Step3.SetActive(true); }
    public void ClickOKFromAlgoHint1_Step3() { if (panelAlgoHint1_Step3) panelAlgoHint1_Step3.SetActive(false); if (panelAlgorithmInfo) panelAlgorithmInfo.SetActive(true); }

    public void ClickBtnAlgoHint2() { firebaseManager.LogThemePreference("theme_classroom"); if (panelAlgoHint2_Step1) panelAlgoHint2_Step1.SetActive(true); }
    public void ClickNextFromAlgoHint2_Step1() { if (panelAlgoHint2_Step1) panelAlgoHint2_Step1.SetActive(false); if (panelAlgoHint2_Step2) panelAlgoHint2_Step2.SetActive(true); }
    public void ClickNextFromAlgoHint2_Step2() { if (panelAlgoHint2_Step2) panelAlgoHint2_Step2.SetActive(false); if (panelAlgoHint2_Step3) panelAlgoHint2_Step3.SetActive(true); }
    public void ClickOKFromAlgoHint2_Step3() { if (panelAlgoHint2_Step3) panelAlgoHint2_Step3.SetActive(false); if (panelAlgorithmInfo) panelAlgorithmInfo.SetActive(true); }

    public void ClickBtnAlgoHint3() { firebaseManager.LogThemePreference("theme_security"); if (panelAlgoHint3_Step1) panelAlgoHint3_Step1.SetActive(true); }
    public void ClickNextFromAlgoHint3_Step1() { if (panelAlgoHint3_Step1) panelAlgoHint3_Step1.SetActive(false); if (panelAlgoHint3_Step2) panelAlgoHint3_Step2.SetActive(true); }
    public void ClickNextFromAlgoHint3_Step2() { if (panelAlgoHint3_Step2) panelAlgoHint3_Step2.SetActive(false); if (panelAlgoHint3_Step3) panelAlgoHint3_Step3.SetActive(true); }
    public void ClickOKFromAlgoHint3_Step3() { if (panelAlgoHint3_Step3) panelAlgoHint3_Step3.SetActive(false); if (panelAlgorithmInfo) panelAlgorithmInfo.SetActive(true); }


    // ====================================================
    // 🔥🔥🔥 Arduino 互動邏輯 🔥🔥🔥
    // ====================================================

    public void ClickCloseArduinoDecomposeInfo() { if (arduinoPanelDecomposeInfo != null) arduinoPanelDecomposeInfo.SetActive(false); }

    public void ClickArduinoBtnHint1()
    {
        if (arduinoPanelHint1_Content) arduinoPanelHint1_Content.SetActive(true);
        if (arduinoPanelHint1_Rule) arduinoPanelHint1_Rule.SetActive(false);
    }
    public void ClickNextFromArduinoHint1_Content()
    {
        if (arduinoPanelHint1_Content) arduinoPanelHint1_Content.SetActive(false);
        if (arduinoPanelHint1_Rule) arduinoPanelHint1_Rule.SetActive(true);
    }
    public void ClickOKFromArduinoHint1_Rule()
    {
        if (arduinoPanelHint1_Rule) arduinoPanelHint1_Rule.SetActive(false);
        if (arduinoPanelDecomposeInfo) arduinoPanelDecomposeInfo.SetActive(true);
    }

    public void ClickArduinoBtnHint2()
    {
        if (arduinoPanelHint2_Content) arduinoPanelHint2_Content.SetActive(true);
        if (arduinoPanelHint2_Rule) arduinoPanelHint2_Rule.SetActive(false);
    }
    public void ClickNextFromArduinoHint2_Content()
    {
        if (arduinoPanelHint2_Content) arduinoPanelHint2_Content.SetActive(false);
        if (arduinoPanelHint2_Rule) arduinoPanelHint2_Rule.SetActive(true);
    }
    public void ClickOKFromArduinoHint2_Rule()
    {
        if (arduinoPanelHint2_Rule) arduinoPanelHint2_Rule.SetActive(false);
        if (arduinoPanelDecomposeInfo) arduinoPanelDecomposeInfo.SetActive(true);
    }

    public void ClickArduinoBtnHint3()
    {
        if (arduinoPanelHint3_Content) arduinoPanelHint3_Content.SetActive(true);
        if (arduinoPanelHint3_Rule) arduinoPanelHint3_Rule.SetActive(false);
    }
    public void ClickNextFromArduinoHint3_Content()
    {
        if (arduinoPanelHint3_Content) arduinoPanelHint3_Content.SetActive(false);
        if (arduinoPanelHint3_Rule) arduinoPanelHint3_Rule.SetActive(true);
    }
    public void ClickOKFromArduinoHint3_Rule()
    {
        if (arduinoPanelHint3_Rule) arduinoPanelHint3_Rule.SetActive(false);
        if (arduinoPanelDecomposeInfo) arduinoPanelDecomposeInfo.SetActive(true);
    }

    public void ClickCloseArduinoPatternInfo() { if (arduinoPanelPatternInfo != null) arduinoPanelPatternInfo.SetActive(false); }

    public void ClickArduinoBtnPatternHint1()
    {
        if (arduinoPanelPatternHint1) arduinoPanelPatternHint1.SetActive(true);
        if (arduinoPanelPatternHint2) arduinoPanelPatternHint2.SetActive(false);
        if (arduinoPanelPatternHint3) arduinoPanelPatternHint3.SetActive(false);
    }
    public void ClickArduinoBtnPatternHint2()
    {
        if (arduinoPanelPatternHint2) arduinoPanelPatternHint2.SetActive(true);
        if (arduinoPanelPatternHint1) arduinoPanelPatternHint1.SetActive(false);
        if (arduinoPanelPatternHint3) arduinoPanelPatternHint3.SetActive(false);
    }
    public void ClickArduinoBtnPatternHint3()
    {
        if (arduinoPanelPatternHint3) arduinoPanelPatternHint3.SetActive(true);
        if (arduinoPanelPatternHint1) arduinoPanelPatternHint1.SetActive(false);
        if (arduinoPanelPatternHint2) arduinoPanelPatternHint2.SetActive(false);
    }
    public void ClickNextFromArduinoPatternHint1()
    {
        if (arduinoPanelPatternHint1) arduinoPanelPatternHint1.SetActive(false);
        if (arduinoPanelPatternHint2) arduinoPanelPatternHint2.SetActive(true);
    }
    public void ClickNextFromArduinoPatternHint2()
    {
        if (arduinoPanelPatternHint2) arduinoPanelPatternHint2.SetActive(false);
        if (arduinoPanelPatternHint3) arduinoPanelPatternHint3.SetActive(true);
    }
    public void ClickCloseArduinoPatternHint3()
    {
        if (arduinoPanelPatternHint3) arduinoPanelPatternHint3.SetActive(false);
    }

    public void ClickCloseArduinoAbstractInfo() { if (arduinoPanelAbstractInfo != null) arduinoPanelAbstractInfo.SetActive(false); }

    public void ClickArduinoBtnAbstractHint1()
    {
        firebaseManager.LogThemePreference("theme_light");
        if (arduinoPanelAbstractHint1_Step1) arduinoPanelAbstractHint1_Step1.SetActive(true);
    }
    public void ClickNextFromArduinoAbstractHint1_Step1()
    {
        if (arduinoPanelAbstractHint1_Step1) arduinoPanelAbstractHint1_Step1.SetActive(false);
        if (arduinoPanelAbstractHint1_Step2) arduinoPanelAbstractHint1_Step2.SetActive(true);
    }
    public void ClickNextFromArduinoAbstractHint1_Step2()
    {
        if (arduinoPanelAbstractHint1_Step2) arduinoPanelAbstractHint1_Step2.SetActive(false);
        if (arduinoPanelAbstractHint1_Step3) arduinoPanelAbstractHint1_Step3.SetActive(true);
    }
    public void ClickOKFromArduinoAbstractHint1_Step3()
    {
        if (arduinoPanelAbstractHint1_Step3) arduinoPanelAbstractHint1_Step3.SetActive(false);
        if (arduinoPanelAbstractInfo) arduinoPanelAbstractInfo.SetActive(true);
    }

    public void ClickArduinoBtnAbstractHint2()
    {
        firebaseManager.LogThemePreference("theme_radar");
        if (arduinoPanelAbstractHint2_Step1) arduinoPanelAbstractHint2_Step1.SetActive(true);
    }
    public void ClickNextFromArduinoAbstractHint2_Step1()
    {
        if (arduinoPanelAbstractHint2_Step1) arduinoPanelAbstractHint2_Step1.SetActive(false);
        if (arduinoPanelAbstractHint2_Step2) arduinoPanelAbstractHint2_Step2.SetActive(true);
    }
    public void ClickNextFromArduinoAbstractHint2_Step2()
    {
        if (arduinoPanelAbstractHint2_Step2) arduinoPanelAbstractHint2_Step2.SetActive(false);
        if (arduinoPanelAbstractHint2_Step3) arduinoPanelAbstractHint2_Step3.SetActive(true);
    }
    public void ClickOKFromArduinoAbstractHint2_Step3()
    {
        if (arduinoPanelAbstractHint2_Step3) arduinoPanelAbstractHint2_Step3.SetActive(false);
        if (arduinoPanelAbstractInfo) arduinoPanelAbstractInfo.SetActive(true);
    }

    public void ClickArduinoBtnAbstractHint3()
    {
        firebaseManager.LogThemePreference("theme_car");
        if (arduinoPanelAbstractHint3_Step1) arduinoPanelAbstractHint3_Step1.SetActive(true);
    }
    public void ClickNextFromArduinoAbstractHint3_Step1()
    {
        if (arduinoPanelAbstractHint3_Step1) arduinoPanelAbstractHint3_Step1.SetActive(false);
        if (arduinoPanelAbstractHint3_Step2) arduinoPanelAbstractHint3_Step2.SetActive(true);
    }
    public void ClickNextFromArduinoAbstractHint3_Step2()
    {
        if (arduinoPanelAbstractHint3_Step2) arduinoPanelAbstractHint3_Step2.SetActive(false);
        if (arduinoPanelAbstractHint3_Step3) arduinoPanelAbstractHint3_Step3.SetActive(true);
    }
    public void ClickOKFromArduinoAbstractHint3_Step3()
    {
        if (arduinoPanelAbstractHint3_Step3) arduinoPanelAbstractHint3_Step3.SetActive(false);
        if (arduinoPanelAbstractInfo) arduinoPanelAbstractInfo.SetActive(true);
    }

    public void ClickCloseArduinoAlgorithmInfo() { if (arduinoPanelAlgorithmInfo != null) arduinoPanelAlgorithmInfo.SetActive(false); }

    public void ClickArduinoBtnAlgoHint1()
    {
        firebaseManager.LogThemePreference("theme_light");
        if (arduinoPanelAlgoHint1_Step1) arduinoPanelAlgoHint1_Step1.SetActive(true);
    }
    public void ClickNextFromArduinoAlgoHint1_Step1()
    {
        if (arduinoPanelAlgoHint1_Step1) arduinoPanelAlgoHint1_Step1.SetActive(false);
        if (arduinoPanelAlgoHint1_Step2) arduinoPanelAlgoHint1_Step2.SetActive(true);
    }
    public void ClickNextFromArduinoAlgoHint1_Step2()
    {
        if (arduinoPanelAlgoHint1_Step2) arduinoPanelAlgoHint1_Step2.SetActive(false);
        if (arduinoPanelAlgoHint1_Step3) arduinoPanelAlgoHint1_Step3.SetActive(true);
    }
    public void ClickOKFromArduinoAlgoHint1_Step3()
    {
        if (arduinoPanelAlgoHint1_Step3) arduinoPanelAlgoHint1_Step3.SetActive(false);
        if (arduinoPanelAlgorithmInfo) arduinoPanelAlgorithmInfo.SetActive(true);
    }

    public void ClickArduinoBtnAlgoHint2()
    {
        firebaseManager.LogThemePreference("theme_radar");
        if (arduinoPanelAlgoHint2_Step1) arduinoPanelAlgoHint2_Step1.SetActive(true);
    }
    public void ClickNextFromArduinoAlgoHint2_Step1()
    {
        if (arduinoPanelAlgoHint2_Step1) arduinoPanelAlgoHint2_Step1.SetActive(false);
        if (arduinoPanelAlgoHint2_Step2) arduinoPanelAlgoHint2_Step2.SetActive(true);
    }
    public void ClickNextFromArduinoAlgoHint2_Step2()
    {
        if (arduinoPanelAlgoHint2_Step2) arduinoPanelAlgoHint2_Step2.SetActive(false);
        if (arduinoPanelAlgoHint2_Step3) arduinoPanelAlgoHint2_Step3.SetActive(true);
    }
    public void ClickOKFromArduinoAlgoHint2_Step3()
    {
        if (arduinoPanelAlgoHint2_Step3) arduinoPanelAlgoHint2_Step3.SetActive(false);
        if (arduinoPanelAlgorithmInfo) arduinoPanelAlgorithmInfo.SetActive(true);
    }

    public void ClickArduinoBtnAlgoHint3()
    {
        firebaseManager.LogThemePreference("theme_car");
        if (arduinoPanelAlgoHint3_Step1) arduinoPanelAlgoHint3_Step1.SetActive(true);
    }
    public void ClickNextFromArduinoAlgoHint3_Step1()
    {
        if (arduinoPanelAlgoHint3_Step1) arduinoPanelAlgoHint3_Step1.SetActive(false);
        if (arduinoPanelAlgoHint3_Step2) arduinoPanelAlgoHint3_Step2.SetActive(true);
    }
    public void ClickNextFromArduinoAlgoHint3_Step2()
    {
        if (arduinoPanelAlgoHint3_Step2) arduinoPanelAlgoHint3_Step2.SetActive(false);
        if (arduinoPanelAlgoHint3_Step3) arduinoPanelAlgoHint3_Step3.SetActive(true);
    }
    public void ClickOKFromArduinoAlgoHint3_Step3()
    {
        if (arduinoPanelAlgoHint3_Step3) arduinoPanelAlgoHint3_Step3.SetActive(false);
        if (arduinoPanelAlgorithmInfo) arduinoPanelAlgorithmInfo.SetActive(true);
    }


    IEnumerator ShowFeedbackRoutine(GameObject feedbackObj)
    {
        feedbackObj.SetActive(true);
        yield return new WaitForSeconds(10f);
        feedbackObj.SetActive(false);
        feedbackCoroutine = null;
    }

    private void CloseAllConfirmAndFeedbackPanels()
    {
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);

        if (panelConfirmHint_Level1 != null) panelConfirmHint_Level1.SetActive(false);
        if (panelConfirmHint_Level2 != null) panelConfirmHint_Level2.SetActive(false);
        if (panelConfirmHint_Level3 != null) panelConfirmHint_Level3.SetActive(false);

        if (arduinoPanelConfirm_Level1 != null) arduinoPanelConfirm_Level1.SetActive(false);
        if (arduinoPanelConfirm_Level2 != null) arduinoPanelConfirm_Level2.SetActive(false);
        if (arduinoPanelConfirm_Level3 != null) arduinoPanelConfirm_Level3.SetActive(false);

        if (feedbackTextObject_Level1 != null) feedbackTextObject_Level1.SetActive(false);
        if (feedbackTextObject_Level2 != null) feedbackTextObject_Level2.SetActive(false);
        if (feedbackTextObject_Level3 != null) feedbackTextObject_Level3.SetActive(false);

        if (arduinoFeedback_Level1 != null) arduinoFeedback_Level1.SetActive(false);
        if (arduinoFeedback_Level2 != null) arduinoFeedback_Level2.SetActive(false);
        if (arduinoFeedback_Level3 != null) arduinoFeedback_Level3.SetActive(false);
    }

    void CloseAllInteractionPanels()
    {
        HideOnlyInfoPanels();
        CloseAllConfirmAndFeedbackPanels();
        if (buttonTriggerHint != null) buttonTriggerHint.SetActive(false);

        TrackPanelTime(null);
    }

    public void ClickOpenConceptMap()
    {
        currentConceptIndex = 0;

        StopAndLogReadingTimer();

        if (slideImageDisplay != null) slideImageDisplay.gameObject.SetActive(false);

        if (buttonNext) buttonNext.gameObject.SetActive(false);
        if (buttonPrev) buttonPrev.gameObject.SetActive(false);
        if (buttonOpenConceptMap) buttonOpenConceptMap.gameObject.SetActive(false);

        if (buttonConceptMapBack) buttonConceptMapBack.gameObject.SetActive(true);
        if (buttonConceptMapNext) buttonConceptMapNext.gameObject.SetActive(true);

        if (textPageNumber) textPageNumber.text = "概念構圖闖關";

        isShowingTargetHint = false;
        hasShownConfirmHint = false;
        if (hintPanelObject) hintPanelObject.SetActive(false);
        if (arduinoHintPanelObject != null) arduinoHintPanelObject.SetActive(false);
        if (webduinoTargetHintPanel != null) webduinoTargetHintPanel.SetActive(false);

        isWorksheetPassed = false;
        if (btnOpenGraderText != null) btnOpenGraderText.text = "批改學習單";

        CloseAllInteractionPanels();
        TrackPanelTime(null);
        ResetAvatarToOriginalState();

        ShowTargetHintUI();
    }

    public void ClickBackFromConceptMap()
    {
        ExitARMode(false);
        CloseWebduinoSlide();
    }

    private System.Collections.IEnumerator WaitForAuthReady()
    {
        while (firebaseManager.auth == null) yield return null;
        firebaseManager.auth.StateChanged += AuthStateChanged;
        firebaseManager.OnInfoMessage += HandleAnyMessage;
        firebaseManager.OnErrorMessage += HandleAnyMessage;
    }

    void OnDestroy() { if (firebaseManager != null && firebaseManager.auth != null) firebaseManager.auth.StateChanged -= AuthStateChanged; }

    void HandleAnyMessage(string msg)
    {
        if (textRemindError)
        {
            textRemindError.text = msg ?? "";

            if (remindErrorCoroutine != null)
            {
                StopCoroutine(remindErrorCoroutine);
            }

            if (!string.IsNullOrEmpty(msg))
            {
                remindErrorCoroutine = StartCoroutine(ClearRemindErrorAfterDelay());
            }
        }
    }

    private System.Collections.IEnumerator ClearRemindErrorAfterDelay()
    {
        yield return new WaitForSeconds(10f);

        if (textRemindError)
        {
            textRemindError.text = "";
        }

        remindErrorCoroutine = null;
    }

    public void Register() => firebaseManager.Register(inputEmail.text, inputPassword.text);
    public void Login() => firebaseManager.Login(inputEmail.text, inputPassword.text);
    public void Logout() { firebaseManager.Logout(); ResetUI(); }

    void ResetUI()
    {
        if (aiAssistant != null) aiAssistant.SetQuizMode(false);
        ShowMicButton();
        currentTopic = "";
        if (firebaseManager != null) firebaseManager.currentTopic = "";

        isWorksheetPassed = false;
        if (btnOpenGraderText != null) btnOpenGraderText.text = "批改學習單";

        if (worksheetGrader != null) worksheetGrader.ResetRetryCount();

        if (textChatHistory != null) textChatHistory.text = "";

        panelLogin.SetActive(true); panelInfo.SetActive(false);
        if (panelRecords != null) panelRecords.SetActive(false);
        if (panelWebduino != null) panelWebduino.SetActive(false);

        if (arCameraObject != null) arCameraObject.SetActive(false);
        if (mainCamera2D != null) mainCamera2D.SetActive(true);
        if (slideImageDisplay != null) slideImageDisplay.gameObject.SetActive(true);
    }

    public void ClickDataAnalysis() => SimpleClick("Data Analysis", "clickDataAnalysis");
    public void ClickDataCrawling() => SimpleClick("Data Crawling", "clickDataCrawling");

    public void ClickArduino()
    {
        SimpleClick("Arduino", "clickArduino");
        currentTopic = "Arduino";

        if (panelInfo != null) panelInfo.SetActive(false);
        if (panelWebduino != null)
        {
            panelWebduino.SetActive(true);
            currentActiveSlides = arduinoSlides;
            currentSlideIndex = 0;

            if (slideImageDisplay != null) slideImageDisplay.gameObject.SetActive(true);
            if (buttonNext) buttonNext.gameObject.SetActive(true);
            if (buttonPrev) buttonPrev.gameObject.SetActive(true);

            if (buttonOpenConceptMap) buttonOpenConceptMap.gameObject.SetActive(false);
            if (buttonConceptMapBack) buttonConceptMapBack.gameObject.SetActive(false);
            if (buttonConceptMapNext) buttonConceptMapNext.gameObject.SetActive(false);
            if (btnOpenGrader != null) btnOpenGrader.gameObject.SetActive(false);

            readingStartTime = Time.time;
            isReading = true;
            hasCompletedReading = false;

            UpdateSlideDisplay();
        }
    }

    public void ClickAutonomousVehicles() => SimpleClick("Autonomous vehicles", "clickAutonomousVehicles");
    public void ClickKebbi() => SimpleClick("Kebbi", "clickKebbi");

    void SimpleClick(string topic, string countKey)
    {
        firebaseManager.SetTopic(topic);
        firebaseManager.IncrementCount(countKey);
    }

    public void ClickLearningRecords()
    {
        if (panelRecords != null) { panelInfo.SetActive(false); panelLogin.SetActive(false); panelRecords.SetActive(true); }
        firebaseManager.FetchLastLogin(UpdateLastLoginUI); firebaseManager.FetchLastLogin(UpdateLastLoginLabelUI); firebaseManager.FetchRecords(UpdateCountsAndEventsUI);
    }
    public void BackToInfoFromRecords() { if (panelRecords != null) panelRecords.SetActive(false); panelInfo.SetActive(true); }
    public void ClickExit() { Application.Quit(); }

    public void ClickWebduino()
    {
        SimpleClick("Webduino", "clickWebduino");
        currentTopic = "Webduino";

        if (panelInfo != null) panelInfo.SetActive(false);
        if (panelWebduino != null)
        {
            panelWebduino.SetActive(true); currentActiveSlides = webduinoSlides; currentSlideIndex = 0;

            if (slideImageDisplay != null) slideImageDisplay.gameObject.SetActive(true);

            if (buttonNext) buttonNext.gameObject.SetActive(true); if (buttonPrev) buttonPrev.gameObject.SetActive(true);

            if (buttonOpenConceptMap) buttonOpenConceptMap.gameObject.SetActive(false);
            if (buttonConceptMapBack) buttonConceptMapBack.gameObject.SetActive(false);
            if (buttonConceptMapNext) buttonConceptMapNext.gameObject.SetActive(false);

            if (btnOpenGrader != null) btnOpenGrader.gameObject.SetActive(false);

            readingStartTime = Time.time;
            isReading = true;
            hasCompletedReading = false;

            UpdateSlideDisplay();
        }
    }

    public void CloseWebduinoSlide()
    {
        if (aiAssistant != null) aiAssistant.SetQuizMode(false);
        ShowMicButton();
        StopAndLogReadingTimer();

        isShowingTargetHint = false;
        hasShownConfirmHint = false;

        currentTopic = "";
        if (firebaseManager != null) firebaseManager.currentTopic = "";

        isWorksheetPassed = false;
        if (btnOpenGraderText != null) btnOpenGraderText.text = "批改學習單";

        if (panelWebduino != null) panelWebduino.SetActive(false);
        if (panelInfo != null) panelInfo.SetActive(true);
        TrackPanelTime(null);

        if (arCameraObject != null) arCameraObject.SetActive(false);
        if (mainCamera2D != null) mainCamera2D.SetActive(true);

        ResetAvatarToOriginalState();

        if (hintPanelObject != null) hintPanelObject.SetActive(false);
        if (arduinoHintPanelObject != null) arduinoHintPanelObject.SetActive(false);
        if (webduinoTargetHintPanel != null) webduinoTargetHintPanel.SetActive(false);

        CloseAllConfirmAndFeedbackPanels();

        if (videoController != null) videoController.OnPageChanged(-1, "");
        if (btnOpenGrader != null) btnOpenGrader.gameObject.SetActive(false);

        if (worksheetGrader != null) worksheetGrader.CloseGrader();
    }

    public void ClickShowWebduinoDetail() => ShowTopicDetailStats("Webduino");
    public void ClickShowArduinoDetail() => ShowTopicDetailStats("Arduino");

    private void ShowTopicDetailStats(string targetTopic)
    {
        if (panelDetailStats != null) panelDetailStats.SetActive(true);
        if (panelRecords != null) panelRecords.SetActive(false);

        if (textStatOverview) textStatOverview.text = "資料載入中...";
        if (textStatAnalytics) textStatAnalytics.text = "資料載入中...";
        if (textStatChatLogs) textStatChatLogs.text = "資料載入中...";

        if (btnStatPrevs != null)
        {
            foreach (var btn in btnStatPrevs)
            {
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => ChangeStatPage(-1));
                }
            }
        }

        if (btnStatNexts != null)
        {
            foreach (var btn in btnStatNexts)
            {
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => ChangeStatPage(1));
                }
            }
        }

        currentStatPageIndex = 0;
        UpdateStatPageDisplay();

        firebaseManager.FetchLearningStats(targetTopic, stats =>
        {
            if (stats == null)
            {
                if (textStatOverview) textStatOverview.text = $"目前還沒有 {targetTopic} 的學習紀錄喔！";
                if (textStatAnalytics) textStatAnalytics.text = "無資料";
                if (textStatChatLogs) textStatChatLogs.text = "無對話紀錄";
                return;
            }

            StringBuilder sb1 = new StringBuilder();
            sb1.AppendLine($"<size=40><b>{targetTopic} 學習成效總覽</b></size>\n");

            sb1.AppendLine("<b>【課後測驗】</b>");
            sb1.AppendLine($"最終得分：<size=50><color=#F39C12>{stats.quiz_score}</color></size> 分");
            sb1.AppendLine($"測驗總耗時：{stats.quiz_duration:F0} 秒\n");

            sb1.AppendLine("<b>【教材閱讀】</b>");
            sb1.AppendLine($"教材閱讀時間：{stats.reading_duration:F0} 秒");
            sb1.AppendLine($"是否看完教材：{(stats.is_reading_completed ? "<color=#F39C12>是</color>" : "<color=#E74C3C>否</color>")}");
            sb1.AppendLine($"選擇專題主題：{GetHighestPreference(stats.preference)}\n");

            sb1.AppendLine("<b>【概念構圖進度】</b>");
            sb1.AppendLine($"- 拆解問題: {(CheckProgress(stats, "step1_decompose") ? "已完成" : "未完成")}");
            sb1.AppendLine($"- 樣式辨識: {(CheckProgress(stats, "step2_pattern") ? "已完成" : "未完成")}");
            sb1.AppendLine($"- 抽象化(接線): {(CheckProgress(stats, "step3_abstract") ? "已完成" : "未完成")}");
            sb1.AppendLine($"- 演算法(邏輯): {(CheckProgress(stats, "step4_algorithm") ? "已完成" : "未完成")}");

            if (textStatOverview) textStatOverview.text = sb1.ToString();

            StringBuilder sb2 = new StringBuilder();
            sb2.AppendLine($"<size=40><b>{targetTopic} 實作與錯題分析</b></size>\n");

            sb2.AppendLine("<b>【學習單 AI 批改分析】</b>");
            sb2.AppendLine($"重試次數：{stats.worksheet_retry_count} 次");
            string fb = string.IsNullOrEmpty(stats.worksheet_ai_feedback) ? "無批改紀錄" : stats.worksheet_ai_feedback;
            sb2.AppendLine($"<color=#2C3E50>最終評語：\n  {fb}</color>\n");

            sb2.AppendLine("<b>【測驗錯題區】</b>");
            if (stats.quiz_wrong_categories.Count == 0) sb2.AppendLine("<color=#2ECC71>太棒了！沒有答錯任何題目！</color>\n");
            else
            {
                foreach (var kvp in stats.quiz_wrong_categories)
                {
                    sb2.AppendLine($"- {kvp.Key} 概念：錯了 {kvp.Value} 題");
                }
                sb2.AppendLine("");
            }

            sb2.AppendLine("<b>【各部分停留時間】</b>");
            sb2.AppendLine($"- 拆解問題: {GetStatDouble(stats.stay_duration, "decompose_panel"):F0} 秒");
            sb2.AppendLine($"- 樣式辨識: {GetStatDouble(stats.stay_duration, "pattern_panel"):F0} 秒");
            sb2.AppendLine($"- 抽象化: {GetStatDouble(stats.stay_duration, "abstract_panel"):F0} 秒");
            sb2.AppendLine($"- 演算法: {GetStatDouble(stats.stay_duration, "algorithm_panel"):F0} 秒");

            if (textStatAnalytics) textStatAnalytics.text = sb2.ToString();

            StringBuilder sb3 = new StringBuilder();
            sb3.AppendLine($"<size=40><b>{targetTopic} AI 助教對話紀錄</b></size>\n");
            if (stats.chat_history.Count == 0)
            {
                sb3.AppendLine("學生還沒有跟 Kelly 助教聊過天喔！");
            }
            else
            {
                foreach (string log in stats.chat_history)
                {
                    sb3.AppendLine(log);
                    sb3.AppendLine("------------------");
                }
            }
            if (textStatChatLogs) textStatChatLogs.text = sb3.ToString();
        });
    }

    private string GetHighestPreference(Dictionary<string, int> pref)
    {
        if (pref == null || pref.Count == 0) return "尚未選擇";
        var max = pref.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
        if (max == "theme_elderly") return "長輩/健康安全";
        if (max == "theme_classroom") return "教室環境/行車安全";
        if (max == "theme_security") return "防盜鈴/自走車";
        return max;
    }

    private double GetStatDouble(Dictionary<string, double> d, string k) { return d.ContainsKey(k) ? d[k] : 0; }

    private void ChangeStatPage(int direction)
    {
        if (currentStatPageIndex == 2 && direction == 1)
        {
            CloseDetailStats();
            return;
        }

        currentStatPageIndex += direction;
        if (currentStatPageIndex < 0) currentStatPageIndex = 0;
        if (currentStatPageIndex > 2) currentStatPageIndex = 2;
        UpdateStatPageDisplay();
    }

    private void UpdateStatPageDisplay()
    {
        for (int i = 0; i < statPages.Length; i++) { if (statPages[i] != null) statPages[i].SetActive(i == currentStatPageIndex); }
        if (avatarObject != null)
        {
            Animator animator = avatarObject.GetComponent<Animator>();
            if (animator != null && animator.enabled)
            {
                animator.Update(0f);
            }
        }
        if (textStatPageNums != null)
        {
            foreach (var txt in textStatPageNums)
            {
                if (txt != null) txt.text = $"{currentStatPageIndex + 1} / 3";
            }
        }

        if (btnStatPrevs != null)
        {
            foreach (var btn in btnStatPrevs)
            {
                if (btn != null) btn.interactable = (currentStatPageIndex > 0);
            }
        }

        if (btnStatNexts != null)
        {
            foreach (var btn in btnStatNexts)
            {
                if (btn != null)
                {
                    btn.interactable = true;
                    TMP_Text nextBtnText = btn.GetComponentInChildren<TMP_Text>();
                    if (nextBtnText != null)
                    {
                        if (currentStatPageIndex == 2) nextBtnText.text = "關閉";
                        else nextBtnText.text = "下一頁";
                    }
                }
            }
        }
    }

    public void CloseDetailStats()
    {
        if (panelDetailStats) panelDetailStats.SetActive(false);
        if (panelRecords) panelRecords.SetActive(true);

        if (statPages != null) { for (int i = 0; i < statPages.Length; i++) { if (statPages[i] != null) statPages[i].SetActive(false); } }
    }

    public void NextSlide() { ChangeSlide(1); }
    public void PrevSlide() { ChangeSlide(-1); }
    void ChangeSlide(int dir)
    {
        CloseAllInteractionPanels(); TrackPanelTime(null);
        if (currentActiveSlides == null) return;
        int next = currentSlideIndex + dir;
        if (next >= 0 && next < currentActiveSlides.Length) { currentSlideIndex = next; UpdateSlideDisplay(); }
    }

    void UpdateSlideDisplay()
    {
        if (slideImageDisplay == null || currentActiveSlides == null) return;

        slideImageDisplay.gameObject.SetActive(true);
        var c = slideImageDisplay.color;
        c.a = 1f;
        slideImageDisplay.color = c;

        slideImageDisplay.sprite = currentActiveSlides[currentSlideIndex];

        if (videoController != null)
        {
            videoController.OnPageChanged(currentSlideIndex + 1, currentTopic);
        }

        if (aiAssistant != null)
        {
            if (currentActiveSlides == webduinoSlides)
            {
                if (slideIntroductions != null && currentSlideIndex < slideIntroductions.Length)
                {
                    string introText = slideIntroductions[currentSlideIndex];
                    if (!string.IsNullOrEmpty(introText)) aiAssistant.SpeakDirectly(introText);
                }
            }
            else if (currentActiveSlides == arduinoSlides)
            {
                if (arduinoIntroductions != null && currentSlideIndex < arduinoIntroductions.Length)
                {
                    string introText = arduinoIntroductions[currentSlideIndex];
                    if (!string.IsNullOrEmpty(introText)) aiAssistant.SpeakDirectly(introText);
                }
            }
        }

        if (buttonPrev) buttonPrev.interactable = currentSlideIndex > 0;
        if (buttonNext) buttonNext.interactable = currentSlideIndex < currentActiveSlides.Length - 1;
        if (textPageNumber) textPageNumber.text = $"頁次 : {currentSlideIndex + 1} / {currentActiveSlides.Length}";

        bool isWebduinoLast = (currentActiveSlides == webduinoSlides) && (currentSlideIndex == currentActiveSlides.Length - 1);
        bool isArduinoLast = (currentActiveSlides == arduinoSlides) && (currentSlideIndex == currentActiveSlides.Length - 1);
        bool isLast = isWebduinoLast || isArduinoLast;

        if (buttonOpenConceptMap) buttonOpenConceptMap.gameObject.SetActive(isLast);

        if (isLast)
        {
            hasCompletedReading = true;
        }

        bool isDecompose = (currentActiveSlides == nextSectionSlides && currentSlideIndex == 1);
        bool isAbstract = (currentActiveSlides == nextSectionSlides && currentSlideIndex == 2);

        bool isWorksheetStep = (currentActiveSlides == nextSectionSlides && currentSlideIndex == 2);
        if (btnOpenGrader != null) btnOpenGrader.gameObject.SetActive(isWorksheetStep);

        if (btnDecomposeInvisible) btnDecomposeInvisible.gameObject.SetActive(isDecompose);
        if (btnPatternInvisible) btnPatternInvisible.gameObject.SetActive(isDecompose);
        if (btnAbstractInvisible) btnAbstractInvisible.gameObject.SetActive(isAbstract);
        if (btnAlgorithmInvisible) btnAlgorithmInvisible.gameObject.SetActive(isAbstract);
    }

    void AuthStateChanged(object sender, System.EventArgs e)
    {
        if (firebaseManager.user != null) { panelLogin.SetActive(false); panelInfo.SetActive(true); }
        else { panelLogin.SetActive(true); panelInfo.SetActive(false); }
    }
    bool CheckProgress(LearningStats s, string k) { return s.progress.ContainsKey(k) && s.progress[k]; }
    int GetStatInt(Dictionary<string, int> d, string k) { return d.ContainsKey(k) ? d[k] : 0; }
    void UpdateLastLoginUI(string t) { if (textInfoPanelSeconds) textInfoPanelSeconds.text = t; }
    void UpdateLastLoginLabelUI(string t) { if (textLastLogin) textLastLogin.text = t; }
    void UpdateCountsAndEventsUI(UserRecords r) { /* ... */ }
    public string GetCurrentTopic()
    {
        return currentTopic;
    }

    public void HideMicButton()
    {
        if (micButtonObject != null) micButtonObject.SetActive(false);
        if (btnToggleChat != null) btnToggleChat.SetActive(false);
        if (chatInputPanel != null) chatInputPanel.SetActive(false);
    }

    public void ShowMicButton()
    {
        if (micButtonObject != null) micButtonObject.SetActive(true);
        if (btnToggleChat != null) btnToggleChat.SetActive(true);
        if (chatInputPanel != null) chatInputPanel.SetActive(false);
    }

    private bool wasARActiveBeforeGrader = false;

    public void PauseCameraForGrader()
    {
        if (!wasARActiveBeforeGrader)
        {
            wasARActiveBeforeGrader = (arCameraObject != null && arCameraObject.activeSelf);
        }

        if (wasARActiveBeforeGrader && arCameraObject.activeSelf)
        {
            Debug.Log("📸 強制關閉 Vuforia 引擎，釋放硬體鏡頭給拍照功能");

            Behaviour vuforia = arCameraObject.GetComponent("VuforiaBehaviour") as Behaviour;
            if (vuforia != null) vuforia.enabled = false;

            arCameraObject.SetActive(false);
            if (mainCamera2D != null) mainCamera2D.SetActive(true);
            if (avatarObject != null) avatarObject.SetActive(false);
        }
    }

    public void ResumeCameraFromGrader()
    {
        if (wasARActiveBeforeGrader)
        {
            Debug.Log("🔙 拍照結束，重新啟動 Vuforia");
            if (mainCamera2D != null) mainCamera2D.SetActive(false);
            if (arCameraObject != null)
            {
                arCameraObject.SetActive(true);
                Behaviour vuforia = arCameraObject.GetComponent("VuforiaBehaviour") as Behaviour;
                if (vuforia != null) vuforia.enabled = true;
            }
            if (avatarObject != null) avatarObject.SetActive(true);
            wasARActiveBeforeGrader = false;
        }
    }

    public void MarkWorksheetPassed()
    {
        isWorksheetPassed = true;
        if (btnOpenGraderText != null)
        {
            btnOpenGraderText.text = "完成學習單";
        }
        Debug.Log("✅ 學習單已通過！按鈕已解鎖，下次點擊將進入測驗。");
    }

    public void OnGraderButtonSmartClick()
    {
        if (isWorksheetPassed)
        {
            Debug.Log("👉 已經通關！關閉 AR 並進入課後測驗");
            ExitARMode(true);
        }
        else
        {
            Debug.Log("📸 還沒通關，準備打開拍照介面");
            StartCoroutine(SwitchToGraderRoutine());
        }
    }

    private IEnumerator SwitchToGraderRoutine()
    {
        PauseCameraForGrader();
        yield return new WaitForSeconds(0.5f);
        if (worksheetGrader != null)
        {
            worksheetGrader.gameObject.SetActive(true);
            worksheetGrader.OpenGrader();
        }
    }

    public void ResumeARCameraWithDelay()
    {
        StartCoroutine(ResumeARCameraRoutine());
    }

    private IEnumerator ResumeARCameraRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        ResumeCameraFromGrader();
    }
}
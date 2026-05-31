using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorksheetFlowManager : MonoBehaviour
{
    [Header("場景依賴")]
    public MainScene mainScene;
    public ARImageTracker arImageTracker;

    [Header("根面板（WorksheetFlowPanel）")]
    public GameObject rootPanel;

    [Header("主題選擇 UI")]
    public GameObject panelThemeSelect;

    [Tooltip("3 個主題說明彈窗，索引對應主題 0/1/2")]
    public GameObject[] themeInfoPanels = new GameObject[3];

    [Header("步驟引導 UI")]
    public GameObject panelStepGuidance;
    public TMP_Text textStepNumber;
    public TMP_Text textStepTitle;
    public TMP_Text textStepContent;
    public Button btnPlayAR;
    public Button btnPrevStep;   // 上一步（步驟3、4才顯示）
    public Button btnNextStep;   // 下一步（步驟1~3）／完成引導（步驟4）

    [Header("AR 模式覆蓋 UI（掃描時顯示）")]
    public Button btnARDone;

    [Header("AR 動畫物件（主題 × 步驟，共 12 格）")]
    [Tooltip("索引 = 主題編號 * 4 + 步驟編號\n" +
             "主題0步驟0=0, 主題0步驟1=1, ..., 主題2步驟3=11")]
    public GameObject[] arAnimations = new GameObject[12];

    [Header("步驟編號文字（3主題 × 4步驟 = 12格，主題優先）")]
    public string[] stepNumbers = new string[12];

    [Header("步驟標題（同上 12格）")]
    public string[] stepTitles = new string[12];

    [Header("步驟引導內容（同上 12格）")]
    [TextArea(4, 8)]
    public string[] stepContents = new string[12];

    private int _selectedTheme = -1;
    private int _currentStep = 0;

    private static readonly string[] StepLabelsFallback =
    {
        "步驟 1 / 拆解問題",
        "步驟 2 / 樣式辨識",
        "步驟 3 / 抽象化",
        "步驟 4 / 演算法"
    };

    public bool IsActive =>
        (panelThemeSelect != null && panelThemeSelect.activeSelf) ||
        (panelStepGuidance != null && panelStepGuidance.activeSelf);

    void Awake()
    {
        if (btnPlayAR != null)   btnPlayAR.onClick.AddListener(ClickPlayAR);
        if (btnPrevStep != null) btnPrevStep.onClick.AddListener(ClickPrevStep);
        if (btnNextStep != null) btnNextStep.onClick.AddListener(ClickNextStep);
        if (btnARDone != null)   btnARDone.onClick.AddListener(OnARDonePressed);
    }

    // ─── 主題選擇 ───────────────────────────────────

    public void ShowThemeSelection()
    {
        _selectedTheme = -1;
        _currentStep = 0;

        SetPanel(rootPanel, true);
        SetPanel(panelThemeSelect, true);
        SetPanel(panelStepGuidance, false);
        SetButton(btnARDone, false);
        CloseAllThemeInfos();

        if (mainScene != null) mainScene.HideAvatar();
    }

    public void SelectTheme(int themeIndex)
    {
        _selectedTheme = themeIndex;
        SetPanel(panelThemeSelect, false);
        CloseAllThemeInfos();
        ShowStepGuidance(0);
    }

    public void ShowThemeInfo(int themeIndex)
    {
        for (int i = 0; i < themeInfoPanels.Length; i++)
            SetPanel(themeInfoPanels[i], i == themeIndex);
    }

    public void CloseAllThemeInfos()
    {
        foreach (var p in themeInfoPanels)
            SetPanel(p, false);
    }

    // 步驟介紹頁叉叉按鈕 → 回到主題選擇
    public void CloseStepGuidance()
    {
        if (mainScene != null) mainScene.SetGraderButtonVisible(false);
        SetPanel(panelStepGuidance, false);
        ShowThemeSelection();
    }

    // 主題選擇畫面右上角叉叉按鈕 → 回到教材最後一頁
    public void CloseThemeSelectPanel()
    {
        mainScene.BackToLastSlide();
    }

    // ─── 步驟引導 ───────────────────────────────────

    void ShowStepGuidance(int stepIndex)
    {
        _currentStep = stepIndex;

        int idx = _selectedTheme * 4 + stepIndex;

        if (textStepNumber != null)
        {
            string numText = (idx < stepNumbers.Length && !string.IsNullOrEmpty(stepNumbers[idx]))
                ? stepNumbers[idx]
                : (stepIndex < StepLabelsFallback.Length ? StepLabelsFallback[stepIndex] : $"步驟 {stepIndex + 1}");
            textStepNumber.text = numText;
        }

        if (textStepTitle != null)
            textStepTitle.text = idx < stepTitles.Length ? stepTitles[idx] : "";

        if (textStepContent != null)
            textStepContent.text = idx < stepContents.Length ? stepContents[idx] : "";

        SetPanel(panelStepGuidance, true);
        SetButton(btnARDone, false);

        SetButton(btnPlayAR, true);

        bool showPrev = (stepIndex >= 1);
        SetButton(btnPrevStep, showPrev);
        SetButton(btnNextStep, true);

        if (mainScene != null) mainScene.SetGraderButtonVisible(stepIndex == 3);
    }

    // ─── AR 動畫播放 ────────────────────────────────

    public void ClickPlayAR()
    {
        int idx = _selectedTheme * 4 + _currentStep;
        GameObject anim = (idx >= 0 && idx < arAnimations.Length) ? arAnimations[idx] : null;

        if (arImageTracker != null)
            arImageTracker.SetActiveAnimation(anim);

        SetPanel(panelStepGuidance, false);
        SetButton(btnARDone, true);

        mainScene.EnterARModeForStep();
    }

    // AR 看完按「完成觀看」後回到步驟介紹，顯示導航按鈕
    public void OnARDonePressed()
    {
        mainScene.ExitARModeForStep();

        SetButton(btnARDone, false);
        SetPanel(panelStepGuidance, true);
        SetButton(btnPlayAR, true);

        // 按鈕顯示規則：
        bool showPrev = (_currentStep >= 1);
        SetButton(btnPrevStep, showPrev);
        SetButton(btnNextStep, true);
    }

    // ─── 步驟導航 ───────────────────────────────────

    public void ClickPrevStep()
    {
        if (_currentStep > 0)
            ShowStepGuidance(_currentStep - 1);
        else
            ShowThemeSelection();
    }

    public void ClickNextStep()
    {
        int next = _currentStep + 1;
        if (next < 4)
        {
            ShowStepGuidance(next);
        }
        else
        {
            SetPanel(panelStepGuidance, false);
            mainScene.OnAllWorksheetStepsDone();
        }
    }

    // ─── 返回邏輯（左上角返回鍵）───────────────────

    public void HandleBack()
    {
        if (panelThemeSelect != null && panelThemeSelect.activeSelf)
        {
            mainScene.CloseWorksheetFlow();
            return;
        }

        if (panelStepGuidance != null && panelStepGuidance.activeSelf)
        {
            if (_currentStep == 0)
            {
                SetPanel(panelStepGuidance, false);
                ShowThemeSelection();
            }
            else
            {
                ShowStepGuidance(_currentStep - 1);
            }
        }
    }

    // ─── 重置 ───────────────────────────────────────

    public void Reset()
    {
        _selectedTheme = -1;
        _currentStep = 0;
        SetPanel(panelThemeSelect, false);
        SetPanel(panelStepGuidance, false);
        SetButton(btnARDone, false);
        CloseAllThemeInfos();
        SetPanel(rootPanel, false);

        if (mainScene != null) mainScene.SetGraderButtonVisible(false);

        if (arImageTracker != null)
            arImageTracker.ResetTracking();
    }

    // ─── 輔助 ───────────────────────────────────────

    static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    static void SetButton(Button btn, bool active)
    {
        if (btn != null) btn.gameObject.SetActive(active);
    }
}

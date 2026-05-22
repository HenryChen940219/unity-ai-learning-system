using UnityEngine;
using UnityEngine.UI;
using TMPro; // 【關鍵】這行是為了讓程式看得懂新版文字

public class HintController : MonoBehaviour
{
    [Header("UI 元件設定")]
    public GameObject hintPanel;    // 整個提示框

    // ↓↓↓ 這裡改成了 TMP_Text，這樣舊版 Text 或新版 TextMeshPro 都能吃
    public TMP_Text titleText;
    public TMP_Text contentText;

    public Button nextButton;       // 下一步按鈕

    [Header("提示內容設定")]
    public string[] titles = new string[3];
    [TextArea]
    public string[] contents = new string[3];

    private int currentIndex = 0;

    void Start()
    {
        // 確保一開始按鈕有被監聽
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClick);
    }

    void OnEnable()
    {
        ResetHints();
    }

    public void ResetHints()
    {
        currentIndex = 0;
        if (hintPanel != null) hintPanel.SetActive(true);
        UpdateUI();
    }

    void OnNextClick()
    {
        currentIndex++;

        if (currentIndex < titles.Length)
        {
            UpdateUI();
        }
        else
        {
            // 提示結束，關閉視窗
            if (hintPanel != null) hintPanel.SetActive(false);
        }
    }

    void UpdateUI()
    {
        if (currentIndex < titles.Length)
        {
            // 這裡加了防呆機制，避免你忘了拉東西進去報錯
            if (titleText != null) titleText.text = titles[currentIndex];
            if (contentText != null) contentText.text = contents[currentIndex];
        }
    }
}
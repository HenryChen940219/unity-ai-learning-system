using UnityEngine;
using System.Collections;

public class Stage1LampController : MonoBehaviour
{
    [Header("燈光")]
    [Tooltip("把燈具的 Point Light 拖到這裡")]
    public Light lampLight;

    [Header("閃爍設定")]
    [Tooltip("亮燈持續秒數")]
    public float onDuration = 2f;
    [Tooltip("熄燈持續秒數")]
    public float offDuration = 2f;

    [Header("提示面板")]
    [Tooltip("把提示文字的 Canvas 或 Panel GameObject 拖到這裡")]
    public GameObject hintPanel;

    private Coroutine _blinkRoutine;

    void OnEnable()
    {
        if (hintPanel != null)
            hintPanel.SetActive(true);

        _blinkRoutine = StartCoroutine(BlinkLoop());
    }

    void OnDisable()
    {
        if (_blinkRoutine != null)
            StopCoroutine(_blinkRoutine);

        if (lampLight != null)
            lampLight.enabled = false;

        if (hintPanel != null)
            hintPanel.SetActive(false);
    }

    private IEnumerator BlinkLoop()
    {
        while (true)
        {
            if (lampLight != null) lampLight.enabled = true;
            yield return new WaitForSeconds(onDuration);

            if (lampLight != null) lampLight.enabled = false;
            yield return new WaitForSeconds(offDuration);
        }
    }
}

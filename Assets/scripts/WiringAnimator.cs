using System.Collections;
using UnityEngine;
using TMPro;

public class WiringAnimator : MonoBehaviour
{
    [System.Serializable]
    public class WireDefinition
    {
        public string label;
        public Transform startPoint;
        public Transform endPoint;
        public Color wireColor = Color.red;
        [Range(0.001f, 0.01f)] public float width = 0.003f;
    }

    [Header("接線定義（Inspector 拖入起點/終點 Transform）")]
    [SerializeField] private WireDefinition[] wires = new WireDefinition[]
    {
        new WireDefinition { label = "光敏電阻 → A0", wireColor = new Color(1f, 0.6f, 0f) },
        new WireDefinition { label = "LED → D13",    wireColor = new Color(1f, 0.15f, 0.15f) }
    };

    [Header("動畫速度設定")]
    [SerializeField] private float drawDuration = 0.7f;
    [SerializeField] private float delayBetweenWires = 0.4f;
    [SerializeField] private float startDelay = 0.3f;

    [Header("循環設定")]
    [SerializeField] private float lightOnDuration = 1.0f;
    [SerializeField] private float lightFadeDuration = 0.5f;
    [SerializeField] private float resetDelay = 0.5f;

    [Header("燈光控制（拖入 Stage1LampController）")]
    [SerializeField] private Stage1LampController lampController;

    [Header("標籤顯示（選填，順序對應 wires）")]
    [SerializeField] private TMP_Text[] wireLabels;

    [Header("接線材質（留空自動建立）")]
    [SerializeField] private Material wireMaterial;

    private LineRenderer[] _lines;
    private Coroutine _animCoroutine;
    private Light _light;
    private float _lightMaxIntensity;

    void OnEnable()
    {
        // 取得燈光並接管控制權（停止 Stage1LampController 的閃爍）
        if (lampController != null)
        {
            lampController.enabled = false;
            _light = lampController.lampLight;

            // Stage1LampController.OnDisable 會把 hintPanel 藏起來，這裡補回來
            if (lampController.hintPanel != null)
                lampController.hintPanel.SetActive(true);
        }

        if (_light != null)
        {
            _lightMaxIntensity = _light.intensity > 0f ? _light.intensity : 1.5f;
            _light.intensity = 0f;
            _light.enabled = false;
        }

        CleanupLines();
        HideAllLabels();
        _animCoroutine = StartCoroutine(PlayLoop());
    }

    void OnDisable()
    {
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        CleanupLines();
        HideAllLabels();

        if (_light != null)
        {
            _light.enabled = false;
            _light.intensity = _lightMaxIntensity;
        }

        // 歸還燈光控制權
        if (lampController != null)
            lampController.enabled = true;
    }

    IEnumerator PlayLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(startDelay);
            yield return StartCoroutine(DrawAllWires());

            // 燈泡淡入
            yield return StartCoroutine(FadeLight(true));

            // 燈亮著停留
            yield return new WaitForSeconds(lightOnDuration);

            // 燈泡淡出 + 線消失
            yield return StartCoroutine(FadeLight(false));
            CleanupLines();
            HideAllLabels();

            // 短暫停頓後重播
            yield return new WaitForSeconds(resetDelay);
        }
    }

    IEnumerator DrawAllWires()
    {
        _lines = new LineRenderer[wires.Length];

        for (int i = 0; i < wires.Length; i++)
        {
            var wire = wires[i];
            if (wire.startPoint == null || wire.endPoint == null)
            {
                Debug.LogWarning($"[WiringAnimator] Wire '{wire.label}' 缺少 startPoint 或 endPoint");
                continue;
            }

            var go = new GameObject($"Wire_{i}");
            go.transform.SetParent(transform, true);

            var lr = go.AddComponent<LineRenderer>();
            InitLineRenderer(lr, wire);
            _lines[i] = lr;

            ShowLabel(i, wire.label);
            yield return StartCoroutine(DrawWire(lr, wire.startPoint.position, wire.endPoint.position));

            if (i < wires.Length - 1)
                yield return new WaitForSeconds(delayBetweenWires);
        }
    }

    IEnumerator DrawWire(LineRenderer lr, Vector3 from, Vector3 to)
    {
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, from);

        float elapsed = 0f;
        while (elapsed < drawDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / drawDuration);
            lr.SetPosition(1, Vector3.Lerp(from, to, t));
            yield return null;
        }
        lr.SetPosition(1, to);
    }

    IEnumerator FadeLight(bool fadeIn)
    {
        if (_light == null) yield break;

        if (fadeIn)
        {
            _light.enabled = true;
            float e = 0f;
            while (e < lightFadeDuration)
            {
                e += Time.deltaTime;
                _light.intensity = Mathf.Lerp(0f, _lightMaxIntensity, e / lightFadeDuration);
                yield return null;
            }
            _light.intensity = _lightMaxIntensity;
        }
        else
        {
            float e = 0f;
            float start = _light.intensity;
            while (e < lightFadeDuration)
            {
                e += Time.deltaTime;
                _light.intensity = Mathf.Lerp(start, 0f, e / lightFadeDuration);
                yield return null;
            }
            _light.intensity = 0f;
            _light.enabled = false;
        }
    }

    void InitLineRenderer(LineRenderer lr, WireDefinition wire)
    {
        // Sprites/Default 支援 LineRenderer 頂點顏色，URP/Unlit 不支援
        var mat = new Material(Shader.Find("Sprites/Default"));
        lr.material = mat;
        lr.startColor = wire.wireColor;
        lr.endColor = wire.wireColor;
        lr.startWidth = wire.width;
        lr.endWidth = wire.width;
        lr.useWorldSpace = true;
        lr.generateLightingData = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.positionCount = 0;
    }

    void ShowLabel(int index, string text)
    {
        if (wireLabels == null || index >= wireLabels.Length || wireLabels[index] == null) return;
        wireLabels[index].text = text;
        wireLabels[index].gameObject.SetActive(true);
    }

    void HideAllLabels()
    {
        if (wireLabels == null) return;
        foreach (var lbl in wireLabels)
            if (lbl != null) lbl.gameObject.SetActive(false);
    }

    void CleanupLines()
    {
        if (_lines == null) return;
        foreach (var lr in _lines)
            if (lr != null) Destroy(lr.gameObject);
        _lines = null;
    }
}

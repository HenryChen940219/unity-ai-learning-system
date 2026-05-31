using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WireAnimator : MonoBehaviour
{
    [Header("接線起終點")]
    public Transform startPin;   // Webduino 板腳位
    public Transform endPin;     // 元件（LED/感測器）腳位

    [Header("動畫參數")]
    [Range(0f, 1f)]
    public float progress = 0f;  // 由 Animator 控制此值 0→1

    [Header("導線外觀")]
    public int segments = 24;    // 越高越平滑
    public float sagAmount = 0.03f;  // 下垂幅度（模擬重力）

    private LineRenderer _lr;

    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (startPin == null || endPin == null) return;
        DrawWire();
    }

    void DrawWire()
    {
        int count = Mathf.Max(2, Mathf.RoundToInt(segments * progress));
        _lr.positionCount = count;

        Vector3 start = startPin.position;
        // 終點隨 progress 從起點移動到真正終點
        Vector3 end = Vector3.Lerp(startPin.position, endPin.position, progress);
        // 控制點：中間偏下，製造自然下垂弧度
        Vector3 ctrl = (start + end) * 0.5f + Vector3.down * sagAmount;

        for (int i = 0; i < count; i++)
        {
            float t = (count > 1) ? (float)i / (count - 1) : 0f;
            _lr.SetPosition(i, Bezier(start, ctrl, end, t));
        }
    }

    static Vector3 Bezier(Vector3 a, Vector3 ctrl, Vector3 b, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * ctrl + t * t * b;
    }
}

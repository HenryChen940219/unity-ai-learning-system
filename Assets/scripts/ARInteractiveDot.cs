using UnityEngine;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch;

[RequireComponent(typeof(SphereCollider))]
public class ARInteractiveDot : MonoBehaviour
{
    [Header("編號顯示")]
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private string dotLabel = "1";

    [Header("連結內容")]
    [SerializeField] private GameObject animationObject;
    [SerializeField] private GameObject uiPanel;

    [Header("共用關閉按鈕（三個 Dot 共用同一個叉叉）")]
    [SerializeField] private GameObject closeButton;

    [Header("相機（留空自動找）")]
    [SerializeField] private Camera arCamera;

    [Header("外觀顏色")]
    [SerializeField] private Color dotColor = new Color(0.8f, 0.1f, 1f);
    [SerializeField] private Color emissionColor = new Color(0.6f, 0f, 1f);
    [SerializeField][Range(0f, 3f)] private float emissionIntensity = 1.5f;

    // 追蹤目前開啟的 Dot，供叉叉按鈕關閉用
    private static ARInteractiveDot _currentOpen;

    private bool _isOpen;
    private Renderer _renderer;

    void OnEnable()  { EnhancedTouchSupport.Enable(); }
    void OnDisable() { EnhancedTouchSupport.Disable(); }

    void Start()
    {
        if (labelText != null) labelText.text = dotLabel;
        if (animationObject != null) animationObject.SetActive(false);
        if (uiPanel != null) uiPanel.SetActive(false);

        GetComponent<SphereCollider>().isTrigger = false;

        _renderer = GetComponentInChildren<Renderer>();
        ApplyColor();

        if (arCamera == null)
        {
            var xr = FindObjectOfType<XROrigin>();
            if (xr != null) arCamera = xr.Camera;
        }
        if (arCamera == null) arCamera = Camera.main;
    }

    void ApplyColor()
    {
        if (_renderer == null) return;
        var mat = _renderer.material;
        mat.color = dotColor;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emissionColor * emissionIntensity);
    }

    void Update()
    {
        if (!TryGetTapPosition(out Vector2 screenPos)) return;
        if (arCamera == null) return;

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.gameObject == gameObject)
                Toggle();
        }
    }

    void Toggle()
    {
        // 如果別的 Dot 正在開著，先關掉
        if (_currentOpen != null && _currentOpen != this)
            _currentOpen.ForceClose();

        _isOpen = !_isOpen;
        if (animationObject != null) animationObject.SetActive(_isOpen);
        if (uiPanel != null) uiPanel.SetActive(_isOpen);

        if (_isOpen)
            _currentOpen = this;
        else
            _currentOpen = null;

        UpdateCloseButton();
    }

    // 叉叉按鈕呼叫這個
    public void ForceClose()
    {
        _isOpen = false;
        if (animationObject != null) animationObject.SetActive(false);
        if (uiPanel != null) uiPanel.SetActive(false);
        if (_currentOpen == this) _currentOpen = null;
        UpdateCloseButton();
    }

    // 給叉叉按鈕的 OnClick 綁定（靜態入口）
    public static void CloseCurrentFromButton()
    {
        if (_currentOpen != null)
            _currentOpen.ForceClose();
    }

    void UpdateCloseButton()
    {
        if (closeButton != null)
            closeButton.SetActive(_isOpen);
    }

    static bool TryGetTapPosition(out Vector2 position)
    {
        foreach (var touch in ETouch.Touch.activeTouches)
        {
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                position = touch.screenPosition;
                return true;
            }
        }

        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            position = mouse.position.ReadValue();
            return true;
        }

        position = default;
        return false;
    }
}

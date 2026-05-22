using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch;

public class DragRotate : MonoBehaviour
{
    [Header("旋轉速度")]
    public float rotateSpeed = 0.4f;

    private Vector2 _lastPos;
    private bool _isDragging;

    void OnEnable()  { EnhancedTouchSupport.Enable(); }
    void OnDisable() { EnhancedTouchSupport.Disable(); }

    void Update()
    {
        var touches = ETouch.Touch.activeTouches;
        if (touches.Count == 1)
        {
            var t = touches[0];
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                _lastPos = t.screenPosition;
                _isDragging = true;
            }
            else if (t.phase == UnityEngine.InputSystem.TouchPhase.Moved && _isDragging)
            {
                float deltaX = t.screenPosition.x - _lastPos.x;
                transform.Rotate(Vector3.up, -deltaX * rotateSpeed, Space.World);
                _lastPos = t.screenPosition;
            }
            else if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended)
                _isDragging = false;
            return;
        }

        // 電腦滑鼠測試
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) return;
        if (mouse.leftButton.wasPressedThisFrame)
        {
            _lastPos = mouse.position.ReadValue();
            _isDragging = true;
        }
        else if (mouse.leftButton.isPressed && _isDragging)
        {
            float deltaX = mouse.position.ReadValue().x - _lastPos.x;
            transform.Rotate(Vector3.up, -deltaX * rotateSpeed, Space.World);
            _lastPos = mouse.position.ReadValue();
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
            _isDragging = false;
    }
}

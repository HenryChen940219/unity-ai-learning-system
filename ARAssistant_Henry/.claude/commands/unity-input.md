---
name: unity-input
description: >
  Unity 6 Input System guide. Use when handling player input, controls, gamepad, keyboard, mouse, touch, or XR controllers. Covers the new Input System package (recommended), Input Actions, Action Maps, Control Schemes, PlayerInput component, and input debugging. Based on Unity 6.3 LTS documentation.
---

# Unity Input System

## Input System Overview: New vs Legacy

| Feature | New Input System (Recommended) | Legacy Input Manager |
|---------|-------------------------------|---------------------|
| Package | `com.unity.inputsystem` | Built-in (`UnityEngine.Input`) |
| Architecture | Action-based, event-driven | Polling-based |
| Device Support | Gamepad, keyboard, mouse, touch, XR, custom | Keyboard, mouse, joystick |
| Rebinding | Runtime rebinding support | Not supported |

**Namespace:** `UnityEngine.InputSystem`

## Quick Start

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    InputAction moveAction;
    InputAction jumpAction;

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    void Update()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        transform.Translate(new Vector3(moveValue.x, 0, moveValue.y) * Time.deltaTime * 5f);

        if (jumpAction.IsPressed()) { /* Jump */ }
    }
}
```

## Reading Input in Code

### Polling (in Update)
```csharp
Vector2 move = moveAction.ReadValue<Vector2>();
if (jumpAction.IsPressed()) { /* held down */ }
if (jumpAction.WasPressedThisFrame()) { /* just pressed */ }
if (jumpAction.WasReleasedThisFrame()) { /* just released */ }
```

### Callbacks (Event-Driven)
```csharp
void OnEnable()
{
    fireAction.performed += OnFirePerformed;
    fireAction.Enable();
}

void OnDisable()
{
    fireAction.performed -= OnFirePerformed;
    fireAction.Disable();
}

void OnFirePerformed(InputAction.CallbackContext ctx) { /* Fire! */ }
```

## Keyboard and Mouse

```csharp
var kb = Keyboard.current;
if (kb.spaceKey.wasPressedThisFrame) { /* space pressed */ }
if (kb.spaceKey.isPressed) { /* space held */ }
if (kb.spaceKey.wasReleasedThisFrame) { /* space released */ }

var mouse = Mouse.current;
Vector2 mousePos = mouse.position.ReadValue();
bool leftClick = mouse.leftButton.isPressed;
```

## Touch

```csharp
// High-level EnhancedTouch API (recommended):
using UnityEngine.InputSystem.EnhancedTouch;

void OnEnable() => EnhancedTouchSupport.Enable();
void OnDisable() => EnhancedTouchSupport.Disable();

void Update()
{
    foreach (var touch in Touch.activeTouches)
        Debug.Log($"{touch.touchId}: {touch.screenPosition}");
}
```

## PlayerInput Component

| Behavior | Mechanism | Best For |
|----------|-----------|----------|
| **Send Messages** | `SendMessage()` | Prototyping |
| **Invoke Unity Events** | Inspector-configured | Designer-friendly |
| **Invoke C# Events** | `onActionTriggered` | Programmer control |

```csharp
public class PlayerActions : MonoBehaviour
{
    public void OnJump() { /* Called by PlayerInput */ }
    public void OnMove(InputValue value)
    {
        Vector2 v = value.Get<Vector2>(); // Only valid during this callback
    }
}

// Switch action maps:
playerInput.SwitchCurrentActionMap("UI");
playerInput.SwitchCurrentActionMap("Player");
```

## Action Lifecycle

Actions begin **disabled**. Call `.Enable()` before they respond to input.
Cannot modify bindings while enabled; call `.Disable()` first.

```csharp
void OnEnable() { moveAction.Enable(); }
void OnDisable() { moveAction.Disable(); }
```

## Anti-Patterns

| Anti-Pattern | Problem | Correct Approach |
|-------------|---------|-----------------|
| Using `Input.GetKey()` (legacy) | Incompatible with new Input System | Use `InputAction` with bindings |
| Reading actions without `.Enable()` | Returns no values | Always call `Enable()` in `OnEnable()` |
| Forgetting `.Disable()` on cleanup | Memory leaks | Call `Disable()` in `OnDisable()` |
| Accessing `Gamepad.current` without null check | Crashes if no gamepad | Always check `if (Gamepad.current == null) return;` |
| Not unsubscribing from action callbacks | Errors on scene reload | Unsubscribe in `OnDisable()` |
| Using `InputValue` outside its callback | Value is only valid during callback frame | Copy value to a field immediately |

## Key API Quick Reference

| API | Purpose |
|-----|---------|
| `InputSystem.actions.FindAction("name")` | Find action by name |
| `action.ReadValue<T>()` | Read current value |
| `action.IsPressed()` | Button held check |
| `action.WasPressedThisFrame()` | Button just pressed |
| `action.WasReleasedThisFrame()` | Button just released |
| `action.Enable()` / `action.Disable()` | Activate/deactivate |
| `Keyboard.current` | Current keyboard |
| `Mouse.current` | Current mouse |
| `Touchscreen.current` | Current touchscreen |
| `EnhancedTouchSupport.Enable()` | Enable enhanced touch API |
| `Touch.activeTouches` | All active touches |
| `PlayerInput.SwitchCurrentActionMap()` | Change active action map |

## Additional Resources

- [Input System Manual](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/index.html)
- [Actions Reference](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/Actions.html)
- [PlayerInput Component](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/PlayerInput.html)

---
name: unity-ui
description: >
  Unity 6 UI development guide. Use when building user interfaces, menus, HUD, buttons, or any UI elements. Covers UI Toolkit (recommended for new projects — USS, UXML, UI Builder, data binding), uGUI/Canvas (legacy runtime UI), TextMeshPro, and IMGUI. Based on Unity 6.3 LTS documentation.
---

# Unity UI Systems

Unity provides three UI frameworks. **UI Toolkit is the recommended system for new projects.** uGUI remains supported for legacy and certain runtime use cases.

## UI System Comparison

| Feature | UI Toolkit | uGUI (Canvas) | IMGUI |
|---|---|---|---|
| Recommended for new projects | Yes | No (legacy) | No |
| Runtime game UI | Yes | Yes | Not recommended |
| Editor extensions | Yes | No | Yes |
| Layout system | Flexbox (Yoga) | RectTransform + Anchors | Immediate mode |
| Styling | USS stylesheets | Per-component properties | GUIStyle / GUISkin |
| Performance | Optimized retained mode | Canvas batching | Redraws every frame |

**Decision guide:**
- New runtime UI (menus, HUD, inventory) → **UI Toolkit**
- Existing project with uGUI → Continue with **uGUI**, migrate incrementally
- Quick debug overlays in Editor → **IMGUI**

---

## UI Toolkit

### Core Architecture

```
UIDocument (MonoBehaviour)
  --> VisualTreeAsset (.uxml)  -- defines structure
  --> StyleSheet (.uss)        -- defines appearance
  --> C# script                -- defines behavior
```

### UXML Structure

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:Style src="MainMenu.uss" />
    <ui:VisualElement name="root-container" class="container">
        <ui:Label text="Game Menu" class="title" />
        <ui:Button text="Play" name="play-button" class="menu-btn" />
        <ui:Toggle label="Fullscreen" name="fullscreen-toggle" />
        <ui:Slider label="Volume" low-value="0" high-value="100" name="volume-slider" />
    </ui:VisualElement>
</ui:UXML>
```

### USS Styling

```css
.menu-btn {
    width: 200px;
    height: 40px;
    font-size: 16px;
    color: #FFFFFF;
    background-color: #2D2D2D;
    border-radius: 4px;
}

.menu-btn:hover { background-color: #555555; }
.menu-btn:disabled { opacity: 0.5; }

:root {
    --primary-color: #4CAF50;
}
.title { color: var(--primary-color); }
```

### C# Setup

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private Button playButton;
    private Slider volumeSlider;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        playButton = root.Q<Button>("play-button");
        volumeSlider = root.Q<Slider>("volume-slider");

        playButton.RegisterCallback<ClickEvent>(OnPlayClicked);
        volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);
    }

    private void OnDisable()
    {
        playButton.UnregisterCallback<ClickEvent>(OnPlayClicked);
        volumeSlider.UnregisterValueChangedCallback(OnVolumeChanged);
    }

    private void OnPlayClicked(ClickEvent evt) => Debug.Log("Play clicked");
    private void OnVolumeChanged(ChangeEvent<float> evt) => AudioListener.volume = evt.newValue / 100f;
}
```

### Key API

| API | Purpose |
|---|---|
| `root.Q<T>("name")` | Query single element by name |
| `root.Q<T>(className: "cls")` | Query single element by class |
| `root.Query<T>().ToList()` | Query multiple elements |
| `RegisterCallback<TEvent>(callback)` | Register event handler |
| `UnregisterCallback<TEvent>(callback)` | Remove event handler |
| `RegisterValueChangedCallback(cb)` | Listen for value changes |
| `SetValueWithoutNotify(value)` | Set value silently |
| `AddToClassList("class")` | Add USS class |
| `style.display = DisplayStyle.None` | Hide element |
| `style.display = DisplayStyle.Flex` | Show element |

---

## uGUI / Canvas System (Legacy)

### Canvas Render Modes

| Mode | Use Case |
|---|---|
| **Screen Space - Overlay** | Standard HUD, menus |
| **Screen Space - Camera** | UI with depth effects |
| **World Space** | In-world displays, AR UI |

### Core Components

- **Visual:** `Image`, `RawImage`, `Text` (use TextMeshPro instead)
- **Interaction:** `Button`, `Toggle`, `Slider`, `Dropdown`, `InputField`, `ScrollRect`
- **Layout:** `HorizontalLayoutGroup`, `VerticalLayoutGroup`, `GridLayoutGroup`, `ContentSizeFitter`

### uGUI Example

```csharp
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Slider volumeSlider;

    private void OnEnable()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnDisable()
    {
        playButton.onClick.RemoveListener(OnPlayClicked);
        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }

    private void OnPlayClicked() => Debug.Log("Play");
    private void OnVolumeChanged(float value) => AudioListener.volume = value;
}
```

---

## TextMeshPro

For all text rendering, use **TextMeshPro** (TMP) — not legacy `UI.Text`.

- Use `TextMeshProUGUI` for Canvas (uGUI) UI
- Use `TextMeshPro` for 3D world text
- Use `SetText("Score: {0}", value)` for zero-allocation updates

```csharp
using TMPro;

[SerializeField] private TMP_Text scoreText;
[SerializeField] private TMP_InputField inputField;

void UpdateScore(int score)
{
    scoreText.SetText("Score: {0}", score); // Zero allocation
    // or: scoreText.text = $"Score: {score}"; // Allocates string
}
```

---

## Anti-Patterns

| Anti-Pattern | Problem | Correct Approach |
|---|---|---|
| Using inline styles everywhere | Per-element memory overhead | Use USS files for shared styles |
| Rebuilding entire UI every frame | Defeats retained-mode benefits | Update only changed elements |
| Multiple Canvases with dynamic content (uGUI) | Canvas rebuild on any child change | Split static/dynamic into separate Canvases |
| Not unregistering callbacks | Memory leaks, stale references | Always unregister in `OnDisable` |
| Using `UI.Text` instead of TextMeshPro | Poor rendering quality | Use `TMP_Text` / `TextMeshProUGUI` |
| Forgetting EventSystem in scene (uGUI) | No input events processed | Ensure one EventSystem exists in scene |

## Additional Resources

- [UI Toolkit](https://docs.unity3d.com/6000.3/Documentation/Manual/UIToolkits.html)
- [USS](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-USS.html)
- [UXML](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-UXML.html)
- [uGUI Canvas](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/UICanvas.html)

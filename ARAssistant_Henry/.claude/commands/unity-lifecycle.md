---
name: unity-lifecycle
description: >
  Unity 6 lifecycle correctness patterns. Use when dealing with initialization order bugs, null reference exceptions from destroyed objects, OnEnable/OnDisable event subscription, editor-vs-runtime differences, async methods with object destruction, or Script Execution Order. Based on Unity 6.3 LTS documentation.
globs:
  - "**/*.cs"
---

# Unity Lifecycle & Execution Order -- Correctness Patterns

## PATTERN: Fake-Null Trap (?. and ?? on Destroyed Objects)

WRONG:
```csharp
myComponent?.DoSomething();          // May call method on destroyed object!
var fallback = myComponent ?? other; // May return a destroyed "fake-null" object!
```

RIGHT:
```csharp
if (myComponent != null)
    myComponent.DoSomething();

// Or use implicit bool operator (equivalent to != null for UnityEngine.Object)
if (myComponent)
    myComponent.DoSomething();
```

GOTCHA: Unity overrides `==` to return true for destroyed objects. `?.`, `??`, `is null`, `is not null`, and pattern matching bypass the override and see a valid (non-null) reference. This is the #1 source of `MissingReferenceException`.

---

## PATTERN: Destroy is Deferred

WRONG:
```csharp
foreach (var e in enemies)
    if (e.health <= 0)
        Destroy(e.gameObject); // Modifying collection during iteration = crash
```

RIGHT:
```csharp
// Destroy happens at END of current frame (after all Updates complete)
var toDestroy = enemies.Where(e => e.health <= 0).ToList();
foreach (var e in toDestroy)
{
    enemies.Remove(e);
    Destroy(e.gameObject);
}
// DestroyImmediate: EDITOR ONLY - never use in runtime code
```

---

## PATTERN: Disabled Component Still Gets Awake

```csharp
void Awake()
{
    // Runs even if this component is disabled (depends on GAMEOBJECT active state)
    _rb = GetComponent<Rigidbody>(); // Self-initialization here
}

void Start()
{
    // DEFERRED until component is first enabled
    _target = FindObjectOfType<Player>(); // Cross-references here
}

void OnEnable()
{
    // Runs every time component is enabled (including first time, AFTER Awake, BEFORE Start)
    SubscribeToEvents();
}
```

GOTCHA: Awake depends on **GameObject** active state. Start and OnEnable depend on **component** enabled state.

---

## PATTERN: OnEnable/OnDisable for Event Subscription

WRONG:
```csharp
void Start() { EventManager.OnPlayerDied += HandlePlayerDied; }
void OnDestroy() { EventManager.OnPlayerDied -= HandlePlayerDied; }
// BUG: If object is disabled/re-enabled, events accumulate
```

RIGHT:
```csharp
void OnEnable()
{
    EventManager.OnPlayerDied += HandlePlayerDied;
    SceneManager.sceneLoaded += OnSceneLoaded;
}

void OnDisable()
{
    EventManager.OnPlayerDied -= HandlePlayerDied;
    SceneManager.sceneLoaded -= OnSceneLoaded;
}
```

GOTCHA: `OnEnable`/`OnDisable` handle disable/enable cycles, scene reloads, and destruction. `Start`/`OnDestroy` fails for pooled objects or `DontDestroyOnLoad` objects.

---

## PATTERN: OnValidate is Editor-Only

WRONG:
```csharp
void OnValidate()
{
    _currentHealth = _maxHealth; // Never runs in builds!
}
```

RIGHT:
```csharp
#if UNITY_EDITOR
void OnValidate()
{
    _maxHealth = Mathf.Max(1, _maxHealth); // Clamp only
}
#endif

void Awake()
{
    _currentHealth = _maxHealth; // Runtime initialization here
}
```

---

## PATTERN: Script Execution Order

WRONG:
```csharp
public class Player : MonoBehaviour
{
    void Awake()
    {
        GameManager.Instance.Register(this); // May be null if Player.Awake runs first!
    }
}
```

RIGHT:
```csharp
[DefaultExecutionOrder(-100)] // Negative = runs earlier
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    void Awake() { Instance = this; }
}

public class Player : MonoBehaviour
{
    void Start() // Use Start for cross-references, not Awake
    {
        GameManager.Instance.Register(this);
    }
}
```

---

## PATTERN: Async Methods + Object Destruction

WRONG:
```csharp
async void Start()
{
    await Awaitable.WaitForSecondsAsync(5f);
    transform.position = Vector3.zero; // MissingReferenceException if destroyed!
}
```

RIGHT:
```csharp
async Awaitable Start()
{
    try
    {
        await Awaitable.WaitForSecondsAsync(5f, destroyCancellationToken);
        transform.position = Vector3.zero; // Safe
    }
    catch (OperationCanceledException)
    {
        // Object was destroyed -- expected
    }
}
```

---

## PATTERN: OnApplicationQuit vs OnDestroy

WRONG:
```csharp
void OnDestroy()
{
    SavePlayerData(); // May fail: other objects might already be destroyed
}
```

RIGHT:
```csharp
void OnApplicationQuit()
{
    // Fires BEFORE OnDisable/OnDestroy -- all objects still accessible
    SavePlayerData();
}

void OnDestroy()
{
    _nativeArray.Dispose(); // Cleanup own resources only
}
```

---

## Lifecycle Timing Quick Reference

| Callback | Fires When | Frequency | Scope |
|----------|-----------|-----------|-------|
| `Awake` | Script instance loads (if GO active) | Once | Self-init |
| `OnEnable` | Component/GO enabled | Every enable | Subscribe events |
| `Start` | Before first Update (if enabled) | Once | Cross-references |
| `FixedUpdate` | Fixed timestep | 0-N per frame | Physics |
| `Update` | Every frame | Once per frame | Game logic |
| `LateUpdate` | After all Updates | Once per frame | Camera, follow |
| `OnDisable` | Component/GO disabled | Every disable | Unsubscribe events |
| `OnDestroy` | Object destroyed | Once | Cleanup own resources |
| `OnApplicationQuit` | App exiting | Once | Save data |
| `OnValidate` | Inspector change (EDITOR ONLY) | Many | Clamp fields |

## Additional Resources

- [Execution Order](https://docs.unity3d.com/6000.3/Documentation/Manual/ExecutionOrder.html)
- [MonoBehaviour API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.html)

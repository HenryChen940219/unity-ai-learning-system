---
name: unity-scripting
description: >
  Unity 6 C# scripting guide. Use when writing MonoBehaviour scripts, handling lifecycle events (Awake, Start, Update, FixedUpdate), using coroutines or async/await (Awaitable), working with ScriptableObjects, events, delegates, or core APIs like Vector3, Quaternion, Time, Debug. Based on Unity 6.3 LTS documentation.
globs:
  - "**/*.cs"
---

# Unity C# Scripting

## Script Fundamentals

C# scripts (`.cs` files) are stored in the `Assets` folder. Scripts gain Unity functionality by inheriting from built-in types:

- **UnityEngine.Object** -- Makes custom types assignable to Inspector fields
- **MonoBehaviour** -- Attaches to GameObjects as components to control behavior in a scene
- **ScriptableObject** -- Standalone data assets not attached to GameObjects

Scripts operate in two contexts:
- **Runtime scripts** -- Execute in the Player build (use `UnityEngine` namespace)
- **Editor scripts** -- Run only in the Editor (use `UnityEditor` namespace, place in `Editor` folders)

## MonoBehaviour Lifecycle

### Key Lifecycle Callbacks

| Callback | Timing | Use For |
|----------|--------|---------|
| `Awake()` | Script instance loads | One-time init, cache references |
| `OnEnable()` | Component enabled | Subscribe to events |
| `Start()` | Before first Update | Init that depends on other Awake() calls |
| `FixedUpdate()` | Fixed timestep (default 0.02s) | Physics calculations, Rigidbody forces |
| `Update()` | Every frame | Input, non-physics game logic |
| `LateUpdate()` | After all Update calls | Camera follow, post-Update adjustments |
| `OnDisable()` | Component disabled | Unsubscribe from events |
| `OnDestroy()` | Before destruction | Final cleanup |

### MonoBehaviour Properties (Unity 6)
| Property | Purpose |
|----------|---------|
| `destroyCancellationToken` | Token raised when MonoBehaviour is destroyed (for async cancellation) |
| `didAwake` | Whether Awake has been called |
| `didStart` | Whether Start has been called |

## Coroutines vs Async/Await

### Coroutines (IEnumerator)

```csharp
IEnumerator Fade()
{
    Color c = renderer.material.color;
    for (float alpha = 1f; alpha >= 0; alpha -= 0.1f)
    {
        c.a = alpha;
        renderer.material.color = c;
        yield return new WaitForSeconds(0.1f);
    }
}
```

**Yield Instructions:**
- `yield return null` -- Resume next frame
- `yield return new WaitForSeconds(t)` -- Resume after t seconds
- `yield return new WaitForFixedUpdate()` -- Resume after FixedUpdate
- `yield return new WaitForEndOfFrame()` -- Resume after rendering
- `yield return new WaitUntil(() => condition)` -- Resume when condition is true

### Awaitable (Unity 6 Async/Await)

```csharp
async Awaitable SampleAsync()
{
    await Awaitable.EndOfFrameAsync();
    var jobHandle = ScheduleSomethingWithJobSystem();
    await Awaitable.NextFrameAsync();
    jobHandle.Complete();
}
```

**Awaitable Methods:**
- `Awaitable.NextFrameAsync()` -- Resume next frame
- `Awaitable.FixedUpdateAsync()` -- Resume at next FixedUpdate
- `Awaitable.EndOfFrameAsync()` -- Resume at end of frame
- `Awaitable.WaitForSecondsAsync(float)` -- Resume after delay
- `Awaitable.MainThreadAsync()` -- Force continuation on main thread
- `Awaitable.BackgroundThreadAsync()` -- Force continuation on background thread

**Critical:** Awaitable instances are pooled -- never `await` the same instance more than once.

## Events and Communication Patterns

### C# Events and Delegates

```csharp
public class Health : MonoBehaviour
{
    public event System.Action<float> OnDamageTaken;
    public event System.Action OnDeath;

    void OnEnable() { /* subscribe */ }
    void OnDisable() { /* always unsubscribe here */ }
}
```

### UnityEvents (Inspector-assignable)

```csharp
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public UnityEvent OnGameStart;
    public UnityEvent<int> OnScoreChanged;
}
```

## ScriptableObjects

```csharp
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SpawnManager")]
public class SpawnManagerScriptableObject : ScriptableObject
{
    public string prefabName;
    public int numberOfPrefabsToCreate;
    public Vector3[] spawnPoints;
}
```

## Serialization Quick Reference

```csharp
[SerializeField] private float _speed = 5f;           // Serialize private field
[field: SerializeField] public float Speed { get; private set; } // Auto-property
[NonSerialized] public float tempValue;                // Exclude from serialization
[HideInInspector] public float hiddenValue;            // Serialize but hide from Inspector
[SerializeReference] private IMyInterface _impl;       // Polymorphic serialization
```

## Core API Quick Reference

### Vector3
```csharp
float dist = Vector3.Distance(a, b);
Vector3 smoothed = Vector3.Lerp(from, to, t);
Vector3 moved = Vector3.MoveTowards(current, target, maxDelta);
// Use sqrMagnitude for comparisons (avoids sqrt)
```

### Quaternion
```csharp
Quaternion.identity;
Quaternion.Euler(0f, 90f, 0f);
Quaternion.LookRotation(direction, Vector3.up);
Quaternion.Slerp(from, to, t);
// Never modify x, y, z, w directly
```

### Time
```csharp
Time.deltaTime        // Seconds since last frame (use in Update)
Time.fixedDeltaTime   // Fixed timestep (use in FixedUpdate)
Time.time             // Time since game start
Time.timeScale        // 0 = paused, 1 = normal
Time.unscaledDeltaTime // Ignores timeScale
```

## Common Patterns

### Cached Component References
```csharp
void Awake()
{
    _rb = GetComponent<Rigidbody>(); // Cache in Awake, never in Update
    _transform = transform;
}
```

### Singleton Pattern
```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

### Async with Cancellation (Unity 6)
```csharp
async Awaitable LoadAndProcessAsync(CancellationToken token)
{
    await Awaitable.BackgroundThreadAsync();
    var result = ComputeExpensiveData(); // Off main thread

    await Awaitable.MainThreadAsync();
    ApplyResult(result); // Back on main thread
}
```

## Anti-Patterns

| Anti-Pattern | Problem | Fix |
|-------------|---------|-----|
| `GetComponent<T>()` in `Update()` | Allocates every frame | Cache in `Awake()` |
| `GameObject.Find()` in `Update()` | Expensive search every frame | Cache or use serialized field |
| Physics logic in `Update()` | Inconsistent at variable framerates | Use `FixedUpdate()` |
| Forgetting to unsubscribe events in `OnDisable` | Memory leaks | Always unsubscribe in `OnDisable()` |
| `await`-ing same `Awaitable` twice | Undefined behavior | Await once only |
| Empty `Update()` / `FixedUpdate()` methods | Unity still calls them | Remove empty event functions |

## Additional Resources

- [Scripting](https://docs.unity3d.com/6000.3/Documentation/Manual/scripting.html)
- [Execution Order](https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html)
- [Coroutines](https://docs.unity3d.com/6000.3/Documentation/Manual/Coroutines.html)
- [Async/Await](https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html)

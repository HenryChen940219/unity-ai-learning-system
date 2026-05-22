---
name: unity-game-architecture
description: >
  Unity 6 game architecture patterns. Use when designing system architecture, choosing between Singleton/Service Locator/DI, deciding MonoBehaviour vs plain C# class, setting up event systems, or bootstrapping managers. Addresses fat MonoBehaviour anti-patterns. Based on Unity 6.3 LTS documentation.
globs:
  - "**/*.cs"
---

# Game Systems Architecture -- Decision Patterns

## PATTERN: Global Service Access

DECISION:
- **Lazy Singleton** -- Tiny project, 1-3 managers, no testing needed.
- **Service Locator** -- Medium project, want to swap implementations for testing.
- **Constructor/Method DI** -- Large project, maximum testability (VContainer/Zenject).

### Service Locator Scaffold

```csharp
public static class Services
{
    private static readonly Dictionary<Type, object> _services = new();

    public static void Register<T>(T service) where T : class
        => _services[typeof(T)] = service;

    public static T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return (T)service;
        throw new InvalidOperationException($"Service {typeof(T).Name} not registered");
    }

    public static bool TryGet<T>(out T service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out var obj)) { service = (T)obj; return true; }
        service = null;
        return false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => _services.Clear(); // Critical for Enter Play Mode Options
}

// Registration:
Services.Register<IAudioService>(new AudioService());

// Usage anywhere:
Services.Get<IAudioService>().PlaySFX("explosion");
```

GOTCHA: Always back services with interfaces (`IAudioService`, not `AudioManager`) so tests can register mocks. The `SubsystemRegistration` reset is critical for domain reload disabled mode.

---

## PATTERN: MonoBehaviour vs Plain C# Class

DECISION:
- **MonoBehaviour** -- Needs Inspector serialization, Unity callbacks (Update, OnTriggerEnter), Transform access, coroutines, or `destroyCancellationToken`.
- **Plain C# class** -- Pure logic: state machines, inventory, damage calculation, save/load DTOs.

### Plain C# Class + MonoBehaviour Wrapper

```csharp
// Pure logic -- testable without Unity
public class HealthSystem
{
    public int Current { get; private set; }
    public int Max { get; }
    public bool IsDead => Current <= 0;
    public event Action OnDied;
    public event Action<int, int> OnChanged;

    public HealthSystem(int maxHealth) { Max = maxHealth; Current = maxHealth; }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        Current = Mathf.Max(0, Current - amount);
        OnChanged?.Invoke(Current, Max);
        if (IsDead) OnDied?.Invoke();
    }
}

// Thin MonoBehaviour wrapper -- bridges Unity and logic
public class HealthComponent : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    public HealthSystem Health { get; private set; }

    void Awake() => Health = new HealthSystem(maxHealth);
    void OnEnable() => Health.OnDied += HandleDeath;
    void OnDisable() => Health.OnDied -= HandleDeath;

    void HandleDeath() => Destroy(gameObject, 2f);
}
```

GOTCHA: Plain C# classes cannot use `[SerializeField]`. Use `[System.Serializable]` for nested Inspector display. They have no `destroyCancellationToken` -- pass one from the owning MonoBehaviour.

---

## PATTERN: Component Composition vs Inheritance

DECISION:
- **Composition with interfaces** (default) -- `GetComponent<IDamageable>()`. Maximum flexibility.
- **Abstract base class** -- Only for genuine IS-A with shared STATE and IMPLEMENTATION.

```csharp
public interface IDamageable
{
    void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal);
    bool IsAlive { get; }
}

[RequireComponent(typeof(Collider))]
public class DamageReceiver : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    private float _currentHealth;
    public bool IsAlive => _currentHealth > 0;

    void Awake() => _currentHealth = maxHealth;

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!IsAlive) return;
        _currentHealth -= amount;
    }
}

// Consumer queries the interface, not the concrete type:
void OnTriggerEnter(Collider other)
{
    if (other.TryGetComponent(out IDamageable target) && target.IsAlive)
        target.TakeDamage(damage, transform.position, transform.forward);
}
```

GOTCHA: `GetComponent<IInterface>()` works in Unity -- interfaces are queryable. Deep MonoBehaviour inheritance hierarchies (3+ levels) are the #1 Unity architecture anti-pattern.

---

## PATTERN: Event Architecture Selection

DECISION:
- **C# events/Actions** -- Within a class or tightly-coupled components.
- **ScriptableObject Event Channels** -- Cross-scene, designer-configurable.
- **Static Event Bus** -- Project-wide typed events, code-only.

### Static Typed Event Bus

```csharp
public struct PlayerDiedEvent { public Vector3 Position; public string CauseOfDeath; }

public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> _handlers = new();

    public static void Subscribe<T>(Action<T> handler) where T : struct
    {
        var type = typeof(T);
        _handlers[type] = _handlers.TryGetValue(type, out var existing)
            ? Delegate.Combine(existing, handler) : handler;
    }

    public static void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var existing)) return;
        var result = Delegate.Remove(existing, handler);
        if (result == null) _handlers.Remove(type); else _handlers[type] = result;
    }

    public static void Publish<T>(T evt) where T : struct
    {
        if (_handlers.TryGetValue(typeof(T), out var handler))
            ((Action<T>)handler)?.Invoke(evt);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => _handlers.Clear();
}

// Usage:
EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
EventBus.Publish(new PlayerDiedEvent { Position = pos, CauseOfDeath = "lava" });
```

GOTCHA: Always unsubscribe in `OnDisable`. Static Event Bus survives scene loads. Use `struct` events to avoid allocation.

---

## PATTERN: Manager Bootstrap Sequence

### Boot Scene (Recommended for production)

```csharp
public class Bootstrapper : MonoBehaviour
{
    [SerializeField] private string firstGameplayScene = "MainMenu";

    async Awaitable Start()
    {
        DontDestroyOnLoad(gameObject);

        var audio = gameObject.AddComponent<AudioService>();
        Services.Register<IAudioService>(audio);

        await SceneManager.LoadSceneAsync(firstGameplayScene);
    }
}
```

### RuntimeInitializeOnLoadMethod (Code-only)

```csharp
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("[Services]");
        Object.DontDestroyOnLoad(go);
        Services.Register<IAudioService>(go.AddComponent<AudioService>());
        // Cannot use async/await here -- have managers do async work in their Start()
    }
}
```

---

## Architecture Anti-Patterns

| Anti-Pattern | Problem | Alternative |
|---|---|---|
| God MonoBehaviour (1000+ lines) | Untestable, hard to modify | Split into focused components + plain C# |
| Singleton for everything | Tight coupling, hidden dependencies | Service Locator for infra |
| `FindObjectOfType` in Update | O(n) search every frame | Cache reference in Awake/Start |
| Direct cross-references between systems | Breaks if system removed | Event channels or Service Locator |
| Static state without domain-reload reset | Stale data between play sessions | `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` |

## Additional Resources

- [Unity Architecture E-book](https://unity.com/how-to/develop-modular-flexible-codebase-game-programming-patterns-e-book)
- [VContainer (DI)](https://vcontainer.hadashikick.jp/)

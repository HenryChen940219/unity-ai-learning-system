---
name: unity-async-patterns
description: >
  Unity 6 async/await and coroutine correctness patterns. Use when writing UnityWebRequest calls, async operations, coroutines, or any code that spans multiple frames. Covers Awaitable double-await bugs, destroyCancellationToken, thread context after BackgroundThreadAsync, coroutine error handling, and batch mode compatibility. Based on Unity 6.3 LTS documentation.
globs:
  - "**/*.cs"
---

# Unity Async & Coroutine Correctness Patterns

## PATTERN: Awaitable Double-Await

WRONG:
```csharp
var awaitable = Awaitable.NextFrameAsync();
await awaitable; // First await -- OK
await awaitable; // BUG: instance returned to pool, may be reused by something else
```

RIGHT:
```csharp
// Await once only -- create a new call for each await
await Awaitable.NextFrameAsync();
await Awaitable.NextFrameAsync();

// If you need to await the same result in multiple places, convert to Task:
var task = Awaitable.NextFrameAsync().AsTask();
await task; // Can be awaited multiple times
```

GOTCHA: After completion, Awaitable instances are returned to the pool and may be reused by a completely different operation. Multiple awaits cause undefined behavior.

---

## PATTERN: Missing destroyCancellationToken

WRONG:
```csharp
async Awaitable Start()
{
    await Awaitable.WaitForSecondsAsync(5f); // No cancellation token!
    _text.text = "Done"; // MissingReferenceException if object destroyed during wait
}
```

RIGHT:
```csharp
async Awaitable Start()
{
    try
    {
        await Awaitable.WaitForSecondsAsync(5f, destroyCancellationToken);
        _text.text = "Done"; // Safe: throws before this line if destroyed
    }
    catch (OperationCanceledException)
    {
        // Object was destroyed during wait -- expected, not an error
    }
}
```

GOTCHA: Always pass `destroyCancellationToken` to `Awaitable` wait methods in MonoBehaviours. The token is raised when `OnDestroy` begins.

---

## PATTERN: Thread Context After BackgroundThreadAsync

WRONG:
```csharp
async Awaitable ProcessAsync()
{
    await Awaitable.BackgroundThreadAsync();
    var result = HeavyComputation();
    _renderer.material.color = Color.red; // CRASH: Unity API called off main thread
}
```

RIGHT:
```csharp
async Awaitable ProcessAsync()
{
    await Awaitable.BackgroundThreadAsync();
    var result = HeavyComputation(); // Safe: background thread

    await Awaitable.MainThreadAsync(); // Switch back!
    _renderer.material.color = Color.red; // Safe: main thread
}
```

GOTCHA: After `BackgroundThreadAsync()`, ALL subsequent code runs on a thread pool thread until you explicitly switch back with `MainThreadAsync()`. Unity APIs are not thread-safe.

---

## PATTERN: async void vs async Awaitable

WRONG:
```csharp
async void LoadData()
{
    await Awaitable.WaitForSecondsAsync(1f);
    throw new Exception("Error!"); // App CRASHES -- exception unhandled
}
```

RIGHT:
```csharp
async Awaitable LoadData()
{
    await Awaitable.WaitForSecondsAsync(1f, destroyCancellationToken);
    // Exceptions propagate to caller properly
}

// Entry points (Start, event handlers) can use async Awaitable:
async Awaitable Start()
{
    try { await LoadData(); }
    catch (Exception ex) { Debug.LogError(ex); }
}
```

GOTCHA: `async void` cannot propagate exceptions -- the app crashes. Use `async Awaitable` (or `async Awaitable<T>`) instead.

---

## PATTERN: Coroutine Error Swallowing

WRONG:
```csharp
IEnumerator LoadData()
{
    yield return new WaitForSeconds(1f);
    throw new Exception("Error!"); // Logged to console, but execution silently stops
    // Code after this never runs -- no indication why
}
```

RIGHT:
```csharp
// Migrate to async Awaitable for proper error handling:
async Awaitable LoadDataAsync(CancellationToken token)
{
    await Awaitable.WaitForSecondsAsync(1f, token);
    try
    {
        ProcessData();
    }
    catch (Exception ex)
    {
        Debug.LogError($"LoadData failed: {ex.Message}");
        throw;
    }
}
```

GOTCHA: C# prevents `yield return` inside `try/catch` blocks. Exceptions in coroutines are logged but execution silently stops with no error propagation to the caller.

---

## PATTERN: WaitForEndOfFrame in Batch Mode / Tests

WRONG:
```csharp
IEnumerator TakeScreenshot()
{
    yield return new WaitForEndOfFrame(); // Hangs forever in headless/batch mode!
    var tex = ScreenCapture.CaptureScreenshotAsTexture();
}
```

RIGHT:
```csharp
IEnumerator TakeScreenshot()
{
    #if UNITY_EDITOR
    if (Application.isBatchMode)
    {
        yield return null; // Use null instead in batch mode
    }
    else
    #endif
    {
        yield return new WaitForEndOfFrame();
    }
    var tex = ScreenCapture.CaptureScreenshotAsTexture();
}
```

GOTCHA: In batch mode (headless server, CI), there is no rendering -- `WaitForEndOfFrame` yields never complete and the coroutine/async hangs forever.

---

## PATTERN: Nested Coroutine Cancellation

WRONG:
```csharp
void Start()
{
    StartCoroutine(Parent());
}

IEnumerator Parent()
{
    StartCoroutine(Child()); // Child launched independently
    yield return new WaitForSeconds(5f);
    // Stopping Parent does NOT stop Child!
}

void StopAll()
{
    StopCoroutine(Parent()); // Child keeps running!
}
```

RIGHT:
```csharp
IEnumerator Parent()
{
    yield return StartCoroutine(Child()); // Parent OWNS Child
    // Now stopping Parent also stops Child
}

// Or track coroutine references:
private Coroutine _childCoroutine;

IEnumerator Parent()
{
    _childCoroutine = StartCoroutine(Child());
    yield return new WaitForSeconds(5f);
}

void StopAll()
{
    if (_childCoroutine != null) StopCoroutine(_childCoroutine);
    StopAllCoroutines();
}
```

---

## PATTERN: Concurrent Awaitable Race Conditions (UnityWebRequest)

WRONG:
```csharp
// User rapidly triggers multiple searches
public async Awaitable Search(string query)
{
    using var request = UnityWebRequest.Get(url + query);
    await request.SendWebRequest();
    _resultText.text = request.downloadHandler.text; // Results can arrive out of order!
}
```

RIGHT:
```csharp
private CancellationTokenSource _searchCts;

public async Awaitable Search(string query)
{
    _searchCts?.Cancel(); // Cancel previous search
    _searchCts = new CancellationTokenSource();
    var token = _searchCts.Token;

    try
    {
        using var request = UnityWebRequest.Get(url + query);
        var op = request.SendWebRequest();
        while (!op.isDone)
        {
            token.ThrowIfCancellationRequested();
            await Awaitable.NextFrameAsync(token);
        }
        _resultText.text = request.downloadHandler.text;
    }
    catch (OperationCanceledException) { /* Superseded by newer search */ }
}
```

---

## Quick Reference: Anti-Patterns

| Anti-Pattern | Problem | Correct Approach |
|-------------|---------|-----------------|
| `await awaitable` twice | Pooled instance reuse -- undefined behavior | Await once, use `.AsTask()` for multi-await |
| No `destroyCancellationToken` | Continues after object destroyed | Pass token to all Awaitable waits |
| Unity API after `BackgroundThreadAsync` | Not thread-safe -- crash | Switch back with `MainThreadAsync()` |
| `async void` | Exceptions crash app unhandled | Use `async Awaitable` |
| `WaitForEndOfFrame` in batch mode | Hangs forever | Check `Application.isBatchMode` |
| Independent `StartCoroutine` nested | Stopping parent doesn't stop child | Use `yield return StartCoroutine(child)` |

## Additional Resources

- [Async/Await in Unity 6](https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html)
- [Coroutines](https://docs.unity3d.com/6000.3/Documentation/Manual/Coroutines.html)
- [UnityWebRequest](https://docs.unity3d.com/6000.3/Documentation/Manual/UnityWebRequest.html)

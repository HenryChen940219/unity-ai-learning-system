using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Editor：從專案根目錄的 secrets.json 讀取。
/// Android Build：從 Assets/StreamingAssets/secrets.json 讀取（需用 UnityWebRequest）。
/// 用法：先在場景掛 SecretsBootstrapper，再使用 SecretLoader.GeminiApiKey。
/// </summary>
public static class SecretLoader
{
    [System.Serializable]
    private class Secrets
    {
        public string gemini_api_key;
        public string google_cloud_api_key;
    }

    public static string GeminiApiKey      { get; private set; } = "";
    public static string GoogleCloudApiKey { get; private set; } = "";
    public static bool   IsReady           { get; private set; } = false;

    public static IEnumerator LoadAsync()
    {
        if (IsReady) yield break;

        string path;

#if UNITY_EDITOR
        path = Path.GetFullPath(Path.Combine(Application.dataPath, "../secrets.json"));
        if (!File.Exists(path))
            path = Path.Combine(Application.streamingAssetsPath, "secrets.json");
#else
        path = Path.Combine(Application.streamingAssetsPath, "secrets.json");
#endif

        using var req = UnityWebRequest.Get(path);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[SecretLoader] 讀取失敗：{req.error}\n路徑：{path}");
            yield break;
        }

        var secrets = JsonUtility.FromJson<Secrets>(req.downloadHandler.text);
        if (secrets == null)
        {
            Debug.LogError("[SecretLoader] secrets.json 解析失敗，請確認 JSON 格式。");
            yield break;
        }

        GeminiApiKey      = secrets.gemini_api_key      ?? "";
        GoogleCloudApiKey = secrets.google_cloud_api_key ?? "";
        IsReady = true;
        Debug.Log("[SecretLoader] 金鑰載入成功");
    }
}

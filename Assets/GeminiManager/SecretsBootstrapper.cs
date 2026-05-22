using UnityEngine;

/// <summary>
/// 場景啟動時自動載入 secrets.json。
/// 請把此腳本掛在場景中任一永遠存活的 GameObject（例如 GeminiManager）。
/// </summary>
public class SecretsBootstrapper : MonoBehaviour
{
    void Awake()
    {
        StartCoroutine(SecretLoader.LoadAsync());
    }
}

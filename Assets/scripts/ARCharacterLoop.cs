using UnityEngine;

public class ARCharacterLoop : MonoBehaviour
{
    [Header("燈具")]
    public Transform lampTransform;
    public Light lampLight;

    [Header("移動設定")]
    public float moveSpeed = 0.08f;
    public float maxDistance = 3.0f;
    public float detectRange = 0.4f;

    [Header("動畫設定（依你的 Animator Controller 填入）")]
    [Tooltip("要播放的動畫狀態名稱（Mixamo Walking 下載後通常叫 'Walking' 或 'mixamo.com'）")]
    public string walkStateName = "Walking";
    [Tooltip("控制走路速度的 Blend 參數名稱，若 Controller 沒有此參數可留空")]
    public string blendParamName = "Blend";

    private Vector3 startLocalPosition;

    void OnEnable()
    {
        startLocalPosition = transform.localPosition;
        StartCoroutine(PlayWalkAfterFrame());
    }

    private System.Collections.IEnumerator PlayWalkAfterFrame()
    {
        yield return null;
        var animator = GetComponent<Animator>();
        if (animator == null) yield break;

        animator.enabled = true;

        if (!string.IsNullOrEmpty(blendParamName))
        {
            // 確認參數存在再設值，避免 Animator 警告
            foreach (var param in animator.parameters)
            {
                if (param.name == blendParamName && param.type == AnimatorControllerParameterType.Float)
                {
                    animator.SetFloat(blendParamName, 1f);
                    break;
                }
            }
        }

        if (!string.IsNullOrEmpty(walkStateName))
            animator.Play(walkStateName, 0, 0f);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);

        float distFromStart = Vector3.Distance(transform.localPosition, startLocalPosition);
        if (distFromStart >= maxDistance)
            transform.localPosition = startLocalPosition;

        if (lampTransform != null && lampLight != null)
        {
            float distToLamp = Vector3.Distance(transform.localPosition, lampTransform.localPosition);
            lampLight.enabled = (distToLamp <= detectRange);
        }
    }
}

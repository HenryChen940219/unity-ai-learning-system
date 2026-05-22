using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARImageTracker : MonoBehaviour
{
    [Header("ARFoundation")]
    [SerializeField] private ARTrackedImageManager trackedImageManager;

    [Tooltip("XRReferenceImageLibrary 裡的圖片名稱（大小寫需一致）")]
    [SerializeField] private string targetImageName = "MyTopicCard";

    [Header("互動點 (3 個 Dot)")]
    [SerializeField] private GameObject[] dots = new GameObject[3];

    [Tooltip("三個點相對於 Target Image 的 local 位置（可在 Inspector 微調）")]
    [SerializeField] private Vector3[] dotLocalPositions = new Vector3[3]
    {
        new Vector3(-0.04f, 0f, 0.01f),
        new Vector3( 0.00f, 0f, 0.01f),
        new Vector3( 0.04f, 0f, 0.01f)
    };

    // 世界空間錨點：掃到圖後位置凍結，圖片移開也不消失
    private GameObject _worldAnchor;
    private bool _hasBeenFound = false;

    void Awake()
    {
        _worldAnchor = new GameObject("ARContentAnchor");
        _worldAnchor.SetActive(false);
    }

    void OnEnable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
    }

    void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
    }

    void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var image in args.added)   HandleImage(image);
        foreach (var image in args.updated) HandleImage(image);
        // removed：不做任何事，內容維持在最後位置
    }

    void HandleImage(ARTrackedImage image)
    {
        if (image.referenceImage.name != targetImageName) return;

        if (image.trackingState == TrackingState.Tracking)
        {
            // 每次追蹤到都更新世界座標，讓位置精準對齊學習單
            _worldAnchor.transform.position = image.transform.position;
            _worldAnchor.transform.rotation = image.transform.rotation;

            if (!_hasBeenFound)
            {
                AttachDots(_worldAnchor.transform);
                _worldAnchor.SetActive(true);
                SetDotsActive(true);
                _hasBeenFound = true;
            }
        }
        // 追蹤遺失時：什麼都不做，錨點凍結在最後位置
    }

    void AttachDots(Transform parent)
    {
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] == null) continue;
            dots[i].transform.SetParent(parent, false);
            if (i < dotLocalPositions.Length)
                dots[i].transform.localPosition = dotLocalPositions[i];
            dots[i].transform.localRotation = Quaternion.identity;
        }
    }

    void SetDotsActive(bool active)
    {
        foreach (var dot in dots)
            if (dot != null) dot.SetActive(active);
    }

    // 需要重新掃描時呼叫
    public void ResetTracking()
    {
        _hasBeenFound = false;
        _worldAnchor.transform.SetParent(null);
        _worldAnchor.SetActive(false);
        SetDotsActive(false);
    }
}

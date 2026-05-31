using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARImageTracker : MonoBehaviour
{
    [Header("ARFoundation")]
    [SerializeField] private ARTrackedImageManager trackedImageManager;

    [Tooltip("XRReferenceImageLibrary 裡的圖片名稱（大小寫需一致）")]
    [SerializeField] private string targetImageName = "MyTopicCard";

    // 由 WorksheetFlowManager 在每次掃描前設定
    private GameObject _currentAnimation;

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

    // WorksheetFlowManager 在 EnterARModeForStep 之前呼叫此方法指定動畫物件
    public void SetActiveAnimation(GameObject animObject)
    {
        if (_currentAnimation != null)
        {
            _currentAnimation.SetActive(false);
            _currentAnimation.transform.SetParent(null);
        }

        _currentAnimation = animObject;
        _hasBeenFound = false;
        _worldAnchor.SetActive(false);

        if (_currentAnimation != null)
            _currentAnimation.SetActive(false);
    }

    void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var image in args.added)   HandleImage(image);
        foreach (var image in args.updated) HandleImage(image);
        // removed 不處理：錨點凍結在最後偵測位置
    }

    void HandleImage(ARTrackedImage image)
    {
        if (image.referenceImage.name != targetImageName) return;

        if (image.trackingState == TrackingState.Tracking)
        {
            // 持續更新位置讓動畫對齊學習單
            _worldAnchor.transform.position = image.transform.position;
            _worldAnchor.transform.rotation = image.transform.rotation;

            if (!_hasBeenFound)
            {
                if (_currentAnimation != null)
                {
                    _currentAnimation.transform.SetParent(_worldAnchor.transform, false);
                    _currentAnimation.transform.localPosition = Vector3.zero;
                    _currentAnimation.transform.localRotation = Quaternion.identity;
                    _currentAnimation.SetActive(true);
                }

                _worldAnchor.SetActive(true);
                _hasBeenFound = true;

                Debug.Log($"[ARImageTracker] 掃描成功，播放動畫：{_currentAnimation?.name}");
            }
        }
    }

    public void ResetTracking()
    {
        _hasBeenFound = false;

        if (_currentAnimation != null)
        {
            _currentAnimation.SetActive(false);
            _currentAnimation.transform.SetParent(null);
        }

        _worldAnchor.SetActive(false);
    }
}

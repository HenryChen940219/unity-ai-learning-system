---
name: unity-xr
description: >
  Unity 6 XR and AR development guide. Use when working with ARFoundation, ARCore, ARKit, plane detection, image tracking, anchors, AR raycasting, XR Interaction Toolkit, or OpenXR. Covers AR session management, AR managers, XR input, and common AR patterns. Based on Unity 6.3 LTS / ARFoundation 6.3.3 -- user project uses ARFoundation 6.0.6, verify API availability.
---

# Unity XR & AR Development Guide

> ⚠️ This skill is based on **ARFoundation 6.3.3** (Unity 6.3 LTS).
> This project uses **ARFoundation 6.0.6** -- most APIs are identical, but newer features
> (e.g., Bounding Box Detection) may not be available. Verify against the
> [ARFoundation 6.0 docs](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/manual/index.html).

## XR Stack Overview

1. **XR Plug-in Management** -- Discovers and loads platform XR SDKs
2. **OpenXR Plugin** -- Cross-platform XR standard
3. **XR Interaction Toolkit (XRI)** -- Component-based interaction system
4. **AR Foundation** -- Cross-platform AR framework
5. **Input System** -- Action-based input for XR

## AR Foundation

AR Foundation uses a two-package architecture:
- **Base Package** -- Provides AR feature interfaces
- **Provider Plug-ins** -- Platform-specific implementations (ARCore for Android, ARKit for iOS)

### Supported Platforms

| Platform | Provider Plug-in |
|----------|-----------------|
| Android | Google ARCore XR Plug-in |
| iOS | Apple ARKit XR Plug-in |
| HoloLens 2 | OpenXR Plug-in |
| Meta Quest | Unity OpenXR: Meta |

### AR Feature Managers

| Feature | Manager Component |
|---------|-------------------|
| Session Management | `ARSession` |
| Device Tracking | `ARTrackedPoseDriver` |
| Camera | `ARCameraManager` |
| Plane Detection | `ARPlaneManager` |
| Image Tracking | `ARTrackedImageManager` |
| Face Tracking | `ARFaceManager` |
| Point Clouds | `ARPointCloudManager` |
| Ray Casting | `ARRaycastManager` |
| Anchors | `ARAnchorManager` |
| Meshing | `ARMeshManager` |
| Occlusion | `AROcclusionManager` |

### Basic AR Scene Setup

1. Add **AR Session** GameObject with `ARSession` component
2. Add **XR Origin** with `ARTrackedPoseDriver` on camera
3. Add **AR Camera Manager** to camera
4. Add feature managers (`ARPlaneManager`, `ARRaycastManager`, etc.) to XR Origin

### Plane Detection

```csharp
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaneDetectionController : MonoBehaviour
{
    [SerializeField] private ARPlaneManager planeManager;

    void OnEnable()
    {
        planeManager.trackablesChanged.AddListener(OnPlanesChanged);
    }

    void OnDisable()
    {
        planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
    }

    void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
    {
        foreach (var plane in args.added)
            Debug.Log($"New plane detected: {plane.trackableId}, size: {plane.size}");

        foreach (var plane in args.updated)
            Debug.Log($"Plane updated: {plane.trackableId}");

        foreach (var plane in args.removed)
            Debug.Log($"Plane removed: {plane.Key}");
    }

    public void SetPlaneDetection(bool enabled)
    {
        planeManager.enabled = enabled;
        foreach (var plane in planeManager.trackables)
            plane.gameObject.SetActive(enabled);
    }
}
```

### Image Tracking

```csharp
using UnityEngine.XR.ARFoundation;

public class ImageTrackingController : MonoBehaviour
{
    [SerializeField] private ARTrackedImageManager trackedImageManager;

    void OnEnable()
    {
        trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
    }

    void OnDisable()
    {
        trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
    }

    void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var trackedImage in args.added)
        {
            Debug.Log($"Image detected: {trackedImage.referenceImage.name}");
            // Spawn content at trackedImage.transform
        }

        foreach (var trackedImage in args.updated)
        {
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                // Update content position to follow tracked image
            }
        }
    }
}
```

### AR Raycasting

```csharp
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARTapToPlace : MonoBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private GameObject objectToPlace;

    private List<ARRaycastHit> _hits = new List<ARRaycastHit>();

    void Update()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        if (raycastManager.Raycast(touch.position, _hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = _hits[0].pose;
            if (objectToPlace == null)
                objectToPlace = Instantiate(prefab, hitPose.position, hitPose.rotation);
            else
                objectToPlace.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
        }
    }
}
```

### Spatial Anchors

```csharp
using UnityEngine.XR.ARFoundation;

public class AnchorManager : MonoBehaviour
{
    [SerializeField] private ARAnchorManager anchorManager;

    public async void CreateAnchor(Pose pose)
    {
        var anchor = await anchorManager.TryAddAnchorAsync(pose);
        if (anchor.status.IsSuccess())
        {
            Debug.Log($"Anchor created: {anchor.value.trackableId}");
            // Attach content to anchor.value.transform
        }
    }
}
```

## XR Interaction Toolkit (for VR/MR)

### Interaction States

| State | Description |
|-------|-------------|
| **Hover** | Interactable is a valid target |
| **Select** | Active interaction (grab) |
| **Activate** | Secondary contextual action |

### Interactor Types

| Component | Purpose |
|-----------|---------|
| XR Direct Interactor | Close-range grab |
| XR Ray Interactor | Distance ray interaction |
| XR Poke Interactor | Poking/touching |
| XR Gaze Interactor | Eye-gaze based |
| XR Socket Interactor | Snap-point interaction |

### Basic XR Rig Setup

1. Add **XR Origin** to scene
2. Add **XR Interaction Manager** (single instance)
3. Add **Tracked Pose Driver** to camera and controllers
4. Add **Interactors** to controller GameObjects
5. Add **Interactables** to interactive objects

## XR Controller Input

```csharp
using UnityEngine.XR;

var devices = new List<InputDevice>();
InputDevices.GetDevicesWithRole(InputDeviceRole.RightHanded, devices);

if (devices.Count > 0)
{
    var device = devices[0];

    bool triggerPressed;
    if (device.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed) && triggerPressed)
        Debug.Log("Trigger pressed");

    Vector2 thumbstick;
    if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out thumbstick))
        Debug.Log($"Thumbstick: {thumbstick}");
}

// Device lifecycle
InputDevices.deviceConnected += device => Debug.Log($"Connected: {device.displayName}");
if (device.isValid) { /* Safe to read features */ }
```

## Anti-Patterns

| Anti-Pattern | Problem | Correct Approach |
|-------------|---------|-----------------|
| Single Interaction Manager missing | All interactions fail silently | Ensure one XRInteractionManager in scene |
| Using legacy Input with OpenXR | Not compatible | Use new Input System package |
| Skipping device validation | Crash on disconnect | Always check `InputDevice.isValid` |
| World-space UI with XR providers | Breaks standard mouse/world-space UI | Use XR UI Input Module |
| Not checking `trackingState` | Using stale positions | Check `TrackingState.Tracking` before using pose |

## Key API Quick Reference

| Class | Namespace | Purpose |
|-------|-----------|---------|
| `ARSession` | UnityEngine.XR.ARFoundation | AR session management |
| `ARPlaneManager` | UnityEngine.XR.ARFoundation | Plane detection |
| `ARTrackedImageManager` | UnityEngine.XR.ARFoundation | Image tracking |
| `ARRaycastManager` | UnityEngine.XR.ARFoundation | AR raycasting |
| `ARAnchorManager` | UnityEngine.XR.ARFoundation | Spatial anchors |
| `ARCameraManager` | UnityEngine.XR.ARFoundation | Camera management |
| `InputDevice` | UnityEngine.XR | Physical XR device |
| `CommonUsages` | UnityEngine.XR | Standard input feature names |

## Additional Resources

- [AR Foundation 6.0 Manual](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/manual/index.html)
- [ARCore XR Plugin](https://docs.unity3d.com/Packages/com.unity.xr.arcore@6.0/manual/index.html)
- [ARKit XR Plugin](https://docs.unity3d.com/Packages/com.unity.xr.arkit@6.0/manual/index.html)
- [XR Interaction Toolkit](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/index.html)

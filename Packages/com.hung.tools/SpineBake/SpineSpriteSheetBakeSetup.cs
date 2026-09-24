using Spine.Unity;
using UnityEngine;

public enum SpineBakeSampleMode
{
    ByFps,
    FixedFrameCount
}

[DisallowMultipleComponent]
public class SpineSpriteSheetBakeSetup : MonoBehaviour
{
    [Header("Source")]
    public SkeletonAnimation skeletonAnimation;
    public string animationName;

    [Header("Bake Settings")]
    public SpineBakeSampleMode sampleMode = SpineBakeSampleMode.ByFps;
    public int frameWidth = 256;
    public int frameHeight = 256;
    public int fps = 24;
    public int fixedFrameCount = 64;
    public bool includeLastFrame = false;
    public int columns = 8;

    [Header("Empty Frame Removal")]
    public bool removeEmptyFrames = true;
    [Range(0, 255)] public int alphaThreshold = 0;

    [Header("Camera")]
    public Camera bakeCamera;
    public Color backgroundColor = new Color(0, 0, 0, 0);

    [Header("Preview")]
    public bool loopPreview = true;
    public int previewFps = 24;
}
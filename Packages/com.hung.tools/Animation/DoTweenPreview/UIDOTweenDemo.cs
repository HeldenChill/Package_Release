using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIDOTweenDemo : MonoBehaviour
{
    public enum UIAnimationPreset
    {
        PopIn,
        PopOut,
        FadeIn,
        FadeOut,
        SlideFromLeft,
        SlideFromRight,
        SlideFromTop,
        SlideFromBottom,
        PunchScale,
        ShakePosition
    }

    [Header("Target")]
    public RectTransform target;
    public CanvasGroup canvasGroup;
    public Graphic graphicFallback;

    [Header("Animation")]
    public UIAnimationPreset preset = UIAnimationPreset.PopIn;
    public float duration = 0.35f;
    public float delay = 0f;
    public Ease ease = Ease.OutBack;

    [Header("Slide")]
    public Vector2 slideOffset = new Vector2(250f, 160f);

    [Header("Scale")]
    public Vector3 hiddenScale = Vector3.zero;
    public Vector3 visibleScale = Vector3.one;
    public Vector3 punchScale = new Vector3(0.18f, 0.18f, 0f);

    [Header("Alpha")]
    [Range(0f, 1f)] public float hiddenAlpha = 0f;
    [Range(0f, 1f)] public float visibleAlpha = 1f;

    [Header("Shake")]
    public Vector2 shakeStrength = new Vector2(20f, 0f);
    public int shakeVibrato = 12;
    public float shakeRandomness = 45f;

    [Header("Behaviour")]
    public bool resetBeforePlay = true;
    public bool autoCaptureOnReset = true;

    [SerializeField] private bool hasSnapshot;
    [SerializeField] private Vector2 snapshotAnchoredPosition;
    [SerializeField] private Vector3 snapshotScale;
    [SerializeField] private Vector3 snapshotEuler;
    [SerializeField] private float snapshotAlpha = 1f;

    private Tween currentTween;

    private void Reset()
    {
        ResolveReferences();

        if (autoCaptureOnReset)
        {
            CaptureSnapshot();
        }
    }

    private void OnValidate()
    {
        ResolveReferences();

        duration = Mathf.Max(0.01f, duration);
        delay = Mathf.Max(0f, delay);
        shakeVibrato = Mathf.Max(1, shakeVibrato);
        shakeRandomness = Mathf.Clamp(shakeRandomness, 0f, 180f);
    }

    private void ResolveReferences()
    {
        if (target == null)
        {
            target = transform as RectTransform;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (graphicFallback == null)
        {
            graphicFallback = GetComponent<Graphic>();
        }
    }

    public void CaptureSnapshot()
    {
        ResolveReferences();

        if (target == null)
        {
            return;
        }

        snapshotAnchoredPosition = target.anchoredPosition;
        snapshotScale = target.localScale;
        snapshotEuler = target.localEulerAngles;
        snapshotAlpha = GetCurrentAlpha();
        hasSnapshot = true;
    }

    public void ResetToSnapshot()
    {
        ResolveReferences();

        if (!hasSnapshot || target == null)
        {
            return;
        }

        KillCurrentTween();

        target.anchoredPosition = snapshotAnchoredPosition;
        target.localScale = snapshotScale;
        target.localEulerAngles = snapshotEuler;
        SetAlpha(snapshotAlpha);
    }

    public void Play()
    {
        Tween tween = BuildTween();

        if (tween != null)
        {
            tween.Play();
        }
    }

    public void Stop()
    {
        KillCurrentTween();
    }

    public Tween BuildTween()
    {
        ResolveReferences();

        if (target == null)
        {
            Debug.LogWarning($"{nameof(UIDOTweenDemo)} requires a RectTransform target.", this);
            return null;
        }

        if (!hasSnapshot)
        {
            CaptureSnapshot();
        }

        KillCurrentTween();

        if (resetBeforePlay)
        {
            ApplySnapshotWithoutKilling();
        }

        Sequence sequence = DOTween.Sequence();
        sequence.SetAutoKill(true);

        if (delay > 0f)
        {
            sequence.AppendInterval(delay);
        }

        switch (preset)
        {
            case UIAnimationPreset.PopIn:
                PrepareScale(hiddenScale);
                SetAlpha(hiddenAlpha);

                sequence.Append(target.DOScale(visibleScale, duration).SetEase(ease));
                AddFadeJoin(sequence, visibleAlpha, duration);
                break;

            case UIAnimationPreset.PopOut:
                sequence.Append(target.DOScale(hiddenScale, duration).SetEase(ease));
                AddFadeJoin(sequence, hiddenAlpha, duration);
                break;

            case UIAnimationPreset.FadeIn:
                SetAlpha(hiddenAlpha);
                sequence.Append(CreateFadeTween(visibleAlpha, duration).SetEase(ease));
                break;

            case UIAnimationPreset.FadeOut:
                sequence.Append(CreateFadeTween(hiddenAlpha, duration).SetEase(ease));
                break;

            case UIAnimationPreset.SlideFromLeft:
                PreparePosition(snapshotAnchoredPosition + Vector2.left * Mathf.Abs(slideOffset.x));
                SetAlpha(hiddenAlpha);

                sequence.Append(target.DOAnchorPos(snapshotAnchoredPosition, duration).SetEase(ease));
                AddFadeJoin(sequence, visibleAlpha, duration);
                break;

            case UIAnimationPreset.SlideFromRight:
                PreparePosition(snapshotAnchoredPosition + Vector2.right * Mathf.Abs(slideOffset.x));
                SetAlpha(hiddenAlpha);

                sequence.Append(target.DOAnchorPos(snapshotAnchoredPosition, duration).SetEase(ease));
                AddFadeJoin(sequence, visibleAlpha, duration);
                break;

            case UIAnimationPreset.SlideFromTop:
                PreparePosition(snapshotAnchoredPosition + Vector2.up * Mathf.Abs(slideOffset.y));
                SetAlpha(hiddenAlpha);

                sequence.Append(target.DOAnchorPos(snapshotAnchoredPosition, duration).SetEase(ease));
                AddFadeJoin(sequence, visibleAlpha, duration);
                break;

            case UIAnimationPreset.SlideFromBottom:
                PreparePosition(snapshotAnchoredPosition + Vector2.down * Mathf.Abs(slideOffset.y));
                SetAlpha(hiddenAlpha);

                sequence.Append(target.DOAnchorPos(snapshotAnchoredPosition, duration).SetEase(ease));
                AddFadeJoin(sequence, visibleAlpha, duration);
                break;

            case UIAnimationPreset.PunchScale:
                sequence.Append(target.DOPunchScale(punchScale, duration, 8, 0.8f).SetEase(ease));
                break;

            case UIAnimationPreset.ShakePosition:
                sequence.Append(
                    target.DOShakeAnchorPos(
                        duration,
                        shakeStrength,
                        shakeVibrato,
                        shakeRandomness,
                        snapping: false,
                        fadeOut: true
                    )
                );
                break;
        }

        currentTween = sequence;
        return currentTween;
    }

    private void PreparePosition(Vector2 anchoredPosition)
    {
        target.anchoredPosition = anchoredPosition;
    }

    private void PrepareScale(Vector3 scale)
    {
        target.localScale = scale;
    }

    private void ApplySnapshotWithoutKilling()
    {
        if (!hasSnapshot || target == null)
        {
            return;
        }

        target.anchoredPosition = snapshotAnchoredPosition;
        target.localScale = snapshotScale;
        target.localEulerAngles = snapshotEuler;
        SetAlpha(snapshotAlpha);
    }

    private void KillCurrentTween()
    {
        if (currentTween != null)
        {
            currentTween.Kill();
            currentTween = null;
        }

        if (target != null)
        {
            target.DOKill();
        }

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }

        if (graphicFallback != null)
        {
            graphicFallback.DOKill();
        }
    }

    private Tween CreateFadeTween(float targetAlpha, float tweenDuration)
    {
        if (canvasGroup != null)
        {
            return canvasGroup.DOFade(targetAlpha, tweenDuration);
        }

        if (graphicFallback != null)
        {
            return graphicFallback.DOFade(targetAlpha, tweenDuration);
        }

        return DOVirtual.Float(GetCurrentAlpha(), targetAlpha, tweenDuration, SetAlpha);
    }

    private void AddFadeJoin(Sequence sequence, float targetAlpha, float tweenDuration)
    {
        Tween fadeTween = CreateFadeTween(targetAlpha, tweenDuration);

        if (fadeTween != null)
        {
            sequence.Join(fadeTween.SetEase(Ease.OutSine));
        }
    }

    private float GetCurrentAlpha()
    {
        if (canvasGroup != null)
        {
            return canvasGroup.alpha;
        }

        if (graphicFallback != null)
        {
            return graphicFallback.color.a;
        }

        return 1f;
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            return;
        }

        if (graphicFallback != null)
        {
            Color color = graphicFallback.color;
            color.a = alpha;
            graphicFallback.color = color;
        }
    }
}
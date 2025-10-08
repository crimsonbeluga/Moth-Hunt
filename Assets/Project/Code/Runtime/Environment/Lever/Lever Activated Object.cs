using UnityEngine;

public class LeverActivatedObject : MonoBehaviour
{
    [Header("Target Local Transform")]
    [Tooltip("Position/Rotation: When useOffsets = true, values are OFFSETS; when false, ABSOLUTE local targets.\n" +
             "Rotation uses DEGREES per axis.\n" +
             "Scale: When scaleAsFactor = true, values are FACTORS (1=unchanged). When false, follows useOffsets.")]
    public Vector3 TargetLocalPosition = Vector3.zero;
    [Tooltip("Rotation in DEGREES around local X/Y/Z.")]
    public Vector3 TargetLocalEulerDegrees = Vector3.zero;   // <<< DEGREES
    public Vector3 TargetLocalScale = Vector3.one;           // factor=1 means no change (when scaleAsFactor=true)

    [Header("Animation")]
    [Min(0f)] public float duration = 0.75f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("First press -> target; next press -> back to start.")]
    public bool toggle = true;

    [Tooltip("Position & Rotation: treat as OFFSETS (true) or ABSOLUTE (false).")]
    public bool useOffsets = true;

    [Tooltip("If true: TargetLocalScale is a FACTOR (1 = no change). If false: scale follows useOffsets.")]
    public bool scaleAsFactor = true;

    // Internals
    Vector3 _startPos, _startScale;
    Quaternion _startRot;
    bool _atTarget;
    bool _isAnimating;

    void Awake()
    {
        _startPos = transform.localPosition;
        _startRot = transform.localRotation;
        _startScale = transform.localScale;
    }

    public void Activate()
    {
        if (_isAnimating) return;

        if (toggle)
        {
            if (_atTarget)
            {
                TweenTo(_startPos, _startRot, _startScale, false);
                return;
            }

            // Compute stable endpoints from the START transform
            Vector3 endPos = useOffsets ? _startPos + TargetLocalPosition
                                          : TargetLocalPosition;

            Quaternion endRot = useOffsets
                ? _startRot * Quaternion.Euler(TargetLocalEulerDegrees)          // DEGREES offset
                : Quaternion.Euler(TargetLocalEulerDegrees);                     // DEGREES absolute

            Vector3 endScale = scaleAsFactor
                ? Vector3.Scale(_startScale, TargetLocalScale)                   // factor: 1=unchanged
                : (useOffsets ? _startScale + TargetLocalScale : TargetLocalScale);

            TweenTo(endPos, endRot, endScale, true);
        }
        else
        {
            // One-shot: compute from CURRENT transform
            Vector3 basePos = transform.localPosition;
            Quaternion baseRot = transform.localRotation;
            Vector3 baseScale = transform.localScale;

            Vector3 endPos = useOffsets ? basePos + TargetLocalPosition
                                          : TargetLocalPosition;

            Quaternion endRot = useOffsets
                ? baseRot * Quaternion.Euler(TargetLocalEulerDegrees)            // DEGREES offset
                : Quaternion.Euler(TargetLocalEulerDegrees);                     // DEGREES absolute

            Vector3 endScale = scaleAsFactor
                ? Vector3.Scale(baseScale, TargetLocalScale)                     // factor: 1=unchanged
                : (useOffsets ? baseScale + TargetLocalScale : TargetLocalScale);

            TweenTo(endPos, endRot, endScale, true);
        }
    }

    void TweenTo(Vector3 pos, Quaternion rot, Vector3 scale, bool markAtTarget)
    {
        StartCoroutine(Co_Tween(pos, rot, scale, markAtTarget));
    }

    System.Collections.IEnumerator Co_Tween(Vector3 endPos, Quaternion endRot, Vector3 endScale, bool markAtTarget)
    {
        _isAnimating = true;

        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;
        Vector3 startScale = transform.localScale;

        float t = 0f;
        float d = Mathf.Max(0.0001f, duration);

        while (t < d)
        {
            t += Time.deltaTime;
            float a = ease.Evaluate(Mathf.Clamp01(t / d));

            transform.localPosition = Vector3.Lerp(startPos, endPos, a);
            transform.localRotation = Quaternion.Slerp(startRot, endRot, a);
            transform.localScale = Vector3.Lerp(startScale, endScale, a);

            yield return null;
        }

        transform.localPosition = endPos;
        transform.localRotation = endRot;
        transform.localScale = endScale;

        _atTarget = markAtTarget;
        _isAnimating = false;
    }
}

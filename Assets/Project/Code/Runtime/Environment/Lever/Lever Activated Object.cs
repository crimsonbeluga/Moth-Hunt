using UnityEngine;

public class LeverActivatedObject : MonoBehaviour
{
    [Header("Target Local Transform")]
    [Tooltip("Position/Rotation: When useOffsets = true, values are OFFSETS; when false, ABSOLUTE local targets.\n" +
             "Rotation uses DEGREES per axis.\n" +
             "Scale: When scaleAsFactor = true, values are FACTORS (1=unchanged). When false, follows useOffsets.")]
    public Vector3 TargetLocalPosition = Vector3.zero;

    [Tooltip("Rotation in DEGREES around local X/Y/Z.")]
    public Vector3 TargetLocalEulerDegrees = Vector3.zero;   // DEGREES

    [Tooltip("If scaleAsFactor = true, 1 means no change.")]
    public Vector3 TargetLocalScale = Vector3.one;

    [Header("Animation")]
    [Min(0f)] public float duration = 0.75f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("TRUE = can toggle back and forth between START and END forever.\nFALSE = one-shot to END, then locked.")]
    public bool toggle = true;

    [Tooltip("Position & Rotation: treat as OFFSETS (true) or ABSOLUTE (false).")]
    public bool useOffsets = true;

    [Tooltip("If true: TargetLocalScale is a FACTOR (1 = no change). If false: scale follows useOffsets.")]
    public bool scaleAsFactor = true;

    // Captured START and precomputed END (from START)
    Vector3 _startPos, _startScale, _endPos, _endScale;
    Quaternion _startRot, _endRot;

    bool _atTarget;     // true when currently at END
    bool _isAnimating;
    bool _locked;       // used only when toggle == false

    void Awake()
    {
        // Capture ORIGINAL local transform as START
        _startPos = transform.localPosition;
        _startRot = transform.localRotation;
        _startScale = transform.localScale;

        // Precompute END strictly from START (prevents drift/cumulative errors)
        _endPos = useOffsets ? _startPos + TargetLocalPosition
                             : TargetLocalPosition;

        _endRot = useOffsets
            ? _startRot * Quaternion.Euler(TargetLocalEulerDegrees)   // degrees offset
            : Quaternion.Euler(TargetLocalEulerDegrees);              // degrees absolute

        _endScale = scaleAsFactor
            ? Vector3.Scale(_startScale, TargetLocalScale)            // factor (1 = unchanged)
            : (useOffsets ? _startScale + TargetLocalScale : TargetLocalScale);
    }

    public void Activate()
    {
        if (_isAnimating) return;

        if (toggle)
        {
            // Repeated toggling between START and END only
            if (_atTarget)
            {
                TweenTo(_startPos, _startRot, _startScale, markAtTarget: false, lockAfter: false);
            }
            else
            {
                TweenTo(_endPos, _endRot, _endScale, markAtTarget: true, lockAfter: false);
            }
        }
        else
        {
            // One-shot to END and then lock
            if (_locked || _atTarget) return;
            TweenTo(_endPos, _endRot, _endScale, markAtTarget: true, lockAfter: true);
        }
    }

    void TweenTo(Vector3 pos, Quaternion rot, Vector3 scale, bool markAtTarget, bool lockAfter)
    {
        StartCoroutine(Co_Tween(pos, rot, scale, markAtTarget, lockAfter));
    }

    System.Collections.IEnumerator Co_Tween(Vector3 endPos, Quaternion endRot, Vector3 endScale, bool markAtTarget, bool lockAfter)
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
        _locked |= lockAfter;
        _isAnimating = false;
    }
}

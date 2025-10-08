using MothHunt.Input;
using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField] private Transform target;
    private CamSettings camSettings;

    [Header("Designer Variables")]
    [SerializeField]
    private CamSettings DefaultCamSettings = new CamSettings
    {
        moveDampening = 0.1f,
        rotationDampening = 0.1f,
        distance = 10f,
        offset = new Vector2(0f, 2f),
        maxLookOffset = new Vector2(1f, 1f),
        tempOffset = Vector2.zero
    };

    [SerializeField]
    public ActionModifiers glidCamModifiers = new ActionModifiers
    {
        camOffsetModifier = new Vector2(0f, -2f),
        camDistanceModifier = 3f
    };

    [SerializeField]
    public ActionModifiers crouchCamModifiers = new ActionModifiers
    {
        camOffsetModifier = new Vector2(0f, 0),
        camDistanceModifier = 0
    };

    [Header("Limitations")]
    [SerializeField] private float minYaw = -20f;
    [SerializeField] private float maxYaw = 20f;

    private Vector3 currentVelocity;
    private float currentYawVelocity;
    private float currentYaw;

    Vector2 accumulatedLookInput;
    Vector2 lookOffset;
    public Vector2 tempOffSet
    {
        get => camSettings.tempOffset;
        set => camSettings.tempOffset = value;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        camSettings = DefaultCamSettings;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;
        UpdateLookOffset();
        UpdatePosition();
        UpdateRotation();
    }

    //process look input
    void UpdateLookOffset()
    {
        accumulatedLookInput += PlayerInputRouter.Look;

        accumulatedLookInput.x = Mathf.Clamp(accumulatedLookInput.x, -camSettings.maxLookOffset.x * 10, camSettings.maxLookOffset.x * 10);
        accumulatedLookInput.y = Mathf.Clamp(accumulatedLookInput.y, -camSettings.maxLookOffset.y * 10, camSettings.maxLookOffset.y * 10);
        lookOffset = accumulatedLookInput / 10;
    }

    // update position
    private void UpdatePosition()
    {
        Vector3 desiredPosition =
            target.position + Vector3.up * camSettings.offset.y //figure out verticality
            + Vector3.right * camSettings.offset.x //figure out where to place camera horizontally
            - target.forward * camSettings.distance //distance behind the target
            + new Vector3(tempOffSet.x, tempOffSet.y, 0f); //apply tempOffSet

        //shift based on look
        desiredPosition.x += lookOffset.x;
        desiredPosition.y += lookOffset.y;

        transform.position = Vector3.SmoothDamp( //smooth move camera to desired position
            transform.position,
            desiredPosition,
            ref currentVelocity,
            camSettings.moveDampening
        );
    }

    private void UpdateRotation()
    {
        // shift target position based on look
        Vector3 effectivePosition = target.position + new Vector3(lookOffset.x, lookOffset.y, 0f);

        Vector3 direction = effectivePosition - transform.position;

        // Get the rotation that looks at the effective position
        Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);

        // Convert to Euler angles and round each to nearest 90 degrees
        Vector3 euler = lookRotation.eulerAngles;
        euler.x = Mathf.Round(euler.x / 90f) * 90f;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        euler.z = Mathf.Round(euler.z / 90f) * 90f;

        // Apply the rounded rotation
        transform.rotation = Quaternion.Euler(euler);
    }

    //sets what to point the camera at
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    //resets camera settings to default
    public void ResetToDefaultSettings()
    {
        camSettings = DefaultCamSettings;
    }

    //sets new camera settings
    public void SetCamSettings(CamSettings newSettings)
    {
        camSettings = newSettings;
    }

    public void ApplyActionModifier(ActionModifiers modifiers)
    {
        camSettings.offset += modifiers.camOffsetModifier;
        camSettings.distance += modifiers.camDistanceModifier;
    }

    public void RemoveActionModifier(ActionModifiers modifiers)
    {
        camSettings.offset -= modifiers.camOffsetModifier;
        camSettings.distance -= modifiers.camDistanceModifier;
    }
}

[Serializable]
public struct CamSettings
{
    public float moveDampening;
    public float rotationDampening;
    public float distance;
    public Vector2 offset;
    public Vector2 maxLookOffset;
    [NonSerialized]
    public Vector2 tempOffset;
}

[Serializable]
public struct ActionModifiers
{
    public Vector2 camOffsetModifier;
    public float camDistanceModifier;
}
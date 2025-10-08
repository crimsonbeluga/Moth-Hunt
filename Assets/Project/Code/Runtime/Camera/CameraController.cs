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

        UpdatePosition();
        UpdateRotation();
    }

    // update position
    private void UpdatePosition()
    {
        Vector3 desiredPosition =
            target.position + Vector3.up * camSettings.offset.y //figure out verticality
            + Vector3.right * camSettings.offset.x //figure out where to place camera horizontally
            - target.forward * camSettings.distance //distance behind the target
            + new Vector3(tempOffSet.x, tempOffSet.y, 0f); //apply tempOffSet

        transform.position = Vector3.SmoothDamp( //smooth move camera to desired position
            transform.position,
            desiredPosition,
            ref currentVelocity,
            camSettings.moveDampening
        );
    }

    private void UpdateRotation()
    {
        // Calculate the desired yaw based on the target's position
        Vector3 lookDir = (target.position + new Vector3(tempOffSet.x, tempOffSet.y, 0f)) - transform.position; //apply tempOffSet

        //figure out the yaw
        float desiredYaw = Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg;

        //clamp to prevent weird behavior
        desiredYaw = Mathf.Clamp(desiredYaw, minYaw, maxYaw);

        // smooth interpolate cause looks nice
        currentYaw = Mathf.SmoothDampAngle(
            currentYaw,
            desiredYaw,
            ref currentYawVelocity,
            camSettings.rotationDampening
        );

        // Apply the rotation
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
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
using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField] private Transform target;

    [SerializeField]
    private CamSettings camSettings;

    private CamSettings defaultCamSettings = new CamSettings
    {
        moveDampening = 0.1f,
        rotationDampening = 0.1f,
        distance = 10f,
        offsetY = 2f,
        offsetX = 0f,
        maxLookOffsetX = 1f,
        maxLookOffsetY = 1f
    };
    [SerializeField] private float minYaw = -20f;
    [SerializeField] private float maxYaw = 20f;

    private Vector3 currentVelocity;
    private float currentYawVelocity;
    private float currentYaw;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        camSettings = defaultCamSettings;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        //position
        Vector3 desiredPosition = target.position
            + Vector3.up * camSettings.offsetY
            + Vector3.right * camSettings.offsetX
            - target.forward * camSettings.distance;

        //move to pos
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            camSettings.moveDampening
        );

        // calc yaw
        Vector3 lookDir = target.position - transform.position;
        float desiredYaw = Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg;

        //no flip
        desiredYaw = Mathf.Clamp(desiredYaw, minYaw, maxYaw);

        // smooth rot
        currentYaw = Mathf.SmoothDampAngle(
            currentYaw,
            desiredYaw,
            ref currentYawVelocity,
            camSettings.rotationDampening
        );

        Quaternion targetRotation = Quaternion.Euler(0f, currentYaw, 0f);
        transform.rotation = targetRotation;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void ResetToDefaultSettings()
    {
        camSettings = defaultCamSettings;
    }

    public void SetCamSettings(CamSettings newSettings)
    {
        camSettings = newSettings;
    }
}

// parameters that control/effect camera behavior
[Serializable]
public struct CamSettings
{
    public float moveDampening;
    public float rotationDampening;
    public float distance;
    public float offsetY;
    public float offsetX;
    public float maxLookOffsetX;
    public float maxLookOffsetY;
}
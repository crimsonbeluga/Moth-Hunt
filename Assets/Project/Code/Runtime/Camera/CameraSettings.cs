using UnityEngine;
namespace MothHunt.Runtime.Camera
{
    //encapsulates all settings for different camera modes
    [System.Serializable]
    public class CameraSettings
    {
        [Header("Shared Settings")]
        [Tooltip("Which camera mode to use for this configuration.")]
        public CameraModes mode = CameraModes.FollowCam;

        [Tooltip("Z distance of the camera relative to the target.")]
        public float distanceZ = -10f;

        [Tooltip("Local offset applied to the camera position.")]
        public Vector3 offset = new Vector3(0f, 0f, -10f);

        [Tooltip("If true, camera will snap immediately when settings change instead of smoothly transitioning.")]
        public bool snapOnChange = false;

        [Space(6f)]
        [Header("FollowCam Settings")]
        [Tooltip("Transform the FollowCam will track.")]
        public Transform target;

        [Tooltip("Lock camera X position to a fixed value.")]
        public bool lockX = false;

        [Tooltip("X position value used when lockX is enabled.")]
        public float lockedXValue = 0f;

        [Tooltip("Lock camera Y position to a fixed value.")]
        public bool lockY = false;

        [Tooltip("Y position value used when lockY is enabled.")]
        public float lockedYValue = 0f;

        [Tooltip("Enable X axis limits for the FollowCam.")]
        public bool limitX = false;

        [Tooltip("Enable Y axis limits for the FollowCam.")]
        public bool limitY = false;

        [Tooltip("Smooth time used by the camera dampening (seconds).")]
        [Range(0f, 1f)]
        public float smoothTime = 0.15f;

        [Space(6f)]
        [Header("FixedCam Settings")]
        [Tooltip("World position for the FixedCam.")]
        public Vector3 roomPosition = Vector3.zero;

        [Tooltip("Facing direction (degrees) for the FixedCam.")]
        [Range(-180f, 180f)]
        public float facingDirection = 0f;

        [Header("Cinematic Cam Settings")]
        public CinematicWaypoint[] cinematicWaypoints;
        public CameraSettings exitSettings;
        public bool lockInput = true;
    }

    //camera modes
    public enum CameraModes
    {
        FollowCam,
        FixedCam,
        CinematicCam
    }
}
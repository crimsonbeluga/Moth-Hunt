using UnityEngine;
namespace MothHunt.Runtime.Camera
{
    //triggers camera transitions
    [RequireComponent(typeof(Collider))]
    public class CameraTrigger : MonoBehaviour
    {
        [SerializeField] //main settings
        private CameraSettings settings;

        [Header("If Cinematic")]
        [SerializeField] //follow-up settings if its a cinematic cam
        private CameraSettings followUpSettings;

        [SerializeField] //destroy after trigger
        private bool destroyAfterTrigger = false;

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Toggle show/hide for camera triggers in the scene view.")]
        static bool showCameraTriggersGizmos = true;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (settings.mode == CameraModes.FollowCam && settings.target == null)
                    settings.target = other.transform;

                //sets up next camera mode if cinematic and next is follow cam for player
                if (followUpSettings?.mode == CameraModes.FollowCam && followUpSettings.target == null)
                    followUpSettings.target = other.transform;

                if (settings.lockInput)
                {

                }

                //get follow-up settings set
                settings.exitSettings = followUpSettings;

                //change camera mode
                CameraManager.instance.ChangeCameraMode(settings);

                //destroy if needed
                if (destroyAfterTrigger)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            foreach (var waypoint in settings.cinematicWaypoints)
            {
                Gizmos.DrawWireSphere(waypoint.position, 0.5f);
            }
        }

        private void OnDrawGizmos()
        {
            if (showCameraTriggersGizmos)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(transform.position, transform.localScale);
            }
        }
    }
}
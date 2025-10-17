using UnityEngine;
namespace MothHunt.Runtime.Camera
{
    public class CameraTrigger : MonoBehaviour
    {
        [SerializeField]
        private CameraSettings settings;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (settings.cameraMode == CameraModes.FollowCam && settings.target == null)
                    settings.target = other.transform;
                CameraManager.instance.ChangeCameraMode(settings);
            }
        }
    }
}
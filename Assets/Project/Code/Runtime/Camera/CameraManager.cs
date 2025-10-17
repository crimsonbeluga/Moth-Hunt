using UnityEngine;
namespace MothHunt.Runtime.Camera
{
    [RequireComponent(typeof(global::UnityEngine.Camera))]
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager instance;
        CameraMode currentMode = new FollowCam();
        [SerializeField] private CameraSettings defaultSettings;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            instance = this;
        }
        void Start()
        {
            ChangeCameraMode(defaultSettings);
        }
        void LateUpdate()
        {
            currentMode.Update();
        }

        public void ChangeCameraMode(CameraSettings settings)
        {
            switch (settings.cameraMode)
            {
                case CameraModes.FollowCam:
                    currentMode = new FollowCam();
                    break;
                case CameraModes.FixedCam:
                    currentMode = new FixedCam();
                    break;
                default:
                    Debug.LogError("CameraManager: Unknown camera mode.");
                    break;
            }

            currentMode.Switch(this.gameObject, settings);
            currentMode.OnChange();
        }
    }
}
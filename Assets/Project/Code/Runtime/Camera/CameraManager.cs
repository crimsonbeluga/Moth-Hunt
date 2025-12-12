using UnityEngine;
namespace MothHunt.Runtime.Camera
{
    //Manages camera mode, state and updating
    [RequireComponent(typeof(global::UnityEngine.Camera))]
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager instance;
        CameraMode currentMode = new FollowCam();
        [SerializeField] private CameraSettings defaultSettings;


        private void Awake()
        {
            //set instance
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            instance = this;
        }
        void Start()
        {
            //load default settings
            ChangeCameraMode(defaultSettings);
        }

        void LateUpdate()
        {
            //update current mode
            currentMode.Update();
        }

        public void ChangeCameraMode(CameraSettings settings)
        {
            switch (settings.mode) //just resolves enum to camera mode class
            {
                case CameraModes.FollowCam:
                    currentMode = new FollowCam();
                    break;
                case CameraModes.FixedCam:
                    currentMode = new FixedCam();
                    break;
                case CameraModes.CinematicCam:
                    currentMode = new CinematicCam();
                    break;
                default:
                    Debug.LogError("CameraManager: Unknown camera mode.");
                    break;
            }

            currentMode.Switch(this.gameObject, settings); //switch to new mode
            currentMode.Start(); //run the setup
        }
    }
}
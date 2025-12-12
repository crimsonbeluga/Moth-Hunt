using UnityEngine;
namespace MothHunt.Runtime.Camera
{
    //abstract base class for camera modes
    public class CameraMode
    {
        //sets up data needed for cam manipulation
        public void Switch(GameObject cameraObject, CameraSettings settings)
        {
            this.cameraObject = cameraObject;
            this.settings = settings;
            Start();
        }
        //called once the mode is switched to allow for mode specific setup
        public virtual void Start() { }

        //called every frame
        public virtual void Update() { }

        protected GameObject cameraObject;
        protected CameraSettings settings;
    }
}
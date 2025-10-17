using UnityEngine;
namespace MothHunt.Runtime.Camera
{
    public class CameraMode
    {
        public void Switch(GameObject cameraObject, CameraSettings settings)
        {
            this.cameraObject = cameraObject;
            this.settings = settings;
            OnChange();
        }
        public virtual void OnChange() { }
        public virtual void Update() { }

        protected GameObject cameraObject;
        protected CameraSettings settings;
    }
}
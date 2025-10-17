using UnityEngine;

namespace MothHunt.Runtime.Camera
{
    public class FixedCam : CameraMode
    {
        public override void OnChange()
        {
            if (cameraObject == null || settings == null)
            {
                Debug.LogError("FixedCam: cameraObject or settings is null in OnChange.");
                return;
            }

            cameraObject.transform.position = settings.roomPosition + settings.offset;
            cameraObject.transform.rotation = Quaternion.Euler(0f, settings.facingDirection, 0f);
        }

        public override void Update()
        {
        }
    }
}
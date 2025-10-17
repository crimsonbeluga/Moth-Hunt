using UnityEngine;

namespace MothHunt.Runtime.Camera
{
    public class FollowCam : CameraMode
    {
        private Vector3 velocity = Vector3.zero;

        public override void OnChange()
        {
            if (settings == null) return;

            if (settings.snapOnChange && settings.target != null)
            {
                Vector3 startPos = settings.target.position;
                startPos.z = settings.distanceZ;
                cameraObject.transform.position = startPos;
            }
        }

        public override void Update()
        {
            if (settings.target == null || settings == null) return;

            Vector3 desiredPos = CalcTargetPosition();

            if (settings.smoothTime <= 0f)
            {
                cameraObject.transform.position = desiredPos;
            }
            else
            {
                cameraObject.transform.position = Vector3.SmoothDamp(cameraObject.transform.position, desiredPos, ref velocity, settings.smoothTime);
            }
        }

        private Vector3 CalcTargetPosition()
        {
            Vector3 basePos = settings.target.position;

            // Use distanceZ from CameraSettings as the camera's Z (zoom)
            float z = settings.distanceZ;

            Vector3 desired = new Vector3(basePos.x, basePos.y, z);

            if (settings.lockX) desired.x = settings.lockedXValue;
            if (settings.lockY) desired.y = settings.lockedYValue;

            return desired;
        }
    }
}
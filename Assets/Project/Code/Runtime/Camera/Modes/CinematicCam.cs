using UnityEngine;

namespace MothHunt.Runtime.Camera
{
    //cam that moves through a series of points
    public class CinematicCam : CameraMode
    {
        private int currentIndex = 0;
        private float timer = 0f;
        private bool playing = false;

        bool camMoving;

        // added fields for transitions
        private Vector3 startPos;
        private Quaternion startRot;
        private float currentTransitionTime = 0f;
        private float currentWaitTime = 0f;

        public override void Start()
        {
            // initialize playback
            if (settings == null || settings.cinematicWaypoints == null || settings.cinematicWaypoints.Length == 0 || cameraObject == null)
            {
                playing = false;
                return;
            }

            currentIndex = 0;
            timer = 0f;
            playing = true;

            var wp = settings.cinematicWaypoints[0];

            if (settings.snapOnChange)
            {
                // immediately snap to first waypoint, then wait its delay before moving to next
                cameraObject.transform.position = wp.position;
                cameraObject.transform.rotation = wp.rotation;
                camMoving = false;
                timer = 0f;
                currentWaitTime = wp.delayBeforeNext;
            }
            else
            {
                // start smooth transition from current transform to first waypoint
                startPos = cameraObject.transform.position;
                startRot = cameraObject.transform.rotation;
                currentTransitionTime = Mathf.Max(0.0001f, wp.transitionTime);
                timer = 0f;
                camMoving = true;
            }
        }

        public override void Update()
        {
            if (!playing || settings == null || settings.cinematicWaypoints == null || settings.cinematicWaypoints.Length == 0 || cameraObject == null)
                return;

            if (camMoving)
                MoveToPosition(Time.deltaTime);
            else
                Wait(Time.deltaTime);
        }
        private void MoveToPosition(float deltaTime)
        {
            timer += deltaTime;
            float t = currentTransitionTime <= 0f ? 1f : Mathf.Clamp01(timer / currentTransitionTime);
            var target = settings.cinematicWaypoints[currentIndex];

            cameraObject.transform.position = Vector3.Lerp(startPos, target.position, t);
            cameraObject.transform.rotation = Quaternion.Slerp(startRot, target.rotation, t);

            if (t >= 1f)
            {
                // transition done
                camMoving = false;
                timer = 0f;
                currentWaitTime = target.delayBeforeNext;
            }
        }
        private void Wait(float deltaTime)
        {
            timer += deltaTime;

            if (timer >= currentWaitTime)
            {
                int nextIndex = currentIndex + 1;
                var waypoints = settings.cinematicWaypoints;
                if (nextIndex >= waypoints.Length)
                {
                    //does it have a follow-up camera mode?
                    if (settings?.exitSettings != null && CameraManager.instance != null)
                    {
                        // something wrong, switch to default settings
                        playing = false;
                        CameraManager.instance.ChangeCameraMode(settings.exitSettings);
                    }
                    else
                    {
                        playing = false;
                    }

                    return;
                }

                // start transition to next waypoint
                currentIndex = nextIndex;
                var target = waypoints[currentIndex];

                startPos = cameraObject.transform.position;
                startRot = cameraObject.transform.rotation;
                currentTransitionTime = Mathf.Max(0.0001f, target.transitionTime);
                timer = 0f;
                camMoving = true;
            }
        }
        public bool IsOver()
        {
            return !playing;
        }
    }
}
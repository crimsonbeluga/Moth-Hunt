using UnityEngine;

namespace MothHunt.Runtime.Camera
{
    //wrapper class for waypoint data
    [System.Serializable]
    public class CinematicWaypoint
    {
        public Vector3 position = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;
        public float transitionTime = 1f;
        public float delayBeforeNext = 0f;
    }
}

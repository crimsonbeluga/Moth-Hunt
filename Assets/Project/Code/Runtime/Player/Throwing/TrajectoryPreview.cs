using System.Collections.Generic;
using UnityEngine;

namespace MothHunt.Throwing
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class TrajectoryPreview : MonoBehaviour, IThrowPreview
    {
        [Header("Sampling")]
        [Tooltip("Seconds of simulated flight to preview.")]
        public float maxSimTime = 2.5f;

        [Tooltip("Simulation timestep (smaller = smoother curve).")]
        public float dt = 0.03f;

        [Tooltip("Stop when a collider is hit.")]
        public bool stopOnHit = true;

        [Tooltip("Radius for hit tests (0 = ray).")]
        public float hitRadius = 0.05f;

        [Tooltip("Physics layers to consider as obstacles.")]
        public LayerMask hitMask = ~0;

        [Header("Appearance")]
        public float startWidth = 0.025f;
        public float endWidth = 0.01f;
        public int capVertices = 4;

        LineRenderer _lr;
        PlayerThrowController _ctrl;
        bool _visible;

        void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.enabled = false;
            _lr.positionCount = 0;
            _lr.startWidth = startWidth;
            _lr.endWidth = endWidth;
            _lr.numCapVertices = capVertices;
            _lr.useWorldSpace = true;
        }

        void OnEnable()
        {
            _lr.startWidth = startWidth;
            _lr.endWidth = endWidth;
            _lr.numCapVertices = capVertices;
        }

        public void Show(PlayerThrowController ctrl)
        {
            _ctrl = ctrl;
            _visible = true;
            _lr.enabled = true;
        }

        public void Hide()
        {
            _visible = false;
            _lr.enabled = false;
            _lr.positionCount = 0;
            _ctrl = null;
        }

        void Update()
        {
            if (!_visible || _ctrl == null || !_ctrl.IsActive || _ctrl.CurrentDef == null)
            {
                if (_lr.enabled) { _lr.enabled = false; _lr.positionCount = 0; }
                return;
            }

            if (!_lr.enabled) _lr.enabled = true;

            if (!_ctrl.GetPredictedLaunch(out var origin, out var velocity))
            {
                _lr.positionCount = 0; return;
            }

            // 🔒 Plane lock to match controller
            bool planeIsXY = _ctrl.planeIsXY;
            if (planeIsXY) { origin.z = _ctrl.hand ? _ctrl.hand.position.z : origin.z; velocity.z = 0f; }
            else { origin.y = _ctrl.hand ? _ctrl.hand.position.y : origin.y; velocity.y = 0f; }

            // --- Per-item tuning ---
            var def = _ctrl.CurrentDef;
            float gMul = Mathf.Max(0.01f, def.gravityMultiplier);
            Vector3 gravity = Physics.gravity * gMul;

            float airDrag = Mathf.Max(0f, def.airDrag);
            var speedOverLife = def.speedOverLife != null ? def.speedOverLife : AnimationCurve.Linear(0, 1, 1, 1);
            var extraUpAccel = def.extraUpAccelOverLife != null ? def.extraUpAccelOverLife : AnimationCurve.Linear(0, 0, 1, 0);

            // Simple life normalization based on preview time
            float life = def.lifeSecondsOverride > 0 ? def.lifeSecondsOverride : Mathf.Max(maxSimTime, 0.0001f);
            float age = 0f;

            var points = new List<Vector3>(Mathf.CeilToInt(maxSimTime / dt) + 1);
            Vector3 p = origin;
            Vector3 v = velocity;

            points.Add(p);

            while (age < maxSimTime)
            {
                // Forces
                Vector3 a = gravity;

                // Extra upward accel from curve
                float nAge = Mathf.Clamp01(age / life);
                a += Vector3.up * extraUpAccel.Evaluate(nAge);

                // Integrate velocity with accel
                v += a * dt;

                // Apply linear drag (Unity-style, approximate)
                if (airDrag > 0f)
                {
                    // Unity uses v *= 1 / (1 + drag*dt*m^-1) internally; this simple form is OK for preview
                    v *= Mathf.Max(0f, 1f - airDrag * dt);
                }

                // Speed shaping over life (multiplicative)
                float nextNAge = Mathf.Clamp01((age + dt) / life);
                float currMul = speedOverLife.Evaluate(nAge);
                float nextMul = speedOverLife.Evaluate(nextNAge);
                if (currMul > 0.0001f)
                {
                    float stepMul = nextMul / currMul;
                    v *= stepMul; // scale speed smoothly
                }

                // Step position
                Vector3 pNext = p + v * dt;

                // Keep the simulated point on the plane
                if (planeIsXY) pNext.z = origin.z;
                else pNext.y = origin.y;

                if (stopOnHit)
                {
                    Vector3 stepDir = pNext - p;
                    float stepLen = stepDir.magnitude;
                    if (stepLen > 0.000001f) stepDir /= stepLen;
                    if (planeIsXY) stepDir.z = 0f; else stepDir.y = 0f;

                    bool hit;
                    RaycastHit hitInfo;

                    if (hitRadius > 0f)
                        hit = Physics.SphereCast(p, hitRadius, stepDir, out hitInfo, stepLen, hitMask, QueryTriggerInteraction.Ignore);
                    else
                        hit = Physics.Raycast(p, stepDir, out hitInfo, stepLen, hitMask, QueryTriggerInteraction.Ignore);

                    if (hit)
                    {
                        Vector3 hp = hitInfo.point;
                        if (planeIsXY) hp.z = origin.z; else hp.y = origin.y;
                        points.Add(hp);
                        break;
                    }
                }

                points.Add(pNext);
                p = pNext;
                age += dt;
            }

            _lr.positionCount = points.Count;
            _lr.SetPositions(points.ToArray());
        }
    }
}

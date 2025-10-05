using UnityEngine;

namespace MothHunt.Throwing
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ThrowableProjectile : MonoBehaviour
    {
        public ThrowItemDef def;
        public float lifeSeconds = 15f;

        // Set by spawner before/at Launch
        public bool planeIsXY = true;

        Rigidbody _rb;
        float _age;
        bool _launched;

        // For speedOverLife application (ratio per step)
        float _prevLifeMul = 1f;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.useGravity = false; // custom gravity in FixedUpdate
        }

        /// <summary>Called by spawner with the final launch velocity (already plane-clamped).</summary>
        public void Launch(Vector3 velocity)
        {
            if (def && def.lifeSecondsOverride > 0f) lifeSeconds = def.lifeSecondsOverride;

            // Set RB damping from def
            if (def)
            {
                _rb.linearDamping = Mathf.Max(0f, def.airDrag);
                _rb.angularDamping = Mathf.Max(0f, def.angularDrag);
            }

            // 1) Lock the disallowed rotation axes
            _rb.constraints = RigidbodyConstraints.None;
            if (planeIsXY)
                _rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
            else
                _rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            // 2) Zero any residual spin and set a plane-locked spawn rotation once
            _rb.angularVelocity = Vector3.zero;

            if (planeIsXY)
            {
                float zDeg = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(zDeg, Vector3.forward);
            }
            else
            {
                float yDeg = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(yDeg, Vector3.up);
            }

            // 3) Optional launch spin (about the visible axis only)
            if (def && Mathf.Abs(def.spinDegPerSec) > 0.01f)
            {
                float radPerSec = def.spinDegPerSec * Mathf.Deg2Rad;
                Vector3 spinAxis = planeIsXY ? Vector3.forward : Vector3.up;
                _rb.maxAngularVelocity = Mathf.Max(_rb.maxAngularVelocity, Mathf.Abs(radPerSec));
                _rb.angularVelocity = spinAxis * radPerSec;
            }

            // 4) Apply initial velocity
            _rb.linearVelocity = velocity;
            _prevLifeMul = def && def.speedOverLife != null ? Mathf.Max(0.0001f, def.speedOverLife.Evaluate(0f)) : 1f;
            _launched = true;
        }

        void FixedUpdate()
        {
            if (!_launched) return;

            _age += Time.fixedDeltaTime;

            // Gravity (+ per-item multiplier)
            Vector3 g = (def ? Physics.gravity * def.gravityMultiplier : Physics.gravity);
            _rb.AddForce(g, ForceMode.Acceleration);

            // Extra upward acceleration over life
            if (def && def.extraUpAccelOverLife != null)
            {
                float life = Mathf.Max(lifeSeconds, 0.0001f);
                float nAge = Mathf.Clamp01(_age / life);
                float upAccel = def.extraUpAccelOverLife.Evaluate(nAge);
                _rb.AddForce(Vector3.up * upAccel, ForceMode.Acceleration);
            }

            // Speed shaping over life (scale by ratio each step)
            if (def && def.speedOverLife != null)
            {
                float life = Mathf.Max(lifeSeconds, 0.0001f);
                float nAge = Mathf.Clamp01(_age / life);
                float currMul = Mathf.Max(0.0001f, def.speedOverLife.Evaluate(nAge));
                float stepMul = currMul / _prevLifeMul;
                if (stepMul > 0f && stepMul != 1f)
                {
                    // Scale current velocity magnitude, keep direction
                    Vector3 v = _rb.linearVelocity;
                    float mag = v.magnitude;
                    if (mag > 0.00001f)
                    {
                        float newMag = mag * stepMul;
                        _rb.linearVelocity = v * (newMag / mag);
                    }
                }
                _prevLifeMul = currMul;
            }

            // Lifetime
            if (_age >= lifeSeconds) Destroy(gameObject);
        }

        void OnValidate()
        {
            if (lifeSeconds <= 0f) lifeSeconds = 15f;
        }
    }
}

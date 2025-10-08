using UnityEngine;
using MothHunt.Inventory;

namespace MothHunt.Throwing
{
    /// <summary>
    /// Defines how a throwable behaves when thrown — speed, gravity, spin, drag, etc.
    /// Each item type (pebble, bottle, mushroom, etc.) gets its own instance of this asset.
    /// </summary>
    public enum ArcStyle { Straight, Lob, Heavy, Custom }

    [CreateAssetMenu(menuName = "MothHunt/Throw/Throw Item", fileName = "ThrowItemDef")]
    public sealed class ThrowItemDef : ScriptableObject
    {
        // ───────────────────────────── LINK TO INVENTORY ─────────────────────────────
        [Header("Link to Inventory")]
        [Tooltip("Reference to the inventory ItemDef that represents this throwable (e.g., PebbleItem, BottleItem, MushroomItem).")]
        public ItemDef item;


        // ───────────────────────────── VISUALS ─────────────────────────────
        [Header("Visuals")]
        [Tooltip("The projectile prefab spawned when thrown. Must include a Rigidbody and (optionally) a Collider. " +
                 "ThrowableProjectile will be added automatically if missing.")]
        public GameObject projectilePrefab;

        [Tooltip("Optional visual shown in the player's hand when equipped. " +
                 "Ignored if your PlayerThrowController has showHeldInHand=false.")]
        public GameObject holdInHandPrefab;


        // ───────────────────────────── LAUNCH SETTINGS ─────────────────────────────
        [Header("Launch")]
        [Tooltip("Base launch speed in meters per second. Controls how fast the projectile starts when thrown.")]
        public float initialSpeed = 14f;

        [Tooltip("Adds random horizontal spread in degrees on each throw. " +
                 "Use this for less precise throws (e.g., bottles wobble slightly).")]
        public float spreadDegrees = 0f;


        // ───────────────────────────── FLIGHT / PHYSICS ─────────────────────────────
        [Header("Flight / Physics")]
        [Tooltip("Scales global Physics.gravity for this projectile. " +
                 "Values above 1 make it fall faster (heavier), below 1 make it floatier (lighter).")]
        public float gravityMultiplier = 1.0f;

        [Tooltip("How much air resistance slows down the projectile over time. " +
                 "Higher drag = more slowdown. Recommended 0–0.5 for subtle effects.")]
        public float airDrag = 0.0f;

        [Tooltip("How much air resistance slows down spinning motion. " +
                 "Used by the Rigidbody to dampen angular velocity (spin).")]
        public float angularDrag = 0.05f;


        // ───────────────────────────── EXTRA FORCES OVER LIFE ─────────────────────────────
        [Header("Extra Forces Over Life (0..1 normalized)")]
        [Tooltip("Applies an additional upward acceleration (in m/s²) over normalized lifetime (0=start, 1=end). " +
                 "Great for floaty or lift-based motion like a drifting mushroom cap. " +
                 "Curve value = extra upward force in m/s².")]
        public AnimationCurve extraUpAccelOverLife = AnimationCurve.Linear(0, 0, 1, 0);


        // ───────────────────────────── SPEED SHAPING ─────────────────────────────
        [Header("Speed Shaping Over Life")]
        [Tooltip("Multiplier applied to projectile speed over normalized lifetime. " +
                 "Use this to make items slow down (curve < 1) or speed up (curve > 1) over time.")]
        public AnimationCurve speedOverLife = AnimationCurve.Linear(0, 1, 1, 1);


        // ───────────────────────────── SPIN / ROTATION ─────────────────────────────
        [Header("Spin On Launch")]
        [Tooltip("Degrees per second of spin applied at launch. Positive = clockwise, negative = counter-clockwise. " +
                 "Set to 0 for no spin.")]
        public float spinDegPerSec = 0f;


        // ───────────────────────────── AIMING BEHAVIOR ─────────────────────────────
        [Header("Aiming")]
        [Tooltip("If true, the throw direction is based on a raycast from the cursor position. " +
                 "If false, the projectile is thrown straight forward/right from the camera or player.")]
        public bool useAimRaycast = true;

        [Tooltip("Maximum aim range (meters) for raycast-based aiming. " +
                 "Prevents infinite rays and limits how far a player can target.")]
        public float maxAimRange = 18f;

        [Tooltip("Describes the general arc style for reference (Straight = fast/flat, Lob = curved, Heavy = steep). " +
                 "Currently informational; can be used for future curve presets.")]
        public ArcStyle arcStyle = ArcStyle.Lob;


        // ───────────────────────────── AUDIO / AI FEEDBACK ─────────────────────────────
        [Header("Audio/Noise (AI hooks)")]
        [Tooltip("Radius in meters for AI to detect the throw sound or impact. " +
                 "Used by stealth or alert systems if integrated.")]
        public float noiseRadius = 6f;


        // ───────────────────────────── THROW CONTROL ─────────────────────────────
        [Header("Throw Control")]
        [Tooltip("Minimum time (in seconds) between allowed throws for this item.")]
        public float cooldown = 0.25f;

        [Tooltip("If true, the item is removed from inventory after each throw.")]
        public bool consumeOnThrow = true;


        // ───────────────────────────── LIFETIME ─────────────────────────────
        [Header("Lifetime")]
        [Tooltip("Optional override for how long the projectile lasts in seconds. " +
                 "If set to 0, the projectile uses its internal default (usually 15s).")]
        public float lifeSecondsOverride = 0f;


        // ───────────────────────────── CHARGE CURVE ─────────────────────────────
        [Header("Optional Charge Curve")]
        [Tooltip("Defines how charge time affects launch speed. " +
                 "X=charge amount (0–1), Y=speed multiplier. Leave empty if you don’t have charged throws.")]
        public AnimationCurve chargeToSpeedCurve;
    }
}

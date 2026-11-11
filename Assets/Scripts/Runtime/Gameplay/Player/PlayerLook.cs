using UnityEngine;
using VertigoShift.Gameplay.Gravity;
using VertigoShift.Gameplay.Panic;
using VertigoShift.Input;

namespace VertigoShift.Gameplay.Player
{
    /// <summary>
    /// Camera rig of the player: mouse look, gravity aligned orientation, field of view kick
    /// and the "dutch angle" that shows up when the player panics.
    /// The camera always keeps the current gravity surface as its up vector, so flipping the
    /// gravity rotates the whole view by 90 degrees.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerLook : MonoBehaviour
    {
        [Header("References")]
        public PlayerMotor motor;
        public GravityShift gravity;
        public DizzinessSystem dizziness;
        public Transform cameraPivot;
        public Camera cam;

        [Header("Look")]
        public float mouseSensitivity = 0.12f;
        public float minPitch = -85f;
        public float maxPitch = 85f;

        [Header("Eye")]
        [Tooltip("Eye height above the centre of the body, along the current up.")]
        public float eyeOffset = 0.72f;
        public float crouchEyeOffset = 0.34f;

        [Header("Field of view")]
        public float baseFov = 78f;
        public float shiftFovKick = 9f;
        public float fireFovKick = 4.5f;

        [Header("Panic")]
        [Tooltip("Dutch angle applied at critical panic (degrees).")]
        public float criticalDutchAngle = 17f;

        float pitch;
        float fovKick;
        float roll;
        float rollTimer;
        float targetRoll;

        void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (gravity == null) gravity = GetComponent<GravityShift>();
            if (dizziness == null) dizziness = GetComponent<DizzinessSystem>();
        }

        void OnEnable()
        {
            if (gravity != null) gravity.GravityChanged += OnGravityChanged;
        }

        void OnDisable()
        {
            if (gravity != null) gravity.GravityChanged -= OnGravityChanged;
        }

        void OnGravityChanged(GravitySide side)
        {
            AddFovKick(shiftFovKick);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            ApplyMouseLook(dt);
            ApplyOrientation(dt);
            ApplyPanicRoll(dt);
            ApplyFov(dt);
        }

        /// <summary>Kicks the field of view (used for the shot / gravity shift impact).</summary>
        public void AddFovKick(float amount)
        {
            fovKick = Mathf.Max(fovKick, amount);
        }

        void ApplyMouseLook(float dt)
        {
            // Mouse look only makes sense while the cursor is locked to the game window.
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector3 up = gravity != null ? gravity.SmoothUp : Vector3.up;

            Vector2 look = ProtoInput.Look;
            if (look.sqrMagnitude <= 0f) return;

            if (motor != null)
            {
                Vector3 rotated = Quaternion.AngleAxis(look.x * mouseSensitivity, up) * motor.Heading;
                motor.Heading = rotated;
            }

            // pitch > 0 = look up (the camera is built with LookRotation, not Euler)
            pitch = Mathf.Clamp(pitch + look.y * mouseSensitivity, minPitch, maxPitch);
        }

        void ApplyOrientation(float dt)
        {
            if (cameraPivot == null) return;

            Vector3 up = gravity != null ? gravity.SmoothUp : Vector3.up;
            Vector3 heading = motor != null ? motor.Heading : Vector3.forward;

            Vector3 fwd = Vector3.ProjectOnPlane(heading, up);
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(Vector3.forward, up);
            fwd.Normalize();
            Vector3 right = Vector3.Cross(up, fwd);
            Vector3 camForward = Quaternion.AngleAxis(-pitch, right) * fwd;

            bool low = motor != null && (motor.IsCrouching || motor.IsSliding);
            float eye = low ? crouchEyeOffset : eyeOffset;

            Vector3 localOffset = transform.InverseTransformDirection(up) * eye;
            cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, localOffset, 1f - Mathf.Exp(-20f * dt));
            cameraPivot.rotation = Quaternion.LookRotation(camForward, up) * Quaternion.Euler(0f, 0f, roll);
        }

        void ApplyPanicRoll(float dt)
        {
            float n = dizziness != null ? dizziness.Normalized : 0f;
            if (n > 0.85f)
            {
                rollTimer -= dt;
                if (rollTimer <= 0f)
                {
                    rollTimer = Random.Range(0.45f, 1.0f);
                    targetRoll = Random.Range(-criticalDutchAngle, criticalDutchAngle);
                }
            }
            else
            {
                rollTimer = 0f;
                targetRoll = 0f;
            }
            roll = Mathf.Lerp(roll, targetRoll, 1f - Mathf.Exp(-6f * dt));
        }

        void ApplyFov(float dt)
        {
            fovKick = Mathf.Lerp(fovKick, 0f, 1f - Mathf.Exp(-9f * dt));
            if (cam != null) cam.fieldOfView = baseFov + fovKick;
        }
    }
}

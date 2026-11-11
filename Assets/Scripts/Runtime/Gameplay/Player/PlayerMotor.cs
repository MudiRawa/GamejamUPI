using UnityEngine;
using VertigoShift.Core;
using VertigoShift.Gameplay.Gravity;
using VertigoShift.Input;

namespace VertigoShift.Gameplay.Player
{
    /// <summary>
    /// Movement rig of the player: walk, run, crouch, slide (momentum boost) and jump.
    /// Everything is gravity relative - "up" is whatever the GravityShift component currently
    /// says, so the same code walks on the floor, on a wall and on the ceiling.
    /// The camera is driven separately by <see cref="PlayerLook"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("References")]
        public GravityShift gravity;

        [Header("Speeds")]
        public float walkSpeed = 5.5f;
        public float runSpeed = 9.5f;
        public float crouchSpeed = 2.6f;
        public float slideSpeed = 13f;
        [Tooltip("Speed injected when the slide starts (momentum boost).")]
        public float slideBoostSpeed = 17f;

        [Header("Acceleration")]
        public float groundAccel = 55f;
        public float groundFriction = 42f;
        public float airAccel = 12f;
        public float airFriction = 1.5f;
        public float jumpSpeed = 7.5f;
        [Tooltip("Small downward push used to stay glued to the surface while grounded.")]
        public float groundStickSpeed = 1f;

        [Header("Slide")]
        public float slideEntrySpeed = 5.5f;
        public float slideExitSpeed = 4.2f;
        public float slideSteerRate = 150f;
        public float slideFriction = 6f;
        public float slideMaxTime = 2.2f;
        [Tooltip("Maximum distance (meters) a single slide may cover before it ends on its own.")]
        public float slideMaxDistance = 6f;
        [Tooltip("Grace period (seconds) during which a slide cannot be cancelled by releasing the movement keys.")]
        public float slideMinTime = 0.15f;
        [Tooltip("How far the body is pushed off the old surface when the gravity flips.")]
        public float surfaceRelease = 0.06f;

        [Header("Body")]
        public float standHeight = 1.8f;
        public float crouchHeight = 1.05f;
        public float radius = 0.35f;

        public bool IsSliding { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsGrounded { get; private set; }
        public float CurrentSpeed { get; private set; }

        /// <summary>The direction the player faces, projected onto the current gravity plane.</summary>
        public Vector3 Heading
        {
            get { return heading; }
            set
            {
                Vector3 projected = Vector3.ProjectOnPlane(value, Up);
                if (projected.sqrMagnitude > 0.0001f) heading = projected.normalized;
            }
        }

        /// <summary>The surface the player currently stands on.</summary>
        public Vector3 Up { get { return gravity != null ? gravity.TargetUp : Vector3.up; } }

        /// <summary>True while the crouch button is held and has not been consumed by a slide.</summary>
        public bool CrouchActive { get { return ProtoInput.CrouchHeld && !crouchConsumed; } }

        Rigidbody rb;
        CapsuleCollider capsule;
        Vector3 heading;
        Vector3 slideDir;
        Vector3 currentUp;
        float slideTimer;
        float slideDistance;
        float jumpBuffer;
        /// <summary>
        /// Set when a slide consumed the crouch button. While it is set the player stays
        /// upright and cannot start another slide until the crouch button is released,
        /// so a finished slide never leaves the character stuck in the crouch/slide pose.
        /// </summary>
        bool crouchConsumed;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            if (gravity == null) gravity = GetComponent<GravityShift>();

            rb.useGravity = false;
            RigidbodyCompat.SetLinearDamping(rb, 0f);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

            capsule.radius = radius;
            capsule.height = standHeight;
            capsule.center = Vector3.zero;
            ApplyFrictionlessMaterial();

            currentUp = Up;
            heading = Vector3.ProjectOnPlane(transform.forward, currentUp);
            if (heading.sqrMagnitude < 0.01f) heading = Vector3.ProjectOnPlane(Vector3.forward, currentUp);
            heading.Normalize();

            if (gravity != null) gravity.GravityChanged += OnGravityChanged;
        }

        void OnDestroy()
        {
            if (gravity != null) gravity.GravityChanged -= OnGravityChanged;
        }

        void OnGravityChanged(GravitySide side)
        {
            Vector3 newUp = gravity.TargetUp;
            Vector3 oldUp = currentUp;

            Vector3 velocity = RigidbodyCompat.GetVelocity(rb);
            velocity = Vector3.ProjectOnPlane(velocity, oldUp);
            RigidbodyCompat.SetVelocity(rb, velocity);
            rb.position = rb.position + oldUp * surfaceRelease;

            currentUp = newUp;

            Vector3 projected = Vector3.ProjectOnPlane(heading, newUp);
            if (projected.sqrMagnitude < 0.01f)
            {
                projected = Vector3.ProjectOnPlane(Vector3.forward, newUp);
                if (projected.sqrMagnitude < 0.01f) projected = Vector3.ProjectOnPlane(Vector3.right, newUp);
            }
            heading = projected.normalized;
        }
        void ApplyFrictionlessMaterial()
        {
#if UNITY_6000_0_OR_NEWER
            PhysicsMaterial mat = new PhysicsMaterial("M_PlayerFrictionless");
            mat.frictionCombine = PhysicsMaterialCombine.Minimum;
#else
            PhysicMaterial mat = new PhysicMaterial("M_PlayerFrictionless");
            mat.frictionCombine = PhysicMaterialCombine.Minimum;
#endif
            mat.dynamicFriction = 0f;
            mat.staticFriction = 0f;
            mat.bounciness = 0f;
            capsule.sharedMaterial = mat;
        }

        void Update()
        {
            // A finished slide consumes the crouch button: while it is still held the
            // player stays upright and cannot slide again. Releasing the button clears
            // the flag so a fresh press crouches / slides again.
            if (ProtoInput.CrouchReleased || !ProtoInput.CrouchHeld) crouchConsumed = false;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (ProtoInput.JumpPressed) jumpBuffer = 0.18f;
            else jumpBuffer -= dt;

            UpdateCrouch();
        }

        void UpdateCrouch()
        {
            // While a slide is active the body stays low. Once the slide is over the
            // crouch button counts as "used up", so the player stands back up even if
            // the button is still physically held down.
            bool wantCrouch = CrouchActive || IsSliding;
            if (!wantCrouch && IsCrouching && !HasHeadroom()) wantCrouch = true;

            if (wantCrouch == IsCrouching) return;
            IsCrouching = wantCrouch;

            float h = IsCrouching ? crouchHeight : standHeight;
            capsule.height = h;
            // Keep the feet where they were: shift the collider centre down by half the delta.
            capsule.center = new Vector3(0f, -(standHeight - h) * 0.5f, 0f);
            rb.centerOfMass = capsule.center;
        }

        bool HasHeadroom()
        {
            Vector3 up = Up;
            Vector3 p = transform.position + up * (standHeight * 0.5f + 0.05f);
            Collider[] hits = Physics.OverlapSphere(p, radius * 0.8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.root != transform.root) return false;
            }
            return true;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (dt <= 0f) return;

            Vector3 up = Up;
            float strength = gravity != null ? gravity.gravityStrength : 26f;

            Vector3 velocity = RigidbodyCompat.GetVelocity(rb);
            Vector3 planar = Vector3.ProjectOnPlane(velocity, up);
            float upSpeed = Vector3.Dot(velocity, up);

            IsGrounded = CheckGround(up);

            // --- wish direction (relative to the current gravity plane)
            Vector2 mv = ProtoInput.Move;
            Vector3 fwd = Vector3.ProjectOnPlane(heading, up);
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(Vector3.forward, up);
            fwd.Normalize();
            Vector3 right = Vector3.Cross(up, fwd);
            Vector3 wish = right * mv.x + fwd * mv.y;
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            bool hasInput = wish.sqrMagnitude > 0.01f;

            bool runHeld = ProtoInput.RunHeld;
            bool crouchHeld = ProtoInput.CrouchHeld;
            bool crouchActive = CrouchActive;

            // --- slide state machine
            if (!IsSliding && crouchActive && runHeld && IsGrounded && planar.magnitude >= slideEntrySpeed)
            {
                IsSliding = true;
                slideTimer = 0f;
                slideDistance = 0f;
                slideDir = planar.normalized;
                planar = slideDir * slideBoostSpeed;
            }

            if (IsSliding)
            {
                slideTimer += dt;

                // Track how far the slide has travelled so it can be cut short after a
                // few meters instead of running until it hits a wall.
                slideDistance += planar.magnitude * dt;

                if (hasInput) slideDir = Vector3.RotateTowards(slideDir, wish, slideSteerRate * dt, 0f);
                planar = Vector3.MoveTowards(planar, slideDir * slideSpeed, slideFriction * dt);

                // Letting go of every movement key ends the slide as well: the remaining
                // momentum is bled off by the normal ground friction below.
                bool releasedMovement = !hasInput && slideTimer > slideMinTime;

                if (!crouchHeld || releasedMovement || planar.magnitude < slideExitSpeed ||
                    slideTimer > slideMaxTime || slideDistance >= slideMaxDistance || !IsGrounded)
                {
                    IsSliding = false;
                    // The crouch button that started this slide is now "used up": the
                    // player returns to standing and must release + re-press it to
                    // crouch or slide again.
                    crouchConsumed = crouchHeld;
                }
            }
            else
            {
                float targetSpeed = crouchActive ? crouchSpeed : (runHeld ? runSpeed : walkSpeed);
                if (hasInput)
                {
                    float accel = IsGrounded ? groundAccel : airAccel;
                    planar = Vector3.MoveTowards(planar, wish * targetSpeed, accel * dt);
                }
                else
                {
                    planar = Vector3.MoveTowards(planar, Vector3.zero, (IsGrounded ? groundFriction : airFriction) * dt);
                }
            }

            // --- vertical (along the current up)
            if (IsGrounded)
            {
                upSpeed = -groundStickSpeed; // gentle stick to the surface
                if (jumpBuffer > 0f)
                {
                    upSpeed = jumpSpeed;
                    jumpBuffer = 0f;
                    IsGrounded = false;
                }
            }
            else
            {
                upSpeed -= strength * dt;
            }

            RigidbodyCompat.SetVelocity(rb, planar + up * upSpeed);
            CurrentSpeed = planar.magnitude;
        }

        bool CheckGround(Vector3 up)
        {
            float verticality = Mathf.Abs(Vector3.Dot(up, Vector3.up));
            float halfExtent = Mathf.Lerp(radius, standHeight * 0.5f, verticality);
            float distance = halfExtent + 0.25f;

            Vector3 origin = transform.position + up * 0.2f;
            RaycastHit hit;
            if (Physics.Raycast(origin, -up, out hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return Vector3.Dot(hit.normal, up) > 0.5f;

            return false;
        }
    }
}

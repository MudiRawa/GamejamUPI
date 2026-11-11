using System;
using UnityEngine;
using VertigoShift.Audio;
using VertigoShift.Gameplay.Panic;
using VertigoShift.Input;

namespace VertigoShift.Gameplay.Gravity
{
    /// <summary>
    /// 4 sided gravity shift (GDD section 3.A).
    /// Owns the global Physics.gravity vector and flips it to one of the four walls.
    /// Spam shifting raises the dizziness meter (handled through DizzinessSystem).
    /// </summary>
    [DisallowMultipleComponent]
    public class GravityShift : MonoBehaviour
    {
        [Header("References")]
        public DizzinessSystem dizziness;
        public SfxBank sfx;

        [Header("Physics")]
        public float gravityStrength = 26f;
        [Tooltip("How fast the view re-orients to the new up vector.")]
        public float reorientSpeed = 13f;

        [Header("Panic cost (percent of the dizziness meter)")]
        public float baseShiftCost = 8f;
        public float rapidShiftCost = 15f;
        public float spamShiftCost = 25f;
        [Tooltip("Two shifts closer than this count as panic spam.")]
        public float rapidWindow = 1.2f;

        public GravitySide CurrentSide { get; private set; } = GravitySide.Floor;
        public Vector3 GravityDirection { get; private set; } = Vector3.down;

        /// <summary>The direction the player is currently standing on (opposite of gravity).</summary>
        public Vector3 TargetUp { get { return -GravityDirection; } }

        /// <summary>TargetUp smoothed over time - used to animate the camera re-orientation.</summary>
        public Vector3 SmoothUp { get; private set; } = Vector3.up;

        public float TimeSinceLastShift { get; private set; } = 99f;
        public int ShiftStreak { get; private set; } = 0;
        public bool HasShifted { get; private set; } = false;

        /// <summary>Raised every time the active gravity surface changes.</summary>
        public event Action<GravitySide> GravityChanged;

        void Awake()
        {
            if (dizziness == null) dizziness = GetComponent<DizzinessSystem>();
            if (sfx == null) sfx = GetComponent<SfxBank>();
            ApplySide(GravitySide.Floor);
            SmoothUp = TargetUp;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            TimeSinceLastShift += dt;

            if (ProtoInput.DigitPressed(1)) RequestShift(GravitySide.Floor);
            else if (ProtoInput.DigitPressed(2)) RequestShift(GravitySide.LeftWall);
            else if (ProtoInput.DigitPressed(3)) RequestShift(GravitySide.Ceiling);
            else if (ProtoInput.DigitPressed(4)) RequestShift(GravitySide.RightWall);
            else if (ProtoInput.QPressed) CycleShift(-1);
            else if (ProtoInput.EPressed) CycleShift(1);

            SmoothUp = Vector3.Slerp(SmoothUp, TargetUp, 1f - Mathf.Exp(-reorientSpeed * dt));
        }

        /// <summary>Returns true when the shift actually happened (costs dizziness).</summary>
        public bool RequestShift(GravitySide side)
        {
            if (side == CurrentSide && HasShifted) return false;

            float cost = baseShiftCost;
            if (TimeSinceLastShift <= rapidWindow)
            {
                ShiftStreak++;
                cost = ShiftStreak >= 3 ? spamShiftCost : rapidShiftCost;
            }
            else
            {
                ShiftStreak = 1;
            }

            ApplySide(side);
            TimeSinceLastShift = 0f;

            if (dizziness != null)
            {
                dizziness.AddDizziness(cost);
                dizziness.RegisterShift();
            }
            if (sfx != null) sfx.PlayWhoosh();

            return true;
        }

        /// <summary>Rotates the gravity surface by one step (clockwise / counter clockwise).</summary>
        public void CycleShift(int direction)
        {
            GravitySide[] order = { GravitySide.Floor, GravitySide.RightWall, GravitySide.Ceiling, GravitySide.LeftWall };
            int index = Array.IndexOf(order, CurrentSide);
            if (index < 0) index = 0;
            index = (index + direction + order.Length) % order.Length;
            RequestShift(order[index]);
        }

        void ApplySide(GravitySide side)
        {
            CurrentSide = side;
            GravityDirection = DirectionFor(side);
            Physics.gravity = GravityDirection * gravityStrength;
            HasShifted = true;
            if (GravityChanged != null) GravityChanged(side);
        }

        public static Vector3 DirectionFor(GravitySide side)
        {
            switch (side)
            {
                case GravitySide.Floor: return Vector3.down;
                case GravitySide.Ceiling: return Vector3.up;
                case GravitySide.LeftWall: return Vector3.left;
                case GravitySide.RightWall: return Vector3.right;
            }
            return Vector3.down;
        }
    }
}

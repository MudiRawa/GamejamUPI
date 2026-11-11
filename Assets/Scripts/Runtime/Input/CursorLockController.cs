using System;
using UnityEngine;

namespace VertigoShift.Input
{
    /// <summary>
    /// Locks the mouse cursor to the game window while playing and frees it again with ESC.
    /// The cursor is released automatically when the application loses focus so the player
    /// never ends up with a locked cursor they cannot escape from.
    /// </summary>
    [DisallowMultipleComponent]
    public class CursorLockController : MonoBehaviour
    {
        [Header("Behaviour")]
        [Tooltip("Lock the cursor as soon as the game starts.")]
        public bool lockOnStart = true;
        [Tooltip("ESC toggles the cursor lock.")]
        public bool escapeToggles = true;
        [Tooltip("Hide the cursor completely while it is locked.")]
        public bool hideCursorWhenLocked = true;
        [Tooltip("Re-lock the cursor when the game window regains focus.")]
        public bool relockOnFocus = true;

        /// <summary>True while the cursor is locked to the game window.</summary>
        public bool IsLocked { get; private set; }

        /// <summary>Raised whenever the lock state changes (true = locked).</summary>
        public event Action<bool> LockStateChanged;

        bool releasedByUser;

        void Start()
        {
            if (lockOnStart) SetLocked(true);
        }

        void Update()
        {
            if (escapeToggles && ProtoInput.EscapePressed)
            {
                releasedByUser = !IsLocked;
                SetLocked(!IsLocked);
                return;
            }

            // The window can be clicked into without the focus event (e.g. alt+tab back in),
            // so re-lock here as well when the player did not release the cursor manually.
            if (!IsLocked && relockOnFocus && !releasedByUser && Application.isFocused)
                SetLocked(true);
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SetLocked(false);
            }
            else if (relockOnFocus && !releasedByUser)
            {
                SetLocked(true);
            }
        }

        void OnDisable()
        {
            SetLocked(false);
        }

        public void SetLocked(bool locked)
        {
            if (IsLocked == locked && Cursor.lockState == (locked ? CursorLockMode.Locked : CursorLockMode.None)) return;

            IsLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !(locked && hideCursorWhenLocked);

            if (LockStateChanged != null) LockStateChanged(locked);
        }
    }
}

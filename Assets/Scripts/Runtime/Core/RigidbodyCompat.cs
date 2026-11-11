using UnityEngine;

namespace VertigoShift.Core
{
    /// <summary>
    /// Unity 6 renamed Rigidbody.velocity -> linearVelocity and drag -> linearDamping.
    /// These helpers keep the project compiling on either API.
    /// </summary>
    public static class RigidbodyCompat
    {
        public static Vector3 GetVelocity(Rigidbody rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVelocity(Rigidbody rb, Vector3 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }

        public static void SetLinearDamping(Rigidbody rb, float value)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearDamping = value;
#else
            rb.drag = value;
#endif
        }
    }
}

using UnityEngine;

namespace VertigoShift.Core
{
    /// <summary>
    /// Anything that can take damage (targets, enemies, ...).
    /// </summary>
    public interface IDamageable
    {
        float Health { get; }
        void TakeDamage(float amount, Vector3 hitPoint);
    }
}

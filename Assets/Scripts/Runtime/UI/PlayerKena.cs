using UnityEngine;

namespace VertigoShift.UI
{
    /// <summary>
    /// Entry point feedback saat player terkena tembakan.
    /// Meneruskan ke DamageFeedback (Canvas/uGUI).
    /// </summary>
    public class PlayerKena : MonoBehaviour
    {
        public float lamaMerah = 0.6f;
        public float lamaPanah = 1.2f;
        public float kuatGeter = 0.45f;
        public Color warna = new Color(1f, 0.1f, 0.15f);

        static PlayerKena instance;

        public static void Kena(Vector3 dariMana)
        {
            // DamageFeedback sudah menangani geter kamera juga.
            DamageFeedback.Kena(dariMana);
        }

        void Awake()
        {
            instance = this;
        }
    }
}

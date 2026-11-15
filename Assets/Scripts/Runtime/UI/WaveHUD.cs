using UnityEngine;
using VertigoShift.Gameplay.Waves;

namespace VertigoShift.UI
{
    /// <summary>
    /// Simple IMGUI counter for the current wave and remaining enemies.
    /// </summary>
    public class WaveHUD : MonoBehaviour
    {
        public EnemyWaveManager waveManager;

        GUIStyle style;

        void EnsureStyle()
        {
            if (style != null) return;
            style = new GUIStyle(GUI.skin.label);
            style.fontSize = 22;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = new Color(0.75f, 0.95f, 1f);
            style.alignment = TextAnchor.MiddleCenter;
        }

        void Update()
        {
            if (waveManager == null)
                waveManager = FindAnyObjectByType<EnemyWaveManager>();
        }

        void OnGUI()
        {
            EnsureStyle();
            if (waveManager == null) return;

            float x = Screen.width * 0.5f - 120f;
            float y = 16f;
            GUI.Label(new Rect(x, y, 240f, 28f), "Wave: " + waveManager.CurrentWave, style);
            GUI.Label(new Rect(x, y + 26f, 240f, 28f), "Enemy Left: " + waveManager.EnemiesRemaining, style);
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using VertigoShift.Gameplay.Player;

namespace VertigoShift.Gameplay.Weapons
{
    // model revolver 3D di tangan (kanan bawah layar). dipasang lewat Tools > Senjata > Pasang Revolver 3D.
    // cuma tampilan: nembaknya tetep diurus Revolver.cs. di sini cuma gerakin modelnya:
    // nendang pas nembak, tabung muter 60 derajat tiap tembakan, miring + tabung muter pas reload,
    // goyang ngikutin mouse, sama ayun pas jalan
    public class RevolverViewmodel : MonoBehaviour
    {
        public Revolver revolver;
        public PlayerMotor motor;

        [Header("Tabung (bagian yang muter)")]
        public Transform[] tabung;
        public Vector3 pusatTabung;                   // titik tengah tabung (koordinat lokal objek ini)
        public Vector3 sumbuTabung = Vector3.forward; // sumbu putar tabung (lokal)

        [Header("Rasa")]
        public float kickMundur = 0.08f;   // seberapa jauh mundur pas nembak (meter)
        public float kickNaik = 14f;       // seberapa dongak pas nembak (derajat)
        public float goyangMouse = 2.5f;   // seberapa ngikutin gerak mouse
        public float ayunJalan = 0.012f;   // seberapa naik turun pas jalan

        Vector3 posAwal;
        Quaternion rotAwal;
        int tembakTadi;
        float kick, reload, waktuJalan;
        float sudutTabung, sudutTarget;
        Vector2 goyang;

        void Start()
        {
            posAwal = transform.localPosition;
            rotAwal = transform.localRotation;
            if (revolver == null) revolver = GetComponentInParent<Revolver>();
            if (motor == null) motor = GetComponentInParent<PlayerMotor>();
            if (revolver != null) tembakTadi = revolver.ShotsFired;
        }

        void LateUpdate()
        {
            // pakai waktu asli biar tetep gerak pas hitstop (game diperlambat sebentar tiap kena musuh)
            float dt = Time.unscaledDeltaTime;
            if (Time.timeScale <= 0f) return;

            // ---- nembak: nendang + tabung muter 1 lubang
            if (revolver != null && revolver.ShotsFired != tembakTadi)
            {
                tembakTadi = revolver.ShotsFired;
                kick = 1f;
                sudutTarget += 60f;
            }
            kick = Mathf.MoveTowards(kick, 0f, dt * 6f);

            // ---- reload: pistol miring ke kiri, tabung muter kenceng
            bool lagiReload = revolver != null && revolver.IsReloading;
            reload = Mathf.MoveTowards(reload, lagiReload ? 1f : 0f, dt * 5f);
            if (lagiReload) sudutTarget += 900f * dt;

            // ---- putar tabung pelan-pelan ke sudut tujuan
            float sebelum = sudutTabung;
            sudutTabung = Mathf.Lerp(sudutTabung, sudutTarget, 18f * dt);
            float beda = sudutTabung - sebelum;
            if (tabung != null && Mathf.Abs(beda) > 0.0001f)
            {
                Vector3 pusat = transform.TransformPoint(pusatTabung);
                Vector3 sumbu = transform.TransformDirection(sumbuTabung);
                foreach (Transform t in tabung)
                    if (t != null) t.RotateAround(pusat, sumbu, beda);
            }

            // ---- goyang ngikutin mouse
            Vector2 mouse = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            Vector2 mau = Vector2.ClampMagnitude(new Vector2(-mouse.y, mouse.x) * 0.02f * goyangMouse, 6f);
            goyang = Vector2.Lerp(goyang, mau, 10f * dt);

            // ---- ayun pas jalan
            float laju = motor != null ? motor.CurrentSpeed : 0f;
            bool napak = motor == null || motor.IsGrounded;
            waktuJalan += dt * laju * 1.2f;
            float kuatAyun = Mathf.Clamp01(laju / 6f) * (napak ? 1f : 0.3f);
            Vector3 ayun = new Vector3(Mathf.Cos(waktuJalan) * ayunJalan, Mathf.Abs(Mathf.Sin(waktuJalan)) * ayunJalan, 0f) * kuatAyun;

            // ---- gabung semua
            float k = kick * kick; // nendangnya cepet, baliknya halus
            transform.localPosition = posAwal + ayun + new Vector3(0f, -0.1f * reload, -kickMundur * k);
            transform.localRotation = rotAwal * Quaternion.Euler(-kickNaik * k + goyang.x - 10f * reload, goyang.y, 35f * reload);
        }
    }
}

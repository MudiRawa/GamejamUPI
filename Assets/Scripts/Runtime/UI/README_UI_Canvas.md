# Panduan UI Canvas/uGUI — GamejamUPI

UI sudah dipindahkan dari **IMGUI (`OnGUI`)** ke **Unity uGUI (Canvas, Text, Image, Button, Slider)**.
Canvas dan semua elemennya sekarang **benar-benar ada di dalam scene**, jadi bisa langsung
dilihat dan diubah lewat **Scene view / Inspector** tanpa menyentuh kode.

---

## Struktur File

| File | Fungsi |
|------|--------|
| `UITheme.cs` | Pusat warna, font, dan ukuran huruf. |
| `UIFactory.cs` | Helper pembuat elemen UI (Text, Image, Button, Slider, Panel). |
| `UIBind.cs` | Helper pencari elemen UI yang sudah ada di scene. |
| `MainMenuCanvas.cs` | Menu utama (Canvas + panel utama/pengaturan/kredit). |
| `HUDController.cs` | Pengatur seluruh HUD in-game. |
| `CrosshairDisplay.cs` | Crosshair di tengah layar. |
| `GravityDisplay.cs` | Indikator gravity di kiri atas. |
| `DizzinessDisplay.cs` | Meter pusing/panik + bar. |
| `AmmoDisplay.cs` | Ammo revolver di kanan bawah. |
| `HelpDisplay.cs` | Petunjuk kontrol + stats (F1). |
| `WaveAnnouncement.cs` | Wave counter + pengumuman tengah layar. |
| `DamageFeedback.cs` | Pinggir merah + panah arah saat kena tembak. |
| `Assets/Editor/UI/CanvasUIBuilder.cs` | Menu Editor untuk membuat/menghapus UI di scene. |

---

## Cara Membuat UI di Scene

Buka menu:

**Tools → Vertigo Shift → UI**

| Menu | Fungsi |
|------|--------|
| **Bangun UI Main Menu (Scene Ini)** | Membuat `Canvas_MainMenu` + semua panel di scene Main Menu. |
| **Bangun UI HUD (Scene Ini)** | Membuat `Canvas_HUD` + `Canvas_Damage` + semua elemen HUD di scene InGame. |
| **Bangun Semua UI (Main Menu + InGame)** | Melakukan keduanya sekaligus (buka & simpan kedua scene). |
| **Hapus UI Buatan (Scene Ini)** | Menghapus Canvas buatan beserta isinya. |

Jalankan sekali. Setelah itu UI tersimpan di scene dan bisa diedit bebas.

> **Hentikan Play Mode dulu** sebelum memakai menu ini.

---

## Mengubah UI Tanpa Kode

Semua elemen adalah GameObject biasa di dalam Canvas. Klik langsung di Hierarchy/Scene view:

| Apa yang mau diubah | Di mana |
|---------------------|---------|
| Posisi / ukuran elemen | Pilih objek → atur `RectTransform` (anchor, pivot, posisi) |
| Warna tombol/teks/garis | Pilih objek → komponen `Image` / `Text` → `Color` |
| Isi tulisan | Pilih objek `Text` → kolom **Text** |
| Font & ukuran huruf | Komponen `Text` → **Font** / **Font Size** |
| Teks judul, slogan, kredit | `MainMenuCanvas` di Inspector |
| Nama scene game | `MainMenuCanvas.sceneGame` |
| Warna/ukuran global | Asset `Assets/UI/UITheme.asset` |
| Gambar ammo (peluru, tabung, lubang) | `AmmoDisplay` → `gambarPeluru`, `gambarTabung`, `gambarLubang` |

### Nama objek penting (dipakai script)

Script mencari elemen berdasarkan **nama** ini, jadi jangan di-rename:

- Menu: `Panel_Utama`, `Panel_Pengaturan`, `Panel_Kredit`, `Judul`, `Slogan`,
  `Btn_Mainkan`, `Btn_Pengaturan`, `Btn_Kredit`, `Btn_Keluar`, `Btn_Kembali`, `Btn_KembaliKredit`,
  `Slider_Volume`, `Slider_Sens`, `Txt_Volume`, `Txt_Sens`
- HUD: `Crosshair_Atas/Bawah/Kiri/Kanan`, `Txt_Gravity`, `Panel_Pusing`, `Panel_Ammo`,
  `Panel_Wave`, `Panel_WaveTengah`, `Panel_Help`, `Panel_Stats`
- Damage: `Canvas_Damage` → `Panel_Merah`, `Img_Panah`

Kalau mau mengganti struktur, cukup ubah **posisi/warna/ukuran**; nama tetap.

---

## Cara Kerja Saat Runtime

1. Script mengecek apakah Canvas/objek UI **sudah ada di scene**.
2. Kalau ada → objek itu yang dipakai (tidak membuat duplikat).
3. Kalau tidak ada → UI dibuat otomatis (mode fallback), jadi game tetap jalan.

Artinya: UI yang sudah dibangun lewat menu Editor akan dipakai, dan kalau Canvas dihapus
game masih bisa membuat UI sendiri.

---

## Detail Penting

- Canvas memakai `CanvasScaler` **Scale With Screen Size**, referensi **1920×1080**,
  jadi otomatis menyesuaikan resolusi.
- `EventSystem` otomatis dibuat (memakai `InputSystemUIInputModule` karena project
  memakai Input System baru).
- `Canvas_Damage` memakai `sortingOrder = 100` supaya efek kena tembak selalu di atas HUD.
- Font Tektur ada di `Assets/UI/Font` (`Tektur-900.ttf` untuk judul, `Tektur-700.ttf` untuk tulisan/angka).

---

## Catatan Migrasi dari IMGUI

Script lama tetap dipertahankan sebagai wrapper karena scene mereferensikannya:

- `MainMenu.cs` → `MainMenuCanvas`
- `PlayerKena.cs` → `DamageFeedback`
- `PrototypeHUD.cs` → `HUDController`
- `WaveHUD.cs` → `WaveAnnouncement`

Semua fungsi lama (tombol menu, slider pengaturan, ammo, dizziness, gravity, wave,
damage feedback) tetap bekerja seperti sebelumnya.

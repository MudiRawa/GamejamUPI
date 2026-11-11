# VERTIGO SHIFT — Prototype v0.2

Prototype mekanik dasar untuk game arena shooter sci-fi berbasis manipulasi gravitasi,
dibangun mengikuti dokumen konsep "VERTIGO SHIFT" (GDD).

## Menjalankan

1. Buka scene **`Assets/Scenes/VertigoShiftPrototype.unity`**
2. Tekan **Play**
3. (Opsional) Rebuild arena dari nol: menu **`Tools > Vertigo Shift > Build Prototype Scene`**

> Klik Game View dulu supaya input keyboard/mouse diterima editor.

## Kontrol

| Aksi | Tombol |
|---|---|
| Jalan / Lari | `WASD` / tahan `SHIFT` |
| Crouch | tahan `CTRL` atau `C` |
| **Slide** (momentum boost) | `SHIFT` (lari) + `CTRL`/`C` saat grounded |
| Jump (relatif gravitasi) | `SPACE` |
| Tembak | `LMB` (auto-fire saat ditahan) |
| Reload | `R` |
| **Gravity Shift** | `1` Floor · `2` Left Wall · `3` Ceiling · `4` Right Wall |
| Gravity shift berputar | `Q` / `E` |
| **Kunci / lepas kursor mouse** | `ESC` |

## Arsitektur code

Pemisahan **runtime vs editor** lewat Assembly Definition, lalu di dalam runtime dipecah
per *domain* (folder = domain, namespace = domain). Aturan main:

> Tiap folder punya satu tanggung jawab. Komponen hanya boleh bergantung ke folder
> yang "di bawah"/lebih umum (Core, Input), tidak boleh circular.

```
Assets/
├── Scripts/
│   └── Runtime/                        asmdef: VertigoShift.Runtime
│       ├── Core/                       utilitas lintas domain
│       │   └── RigidbodyCompat.cs          kompat Unity 6 (velocity → linearVelocity)
│       ├── Input/                      semua input +	kursor
│       │   ├── ProtoInput.cs                abstraksi keyboard/mouse (Input System / legacy)
│       │   └── CursorLockController.cs      kunci kursor mouse saat main
│       ├── Gameplay/                   inti permainan
│       │   ├── Gravity/                    gravitasi 4 sisi
│       │   │   ├── GravitySide.cs              enum sisi arena
│       │   │   └── GravityShift.cs             owner Physics.gravity + biaya spam shift
│       │   ├── Player/                    CPP motion + kamera dipisah
│       │   │   ├── PlayerMotor.cs               gerak badan (walk/run/crouch/slide/jump)
│       │   │   └── PlayerLook.cs                mouse look, orientasi gravitasi, FOV, dutch angle
│       │   ├── Panic/                     bar pusing + feedback-nya
│       │   │   └── DizzinessSystem.cs
│       │   ├── Weapons/                   senjata
│       │   │   └── Revolver.cs
│       │   └── Targets/                   target shootable + gibs
│       │       └── TargetDummy.cs
│       ├── Audio/                      SFX prosedural (tanpa aset audio)
│       │   └── SfxBank.cs
│       └── UI/                         HUD debug (IMGUI)
│           └── PrototypeHUD.cs
└── Editor/                            asmdef: VertigoShift.Editor (includePlatforms: Editor)
    └── SceneBuilder/
        └── PrototypeSceneBuilder.cs     membangun scene + material + lighting dari nol
```

### Kenapa dipisah begini

- **`PlayerMotor` vs `PlayerLook`** — motor hanya mengurus *badan* (collider, velocity,
  ground check), `PlayerLook` hanya mengurus *kamera* (look, pitch, FOV, dutch angle).
  Keduanya berbagi satu state: `PlayerMotor.Heading` (arah pandang pada bidang gravitasi).
- **`GravityShift` sebagai sumber kebenaran gravitasi** — komponen lain menanyakan
  `TargetUp` / `SmoothUp`, tidak ada yang menyimpan vektor gravitasi sendiri.
- **Event `GravityChanged`** — `PlayerMotor` (physics) dan `PlayerLook` (visual)
  bereaksi ke perubahan gravitasi tanpa saling saling memanggil.
- **`CursorLockController` terpisah** — concerns UI/input, bukan movement.

## Struktur scene

```
Arena/            Kubus 40x40x40 (lantai, 4 dinding, langit-langit), cover block,
                  platform di dinding/langit-langit, neon strip
Player/           Rigidbody + CapsuleCollider (layer Ignore Raycast)
  CameraPivot/    Offset mata relatif gravitasi
    PlayerCamera/ Camera (tag MainCamera) + AudioListener + post processing
      Tracer/     LineRenderer peluru
      MuzzleFlash/ Point light tembakan
Targets/          7 target shootable (lantai, dinding kiri/kanan, langit-langit)
Global Volume/    VolumeProfile: Chromatic Aberration, Vignette, Color Adjustments
PrototypeHUD/     HUD debug (IMGUI): peluru, reload, sisi gravitasi, bar pusing, speed
```

## Yang sudah sesuai GDD

- **4-Sided Gravity Shift** — `Physics.gravity` di-update instan ke 4 sisi arena.
  Pemain jatuh ke dinding, kamera ikut miring mengikuti gravitasi baru.
- **Bar Pusing (Dizziness)** — shift biasa `+8%`, spam (<1.2s) `+15%`, spam lanjutan `+25%`.
  Ranged kill `-5%`, close-range execution `-18%`. Tiga state visual
  (Normal / High Tension / Critical Panic) + dutch angle + audio low-pass.
- **Feel** — hitstop 0.045s, tracer, muzzle flash, FOV kick saat shift & tembakan,
  particle *gibs* low-poly, SFX prosedural.
- **Mouse cursor terkunci** saat main, dilepas dengan `ESC`, otomatis dilepas saat
  jendela game kehilangan fokus.

## Known issues / belum dikerjakan

1. **Belum ada musuh** (ranged turret & melee chaser) — ini prioritas berikutnya.
   `TargetDummy` sudah menjadi placeholder untuk menguji raycast & relief pusing.
2. **Motion blur** belum aktif — di URP 17 `MotionBlur` adalah *Renderer Feature*, bukan
   komponen Volume. Butuh asset renderer feature + setup pipeline asset.
3. **Body capsule selalu vertikal di world** (konsekuensi: crouch selalu shrunk
   di sumbu Y world). Capsule yang ikut berotasi dengan gravitasi akan lebih "benar"
   secara fisika, tapi berisiko pop-up CapsuleCollider saat rotasi.
4. **Belum ada weapon viewmodel** (peluru hanya tracer + HUD).
5. Recovery pusing pasif (`decayPerSecond = 1.2`) **belum ada di GDD** — ini proposals,
   silakan hapus/tuning di `DizzinessSystem`.
6. Belum ada audio ambient/musik, settings menu, maupun save system.

## Catatan teknis

- Player memakai **frictionless physics material** saat runtime. Tanpa itu, friction default
  Unity (0.6) membatalkan seluruh kecepatan horizontal tiap frame.
- Saat gravitasi berganti, motor menghapus komponen velocity Along "lantai lama" dan
  mendorong badan `surfaceRelease` (6cm) dari permukaan lama — tanpa ini capsule yang
  masih vertikal menggosok lantai lama dan solver melempar player ke atas.
- `Physics.gravity` bersifat global → semua `Rigidbody` di scene ikut terpengaruh.
  Target sengaja tidak memakai Rigidbody agar tetap diam.
- `PlayerLook` hanya memproses mouse look saat `Cursor.lockState == Locked`, supaya
  kursor yang sengaja dilepas (ESC) tidak bikin kamera liar.

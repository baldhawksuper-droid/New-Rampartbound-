# Rampartbound: Merge Defense — Panduan Drop-In Asset Audio & Musik

Dokumen ini adalah panduan resmi divisi audio bagi pengembang, komposer musik, dan sound designer untuk mengganti audio sintetis prosedural bawaan dengan file audio orisinal (`.wav`, `.mp3`, `.ogg`) tanpa perlu mengedit kode C#.

---

## 1. Arsitektur Hybrid Audio System

Game `Rampartbound: Merge Defense` menggunakan arsitektur **Hybrid Audio Engine**:
1. **Mode Prosedural (Default Fallback):** Jika file audio asli belum dimasukkan, game secara otomatis mensintesis gelombang frekuensi (nada pedang, hantaman gada, gempa bumi, sihir meteor, raungan naga) secara langsung di memori perangkat. Game tidak akan pernah hening atau bisu!
2. **Mode Drop-In Real Audio (Prioritas Utama):** Begitu file audio nyata berformat `.wav` atau `.mp3` diletakkan di dalam folder `Assets/Resources/Audio/`, game secara dinamis mendeteksi dan memutar file audio asli tersebut secara instan.

---

## 2. Struktur Folder Drop-In Audio

Letakkan file audio Anda pada direktori berikut:

```
Assets/
└── Resources/
    └── Audio/
        ├── Music/              <- Letakkan BGM musik latar di sini (.mp3 / .ogg)
        │   ├── BGM_Citadel_Lobby.mp3
        │   └── BGM_Battle_Theme.mp3
        │
        └── SFX/                <- Letakkan efek suara jurus & aksi di sini (.wav / .mp3)
            ├── Slash.wav
            ├── DualSlash.wav
            ├── BladeTempest.wav
            ├── ShieldSlam.wav
            ├── Earthshaker.wav
            ├── MagicSpark.wav
            ├── MysticBurst.wav
            ├── MeteorImpact.wav
            ├── ArrowShot.wav
            ├── PiercingGale.wav
            ├── TornadoImpact.wav
            ├── MergeSuccess.wav
            ├── SummonHero.wav
            ├── CastleHit.wav
            ├── VictoryFanfare.wav
            ├── DefeatChime.wav
            ├── ButtonClick.wav
            ├── GachaPull.wav
            ├── GachaLegendary.wav
            └── BossRoar.wav
```

---

## 3. Daftar Nama File Resmi (Audio Manifest)

| Kategori | Nama File Drop-In | Format Disarankan | Penggunaan di Game |
| :--- | :--- | :--- | :--- |
| **BGM** | `BGM_Citadel_Lobby` | `.mp3` / `.ogg` (Loop) | Musik megah istana Citadel Lobby |
| **BGM** | `BGM_Battle_Theme` | `.mp3` / `.ogg` (Loop) | Musik perang arena pertempuran |
| **Cedric (DPS)** | `Slash` | `.wav` (0.2s - 0.4s) | Tebasan pedang biasa Tier 1 |
| **Cedric (DPS)** | `DualSlash` | `.wav` (0.4s - 0.6s) | Tebasan ganda cepat Tier 2 |
| **Cedric (DPS)** | `BladeTempest` | `.wav` (0.8s - 1.2s) | Badai tebasan pedang Tier 3 |
| **Rose (Tank)** | `ShieldSlam` | `.wav` (0.3s - 0.5s) | Hantaman perisai Tier 1 & 2 |
| **Rose (Tank)** | `Earthshaker` | `.wav` (0.8s - 1.5s) | Gempa bumi jurus pamungkas Tier 3 |
| **Rabbit (Mage)** | `MagicSpark` | `.wav` (0.3s - 0.5s) | Percikan sihir kristal Tier 1 |
| **Rabbit (Mage)** | `MysticBurst` | `.wav` (0.5s - 0.8s) | Ledakan sihir area Tier 2 |
| **Rabbit (Mage)** | `MeteorImpact` | `.wav` (1.0s - 1.8s) | Hantaman meteor api Tier 3 |
| **Wind (Ranger)** | `ArrowShot` | `.wav` (0.2s - 0.4s) | Lesatan anak panah Tier 1 |
| **Wind (Ranger)** | `PiercingGale` | `.wav` (0.4s - 0.7s) | Tembakan panah angin tembus Tier 2 |
| **Wind (Ranger)** | `TornadoImpact` | `.wav` (0.9s - 1.5s) | Tornado angin hisap Tier 3 |
| **Sistem & UI** | `ButtonClick` | `.wav` (0.05s - 0.1s) | Sentuhan tombol UI & navigasi |
| **Sistem & UI** | `MergeSuccess` | `.wav` (0.3s - 0.6s) | Penggabungan pahlawan di petak |
| **Sistem & UI** | `SummonHero` | `.wav` (0.4s - 0.7s) | Pemanggilan pahlawan baru |
| **Sistem & UI** | `CastleHit` | `.wav` (0.4s - 0.8s) | Benteng Citadel terkena monster |
| **Sistem & UI** | `VictoryFanfare` | `.wav` / `.mp3` (2s - 4s) | Terompet kemenangan stage |
| **Sistem & UI** | `DefeatChime` | `.wav` / `.mp3` (1.5s - 3s) | Lonceng kekalahan |
| **Sistem & UI** | `GachaPull` | `.wav` (0.8s - 1.2s) | Animasi tarik gacha summon |
| **Sistem & UI** | `GachaLegendary` | `.wav` (1.5s - 2.5s) | Efek bintang emas pahlawan legendaris |
| **Boss AI** | `BossRoar` | `.wav` (1.0s - 2.0s) | Raungan menggelegar fase Enrage bos |

---

## 4. Cara Menumpuk File (Drop-In Replacement)

1. Siapkan file audio yang ingin Anda masukkan dengan penamaan tepat sesuai tabel di atas.
2. Buka folder proyek di Windows Explorer: `Assets/Resources/Audio/SFX/` atau `Assets/Resources/Audio/Music/`.
3. Salin (*paste*) file audio tersebut ke folder tujuan.
4. Kembali ke Unity Editor: Unity akan secara otomatis mengimpor aset tersebut.
5. Jalankan scene (*Play Mode*): Game langsung memutar file audio asli Anda tanpa perlu menekan tombol apa pun di Inspector!

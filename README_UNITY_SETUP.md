# Rampartbound: Unity Setup Guide

Panduan membuka dan menyiapkan project Unity. Rujuk [`docs/PROJECT_STATUS.md`](docs/PROJECT_STATUS.md) untuk status proyek dan keputusan desain yang belum disepakati.

Folder project tersedia sebagai project Unity; keberadaan file bukan bukti bahwa compile, build, atau gameplay sudah tervalidasi.

---

## 📁 Struktur Direktori Proyek

```text
<project-root>\
├── Assets\
│   ├── Animations\           (Animasi squash-and-stretch & procedural visual juice)
│   ├── Art\Sprites\          (Pixel art Cedric, Rose, Rabbit, Wind Ranger, Monsters, Grid, UI)
│   ├── Audio\                (Synthesized BGM Lobby, BGM Battle, SFX procedural)
│   ├── Prefabs\              (Prefab Grid, Proyektil, Partikel VFX)
│   ├── Scenes\
│   │   └── RampartboundMainScene.unity  (Scene arena utama siap Play)
│   ├── ScriptableObjects\
│   │   ├── Artifacts\        (12 SO Artefak: Guardian's Vow, Berserker's Fury, Sorcerer's Surge)
│   │   ├── Levels\           (SO_Level1_Config.asset, SO_EndlessTrial_Config.asset)
│   │   ├── Monsters\         (SO_Monster_Boss, SO_Monster_Brute, SO_Monster_Creep)
│   │   ├── Talents\          (SO_Talent_CitadelAegis, SO_Talent_EconomicProsperity, dll)
│   │   ├── Units\            (SO_Cedric, SO_RoseTanker, SO_RabbitMage, SO_WindRanger)
│   │   └── Waves\            (SO_Wave_1, SO_Wave_2, SO_Wave_3)
│   └── Scripts\              (jumlah script dan status kompilasi perlu diperbarui dari hasil verifikasi terbaru)
│       ├── Combat\           (CedricUnit, RoseTankerUnit, RabbitMageUnit, WindRangerUnit, EliteMonsterAffix, AutoMergeAssistant, BossMonsterController, dll)
│       ├── Core\             (GameManager, GridManager, TileCell, MobileCamera, Economy, Audio, DevicePerformance, Weather)
│       ├── Metagame\         (CitadelLobbyManager, ArtifactManager, StageManager, CitadelSummonManager, QuestManager, EndlessTrialManager, RelicForgeManager, CitadelTalentManager, GuildManager, BattlePassManager, CitadelStoreManager)
│       ├── Save\             (SaveManager, LocalSaveService [SHA-256 Checksum], CloudSaveSyncService, CloudSaveAdapter)
│       ├── UI\               (UIBattleView [Speed 3x & Auto-Merge], DragDropHandler, UIArtifactView, UISummonHallView, UIHeroUpgradeView, UIStageMapView, UIEndlessTrialView, UIRelicForgeView, UICitadelTalentView, UIAudioSettingsView, UIGuildView, UIBattlePassView, UIStoreView)
│       └── Editor\           (RampartboundSceneSetup.cs, RampartboundAssetValidator.cs, RampartboundBuildPipeline.cs)
├── Packages\
│   └── manifest.json         (Dependensi Unity: 2D Animation, 2D Sprite, UGUI, Particle, Audio)
├── ProjectSettings\          (Terkalibrasi Unity 6000.6.5f1, Bundle ID: com.rampartbound.mergedefense)
└── docs\                     (MASTER_GAME_DESIGN_DOCUMENT.md, TEAM_PROJECT_WORKFLOW.md)
```

---

## 🎨 Panduan Menumpuk File Aset (Drop-In Asset Replacement Pipeline)

Fitur arsitektur ini dirancang khusus agar komandan dapat **langsung menumpuk/mengganti file desain grafis baru** tanpa perlu menata ulang ScriptableObjects, Prefabs, atau hierarki Unity satu per satu!

### 1. Aturan Emas Menumpuk File (The Golden Rule)
> **⚠️ PENTING:** 
> Saat menimpa file gambar desain baru ke dalam folder `Assets/Art/Sprites/`:
> 1. Gunakan **nama file PNG yang sama persis** dengan yang sudah terdaftar di tabel manifest.
> 2. **JANGAN PERNAH menghapus file pendamping berakhiran `.meta`** (misalnya `Cedric_T1.png.meta`).
> 
> *Mengapa?* Unity menyimpan nomor identitas rahasia (**GUID**) di dalam file `.meta`. Selama file `.meta` tidak terhapus, Unity akan langsung memperbarui tampilan grafis di arena permainan secara instan dengan referensi data ScriptableObject tetap terhubung selama GUID `.meta` yang lama dipertahankan; verifikasi referensi di Unity setelah mengganti aset!

---

### 2. Standar Spesifikasi Ekspor Desain Grafis

| Parameter | Spesifikasi Resmi | Catatan Desainer |
| :--- | :--- | :--- |
| **Format File** | PNG (32-bit RGBA) | Wajib transparansi latar belakang (*Alpha Channel*). |
| **Dimensi Token Hero** | `128 x 128 px` atau `256 x 256 px` | Rasio kotak 1:1, disarankan kanvas simetris. |
| **Dimensi Monster** | `128 x 128 px` (Creep/Brute) / `256 x 256 px` (Boss) | Skala monster boss diperbesar via sistem runtime. |
| **Dimensi Grid Tile** | `128 x 128 px` | Petak ubin arena 1:1 kotak. |
| **Dimensi Barikade** | `512 x 128 px` | Pagar benteng horizontal (rasio 4:1). |
| **Pixels Per Unit (PPU)** | **100 PPU** | Standar engine Unity agar skala di arena pas 1 tile unit. |
| **Sprite Pivot** | **Center (0.5, 0.5)** | Menjamin karakter berdiri tepat di tengah petak tanpa melayang. |

---

### 3. Tabel Manifest Berkas Aset Visual

Komandan cukup menimpa file-file di bawah ini saat aset desain final telah siap:

#### A. Kuartet Pahlawan (Heroes) — Direktori: `Assets/Art/Sprites/Heroes/`
| Nama File | Pahlawan | Tier / Fase | Deskripsi Visual |
| :--- | :--- | :--- | :--- |
| `Cedric_T1.png` | Cedric | Tier 1 | Hood merah, zirah ringan, arming sword satu tangan. |
| `Cedric_T2.png` | Cedric | Tier 2 | Plate dada baja, dual swords kembar. |
| `Cedric_T3.png` | Cedric | Tier 3 | Zirah ksatria agung bercahaya, pedang badai aura merah. |
| `Rose_T1.png` | Rose Tanker | Tier 1 | Rambut kuncir pink, warhammer kecil & tameng kayu. |
| `Rose_T2.png` | Rose Tanker | Tier 2 | Hammer godam besar, perisai lambang mawar rose-gold. |
| `Rose_T3.png` | Rose Tanker | Tier 3 | Armor rose-gold megah, aegis duri berpendar pelindung. |
| `RabbitMage_T1.png` | Rabbit Mage | Tier 1 | Kelinci putih bertudung murid, staf kayu kecil. |
| `RabbitMage_T2.png` | Rabbit Mage | Tier 2 | Jubah penyihir cyan, topi runcing kristal mistis. |
| `RabbitMage_T3.png` | Rabbit Mage | Tier 3 | Jubah Archmage kosmik, staf bintang bercahaya aura. |
| `WindRanger_T1.png` | Wind Ranger | Tier 1 | Jubah hijau zamrud, busur komposit pemanah angin. |
| `WindRanger_T2.png` | Wind Ranger | Tier 2 | Quiver badai, panah berbulu elang penembus angin. |
| `WindRanger_T3.png` | Wind Ranger | Tier 3 | Jubah elang badai legendaris, busur tornado purba. |

#### B. Monster Serbuan (Monsters) — Direktori: `Assets/Art/Sprites/Monsters/`
| Nama File | Kategori | Deskripsi Visual |
| :--- | :--- | :--- |
| `Monster_Creep.png` | Creep (Goblin/Kobold) | Monster kecil lincah berlari cepat dalam gerombolan. |
| `Monster_Brute.png` | Brute (Orc/Troll) | Monster bertubuh tebal membawa perisai penahan hantaman. |
| `Monster_Boss.png` | Boss (Orc Berserker / Drake) | Monster raksasa pemecah benteng dengan efek gempa. |

#### C. Arena & Pertahanan (Grid & UI) — Direktori: `Assets/Art/Sprites/Grid/` & `UI/`
| Nama File | Folder | Deskripsi Visual |
| :--- | :--- | :--- |
| `Tile_Empty.png` | `Grid/` | Ubin petak papan arena saat kondisi kosong. |
| `Tile_Hover.png` | `Grid/` | Sorotan ubin saat disentuh / validasi penggabungan merge. |
| `Barricade_Fence.png` | `UI/` | Pagar barikade benteng Citadel di bawah arena. |
| `Frame_OrnateGold_9Slice.png` | `UI/` | Bingkai berornamen emas 9-slice untuk dialog modal & header utama. |
| `Frame_StoneBastion_9Slice.png` | `UI/` | Bingkai batu benteng 9-slice untuk 13 sub-panel metagame. |
| `Btn_Primary_Gold.png` | `UI/` | Tombol primer bertekstur emas fantasi untuk aksi utama. |
| `Btn_Primary_Gold_Pressed.png` | `UI/` | Variasi tombol primer saat ditekan (tactile feedback). |
| `Btn_Secondary_Stone.png` | `UI/` | Tombol sekunder bertekstur batu untuk aksi batal/kembali/surrender. |
| `Btn_Secondary_Stone_Pressed.png` | `UI/` | Variasi tombol sekunder saat ditekan. |
| `Btn_Close_X.png` | `UI/` | Tombol silang tutup (X) universal untuk seluruh sub-panel. |
| `Btn_Close_X_Pressed.png` | `UI/` | Variasi tombol silang saat ditekan. |
| `Header_Banner_Ribbon.png` | `UI/` | Pita banner pita emas/biru gelap untuk judul sub-panel. |
| `Icon_Resource_Pill.png` | `UI/` | Kapsul wadah ikon komoditas (Gold, Gems, Shards, Scrap). |
| `Rampartbound_KeyVisual_Banner.jpg` | `UI/` | Banner visual utama benteng Citadel di Title Screen. |

---

## 🛠️ Utilitas Otomatis di Unity Editor

Kami telah menyertakan dua utilitas cerdas 1-klik di menu atas Unity Editor:

### 1. `Rampartbound` ➡️ `Validate & Audit Assets (Drop-In Pipeline)`
Jendela diagnostik canggih untuk memverifikasi seluruh aset:
* **Status Checklist Real-Time:** Memeriksa apakah file ada, berformat Sprite 2D, PPU 100, dan terhubung ke ScriptableObject.
* **Tombol "1-Click Auto-Fix Sprite Importers":** Jika komandan menumpuk gambar baru dan Unity secara default membacanya sebagai "Texture biasa", klik tombol ini sekali untuk mengubah semua pengaturan impor menjadi Sprite 2D, PPU 100, dan transparan secara instan!
* **Tombol "Re-Bind ScriptableObjects":** Mengaitkan ulang seluruh data hero dan monster ke sprite fisiknya jika ada relasi yang terputus.

### 2. `Rampartbound` ➡️ `1-Click Setup Arena Scene`
Merakit ulang seluruh hierarki arena secara otomatis dari nol ke dalam scene `Assets/Scenes/RampartboundMainScene.unity`:
* Menyiapkan Kamera Mobile Portrait Responsif.
* Mengaitkan GridManager dengan sprite `Tile_Empty` & `Tile_Hover`.
* Menghubungkan WaveSpawner, HeroSummoner, Audio, VFX, BattlefieldWeatherManager (Cuaca), ArtifactManager (Set Synergies), AutoMergeAssistant (Asisten Fusi Otomatis), GuildManager (Aliansi & Titan Raid), BattlePassManager (Pass 30 Tiers), CitadelStoreManager (Toko IAP), CloudSaveSyncService, dan UI Canvas (Battle HUD dengan Speed 3x, Endless Trial, Relic Forge, Citadel Talents Tree, UI Artifacts, Guild View, Battle Pass View, Store View, System Settings).

### 3. `Rampartbound` ➡️ `Build`
Menu ekspor otomatis 1-klik untuk distribusi, staging, dan rilis Google Play Store:
* **`1-Click Build Windows Standalone (64-bit)`:** Mengompilasi binary PC desktop langsung ke `Builds/Windows/Rampartbound.exe`.
* **`1-Click Build Android Sideload APK (Testing)`:** Menargetkan output `Builds/Android/Rampartbound_debug.apk` menurut kode build pipeline. File output terbaru belum dibangun/dijalankan dalam pemeriksaan ini.
* **`1-Click Build Google Play App Bundle (.aab - Release)`:** Mengompilasi paket resmi Google Play App Bundle (.aab) dengan arsitektur IL2CPP 64-bit ARM64 + ARMv7, Target SDK 34+, dan Play Asset/Feature delivery.
* **`Google Play Release Readiness Audit`:** Memeriksa kesiapan paket, App ID, Target API 34+, dan 64-bit compliance sebelum proses submission ke Google Play Console.
* **`Configure Project Settings (Portrait & Mobile)`:** Otomatis mengunci orientasi layar ke Portrait, package ID `com.rampartbound.mergedefense`, dan konfigurasi grafis mobile.

### 4. 🤖 Otomatisasi Build APK via Git & GitHub Actions (CI/CD)
Proyek ini dilengkapi pipeline otomatis untuk memproduksi APK langsung melalui Git / GitHub Actions:
* **Workflow File:** `.github/workflows/build-android-apk.yml`
* **Cara Eksekusi via GitHub Web:**
  1. Buka repositori: https://github.com/baldhawksuper-droid/New-Rampartbound-
  2. Buka tab **Actions**.
  3. Pilih workflow **"Build Android APK & Release"**.
  4. Klik **Run workflow**, pilih tipe build (`apk` atau `aab`), lalu klik tombol hijau **Run workflow**.
  5. Setelah selesai, file APK siap di-download di bagian **Artifacts**!
* **Cara Eksekusi via Terminal / Git CLI:**
  ```bash
  # Memicu build cloud langsung dari terminal:
  gh workflow run build-android-apk.yml -f buildType=apk

  # Atau jalankan script batch otomatis di folder scripts:
  .\scripts\build_android_apk.bat
  ```

---

## 🚀 Panduan Membuka Proyek di Unity Hub

1. Buka aplikasi **Unity Hub**.
2. Klik tombol **Add** (atau *Add project from disk*).
3. Arahkan dan pilih folder:  
   👉 `<project-root>` (folder yang berisi `Assets`, `Packages`, dan `ProjectSettings`)
4. Gunakan versi Editor yang tercatat di `ProjectSettings/ProjectVersion.txt` (`6000.6.5f1`); kompatibilitas versi lain belum diverifikasi.
5. Klik nama proyek untuk masuk ke **Unity Editor**.
6. Buka scene: `Assets/Scenes/RampartboundMainScene.unity`.
7. Tekan tombol **Play (▶️)** di Unity Editor untuk langsung menikmati game!
8. Untuk mengekspor installer APK ponsel, Google Play App Bundle (.aab), atau file EXE Windows, cukup pilih menu **`Rampartbound` ➡️ `Build`**!

---

## Build Android

Pipeline menargetkan output `Builds/Android/Rampartbound_debug.apk`. Folder repositori juga berisi APK lama dengan nama/versi berbeda; saya belum membangun, memasang, atau menguji paket APK pada perangkat dalam Sprint 49. Jangan menganggap artefak lama sebagai build terkini atau rekomendasi rilis.

Untuk menghasilkan paket terbaru, gunakan menu `Rampartbound > Build > 1-Click Build Android Sideload APK (Testing)` di Unity Editor yang cocok dengan versi proyek.



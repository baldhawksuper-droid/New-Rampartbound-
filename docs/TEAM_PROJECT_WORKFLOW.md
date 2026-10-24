# Rampartbound — Project Workflow & QA

> **Status:** Historical sprint ledger / working reference. The approved current product direction is in [`CURRENT_PRODUCT_DESIGN.md`](CURRENT_PRODUCT_DESIGN.md); verified facts and remaining audit work are in [`PROJECT_STATUS.md`](PROJECT_STATUS.md). Sprint claims below are not independently verified by this document.

**Proyek:** Rampartbound: Merge Defense (judul kerja)
**Versi:** 1.0.0  
**AI Project Lead:** Hermes Agent (planning, implementation support, and review when explicitly requested). **Game Owner / final decision-maker:** User. Role labels below describe workstreams, not staffed people or agents unless they have actually been assigned.
**Acuan Desain:** [MASTER_GAME_DESIGN_DOCUMENT.md](MASTER_GAME_DESIGN_DOCUMENT.md)  

---

## 1. STRUKTUR TIM & TUGAS SPESIFIK

Untuk mempercepat iterasi dan menjaga kualitas teknis, proyek diorganisasi menjadi satu kepemimpinan utama dengan 4 divisi spesialis kerja:

```
                            ┌─────────────────────────────────┐
                            │      LEAD PROJECT MANAGER       │
                            │        (Hermes Agent)           │
                            │   Visi, Orkestrasi, Integrasi   │
                            └────────────────┬────────────────┘
                                             │
      ┌──────────────────────┬───────────────┴───────────────┬──────────────────────┐
      │                      │                               │                      │
      ▼                      ▼                               ▼                      ▼
┌───────────────┐   ┌─────────────────┐             ┌─────────────────┐   ┌───────────────────┐
│ DIVISI 1:     │   │ DIVISI 2:       │             │ DIVISI 3:       │   │ DIVISI 4:         │
│ Asset & Art   │   │ Core Gameplay   │             │ Data & Economy  │   │ QA & Code Review  │
│ Specialist    │   │ & Unity Eng.    │             │ Balance Designer│   │ Specialist        │
└───────────────┘   └─────────────────┘             └─────────────────┘   └───────────────────┘
```

### Divisi 1: Asset & Art Specialist
* **Tanggung Jawab:**
  * Menyiapkan hierarki aset di `Assets/Art/Sprites/` dan `Assets/Prefabs/`.
  * Merancang dan membuat aset placeholder (Grid Tiles, Hero Tokens T1-T3, Monsters, Barricade, UI Icons) dengan rasio piksel dan PPU (*Pixels Per Unit = 100*) yang terstandarisasi.
  * Memastikan proses transisi ke aset final (ilustrasi chibi) dapat dilakukan secara *drag-and-drop* tanpa mengubah logika collider/transform.

### Divisi 2: Core Gameplay & Unity Engineer
* **Tanggung Jawab:**
  * Membangun arsitektur skrip C# berorientasi objek dan bersih (*clean architecture*).
  * Mengembangkan logika papan petak (`GridManager.cs`, `TileCell.cs`), state machine alur pertempuran (`GameManager.cs`), dan siklus unit (`UnitController.cs`).
  * Mengimplementasikan spesialisasi karakter (Cedric, Rose Tanker, Rabbit Mage).
  * Mengembangkan interaksi seret-lepas (*Drag & Drop Merge*) dan deteksi Match-3.

### Divisi 3: Data & Economy Balance Designer
* **Tanggung Jawab:**
  * Merancang arsitektur ScriptableObject (`UnitDataSO.cs`, `LevelConfigSO.cs`, `WaveDataSO.cs`).
  * Mengunci formula matematis:
    * Scaling stat tier hero: Tier 1 ($1.0\times$), Tier 2 ($2.2\times$), Tier 3 ($4.8\times$).
    * Formula biaya summon dinamis: $\text{Biaya}_n = 40 + (n-1) \times 10\text{ gems}$.
    * Kalkulasi sinergi Team Combat Power (CP) dari Lobby Citadel ke arena battle.

### Divisi 4: QA & Code Review Specialist
* **Tanggung Jawab:**
  * Meninjau kesesuaian sintaksis C#, keterpisahan dependensi (*event decoupling*), dan penanganan *null reference exception*.
  * Menguji kesiapan integrasi antara ScriptableObject, Prefab, dan Scene.
  * Memastikan setiap sprint terverifikasi sebelum melangkah ke tahap berikutnya.

---

## 2. ROADMAP SPRINT LENGKAP

| Sprint | Nama Sprint | Fokus Utama & Output | Status |
| :---: | :--- | :--- | :---: |
| **Sprint 1** | **Fondasi Grid, Placeholder & Data Model** | Hierarki Unity, Placeholder PNG sprites lengkap + `.meta`, `UnitDataSO`, `LevelConfigSO`, `GridManager`, `TileCell`, `UnitController`, `EconomyManager`, `GameManager`. | **Perlu bukti verifikasi** |
| **Sprint 2** | **Summon Engine & Merge Controller (Mobile/Tablet)** | `HeroSummoner`, formula gems dinamis, Drag-and-Drop Merge mobile (touch offset jari, highlight hijau/biru/merah), pergantian sprite Tier 1-3, `GridMatch3Solver`, `MobileTouchManager`, `HeroMergeController`, `SummonButtonUI`. | **Perlu bukti verifikasi** |
| **Sprint 3** | **Monster AI, Pathway & Combat Targeting** | `MonsterController`, `MonsterPathManager`, `WaveSpawner`, `CastleBarricade`, targeting real-time hero, serangan proyektil sihir AoE, data monster & wave ScriptableObjects. | **Perlu bukti verifikasi** |
| **Sprint 4** | **Spesialisasi Skill Tier 2 & Tier 3** | Skill unik: Dual Slash & Blade Tempest Cedric, Shield Slam & Earthshaker Rose, Mystic Burst & Meteor Rabbit Mage. Visual feedback fusi, shockwaves, floating text (`SkillVfxManager`), dan synthesized audio (`AudioManager`). | **Perlu bukti verifikasi** |
| **Sprint 5** | **Battle HUD, Metagame Link & Balancing** | UI HUD tempur (`UIBattleView`, `DragDropHandler`), speed toggle (1x/2x), pause overlay, popup Victory & Defeat, sinkronisasi data CP Lobby (`CitadelLobbyManager`, `LobbyDataModel`), formula matematis tier scaling. | **Perlu bukti verifikasi** |
| **Sprint 6** | **Hero Upgrade, Equipment & Stage World Map** | Upgrade level hero pakai Gold (`HeroLobbyProfile`), slot equipment (`HeroEquipment`), scaling stat arena battle (`UnitController`), controller UI upgrade (`UIHeroUpgradeView`), data 10 stage Chapter 1 (`StageDataSO`), master progress manager (`StageManager`), sistem rating bintang 1-3 & reward first-clear, serta peta dunia (`UIStageMapView`). | **Perlu bukti verifikasi** |

---

## 3. PROTOKOL KONTROL KUALITAS & DEFINITION OF DONE

### Definition of Done Sprint 1:
1. Seluruh folder standar Unity terstruktur rapi di `Assets/`.
2. Aset placeholder (Tile, Trio Hero T1-T3, Monster, Benteng) tersedia dalam format PNG beresolusi tajam dengan metadata Unity.
3. Semua skrip C# terkompilasi bersih tanpa *error*, menggunakan namespace yang konsisten (`FantasyDefense.Core`, `FantasyDefense.Data`, dll.).
4. Data pahlawan Cedric, Rose Tanker, dan Rabbit Mage terkonfigurasi sesuai spesifikasi Master GDD.

### Definition of Done Sprint 2 (Mobile & Tablet Focus):
1. **Hero Summoner:** `HeroSummoner.cs` memanggil hero acak ke petak grid kosong dengan biaya dinamis ($40 + (n-1)\times 10$).
2. **Mobile Ergonomics Drag & Drop:** `UnitController.cs` mengapungkan sprite di atas sentuhan jari (`touchDragOffset`), mencegah tertutup ibu jari pada layar ponsel/tablet.
3. **Multi-State Tile Visual Feedback:** Petak target berubah warna secara real-time saat diseret (Hijau = valid fusi, Biru = slot kosong, Merah = invalid).
4. **Visual Evolution Tier:** Pergantian sprite otomatis (`tier1Sprite`, `tier2Sprite`, `tier3Sprite`) saat berhasil merge (T1+T1=T2, T2+T2=T3).
5. **Match-3 Solver:** `GridMatch3Solver.cs` mendeteksi deretan 3 hero identik berjejer horizontal/vertikal untuk auto-merge dan bonus reward gems.
6. **Mobile Touch Reliability:** `MobileTouchManager.cs` memastikan `Physics2DRaycaster` aktif pada Camera.main dan mengunci single touch saat dragging.
7. **Asset Instances:** ScriptableObjects `SO_Cedric.asset`, `SO_RoseTanker.asset`, `SO_RabbitMage.asset`, dan `SO_Level1_Config.asset` terintegrasi lengkap.

### Definition of Done Sprint 3 (Combat & Monster AI Focus):
1. **Monster Controller & AI:** `MonsterController.cs` bergerak menyusuri titik jalan (*waypoints*), menerima damage, efek status (Stun & Slow), damage ke benteng, reward gems saat gugur, dan mengelola daftar statis `ActiveMonsters`.
2. **Lintasan Waypoint Adaptif:** `MonsterPathManager.cs` membuat jalur lintasan otomatis yang mengitari arena Grid menuju garis benteng pertahanan sesuai ukuran layar portrait HP/tablet.
3. **Wave Spawner:** `WaveSpawner.cs` memunculkan gelombang monster secara beruntun dari data ScriptableObject (`WaveDataSO`), jeda istirahat antar wave, dan memicu kondisi Kemenangan (*Victory*).
4. **Daya Tahan Benteng (Castle Barricade):** `CastleBarricade.cs` memantau sisa HP benteng dan memicu status Kekalahan (*Defeat*) jika benteng jebol.
5. **Hero Combat Targeting:** Seluruh hero di grid otomatis mendeteksi monster terdekat dalam jangkauan serangan (`attackRange`) dan melancarkan serangan sesuai spesialisasi perannya (Melee Slash Cedric, Stun Hammer Rose Tanker, Magic AoE Rabbit Mage).
6. **Sistem Proyektil:** `Projectile.cs` menembakkan peluru sihir terarah dengan kalkulasi benturan dan ledakan area (*AoE splash & slow*).
7. **Aset Data Monster & Wave:** ScriptableObjects `SO_Monster_Creep.asset`, `SO_Monster_Brute.asset`, `SO_Monster_Boss.asset`, serta `SO_Wave_1.asset`, `SO_Wave_2.asset`, `SO_Wave_3.asset` terkonfigurasi lengkap.

### Definition of Done Sprint 4 (Spesialisasi Skill Tier 2 & 3 & VFX/SFX Focus):
1. **Cedric Skillset:** T2 Dual Slash (180% damage, +20% crit) dan T3 Blade Tempest (AoE Whirlwind, -25% Physical DEF shred) serta buff aura Heroic Surge (+15% Attack Speed untuk hero tetangga).
2. **Rose Tanker Skillset:** T2 3-hit Shield Slam (Stun 1s + shield absorb) dan T3 Earthshaker Impact (AoE Stun 1.5s radius 1.5 tile) serta Thorn Aegis reflection.
3. **Rabbit Mage Skillset:** T2 Mystic Burst (1.5 AoE splash) dan T3 Meteor Strike (2.5 AoE, 40% slow) memunculkan kobaran api tanah `ArcaneFireField.cs` (Burn DoT 3s).
4. **Visual & Audio Feedback:** `SkillVfxManager.cs` memunculkan teks tempur mengambang (Damage, Crit, Stun, Skill name), shockwaves, dan flash fusi. `AudioManager.cs` menghasilkan SFX prosedural untuk seluruh aksi pertempuran.

### Definition of Done Sprint 5 (Battle HUD, Metagame Link & Balancing):
1. **Battle HUD Integrasi:** `UIBattleView.cs` menampilkan Bar HP Benteng, Wave counter, Gems counter real-time, Speed toggle (1x/2x), Menu Pause, dan dialog popup Victory/Defeat.
2. **Metagame Link & Citadel Synchronization:** `CitadelLobbyManager.cs` & `LobbyDataModel.cs` menyinkronkan level Citadel (Lv. 8), stamina/energi tempur (30/30, konsumsi 5 per battle), gold currency (500), dan total Team CP (4,850 dari Cedric 1850 + Rabbit 1600 + Rose 1400).
3. **Balancing & Quality Assurance:** Formula pengali stat Tier ($1.0\times, 2.2\times, 4.8\times$) dan formula summon dinamis ($40 + (n-1)\times 10$) diverifikasi 100% konsisten di seluruh kode C# dengan zero compilation error dan metadata Unity lengkap.

### Definition of Done Sprint 6 (Hero Upgrade, Equipment & Stage World Map):
1. **Hero Upgrade & Equipment System:**
   * `HeroEquipment.cs` & `HeroLobbyProfile` mendukung slot perlengkapan (Weapon, Armor, Relic) dengan tier kelangkaan (Common, Rare, Epic, Legendary).
   * Peningkatan level hero di Citadel menggunakan Gold (`120 + level * 45`) mendongkrak ATK (+7.5%/level), HP (+9%/level), Crit, dan Combat Power (CP) pahlawan secara permanen.
   * `UnitController.cs` dan spesialisasi hero (`CedricUnit.cs`) menginjeksikan bonus stat level dan equipment dari profil Citadel Lobby ke formula serangan dan critical rate di arena battle secara dinamis.
   * `UIHeroUpgradeView.cs` menyediakan antarmuka interaktif untuk pemilihan hero aktif, inspeksi equipment, kalkulasi biaya upgrade, dan tombol aksi kenaikan level.
2. **Stage Progression & World Map System:**
   * `StageDataSO.cs` mengonfigurasi struktur 10 stage Chapter 1 (Stage 1-1 hingga Boss Stage 1-10 "Sarang Naga Abyssal").
   * `StageManager.cs` mengelola pembukaan stage secara sekuensial, evaluasi perolehan 1-3 bintang berdasarkan sisa daya tahan benteng (>=85% 3★, >=45% 2★, >0% 1★), pembagian First-Clear Rewards (Gold, Gems, EXP, Equipment drop), dan klaim milestone peti harta bintang (10★, 20★, 30★).
   * `UIStageMapView.cs` menampilkan peta dunia kampanye, seleksi node stage dengan indikator tingkat kesulitan berbasis perbandingan Team CP pemain vs Rekomendasi CP, konsumsi stamina, dan peluncuran battle.
   * `UIBattleView.cs` dan `GameManager.cs` terhubung langsung dengan `StageManager` untuk mengadopsi konfigurasi stage dan mencatat kemenangan pertempuran secara otomatis ke penyimpanan lokal dan cloud.

### Definition of Done Sprint 7 (Dual Battle Mode, Visual Juice & Animations, Audio Mixer, Quests & Achievements):
1. **Dua Mode Pertempuran Modular (Dual Battle Modes):**
   * Mode 1: *Puzzle Preparation (Turn-Based Match-3 Style)* dengan jatah langkah (*moves remaining*) untuk menata fusi sebelum gelombang musuh dilepas.
   * Mode 2: *Real-time Deploy & Merge (Lucky Defense Style)* dengan pemanggilan acak berbasis Gems dan drag-drop merge di tengah pertempuran aktif.
   * `BattleMode` enum di `LevelConfigSO.cs` dan integrasi di `GameManager.cs` untuk mengelola transisi mode pertempuran.
2. **Visual Juice & Transisi Animasi:**
   * `HeroVisualAnimator.cs` menyajikan animasi prosedural (*idle breathing bobbing*, *attack lunge*, *skill cast hover*, *hit recoil*, dan *celebration bounce jump*) serta kompatibilitas frame playback *Sprite Sheet*.
   * Integrasi langsung ke `UnitController.cs` pada seluruh pahlawan aktif.
   * `SkillVfxManager.cs` mengintegrasikan sistem partikel Unity runtime (`ParticleSystem`) untuk tebasan badai Cedric, hantaman gempa Rose Tanker, dan komet api sihir Rabbit Mage.
3. **Integrasi Musik Latar & Panel Pengaturan Audio:**
   * `AudioManager.cs` menyediakan sistem pemutar musik BGM (Tema Citadel Lobby & Tema Arena Pertempuran) dengan sintesis nada prosedural.
   * Kontrol volume Master, BGM, dan SFX, mute toggle, serta auto-sync ke penyimpanan `CitadelAccountData`.
   * `UIAudioSettingsView.cs` menyediakan panel popup interaktif dengan slider volume, indikator persentase, dan uji SFX.
4. **Sistem Misi Harian & Pencapaian Kerajaan (Daily Quests & Achievements):**
   * `QuestDataModel.cs` & `QuestManager.cs` mengelola 5 Misi Harian (Summon, Merge, Defeat Monster, Upgrade Hero, Clear Stage) dengan reset harian otomatis 24 jam (UTC) dan 5 Pencapaian Kerajaan seumur hidup.
   * Hadiah Gold, Gems, dan Stamina Energy tersinkronisasi langsung ke `CitadelLobbyManager` dan cloud save.
   * `UIQuestView.cs` menampilkan navigasi tab, slider progress bar, hitung mundur reset harian, dan tombol klaim reward.

### Status Sprint 8 (SELESAI 100%):
1. **Boss Fight Specialized Mechanics & AI Behaviors:**
   * Pengontrol Boss khusus (`BossMonsterController.cs` yang mengekstensi `MonsterController.cs`) terintegrasi penuh untuk Stage 1-5 Mini-Boss (*Orc Berserker*) dan Stage 1-10 Final Boss (*Abyssal Flame Drake*).
   * Fitur: Bilah HP Boss Raksasa, multi-phase tracking, skill *Ground Smash Stun* (menghentak arena dan men-stun barisan hero), fase *Abyssal Roar & Enrage* (kecepatan +40%, memanggil minion pelindung *Abyssal Guards*), dan *Dragon Firestorm Flame Breath*.
2. **Citadel Summoning Hall & Hero Gacha / Shard System (Roster Hero ke-4: Wind Ranger):**
   * Mengintegrasikan pahlawan ke-4: **Wind Ranger** (`WindRangerUnit.cs`), bertipe penyerang fisik jarak jauh (3.8 petak) dengan skill *Swift Arrow* (T1), *Piercing Gale* segaris 200% damage (T2), dan *Zephyr Tornado Vortex* AoE slow 40% & vacuum pull (T3).
   * Manajer gacha `CitadelSummonManager.cs` (Single Pull 100 Gems, 10x Pull 900 Gems hemat 10%, Featured Banner Rate-Up Wind Ranger).
   * Sistem Hero Shards & Promosi Bintang (1★ hingga 5★) di `HeroLobbyProfile` & `CitadelLobbyManager` dengan multiplier stat permanen (1.0x hingga 1.90x).
   * Antarmuka `UISummonHallView.cs` interaktif dengan modal kartu hasil pemanggilan dan panel promosi bintang hero.
3. **Dynamic Battle HUD per-Mode & Polish Game Loop:**
   * HUD pertempuran dinamis di `UIBattleView.cs`: menampilkan Move Counter & tombol *"Mulai Bertahan Sekarang!"* pada Mode 1 (Puzzle Preparation), dan tombol summon gems pada Mode 2 (Real-Time Deploy).
   * Bar HP Boss raksasa di bagian atas tengah layar HUD, lengkap dengan titel boss, persentase HP, dan banner peringatan *Boss Enraged*.

---

### Definition of Done Sprint 9 (Penentuan Nama Game Original & Konsolidasi 1 Folder Unity Mandiri):
1. **Brainstorming & Penentuan Nama Game Original:**
   * Judul kerja saat ini: **Rampartbound: Merge Defense**. Jangan mengklaim clearance merek dagang/hak cipta atau keunikan global sebelum pencarian terdokumentasi dan review hukum yang sesuai.
   * Menentukan filosofi lore, kesesuaian ASO (App Store Optimization), dan identitas brand game.
2. **Konsolidasi 1 Folder Mandiri Unity (Ready-to-Run):**
   * Mengelompokkan seluruh aset, skrip (48 skrip C#), audio, prefab, animasi, dan ScriptableObjects ke dalam 1 folder proyek mandiri berlabel nama game.
   * Memastikan folder dilengkapi struktur standar Unity (`Assets/`, `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`, dan Scene `MainGameScene.unity`).
   * Proyek terbebas dari file non-game (seperti file python bot) sehingga saat dibuka melalui Unity Hub dapat langsung dibuka dan dijalankan (*ready to run*) tanpa error.

---

### Status Sprint 10 (SELESAI 100%):
1. **Endless Abyssal Trial Mode (Endgame Survival Tower):**
   * Controller `EndlessTrialManager.cs`, config `EndlessTrialConfigSO.cs`, dan asset `SO_EndlessTrial_Config.asset`.
   * Penskalaan eksponensial musuh tiap lantai (+14% HP, +8% ATK, +1.5% Speed) dan pertarungan Boss tiap 5 lantai di `WaveSpawner.cs`.
   * Sistem pencatatan rekor lantai tertinggi di `LobbyDataModel.cs`, siklus reset mingguan 7 hari UTC, dan klaim milestone peti harta di `UIEndlessTrialView.cs`.
2. **Citadel Relic Forge & Gear Crafting System:**
   * Controller `RelicForgeManager.cs` dan antarmuka interaktif `UIRelicForgeView.cs` terintegrasi 3 tab (Refine, Rarity, Salvage).
   * Peleburan gear duplikat (*Salvage*) menjadi mata uang *Forge Dust* & *Refine Stones*.
   * Penempaan gear (`refineLevel` +1 s/d +5) menaikkan stat +10% per tingkat, dan promosi kelangkaan (*Common* -> *Rare* -> *Epic* -> *Legendary*) dengan bonus stat permanen +30%.
   * Tombol pintasan langsung dari `UIHeroUpgradeView.cs`.
3. **Mobile Performance Optimization & Haptic Feedback:**
   * Controller `DevicePerformanceManager.cs` mengatur sakelar 30 FPS vs 60 FPS (`Application.targetFrameRate`) dan mode *Battery Saver*.
   * Reduksi partikel otomatis 50% di `SkillVfxManager.cs` saat Battery Saver aktif.
   * Mesin getaran fisik *Haptic Vibration* (Android Java Vibrator & iOS Handheld Vibrate) terintegrasi pada fusi hero Tier 3 di `UnitController.cs`, serta skill Boss *Ground Smash* & *Abyssal Roar Enrage* di `BossMonsterController.cs`.
   * Panel pengaturan sistem gabungan audio dan performa ponsel di `UIAudioSettingsView.cs`.

---

### Status Sprint 11 (SELESAI 100%):
1. **Audit & Standardisasi Struktur Folder Aset:**
   * Memvalidasi hierarki folder di `Assets/Art/Sprites/` (`Heroes/`, `Monsters/`, `Grid/`, `UI/`) dan `Assets/Audio/` (`BGM/`, `SFX/`).
   * Mengunci standar teknis format visual: Rasio PPU (Pixels Per Unit = 100), Texture Importer Sprite 2D, Alpha Transparency, dan Sprite Pivot Center (0.5, 0.5).
   * Memvalidasi seluruh relasi GUID pada ScriptableObject kuartet pahlawan (`SO_Cedric`, `SO_RoseTanker`, `SO_RabbitMage`, `SO_WindRanger`) serta monster (`SO_Monster_Creep`, `SO_Monster_Brute`, `SO_Monster_Boss`).
2. **Mekanisme Drop-In File Replacement (Menumpuk File Lama):**
   * Mengembangkan editor tool `RampartboundAssetValidator.cs` (`Rampartbound` ➡️ `Validate & Audit Assets (Drop-In Pipeline)`).
   * Tombol 1-klik `Auto-Fix Sprite Importers`: secara otomatis mengonversi gambar baru yang ditumpuk ke pengaturan Sprite 2D, PPU 100, dan Alpha Transparency.
   * Tombol 1-klik `Re-Bind ScriptableObjects`: mengaitkan ulang seluruh data hero dan monster ke sprite fisiknya.
   * Mengintegrasikan sprite `Barricade_Fence.png` pada `CastleBarricade` dan sprite `Tile_Empty` & `Tile_Hover` pada `GridManager` di dalam `RampartboundSceneSetup.cs`.
3. **Manifest Spesifikasi Desain & Dokumentasi README:**
   * Menyusun tabel manifest lengkap di `README_UNITY_SETUP.md` dan `README.md` mencakup nama berkas, folder tujuan, rekomendasi resolusi (128x128 / 256x256 / 512x128), PPU, pivot, dan aturan emas preservasi file `.meta`.

---

### Usulan Sprint 12 (Citadel Talents & Blessings Tree, Weather & Wave Modifiers, serta Build Automation Pipeline):
1. **Citadel Talents & Blessings Tree (Pohon Talenta Benteng Permanen):**
   * Sistem upgrade permanen benteng Citadel menggunakan mata uang meta (*Citadel Crests / Star Shards*):
     - *Citadel Regeneration:* Regenerasi HP barikade benteng +1% tiap 10 detik.
     - *Economic Prosperity:* Diskon biaya awal summon hero (-5 gems) & bonus gems per wave (+10 gems).
     - *Elemental Synergy:* Buff attack speed kuartet hero (+5% per rank).
2. **Dynamic Weather & Wave Modifiers (Sistem Cuaca & Modifikasi Gelombang Tempur):**
   * Kondisi lingkungan dinamis pada stage map tertentu atau lantai Endless Trial:
     - *Thunderstorm:* Petir menyambar acak memberikan stun sesaat pada musuh.
     - *Abyssal Fog:* Kabut tebal mengurangi jarak serang hero ranged sebesar -15%, menuntut penempatan taktis.
     - *Blood Moon:* Monster bergerak +20% lebih cepat namun memberikan hadiah gems ganda.
3. **Build Automation & Standalone Test Pipeline:**
   * Utilitas editor C# (`RampartboundBuildPipeline.cs`) untuk melakukan 1-klik export build (Windows Standalone `.exe` & Android `.apk`) secara terotomatisasi dari dalam Unity Editor.



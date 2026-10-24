# LEGACY GDD — Core Gameplay Mechanics

> **Status:** Historical reference; current product direction is in [`CURRENT_PRODUCT_DESIGN.md`](CURRENT_PRODUCT_DESIGN.md). Verify all details against current code and owner decisions before implementation.

# GAME DESIGN DOCUMENT (GDD) & ARCHITECTURE
**Project:** Fantasy Defense RPG  
**Module:** Core Battle Gameplay, Parameters, Trio Heroes & Unity C# Architecture  
**Document Ref:** `docs/GDD_CORE_GAMEPLAY_MECHANICS.md`  
**Version:** 1.0.0  
**Status:** Approved Specification  

---

## 1. Ringkasan Eksekutif & Konsep Utama

Dokumen ini merupakan spesifikasi komprehensif untuk sistem pertempuran (*core gameplay mechanics*), parameter level & ekonomi in-game, trio karakter pembuka, serta arsitektur teknis modular menggunakan Unity C#.

Sistem dirancang mendukung dua mode permainan utama yang adiktif:
1. **Mode 1: Puzzle Preparation & Wave Defense (Turn-based / SupaMerge Style)**
2. **Mode 2: Real-time Deploy & Active Defense (Real-time / Lucky Defense & Rush Royale Style)**

---

## 2. Parameter Gameplay & Kurva Belajar (Learning Curve)

### 2.1 Mode 1: Puzzle Preparation & Wave Defense

Pemain mengatur unit di grid sebelum gelombang musuh menyerang. Setiap pergeseran (*match/swap*) mengonsumsi jatah langkah (*moves*).

| Fase / Tingkat Level | Ukuran Grid | Jatah Langkah (*Moves*) | Jumlah Wave | Karakteristik Musuh & Tantangan |
| :--- | :---: | :---: | :---: | :--- |
| **Level 1 – 3 (Pengenalan)** | 5x5 | 12 – 15 langkah | 3 Wave | Kecepatan musuh lambat, tanpa bos, variasi monster dasar (Melee Creep). Membiasakan pemain pada mekanik match-3 dan summon T1. |
| **Level 4 – 9 (Menengah)** | 6x6 | 15 – 18 langkah | 4 – 5 Wave | Monster bertameng (Shielded), monster pelari cepat (Fast Runner). Menuntut kombinasi T2 untuk crowd-control & DPS. |
| **Level 10+ (Lanjut / Hard)** | 6x7 / 7x7 | 18 – 22 langkah | 5 – 6 Wave + 1 Boss Wave | Monster kebal debuff, healer, dan Final Boss dengan bar HP tebal serta skill pemecah barisan. Membutuhkan minimal 1–2 Hero T3. |

---

### 2.2 Mode 2: Real-time Deploy & Active Defense

Pertarungan berjalan secara *real-time*. Pemain mengumpulkan gems dari monster yang gugur dan terus melakukan summon serta fusi (*merge*) di tengah gempuran musuh.

* **Modal Gems Awal:** 100 – 150 Gems (diberikan saat fase persiapan 5–10 detik sebelum wave 1 dimulai).
* **Sistem Biaya Summon Dinamis:**
  $$\text{Cost}_n = \text{BaseCost} + (n - 1) \times \text{CostIncrement}$$
  * *Base Cost (Summon 1):* 40 Gems.
  * *Increment:* +10 Gems per panggilan (Panggilan ke-2 = 50, ke-3 = 60, ke-4 = 70, dst.).
  * *Tujuan:* Mencegah spam unit tanpa perhitungan taktis dan mendorong fusi unit secara efisien.
* **Pendapatan Gems (In-Game Economy):**
  * *Monster Biasa (Normal Creep):* +5 s/d +10 Gems per kill.
  * *Monster Elite / Mini-Boss:* +30 s/d +50 Gems per kill.
  * *Pendapatan Pasif (Passive Generation):* +2 s/d +5 Gems per detik (dapat ditingkatkan via talenta/riset).
* **Struktur Wave:** 3 – 5 wave bertempo cepat (jeda 3–5 detik antar wave), diakhiri puncak pertarungan melawan Boss Stage.

---

## 3. Trio Karakter Awal & Sistem Fusi / Evolusi Tier

Formasi trio ini mengadopsi sinergi *Holy Trinity RPG* klasik yang seimbang:

```
          [ Garis Depan ]
       🛡️ IRON KNIGHT (Tank)
         (Tahan Laju Musuh)
                ▲
                │ Sinergi
    ┌───────────┴───────────┐
    │                       │
🏹 WIND RANGER          🔥 FIRE MAGE
(Single Burst DPS)      (AoE Damage & CC)
```

### 3.1 Detail Karakter & Evolusi Tier

#### 1. Iron Knight (Melee Tank / Garis Depan)
* **Warna Gem / Token:** Merah (Red)
* **Role & Posisi:** Menahan pergerakan musuh, memiliki HP dan Defense tebal.
* **Jangkauan Serangan:** Melee (1 tile).
* **Evolusi Tier:**
  * **Tier 1 (T1) - Recruit Soldier:** Serangan tebasan pedang standar tunggal (*Single Target Physical Damage*).
  * **Tier 2 (T2) - Shield Guard:** Hantaman perisai (*Shield Bash*) yang menghasilkan stun singkat selama 0.5 detik setiap 4 kali pukulan.
  * **Tier 3 (T3) - Iron Paladin:** Serangan berputar area (*Spin Slash*) radius 1 tile sekitar + Aura Pasif (*Aegis Aura*) yang mengurangi damage serangan musuh sebesar 20%.

#### 2. Wind Ranger (Ranged DPS / Single Target Burst)
* **Warna Gem / Token:** Hijau (Green)
* **Role & Posisi:** Penembak jitu jarak jauh, eliminasi monster berkecepatan tinggi atau monster target prioritas.
* **Jangkauan Serangan:** Jarak Jauh (Garis lurus / prioritas monster paling depan atau terjauh).
* **Evolusi Tier:**
  * **Tier 1 (T1) - Scout Archer:** Tembakan anak panah tunggal dengan attack speed stabil.
  * **Tier 2 (T2) - Wind Striker:** *Multi-Shot* (menembakkan 2 anak panah simultan ke 2 target terdepan).
  * **Tier 3 (T3) - Storm Hunter:** *Rain of Arrows* (menembakkan hujan panah ke area barisan musuh dengan critical chance +35%).

#### 3. Fire Mage (Magic DPS / Area of Effect)
* **Warna Gem / Token:** Biru / Ungu (Blue / Magenta)
* **Role & Posisi:** Pembersih kerumunan monster kecil (*Swarm Cleaner*).
* **Jangkauan Serangan:** Ranged Area (Jarak menengah dengan radius ledakan).
* **Evolusi Tier:**
  * **Tier 1 (T1) - Apprentice Caster:** Percikan bola api kecil pada 1 target dengan efek *burn* ringan selama 1 detik.
  * **Tier 2 (T2) - Flame Sorcerer:** *Fireball Explosion* (bola api meledak dengan area ledakan 1.5 tile di sekitar titik benturan).
  * **Tier 3 (T3) - Archmage Pyromancer:** *Meteor Strike* (jatuhan meteor dengan damage ledakan masif + area tanah terbakar (*Burning Ground*) selama 3 detik yang memperlambat dan membakar musuh).

---

### 3.2 Mekanisme Summon & Fusi (Merge)

1. **Mode 1 (Puzzle Match-3 Preparation):**
   * Papan terisi oleh batu permata (Merah, Hijau, Biru).
   * Menjajarkan $\ge 3$ permata warna sama akan meleburkannya menjadi **Token Karakter Tier 1 (T1)**.
   * Menjajarkan 3 Token T1 akan menggabungkannya menjadi **Token T2**.
   * Menjajarkan 3 Token T2 akan menjadikannya **Token T3**.
   * Ketika *moves* habis (0), semua Token di grid bertransformasi menjadi unit tempur aktif yang menempati posisi petak tersebut.

2. **Mode 2 (Real-time Summon & Drag-Drop Merge):**
   * Pemain menekan tombol **Summon** (biaya dinamis gems). Unit T1 acak (Knight, Ranger, atau Mage) muncul di petak grid kosong.
   * Pemain dapat melakukan *drag & drop* unit A ke unit B yang memiliki **Tipe dan Tier yang sama**.
   * Kedua unit melebur menghasilkan 1 unit Tier berikutnya secara instan (T1 + T1 = T2; T2 + T2 = T3).

---

## 4. Arsitektur Teknis Unity C# (Clean Architecture & Modular Pattern)

Untuk memastikan sistem game mudah dikembangkan, diuji, dan dipelihara, kita membagi codebase ke dalam arsitektur berorientasi data (*Data-Driven*) menggunakan `ScriptableObject` dan pola *Manager-Controller*:

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs              // State machine game (Preparation, WaveActive, Victory, Defeat)
│   │   ├── GridSystem/
│   │   │   ├── GridManager.cs          // Logika matriks 2D, koordinat tile, validasi slot
│   │   │   ├── TileCell.cs             // Representasi objek tile di layar
│   │   │   └── MatchDetector.cs        // Algoritma pendeteksi Match-3 / Match-N (Mode 1)
│   │   ├── Economy/
│   │   │   └── EconomyManager.cs       // Manajemen gems, hitung biaya summon dinamis, passive income
│   │   ├── Wave/
│   │   │   ├── WaveSpawner.cs          // Penjadwalan kemunculan musuh & kurva kesulitan
│   │   │   └── WaveDataSO.cs           // ScriptableObject konfigurasi wave & tipe monster
│   │   └── Combat/
│   │       ├── UnitController.cs       // Base class pergerakan & siklus hidup hero
│   │       ├── KnightUnit.cs           // Override serangan & pasif Iron Knight (T1-T3)
│   │       ├── RangerUnit.cs           // Override serangan & pasif Wind Ranger (T1-T3)
│   │       ├── MageUnit.cs             // Override serangan & pasif Fire Mage (T1-T3)
│   │       ├── MonsterController.cs    // Jalur gerak, HP bar, pathfinding monster
│   │       └── Projectile.cs           // Peluru panah, bola api, visual efek ledakan
│   ├── Data/
│   │   ├── UnitDataSO.cs               // Konfigurasi stats (HP, ATK, Range, AttackRate, Sprite, Tier)
│   │   └── LevelConfigSO.cs            // Konfigurasi ukuran grid, batas move, wave per level
│   └── UI/
│       ├── UIBattleView.cs             // Tampilan HUD (HP base, wave counter, move/gem counter)
│       ├── DragDropHandler.cs          // Sistem interaksi drag and drop merge di Mode 2
│       └── SummonButton.cs             // Controller tombol summon & update label biaya gems
```

### 4.1 Prinsip Desain:
* **Decoupled via Events / Actions:** `GameManager` memicu event seperti `OnWaveStarted`, `OnMonsterKilled`, `OnHeroMerged`. Modul lain mendengarkan event tanpa saling ketergantungan erat (*loose coupling*).
* **ScriptableObjects untuk Balance Data:** Nilai damage, attack speed, biaya summon, dan properti tiap tier tersimpan di ScriptableObject, sehingga Game Designer dapat menyeimbangkan permainan tanpa harus mengubah baris kode.
* **Object Pooling:** Menggunakan pool untuk monster, proyektil (panah, bola api), dan damage text popup demi menjaga performa tinggi 60 FPS pada perangkat seluler.

---

## 5. Roadmap Pengembangan & Pembagian Peran

| Fase / Sprint | Target & Deliverables | PIC / Peran Utama |
| :--- | :--- | :--- |
| **Sprint 1: Fondasi Grid & Data Model** | • Implementasi `GridManager` (5x5 hingga 7x7)<br>• Pembuatan `UnitDataSO` untuk Iron Knight, Wind Ranger, Fire Mage<br>• Setup visual papan catur & tile selection | Gameplay Programmer |
| **Sprint 2: Mekanik Summon & Merge** | • Pembuatan sistem Match-3 detektor (Mode 1)<br>• Pembuatan Drag-Drop fusi unit & formula dinamis Summon Cost (Mode 2)<br>• Sistem ekonomi gems & counter langkah (*moves*) | Gameplay & Systems Programmer |
| **Sprint 3: Sistem Tempur & Wave Spawner** | • AI Monster pergerakan jalur (*pathway*) ke benteng<br>• Sistem targeting hero (Knight melee, Ranger line burst, Mage AoE)<br>• Visual fx proyektil, damage calc, dan hit detection | Combat Programmer & VFX Artist |
| **Sprint 4: Tier Evolution & Skill VFX** | • Implementasi skill Tier 2 & 3 (Shield bash stun, multi-shot, meteor flame)<br>• Audio sfx serangan & animasi transisi merge | Tech Artist & Sound Designer |
| **Sprint 5: UI/HUD & Balancing Polish** | • Tampilan HUD lengkap, win/lose screen, tuning angka parameter curve | UI/UX & Game Balance Designer |

# LEGACY MASTER GDD — Historical Reference

> **Status:** Superseded for current product direction by [`CURRENT_PRODUCT_DESIGN.md`](CURRENT_PRODUCT_DESIGN.md). This document contains legacy concepts and may not describe current implementation. Verify code/data before treating a feature statement as implemented.

# MASTER GAME DESIGN DOCUMENT (MASTER GDD) & TECHNICAL SPECIFICATION
**Project Name:** Fantasy Citadel Defense  
**Genre:** Stylized Chibi Fantasy Defense RPG (Puzzle Merge & Strategy Defense)  
**Target Engine:** Unity 2022.3 LTS+ (C# Clean Architecture)  
**Document Ref:** docs/MASTER_GAME_DESIGN_DOCUMENT.md  
**Version:** 2.0.0 (Unified Master Document)  
**Status:** Approved for Implementation  

---

## 1. VISI GAME & RINGKASAN EKSEKUTIF

**Fantasy Citadel Defense** adalah game strategi pertempuran bertema *Stylized Chibi Fantasy*. Game ini memadukan kedalaman pengelolaan markas (*Lobby Metagame*) dengan keseruan aksi pertahanan benteng berbasis petak (*Grid Merge Defense*).

### 1.1 Dua Pilar Utama Permainan
1. **Pilar Luar Pertempuran (Metagame & Citadel Lobby):**
   * Mengelola markas kerajaan (*Citadel*).
   * Meningkatkan level dan kemampuan trio hero utama.
   * Menempa senjata dan perlengkapan di pandai besi (*Forge*).
   * Membangun total kekuatan tempur tim (*Team Combat Power / CP*).
2. **Pilar Dalam Pertempuran (Core Battle & Grid Defense):**
   * Bertahan dari serbuan gelombang monster menuju benteng.
   * Menggabungkan (*merge*) hero untuk mengevolusikan unit dari **Tier 1 (T1) -> Tier 2 (T2) -> Tier 3 (T3)**.
   * Mengatur sinergi taktis antara penahan serangan (*Tank*), penyerang jarak dekat (*Melee DPS*), dan penyihir area (*Magic AoE*).

---

## 2. METAGAME & ANTARMUKA LOBBY (CITADEL)

Layar Lobby mengusung gaya visual *Stylized Chibi Fantasy* yang cerah, ramah, dan memanjakan mata dengan latar belakang benteng kerajaan megah di atas bukit hijau.

`
+-------------------------------------------------------------+
| [Citadel Lv.8]           [Energy 30/30]           [Gold 500]|
+-------------------------------------------------------------+
|                      [TEAM CP: 4,850]                       |
+-------------------------------------------------------------+
|                                                             |
|                         [KASTIL / CITADEL]                  |
|                                                             |
|        Rabbit Mage           Cedric            Rose Tanker  |
|     (Penyihir Kelinci)     (Ksatria Merah)     (Ksatria Pink)|
|     [Lv.12 | CP 1,600]    [Lv.15 | CP 1,850]  [Lv.14 | CP 1,400]|
|                                                             |
|                     [ BATTLE (5 Energy) ]                   |
+-------------------------------------------------------------+
|   [Citadel]       [Forge]       [Heroes]        [Bag]       |
+-------------------------------------------------------------+
`

### 2.1 Header Bar (Status Utama Pemain)
1. **Citadel Level (Citadel Lv.8):** Level markas akun pemain. Kenaikan level membuka fitur baru, menaikkan kapasitas stamina, dan memberikan bonus pasif statistik untuk seluruh pahlawan.
2. **Energy / Stamina (30/30):** Modal untuk memulai battle (konsumsi: 5 energi per pertempuran). Regenerasi alami: 1 energi tiap 5 menit.
3. **Gold Currency (500 Gold):** Mata uang reguler untuk upgrade hero, penempaan gear di Forge, dan transaksi toko.

### 2.2 Banner Team Combat Power (Team CP)
* **Kalkulasi Matematis:**
  \text{Team CP} = \text{CP Cedric (1,850)} + \text{CP Rabbit Mage (1,600)} + \text{CP Rose Tanker (1,400)} = 4,850
* **Fungsi:** Sebagai acuan kesiapan tim menghadapi stage tertentu. Setiap stage memiliki rekomendasi batas CP agar pemain mengetahui tingkat kelayakannya.

### 2.3 Tombol Aksi Utama (BATTLE Button)
* Tombol lonjong merah berbingkai emas: BATTLE (5 Energy).
* Memeriksa kecukupan stamina >= 5. Jika cukup, membuka pemilihan stage atau langsung meluncur ke arena pertahanan.

### 2.4 Navigasi Bawah (Bottom Bar)
1. **Citadel:** Layar utama markas kerajaan (tampilan saat ini).
2. **Forge (Pandai Besi):** Tempat membuat (*crafting*), memperkuat (*enhance*), dan memasang batu permata (*gem socketing*) pada perlengkapan.
3. **Heroes:** Menu daftar koleksi pahlawan, pengaturan formasi aktif, kenaikan level skill, dan pembagian equipment.
4. **Bag:** Inventori untuk menyimpan material tempa, ramuan energi, dan peti harta karun.

---

## 3. ROSTER KARAKTER: TRIO UTAMA & ARSIP HERO

Susunan trio utama di arena mengadopsi sinergi pertempuran yang saling melengkapi: **Rose Tanker (Garda Terdepan)** menahan musuh, **Cedric (Ksatria Merah)** menebas musuh dengan damage fisik jarak dekat yang masif, dan **Rabbit Mage (Penyihir)** membakar kerumunan musuh dari jarak jauh.

`
                      [ BENTENG / JALUR MUSUH ]
                                  |
                                  v
                    ROSE TANKER (Ksatria Pink)
                   (Menahan & Menyerap Gempuran)
                                  ^
                                  | Sinergi Garda Depan
                    CEDRIC (Ksatria Merah)
                 (DPS Fisik Jarak Dekat / Finisher)
                                  ^
                                  | Sinergi Lini Belakang
                    RABBIT MAGE (Penyihir Kelinci)
                 (Sihir Area / Pembersih Kerumunan)
`

---

### 3.1 Karakter 1: Cedric (The Red Knight - Melee DPS Striker)
* **Visual:** Pemuda pemberani berambut cokelat bertudung merah dan berzirah besi lengkap, bersenjatakan pedang baja tajam (*arming sword*).
* **Warna Token / Gem:** Merah (Red).
* **Posisi & Peran:** Melee Striker (1 Tile). Bergerak cepat menyayat monster yang sedang tertahan oleh Tanker.
* **Statistik Utama:** High Physical ATK, High Critical Rate, Moderate DEF.
* **Evolusi Tier di Arena:**
  * **Tier 1 (Recruit Swordsman):** Tebasan pedang tajam tunggal (*Single Target Slash*) dengan kecepatan serang 1.0s.
  * **Tier 2 (Crimson Blade):** *Dual Slash*, tebasan menyilang ganda bertubi-tubi yang memberikan 180% damage serta peluang kritikal +20%.
  * **Tier 3 (Grand Blade Master):** *Blade Tempest*, serangan putaran pedang badai yang merobek armor musuh di sekitarnya (-25% Physical DEF musuh) dan memicu buff *Heroic Surge* (+15% Attack Speed untuk seluruh hero di petak terdekat).

---

### 3.2 Karakter 2: Rose Tanker (The Pink Paladin - Heavy Tank & CC)
* **Visual:** Ksatria wanita tangguh berkuncir kuda merah muda (*pink hair*), mengenakan baju zirah pelat tebal berlambang mawar emas, mengayunkan palu perang raksasa (*warhammer*) dan perisai baja.
* **Warna Token / Gem:** Pink / Rose Gold.
* **Posisi & Peran:** Heavy Frontline Vanguard (1 Tile). Menahan gelombang monster agar tidak menembus batas benteng.
* **Statistik Utama:** Extreme HP, High Physical & Magic Resistance, Taunt / Crowd Control.
* **Evolusi Tier di Arena:**
  * **Tier 1 (Hammer Guard):** Hantaman palu perang tunggal berdamage sedang dengan ketahanan fisik pasif tebal.
  * **Tier 2 (Rose Paladin):** *Shield Slam*, hantaman perisai mawar yang memicu efek *Stun* 1 detik pada monster terdepan setiap 3 kali serangan, sekaligus menyerap 25% damage berikutnya.
  * **Tier 3 (Aegis Vanguard):** *Earthshaker Impact*, hantaman palu raksasa ke tanah yang memicu gempa stun area (radius 1.5 tile) selama 1.5 detik + *Thorn Aegis* (aura duri mawar yang memantulkan 20% damage serangan monster kembali ke mereka).

---

### 3.3 Karakter 3: Rabbit Mage (The Mystic Caster - Magic AoE)
* **Visual:** Karakter ras kelinci putih cerdas berjubah dan bertopi penyihir biru, menggenggam tongkat kayu bertatahkan kristal sihir bercahaya toska.
* **Warna Token / Gem:** Biru / Ungu (Blue / Mystic Violet).
* **Posisi & Peran:** Ranged Magic DPS (3-4 Tile). Menghabisi gerombolan monster kecil dari jarak aman.
* **Statistik Utama:** High Magic ATK, Area of Effect (AoE), Crowd Control (Slow/Burn).
* **Evolusi Tier di Arena:**
  * **Tier 1 (Apprentice Caster):** Tembakan percikan kristal energi sihir tunggal dengan jangkauan jauh.
  * **Tier 2 (Flame Sorcerer):** *Mystic Burst*, ledakan bola energi sihir dengan radius 1.5 tile di sekitar titik benturan.
  * **Tier 3 (Archmage Pyromancer):** *Meteor / Starfall Impact*, hantaman komet sihir masif yang memberikan damage ledakan dahsyat dan meninggalkan area kobaran sihir (*Arcane Fire*) selama 3 detik, memperlambat kecepatan jalan musuh sebesar 40% dan membakar mereka per detik.

---

### 3.4 Arsip Roster Mendatang (Upcoming Hero Roster): Wind Ranger
* **Status Arsip:** Disimpan rapi untuk pembaruan stage berikutnya (*Chapter 2 / Event Summon*).
* **Identitas:** Pemanah lincah berbusana hijau pemburu rimba (*Emerald Archer*).
* **Peran Taktis:** Ultra-Long Ranged Single DPS / Target Sniper. Menembakkan panah beruntun (*Multi-shot*) dan *Rain of Arrows* dengan prioritas target monster terbang atau pembawa bendera musuh.

---

## 4. CORE BATTLE MECHANICS & SISTEM GAMEPLAY

Pertempuran berlangsung di atas papan petak (*grid*) berukuran **5x5 hingga 7x7**, di mana musuh bergerak melalui lintasan jalur menuju benteng pertahanan pemain.

Game dirancang untuk mengakomodasi dua mode gameplay modular:

### 4.1 Mode 1: Puzzle Preparation (Turn-Based Match-3 Style)
* **Fase Taktis Sebelum Wave:** Pemain diberikan jatah langkah (*moves*, 12-18 langkah) untuk menata papan sebelum monster meluncur.
* **Logika Match-3:**
  * Mencocokkan 3 batu permata warna senada -> Melebur menjadi **Token Tier 1 (T1)**.
  * Mencocokkan 3 Token T1 -> Melebur menjadi **Token Tier 2 (T2)**.
  * Mencocokkan 3 Token T2 -> Melebur menjadi **Token Tier 3 (T3)**.
* **Fase Pertahanan:** Saat jatah langkah habis (*moves = 0*), seluruh token seketika bertransformasi menjadi unit tempur aktif yang berdiri di atas petak masing-masing dan otomatis menyerang musuh yang melintas.

### 4.2 Mode 2: Real-time Deploy & Merge (Lucky Defense / Rush Royale Style)
* **Fase Tempur Aktif:** Pertempuran berjalan secara langsung (*real-time*).
* **Modal Awal:** 100 - 150 Gems diberikan pada fase persiapan 5 detik sebelum wave 1.
* **Biaya Summon Dinamis (*Dynamic Summon Cost*):**
  \text{Biaya}_n = 40 + (n - 1) \times 10\text{ Gems}
  *(Panggilan 1 = 40 gems, Panggilan 2 = 50 gems, Panggilan 3 = 60 gems, dst).*
* **Ekonomi Pertempuran:**
  * Eliminasi monster biasa: +5 s/d +10 gems.
  * Eliminasi mini-boss: +30 s/d +50 gems.
  * Pendapatan pasif: +2 s/d +5 gems per detik.
* **Mekanik Merge Drag & Drop:**
  Pemain dapat menyeret (*drag*) satu hero ke hero lain yang bertipe dan bertier sama di grid untuk menggabungkannya secara instan:
  \text{T1} + \text{T1} \rightarrow \text{T2} \quad \text{dan} \quad \text{T2} + \text{T2} \rightarrow \text{T3}

---

## 5. KURVA PROGRESI LEVEL & STATISTIK TIER

### 5.1 Tabel Desain Level Pertempuran
| Kategori Level | Ukuran Grid | Jatah Moves (Mode 1) | Jumlah Wave | Komposisi Musuh | Target Strategi |
| :--- | :---: | :---: | :---: | :--- | :--- |
| **Level 1 - 3** | 5x5 | 12 - 14 | 3 Wave | Monster Lambat (*Slime/Goblin Creep*) | Membiasakan formasi dasar & fusi T1 |
| **Level 4 - 9** | 6x6 | 15 - 18 | 4 - 5 Wave | Monster Cepat (*Wolf Runner*) + Monster Perisai (*Shielded Orc*) | Menuntut kehadiran Rose Tanker T2 & Cedric T2 |
| **Level 10+ (Boss)** | 7x7 | 18 - 22 | 5 Wave + 1 Boss | Monster Pelindung + Final Boss (HP Tebal & Skill Smash) | Wajib membentuk minimal 1-2 Hero T3 |

### 5.2 Multiplier Kekuatan Tier di Arena
Kekuatan hero di dalam arena tempur ditarik langsung dari base stat hero di Lobby, kemudian dikalikan dengan faktor pengali Tier:
\text{Stat Tempur} = \text{Base Stat Lobby} \times \text{Tier Multiplier}

* **Tier 1:** 1.0x (100% Base ATK & HP) - Serangan dasar standar.
* **Tier 2:** 2.2x (220% Base ATK & HP) - Membuka skill unik aktif (Stun / Dual Slash / Mystic Burst).
* **Tier 3:** 4.8x (480% Base ATK & HP) - Membuka ultimate aura dan skill area masif.

---

## 6. ARSITEKTUR TEKNIS UNITY C# (CLEAN & MODULAR)

Seluruh arsitektur kode dirancang menggunakan pola berorientasi data (*Data-Driven*) via ScriptableObject dan komunikasi berbasis *C# Event/Action* agar bebas dari ketergantungan yang kaku (*decoupled*).

### 6.1 Struktur Hierarki Direktori Unity
`
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs              // State machine alur game (Preparation, WaveActive, Victory, Defeat)
│   │   ├── GridSystem/
│   │   │   ├── GridManager.cs          // Logika matriks 2D, ukuran grid (5x5 - 7x7), koordinat petak
│   │   │   ├── TileCell.cs             // Komponen fisik petak/slot pada grid
│   │   │   └── MatchDetector.cs        // Algoritma pendeteksi pola Match-3 & gravitasi token (Mode 1)
│   │   ├── Economy/
│   │   │   └── EconomyManager.cs       // Manajemen saldo gems, formula kenaikan biaya summon dinamis
│   │   ├── Wave/
│   │   │   ├── WaveSpawner.cs          // Pengatur kemunculan monster per wave & jalur jalan (pathway)
│   │   │   └── WaveDataSO.cs           // ScriptableObject konfigurasi komposisi monster tiap wave
│   │   └── Combat/
│   │       ├── UnitController.cs       // Base class siklus hidup, HP bar, targeting, dan serangan hero
│   │       ├── CedricUnit.cs           // Override: Tebasan cepat, dual slash, blade tempest (Melee DPS)
│   │       ├── RoseTankerUnit.cs       // Override: Hantaman palu, taunt, shield slam, earthshaker (Tanker)
│   │       ├── RabbitMageUnit.cs       // Override: Percikan kristal, mystic burst, meteor strike (Magic AoE)
│   │       ├── MonsterController.cs    // Pergerakan monster menyusuri waypoint, HP bar, damage ke benteng
│   │       └── Projectile.cs           // Sistem proyektil (peluru sihir, efek benturan, ledakan AoE)
│   ├── Data/
│   │   ├── UnitDataSO.cs               // ScriptableObject parameter stats (HP, ATK, AttackRate, Range, Tier)
│   │   └── LevelConfigSO.cs            // ScriptableObject pengaturan grid size, move limit, daftar wave
│   └── UI/
│       ├── UIBattleView.cs             // Tampilan HUD tempur (HP benteng, wave counter, move/gem counter)
│       ├── DragDropHandler.cs          // Controller interaksi geser & lepas (drag-and-drop merge di Mode 2)
│       └── SummonButton.cs             // Controller tombol summon unit & label biaya gems dinamis
`

### 6.2 Gambaran Logika Kunci Tiap Skrip

1. **UnitDataSO.cs (ScriptableObject):**
   `csharp
   public enum UnitType { Cedric, RoseTanker, RabbitMage, WindRanger }
   public enum UnitTier { Tier1, Tier2, Tier3 }

   [CreateAssetMenu(fileName = "NewUnitData", menuName = "GameData/Unit Data")]
   public class UnitDataSO : ScriptableObject
   {
       public UnitType unitType;
       public UnitTier tier;
       public string unitName;
       public float baseDamage;
       public float attackRate;
       public float attackRange;
       public float maxHealth;
       public Sprite unitIcon;
       public GameObject unitPrefab;
   }
   `

2. **GridManager.cs:**
   Membuat matriks petak secara dinamis sesuai konfigurasi level (5x5, 6x6, atau 7x7) dan memvalidasi ketersediaan petak kosong untuk pemanggilan hero.

3. **EconomyManager.cs:**
   Menghitung biaya summon dinamis secara otomatis:
   `csharp
   public int GetCurrentSummonCost()
   {
       return baseSummonCost + (totalSummonCount * costIncrementPerSummon);
   }
   `

4. **UnitController.cs & Turunannya (CedricUnit, RoseTankerUnit, RabbitMageUnit):**
   * Menemukan target terdekat dalam jangkauan (*attackRange*).
   * Memicu animasi serang dan menghasilkan damage/proyektil.
   * CedricUnit: Mengutamakan monster terdekat di garis depan dengan damage fisik tinggi.
   * RoseTankerUnit: Menahan laju monster terdepan dan memicu efek stun berkala.
   * RabbitMageUnit: Menargetkan monster dalam kerumunan dan memicu damage area.

---

## 7. ROADMAP EKSEKUSI PENGEMBANGAN (SPRINT 1 - 5)

* **Sprint 1: Fondasi Grid & Data Model**
  * Membuat struktur direktori Assets/Scripts/....
  * Membuat kelas UnitDataSO dan asset ScriptableObject untuk Cedric, Rose Tanker, dan Rabbit Mage.
  * Mengembangkan GridManager dan TileCell dengan ukuran dinamis 5x5 s/d 7x7.
* **Sprint 2: Mekanik Summon & Merge Engine**
  * Mengembangkan logika pemanggilan hero acak dan kalkulasi biaya di EconomyManager.
  * Mengembangkan sistem fusi *drag-and-drop* (T1 + T1 = T2; T2 + T2 = T3).
  * Mengembangkan detektor Match-3 untuk mode turn-based.
* **Sprint 3: Sistem Pertempuran & AI Monster**
  * Pembuatan MonsterController dan pergerakan menyusuri *waypoint* jalur benteng.
  * Implementasi sistem targeting dan serangan di UnitController (Melee Cedric, Tank Rose, AoE Rabbit Mage).
  * Pembuatan sistem proyektil dan deteksi benturan ledakan.
* **Sprint 4: Skill Spesifik Tier 2 & Tier 3**
  * Implementasi efek stun palu Rose Tanker, dual slash Cedric, dan meteor Rabbit Mage.
  * Penambahan efek visual fusi (*merge flash*) dan audio SFX.
* **Sprint 5: Antarmuka Tempur (HUD) & Penyeimbangan (Balancing)**
  * Menghubungkan tampilan HUD (HP benteng, wave counter, tombol summon).
  * Menyelaraskan formula damage dan kurva biaya gems untuk pengalaman bermain yang memuaskan.

---

*Dokumen ini merupakan acuan tunggal dan mutlak untuk seluruh implementasi program, aset visual, dan mekanik pertempuran proyek Fantasy Citadel Defense.*

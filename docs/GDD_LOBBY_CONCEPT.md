# LEGACY GDD — Lobby & Hero System

> **Status:** Historical concept; current product direction is in [`CURRENT_PRODUCT_DESIGN.md`](CURRENT_PRODUCT_DESIGN.md). Verify details against current UI and owner decisions before implementation.

# GAME DESIGN DOCUMENT (GDD) - LOBBY & HERO SYSTEM
**Project:** Fantasy Heroes RPG  
**Module:** Main Lobby UI & Trio Character Formation  
**Asset Ref:** `photo_5597248348_1791733704_AQADuxRrG0xNWFZ-.jpg`  
**Version:** 1.0.0  
**Status:** Approved Concept  

---

## 1. Ringkasan Eksekutif & Visual Overview

Dokumen ini mendefinisikan spesifikasi game design, antarmuka pengguna (UI), arsitektur tim, dan gameplay loop dari layar **Lobby Utama (Citadel)** berdasarkan visual konsep yang disediakan.

### 1.1 Gaya Visual & Art Direction
* **Tema & Genre:** Stylized Chibi Fantasy RPG (Mobile/Cross-Platform).
* **Nuansa Visual:** Cerah, vibrant, ramah pengguna, berpetualang, dengan palet warna cerah (langit biru, kastil megah beratap biru, pekarangan rumput hijau, serta jalan setapak bebatuan).
* **Karakter:** Desain karakter chibi 2D/3D dengan proporsi kepala-tubuh ekspresif, siluet unik dan mudah dikenali antar kelas.

---

## 2. Analisis & Spesifikasi Elemen UI Lobby

```
+-------------------------------------------------------------+
| [🛡️ Citadel Lv.8]       [⚡ 30/30]         [🪙 500]         |
+-------------------------------------------------------------+
|                      [⚡ TEAM CP: 4,850]                    |
+-------------------------------------------------------------+
|                                                             |
|                         [KASTIL / CITADEL]                  |
|                                                             |
|        🧙 (Mage)             ⚔️ Cedric            🔨 (Tanker)  |
|       Kelinci Sihir         Pendekar Pedang      Ksatria Mawar |
|     [Lv.12 | CP 1,600]     [Lv.15 | CP 1,850]   [Lv.14 | CP 1,400]|
|                                                             |
|                     [ ⚔️ BATTLE (5 ⚡) ]                     |
+-------------------------------------------------------------+
|   [🏰 Citadel]    [⚒️ Forge]    [🪖 Heroes]    [🎒 Bag]     |
+-------------------------------------------------------------+
```

### 2.1 Header Bar (Top Bar Status)
1. **Citadel Level (`Citadel Lv.8`):**
   * **Ikon:** Perisai emas dengan lambang benteng.
   * **Fungsi:** Level progres akun/markas pemain. Membuka fitur baru, meningkatkan batas kapasitas resource, dan memberikan buff pasif tim.
2. **Energy / Stamina Bar (`⚡ 30/30`):**
   * **Ikon:** Petir kuning pada bilah hijau berbingkai emas.
   * **Fungsi:** Sumber daya untuk memulai pertempuran (konsumsi: 5 energi per battle). Regenerasi: 1 energi setiap 5 menit (estimasi 150 menit dari 0 ke penuh).
3. **Gold Currency (`🪙 500`):**
   * **Ikon:** Koin emas bermahkota.
   * **Fungsi:** Mata uang reguler (soft currency) untuk upgrade senjata di Forge, leveling karakter, dan transaksi toko.

### 2.2 Team Combat Power Banner
* **Komponen:** Plakat heksagonal emas bertuliskan `⚡ TEAM CP: 4,850`.
* **Kalkulasi Matematis:**
  $$\text{Team CP} = \text{CP Mage (1,600)} + \text{CP Cedric (1,850)} + \text{CP Tanker (1,400)} = 4,850$$
* **Fungsi:** Mengindikasikan total daya tempur tim aktif yang digunakan sebagai tolok ukur rekomendasi tingkat kesulitan stage pertempuran.

### 2.3 Tombol Aksi Utama (BATTLE Button)
* **Visual:** Tombol lonjong merah cerah dengan bingkai emas timbul berornamen permata.
* **Label:** `⚔️ BATTLE (5 ⚡)`.
* **Mekanik:**
  * Menampilkan biaya stamina langsung pada tombol (`5 ⚡`).
  * Jika stamina $\ge 5$, menekan tombol akan membuka Chapter/Stage Selection atau langsung masuk ke stage progresif.
  * Jika stamina $< 5$, tombol memicu pop-up konfirmasi pengisian energi menggunakan item atau permata premium.

### 2.4 Navigasi Bawah (Bottom Navigation Bar)
Terdiri dari 4 menu inti permainan:
1. **Citadel (Active Tab):**
   * Tampilan saat ini (Lobby). Memperlihatkan markas kerajaan, tim aktif, dan visual lingkungan.
2. **Forge (Pandai Besi):**
   * Ikon kuali/landasan tempa emas.
   * Fitur: Pembuatan perlengkapan (crafting), penempaan senjata/baju zirah (enhance), fusi perlengkapan, dan soket batu permata.
3. **Heroes (Manajemen Karakter):**
   * Ikon helm ksatria emas dengan batu delima merah.
   * Fitur: Daftar koleksi hero, penggantian formasi tim, skill upgrade, gear equipping, dan breakthrough level.
4. **Bag (Tas / Inventori):**
   * Ikon ransel kulit petualang.
   * Fitur: Penyimpanan consumable (potion energi, scroll), material tempa, peti hadiah, dan shard karakter.

---

## 3. Desain Tiga Karakter Utama (Trio Holy Trinity)

Formasi tim mengadopsi sinergi *Holy Trinity RPG* klasik: **Offensive Striker (Cedric) + Heavy Tanker (Pink Knight) + Ranged Magic Support/Burst (Rabbit Mage)**.

| Karakter | Posisi Formasi | Role & Kelas | Level | Combat Power (CP) | Karakteristik Visual & Senjata |
| :--- | :---: | :---: | :---: | :---: | :--- |
| **Rabbit Mage** | Kiri (Backline) | Magic DPS / Buffer | Lv. 12 | 1,600 | Ras kelinci putih, topi & jubah penyihir biru, tongkat kayu dengan kristal sihir bercahaya toska. |
| **Cedric** | Tengah (Leader / Midline) | Swordman / Balance Fighter | Lv. 15 | 1,850 | Pemuda berambut cokelat, tudung merah, zirah besi lengkap, pedang baja (*arming sword*), perisai ksatria. |
| **Rose Tanker** | Kanan (Frontline) | Tanker / Paladin | Lv. 14 | 1,400 | Ksatria wanita kuncir kuda merah muda (*pink*), zirah pelat dengan lambang bunga mawar emas, palu perang raksasa (*warhammer*). |

### 3.1 Detail Karakter

#### A. Cedric (The Brave Knight / Swordman)
* **Identitas:** Protagonis / Pemimpin Regu.
* **Atribut Utama:** Balanced Physical ATK & DEF.
* **Peran Taktis:**
  * Memberikan *consistent melee damage* dengan tebasan pedang.
  * Mampu menahan serangan minor menggunakan perisai.
  * Memiliki *leadership aura* yang meningkatkan ATK seluruh anggota tim.

#### B. Rabbit Mage (The Mystic Caster)
* **Identitas:** Penyihir cerdas dari bangsa Beastfolk.
* **Atribut Utama:** High Magic ATK, Area of Effect (AoE), Crowd Control.
* **Peran Taktis:**
  * Menyerang dari jarak aman di barisan belakang.
  * Menggunakan sihir kristal untuk memberikan efek *Freeze/Slow* atau *Magic Burst* kepada kelompok musuh.
  * Memberikan buff magic barrier atau akselerasi energi untuk Cedric dan Tanker.

#### C. Rose Tanker (The Iron Vanguard)
* **Identitas:** Pelindung garis depan dengan armor berat bermotif mawar.
* **Atribut Utama:** High Maximum HP, High Armor / Physical DEF, Taunt.
* **Peran Taktis:**
  * Berdiri di baris terdepan untuk menyerap serangan utama musuh (*aggro drawing*).
  * Menghantam tanah dengan palu perang untuk menimbulkan efek *Stun / Knockdown*.
  * Menjaga Cedric dan Mage agar dapat menyerang musuh tanpa terganggu.

---

## 4. Core Gameplay Loop

```
[ LOBBY (CITADEL) ]
       │
       ▼ (Konsumsi 5 Stamina)
[ PERTEMPURAN (BATTLE) ]
       │
       ├── Menang Stage ──► Mendapatkan EXP, Gold, Gear & Mat
       └── Gagal Stage  ──► Evaluasi Formasi & Upgrade
       │
       ▼
[ UPGRADE & PROGRESSION ]
       ├── Heroes Menu: Naikkan Level Hero & Skill
       ├── Forge Menu: Tempa / Tingkatkan Senjata & Zirah
       └── Citadel Menu: Upgrade Level Kastil
       │
       ▼ (CP Tim Meningkat)
[ TANTANGAN STAGE BERIKUTNYA ]
```

---

## 5. Rencana Pengembangan Selanjutnya (Next Milestones)

1. **Implementasi UI Wireframe & Slicing Asset:**
   * Ekstraksi sprite/ilustrasi: Karakter (Rabbit Mage, Cedric, Rose Tanker), Background Citadel, dan Icon UI (Top bar & Bottom bar).
2. **Logic & Data Model:**
   * Model data pemain (PlayerProfile): Level Citadel, Current/Max Energy, Gold, Gem.
   * Model data hero (HeroModel): ID, Name, Role, Level, Base Stats, Gear, Current CP.
   * Sistem kalkulasi dinamis Team CP ($CP_{total} = \sum CP_{hero}$).
3. **Mekanik Battle & State Management:**
   * Handler tombol Battle, verifikasi stamina, dan transisi ke Scene Battle.

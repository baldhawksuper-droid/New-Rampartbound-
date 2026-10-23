# Game Design Document: Rampartbound: Pixel Defense
**Status:** Official Production GDD (Approved)  
**Project Lead:** Project Manager (PM)  
**Game Title:** Rampartbound: Pixel Defense  
**Genre:** Hybrid Merge-Grid Defense + Roguelike Strategy  
**Art Style:** 16-Bit Medieval Isometric Pixel Art (SNES/GBA Nostalgia)  
**Target Platform:** Mobile (Android / iOS) via Unity 6 (URP 2D)

---

## 1. Konsep Inti & Filosofi Nama (Rampartbound)
* **Rampart:** Tembok pembatas benteng pertahanan kerajaan tempat para pahlawan berjaga.
* **Bound:** Terikat takdir perjuangan tanpa henti mempertahankan kerajaan dari serbuan kawanan monster malam hari (*roguelike survival loop*).
* **Core Loop:**
  1. **Summon & Merge:** Pemain memanggil hero di grid 4x4 menggunakan Mana/Gold, lalu menggabungkan (merge) dua unit tier sama menjadi tier lebih tinggi.
  2. **Active Rampart Defense:** Unit hero yang ditempatkan di grid menembaki monster secara otomatis dari atas tembok pertahanan.
  3. **Wave Cleared -> Roguelike Perk Cards:** Setiap wave selesai, pemain memilih 1 dari 3 kartu berkah (Card Buffs) untuk memperkuat sinergi tim.
  4. **Camp & Meta Progression:** Di lobby desa medieval, pemain mengupgrade Talents permanen (damage, max HP rampart, recovery) menggunakan koin hasil pertempuran.

---

## 2. Struktur Layar Mobile (Portrait View 9:16)
```text
+-----------------------------------+
|  [Wave 1/10]   [Rampart HP: 100%] | <- Header HUD & Status Benteng
+-----------------------------------+
|                                   |
|   Jalur Musuh (Monster Spawner)    | <- Area serbuan monster dari utara
|      👾 Slime       👹 Goblin     |
|         ↓              ↓          |
|                                   |
+===================================+ <- [ DINDING BENTENG / RAMPART ]
|                                   |
|   Papan Petak Hero (Grid 4x4)     |
|   [ 🏹 T1 ]  [ 🧙 T2 ]  [ 🛡️ T1 ]  | <- Hero menembak ke atas otomatis
|   [      ]  [ 🏹 T1 ]  [ 🧙 T3 ]  |    Pemain drag & merge real-time
|                                   |
+-----------------------------------+
|  [Summon Hero: 100 Mana] [Skills] | <- Footer & Tombol Aksi
+-----------------------------------+
```

---

## 3. Rantai Evolving Hero (16-Bit Pixel Units)

| Tier | Nama Unit | Atk Speed | Base Damage | Tipe Serangan |
|---|---|---|---|---|
| **Tier 1 (T1)** | Novice Archer | 1.0s | 15 dmg | Tembakan Panah Tunggal |
| **Tier 2 (T2)** | Scout Sniper | 0.8s | 35 dmg | Panah Cepat & Jarak Jauh |
| **Tier 3 (T3)** | Arcane Mage | 1.2s | 90 dmg | Bola Sihir Splash Area (AoE) |
| **Tier 4 (T4)** | Cannoner | 1.5s | 220 dmg | Ledakan Bom Area Besar |
| **Tier 5 (T5)** | Storm Sorcerer | 0.7s | 450 dmg | Rantai Petir Multi-Target |

---

## 4. Roguelike Battle Cards (Pilihan Tiap Wave)
Setiap kali wave musuh dibersihkan, muncul pilihan **1 dari 3 kartu acak**:
1. **Swift Fingers:** +20% Attack speed semua hero di grid.
2. **Heavy Arrows:** +25% Damage tembakan proyektil.
3. **Mage Mastery:** Peluru sihir memiliki 30% peluang ledakan kritis (*Crit 2x*).
4. **Fortify Wall:** Memulihkan 30 HP Benteng (*Rampart Repair*).
5. **Mana Overflow:** Biaya summon hero berkurang 25%.

---

## 5. Meta-Progression (Desa & Bengkel Talenta)
1. **Bengkel Tempa (Blacksmith):** Menempa senjata hero untuk meningkatkan base damage global.
2. **Batu Benteng (Stoneworks):** Mempertebal HP tembok pertahanan benteng.
3. **Lumbung Makanan (Granary):** Meningkatkan efisiensi regenerasi energi harian.

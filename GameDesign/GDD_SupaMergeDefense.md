# Game Design Document: SupaMerge Defense Style

## 1. Konsep Utama
Menggabungkan **Papan Merge (Grid)** dengan **Tower Defense Real-Time**.
Pemain memunculkan (*summon*) Hero di petak papan, menggabungkan (*merge*) hero level rendah menjadi level tinggi, sementara hero tersebut secara otomatis menembaki monster yang menyerbu menuju benteng (*Rampart*).

---

## 2. Struktur Layar Mobile (Portrait View)
```text
+-----------------------------------+
|  [Wave 1/10]   [Base HP: 100/100]  | <- Header HUD & Status Benteng
+-----------------------------------+
|                                   |
|   Jalur Musuh (Monster Spawner)    | <- Area Monster berjalan ke bawah
|      👾 Slime       👹 Goblin     |
|         ↓              ↓          |
|                                   |
+===================================+ <- [ DINDING BENTENG / RAMPART ]
|                                   |
|   Papan Petak Hero (Grid 4x4)     |
|   [ 🏹 T1 ]  [ 🧙 T2 ]  [ 🛡️ T1 ]  | <- Hero menembak ke atas otomatis
|   [      ]  [ 🏹 T1 ]  [ 🧙 T3 ]  |    Pemain bisa drag & merge hero
|                                   |
+-----------------------------------+
|  [Summon Hero: 100 Mana] [Skills] | <- Footer & Tombol Aksi
+-----------------------------------+
```

---

## 3. Rantai Evolving Hero (Minimalist Tier)

| Tier | Nama Unit | Atk Speed | Base Damage | Tipe Serangan |
|---|---|---|---|---|
| **Tier 1 (T1)** | Novice Archer | 1.0s | 15 dmg | Tembakan Panah Tunggal |
| **Tier 2 (T2)** | Scout Sniper | 0.8s | 35 dmg | Panah Cepat & Jarak Jauh |
| **Tier 3 (T3)** | Arcane Mage | 1.2s | 90 dmg | Bola Sihir Splash Area (AoE) |
| **Tier 4 (T4)** | Cannoner | 1.5s | 220 dmg | Ledakan Bom Area Besar |
| **Tier 5 (T5)** | Storm Sorcerer | 0.7s | 450 dmg | Rantai Petir Multi-Target |

---

## 4. Roguelike Battle Cards (Pilihan Tiap Wave)
Setiap kali wave musuh dibersihkan, muncul pop-up pilihan **1 dari 3 kartu acak**:
1. **Swift Fingers:** +20% Attack speed semua hero di grid.
2. **Heavy Arrows:** +25% Damage tembakan.
3. **Mage Mastery:** Peluru sihir memiliki 30% peluang ledakan kritis (*Crit 2x*).
4. **Fortify Wall:** Memulihkan 30 HP Benteng (*Repair*).
5. **Mana Overflow:** Biaya summon hero berkurang 25%.

# Panduan & Katalog Aset Resmi: Rampartbound — Pixel Defense (v2.15.0)

Dokumen ini adalah rujukan resmi (*single source of truth*) katalog seluruh aset grafis, sprite, arena pertempuran, dan UI yang digunakan dalam game **Rampartbound: Pixel Defense**.

---

## 1. Evolusi dari Prototype Awal ke Visual Final

| Fase Proyek | Karakter & Hero | Arena & Lingkungan | Format & Dimensi |
|---|---|---|---|
| **Early Prototype (SupaMerge)** | Bentuk geometris sederhana (Lingkaran, Belah Ketupat, Hexagon) | Background polos hijau prosedural (*Meadow Noise*) | Berkas vektor `.svg` di folder legacy |
| **Rampartbound Final (v2.x)** | Hero Celtic Pixel Fantasy: Cedric Emberblade, Pippin Runemage, Commander Rose | Benteng batu pertahanan, jalan bebatuan, atmosfer cuaca, benteng kastil (`BattleField.jpg` / Level Outpost & Siege) | Berkas raster resolusi tinggi `.png` & `.jpg` (PPU 100) |

---

## 2. Struktur Direktori Aset di Unity (`Rampartbound/Assets/Art/`)

Seluruh aset resmi yang aktif digunakan runtime berada di dalam subfolder `Assets/Art/`:

```text
Rampartbound/Assets/Art/
├── BattleField.jpg                         <-- Arena Pertempuran Resmi (Celtic Dark Fantasy)
├── TitleScreen.jpg                         <-- Layar Judul Utama (Gilded Frame Title)
├── lobby_mockup_redesign.jpg               <-- Layar Utama Lobby Kota Kastil
├── gameplay_l1_outpost_mockup.jpg          <-- Arena Level 1: Outpost Defense
├── gameplay_l2_battle_mockup.jpg           <-- Arena Level 2: Fortress Siege Battle Phase
├── gameplay_l2_puzzle_mockup.jpg           <-- Arena Level 2: Fortress Siege Puzzle Phase
├── Sprites/                                <-- Seluruh Sprite Hero, Token, Monster, & Dinding
└── UI/                                     <-- Frame Modal Emas, Kartu Perk, Parchment Map
```

---

## 3. Katalog Hero & Token Karakter (`Assets/Art/Sprites/`)

### A. Tiga Hero Utama & Evolusi Token Medali
Setiap hero memiliki medallion token 3 tier (Bronze, Silver, Gold):

1. **Cedric Emberblade (Vanguard / Melee Bara Api):**
   * Tier 1 (Bronze): `token_cedric_t1.png`
   * Tier 2 (Silver): `token_cedric_t2.png`
   * Tier 3 (Gold): `token_cedric_t3.png`
   * Avatar Berdiri: `hero_cedric_pixel.png` / `hero_cedric_standing.png`
   * Animasi Tempur 4-Frame: `spritesheet_cedric_4f.png` (Idle, Attack Windup, Slash, Recovery)

2. **Pippin Astral Runemage (Mage / Magic Listrik Arcane):**
   * Tier 1 (Bronze): `token_pippin_t1.png`
   * Tier 2 (Silver): `token_pippin_t2.png`
   * Tier 3 (Gold): `token_pippin_t3.png`
   * Avatar Berdiri: `hero_pippin_pixel.png` / `hero_pippin_standing.png`
   * Animasi Tempur 4-Frame: `spritesheet_pippin_4f.png`

3. **Commander Rose (Ranger / Piercing Arrows):**
   * Tier 1 (Bronze): `token_rose_t1.png`
   * Tier 2 (Silver): `token_rose_t2.png`
   * Tier 3 (Gold): `token_rose_t3.png`
   * Avatar Berdiri: `hero_rose_pixel.png` / `hero_rose_standing.png`
   * Animasi Tempur 4-Frame: `spritesheet_rose_4f.png`

---

## 4. Katalog Dinding Benteng & Pertahanan (`Assets/Art/Sprites/`)

* **Palisade Wall Intact:** `palisade_wall.png` / `rampart_wall_intact.png` (Dinding kayu/batu kokoh 100% HP)
* **Palisade Wall Damaged:** `rampart_wall_damaged.png` (Retak & terbakar saat HP < 60%)
* **Palisade Wall Critical:** `rampart_wall_critical.png` (Runtuh & puing saat HP < 25%)
* **Turret Meriam Kembar:** `cannon_turret.png` & `ballista_turret.png`
* **Arcane Core:** `arcane_cube.png` / `arcane_rune_cube.png`

---

## 5. Katalog Monster & Musuh (`Assets/Art/Sprites/`)

* **Slime Gemetite:** `monster_slime_gemetite.png` / `monster_slime.png` (Monster dasar lambat, HP sedang)
* **Goblin Mire:** `monster_goblin_mire.png` / `monster_goblin_slime.png` (Monster cepat, swarm)
* **Obsidian Orc:** `monster_orc_obsidian.png` / `monster_demon.png` (Monster tank armor tebal)
* **Boss Malakor the Abyssal:** `monster_boss_malakor.png` / `monster_boss.png` (Boss raksasa ber-HP bar besar)

---

## 6. Katalog UI Glassmorphism & Gilded Royal Frame (`Assets/Art/UI/`)

* **Header Modal Emas:** `ui_modal_header_gilded.png`
* **Bingkai Kartu Perk Roguelike:** `ui_card_frame_gilded.png`
* **Peta Ekspedisi Perkamen:** `ui_worldmap_parchment.png`
* **Tombol 3D Empuk (Juicy):** `ui_btn_gold_solid.png` & `ui_btn_emerald_solid.png`
* **Badge Rarity:** `ui_emblem_common.png`, `ui_emblem_rare.png`, `ui_emblem_epic.png`, `ui_emblem_legendary.png`

# UNITY 2D ASSET GUIDE (untuk AI)

Dokumen ini adalah panduan bagi AI untuk membuat **aset 2D, sprite, dan animasi** yang siap dipakai di **Unity 2D (mobile)**. Baca seluruh dokumen sebelum membuat aset apa pun. Patuhi aturan di sini; jika ada konflik dengan permintaan user, tanyakan atau jelaskan dulu.

---

## PERAN: SUB-AGENT ASSET / SPRITE DESIGN

Dokumen ini dibaca oleh **sub-agent Asset/Sprite Design**, bukan oleh agent utama. Agent utama mengurus gameplay, kode, dan scene; sub-agent ini hanya mengurus **aset visual 2D**.

### Lingkup tugas

- Membuat dan memperbaiki sprite, spritesheet, ikon, UI art, background, dan efek visual.
- Menyiapkan aset agar siap diimpor ke Unity (nama, ukuran, pivot, metadata).
- Menulis skrip pendukung aset saja (skrip Editor untuk import, skrip Python untuk trim/normalisasi).

### Di luar lingkup (serahkan ke agent utama)

- Logika gameplay, aturan swap/merge, balancing, dan script C# runtime.
- Penyusunan scene, prefab, dan wiring komponen, kecuali diminta agent utama.
- Keputusan desain game (jumlah tier, stat, wave). Tanyakan ke agent utama bila butuh.

### Aturan non-destruktif (penting)

1. **Jangan menghapus atau menimpa** file/aset yang sudah ada, termasuk dokumen `.md` ini.
2. Perubahan pada aset lama dibuat sebagai **versi baru** (`gem_red_t0_v2.png`) atau di folder terpisah, lalu agent utama/user yang memutuskan penggantian.
3. Dokumen ini hanya boleh **ditambah** (bagian baru), tidak boleh dipangkas atau ditulis ulang.
4. Jangan mengubah nama file yang sudah dipakai di Unity, karena referensi (`.meta`/GUID) bisa rusak.

### Alur serah-terima dengan agent utama

**Input yang diharapkan dari agent utama:** daftar aset yang dibutuhkan, ukuran sel grid/ukuran tampil, PPU proyek, dan gaya visual terpilih.

**Output yang diserahkan:** ikuti **Bagian 11** (daftar file, spesifikasi, langkah import, asumsi, pertanyaan). Tambahkan satu baris status per aset: `selesai` / `perlu revisi` / `menunggu keputusan`.

**Bila ada yang tidak jelas:** tulis asumsi secara eksplisit dan lanjutkan, kecuali keputusan itu mengubah banyak aset (misalnya gaya visual atau ukuran kanvas); untuk itu minta konfirmasi dulu.

---

## 0. Konteks Proyek (isi/ubah sesuai proyek)

| Item | Nilai |
|---|---|
| Engine | Unity 2D (URP 2D atau Built-in 2D) |
| Platform | Mobile (portrait) |
| Genre | Puzzle swap-merge + tower defense ringan |
| Grid | 5x5 |
| Tier | T0 (gem) → T1, T2, T3 (token) → token jadi hero saat wave |
| Musuh | Monster per wave (10 wave) |
| Pertahanan | Barikade di bawah, 2 turret kiri & kanan |
| Bahasa UI | Indonesia (contoh: Benteng, Sisa Langkah, Mulai Tempur) |

> Jika proyek berbeda, ganti tabel ini dan sesuaikan daftar aset di Bagian 9.

---

## 1. Aturan Emas

1. **Satu gaya visual untuk semua aset.** Jangan campur gaya (flat vs realistis, outline tebal vs tanpa outline).
2. **Background wajib transparan** (PNG-32 dengan alpha). Tidak boleh ada background putih, kotak-kotak palsu, atau bayangan yang menyatu dengan background.
3. **Satu objek per file/frame**, ditaruh di tengah kanvas, dengan **padding seragam**.
4. **Ukuran kanvas seragam** untuk satu kategori aset (semua gem sama ukuran, semua token sama ukuran).
5. **Isi kanvas harus maksimal.** Objek mengisi sekitar 85–90% kanvas. Padding kosong berlebih membuat ikon tampak kecil di game.
6. **Pivot konsisten.** Gem/token/hero: Center. Karakter berdiri/monster: Bottom Center (kaki). Jangan ubah pivot antar frame animasi.
7. **Mudah dibaca di layar kecil.** Siluet jelas, kontras tinggi, detail halus dihindari.
8. **Jangan ada teks di dalam sprite** (teks UI dibuat di Unity, supaya bisa dilokalisasi).
9. **Beri nama file sesuai konvensi** (Bagian 5).
10. **Selalu keluarkan ringkasan spesifikasi** bersama aset (Bagian 11).

---

## 2. Spesifikasi Teknis

### 2.1 Format & ukuran

- Format: **PNG, RGBA 32-bit**, tanpa kompresi lossy.
- Ukuran kanvas: **kelipatan 4** (ideal power of two: 64, 128, 256, 512, 1024) agar kompresi tekstur efisien.
- Resolusi referensi untuk mobile:

| Kategori | Ukuran kanvas | Catatan |
|---|---|---|
| Gem / token (tile grid) | 256x256 | Tampil sekitar 120–200 px di layar |
| Hero / monster | 256x256 atau 512x512 | Per frame animasi |
| Boss | 512x512 – 1024x1024 | |
| Turret / barikade | 256x512 atau 512x256 | Sesuai bentuk |
| Efek (VFX) per frame | 128x128 – 256x256 | |
| Ikon UI | 128x128 | |
| Tombol / panel UI | Pakai **9-slice** (lihat 2.4) | |
| Background | 1080x1920 (portrait) | Atau 1440x2560 |

- Semua aset dibuat **2x lebih besar dari ukuran tampil terbesar** agar tetap tajam di layar high-DPI.

### 2.2 Pixels Per Unit (PPU)

- Tentukan **satu PPU global** untuk proyek, misalnya `100`.
- Rumus ukuran di dunia Unity: `ukuran_unit = piksel / PPU`.
- Contoh: sprite 256 px dengan PPU 100 = 2.56 unit.
- Ukuran sel grid di Unity harus dihitung dari sini. Jika ikon terasa kecil, **ubah PPU atau scale**, jangan menambah padding kosong di gambar.

### 2.3 Import Settings Unity (wajib)

| Setting | Nilai |
|---|---|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single (aset tunggal) / Multiple (spritesheet) |
| Pixels Per Unit | Sesuai PPU global |
| Mesh Type | Tight (default) atau Full Rect untuk aset UI |
| Pivot | Center / Bottom (sesuai Aturan Emas #6) |
| Generate Mip Maps | **Off** |
| Filter Mode | **Bilinear** (art halus) / **Point (no filter)** (pixel art) |
| Compression | Normal Quality (mobile: ASTC 6x6 atau 8x8) |
| Max Size | Sesuai ukuran asli, jangan lebih kecil dari file |
| Read/Write | Off |
| Alpha Is Transparency | On |

### 2.4 9-Slice (panel & tombol UI)

- Buat panel/tombol dengan sudut yang tidak terdistorsi saat di-scale.
- Atur **Border** (L/R/T/B) di Sprite Editor, lalu pakai Image Type = **Sliced**.
- Bagian tengah harus polos/tile-able (tanpa detail unik).

### 2.5 Sprite Atlas

- Kelompokkan sprite satu kategori ke **Sprite Atlas** (Create > 2D > Sprite Atlas) agar draw call rendah.
- Jangan campur sprite yang tidak tampil bersamaan dalam satu atlas besar.

---

## 3. Style Guide Visual (isi sebelum membuat aset)

AI harus menetapkan dan **menulis ulang** style guide ini di awal pekerjaan, lalu memakainya konsisten.

```
GAYA: [contoh: cartoon flat, chibi, outline tebal, warna cerah]
OUTLINE: [contoh: 4 px, warna gelap sewarna objek (bukan hitam murni)]
SHADING: [contoh: 2 tingkat (base + shadow), highlight kecil di kiri atas]
SUMBER CAHAYA: [contoh: kiri atas, konsisten di semua aset]
PALET: [daftar hex, maksimal 12–16 warna utama]
BENTUK: [contoh: sudut membulat, proporsi chibi 1:1 kepala-badan]
```

### Contoh palet gem T0 (kontras jelas satu sama lain)

| Warna | Hex utama | Hex bayangan |
|---|---|---|
| Merah | `#E53935` | `#A02020` |
| Biru | `#1E88E5` | `#1459A0` |
| Hijau | `#43A047` | `#2A6E2E` |
| Kuning | `#FDD835` | `#C2A100` |
| Ungu | `#8E24AA` | `#5C1673` |

**Aksesibilitas:** setiap gem harus bisa dibedakan juga lewat **bentuk** (bukan hanya warna): misalnya bulat, kotak, segitiga, hexagon, bintang.

### Progres visual tier (aset merge)

- **T0 (gem):** bentuk sederhana, polos.
- **T1:** gem + bingkai/aksesori sederhana.
- **T2:** lebih ornamen, ada glow ringan.
- **T3:** paling mewah, siluet paling besar, glow jelas.
- Naik tier harus **terlihat jelas dalam satu pandangan** lewat ornamen, bukan hanya perbedaan warna kecil.

---

## 3A. GAYA VISUAL RESMI PROYEK: RAMPARTBOUND

Bagian ini **mengisi** template style guide di Bagian 3 dan **berlaku sebagai acuan utama**. Sumber: *Rampartbound Master Specification v2.10.0* (layar judul / Title Screen). Jika ada konflik dengan contoh di Bagian 3, **Bagian 3A yang menang**.

### Nama gaya

**Soft-Shaded Chibi Anime Fantasy**: kartun fantasi cerah dengan karakter chibi imut, shading lembut, dan ornamen emas mewah.

### Ciri visual (diamati dari layar judul)

| Aspek | Ciri |
|---|---|
| Karakter | Chibi: kepala besar (sekitar 1/2 tinggi badan), mata besar berkilau, pipi merona, badan kecil, ekspresi ceria |
| Outline | Tebal, warna **gelap sewarna objek** (cokelat tua/biru tua), bukan hitam murni; ujung membulat |
| Shading | Lembut (soft-shaded): 2–3 tingkat dengan gradasi halus, bukan cel-shading keras; rim light tipis; highlight kecil mengilap |
| Warna | Jenuh, hangat, dan cerah; kontras jelas antara karakter dan latar |
| Bentuk | Membulat, ramah, siluet mudah terbaca |
| Material | Logam emas (bingkai, logo, tombol), permata mengilap berfaset, kain bertekstur ringan, kilau/glow ajaib (sparkle, partikel) |
| Latar | Lukisan fantasi lembut: langit biru berawan, hutan, bukit hijau, kastil batu bernuansa biru-abu; kedalaman lapisan (depan–tengah–belakang) |
| UI | Bingkai emas ornamen (scrollwork) dengan hiasan sudut, tombol emas mengilap dengan aksen berlian cyan, logo berbingkai bersayap |
| Elemen sihir | Kubus/rune berkilau hijau-zamrud dengan bingkai perunggu dan cahaya cyan |

### Palet acuan (perkiraan dari gambar; verifikasi dengan color picker pada gambar sumber)

| Peran | Hex perkiraan |
|---|---|
| Langit | `#7EC8F0` – `#BFE6FA` |
| Rumput / hutan | `#4E9A3A`, `#2F6B2A` |
| Emas (bingkai, tombol, logo) | `#F2B52E`, `#C78A12`, `#FFE08A` (highlight) |
| Merah (jubah kesatria) | `#C62828`, `#8E1B1B` |
| Biru (topi penyihir, kastil) | `#2F6DB5`, `#5C7FA6` |
| Zamrud (rune/sihir) | `#2EC27E`, `#1B8F5C` |
| Ungu/magenta (permata logo) | `#8E3FD0`, `#C04BCB` |
| Cyan (aksen berlian, glow) | `#4FD8F0` |
| Outline | `#2B2233` (ungu-gelap), atau tua sewarna objek |

### Karakter yang terlihat dalam spesifikasi

Nama berikut terbaca pada teks spesifikasi; **cocokkan dengan gambar sebelum dipakai** (pencocokan nama ke gambar adalah dugaan):

- **Pippin, Astral Runemage:** kelinci penyihir bertopi biru dengan tongkat sihir (kemungkinan).
- **Cedric, Emberblade Vanguard:** kesatria berkerudung merah dengan pedang dan perisai (kemungkinan).
- **Commander Rose** dan pahlawan lain disebut pada teks spesifikasi (daftar lengkap ada di dokumen sumber).

Seluruh pahlawan harus mengikuti gaya yang sama. Spesifikasi menyebut pahlawan lama diganti dengan **pahlawan chibi orisinil**; **jangan memakai desain referensi lama atau karakter dari game lain**.

### Aturan khusus gaya ini

1. **Konsisten antar aset:** kepala besar, mata besar berkilau, outline tebal berwarna, shading lembut, dan sumber cahaya dari kiri atas.
2. **Permata/gem:** mengilap, berfaset, dengan highlight putih kecil. Boleh dibingkai logam emas pada tier tinggi (T2, T3) agar progres tier terlihat.
3. **Progres tier:** makin tinggi tier, makin banyak ornamen emas, sayap kecil, dan glow.
4. **UI:** gunakan bahasa visual emas bersudut ornamen; panel dan tombol dibuat **9-slice** (Bagian 2.4). Bagian tengah panel dibuat polos agar bisa di-scale.
5. **Karakter:** pivot Bottom Center, kanvas seragam, animasi minimal `idle`, `attack`, `hit`, `death` (Bagian 6).
6. **Latar belakang:** boleh lebih detail (lukisan), tetapi tetap kontras lebih rendah daripada karakter agar unit dan tile menonjol.
7. **Pengecualian aturan "tanpa teks" (Aturan Emas #8):** hanya **logo judul** yang boleh memuat teks pada gambar. Teks tombol (misalnya "Mulai Tempur"), label, dan angka tetap dibuat sebagai teks di Unity.
8. **Warna gem harus tetap bisa dibedakan lewat bentuk**, bukan hanya warna (Bagian 3).

### Template prompt gambar gaya Rampartbound

**Awalan gaya (tempel di setiap prompt):**

```
soft-shaded chibi anime fantasy game art, thick colored outline (dark, not pure black),
soft gradient cel shading with small glossy highlights, rim light, vibrant saturated colors,
cute big-head proportions, large sparkling eyes, rounded shapes, golden ornate accents,
light from top-left, clean 2D mobile game asset, no text, no watermark
```

**Karakter:**

```
[awalan gaya], full body chibi [peran: red-hooded knight / bunny wizard / ...],
[senjata & kostum], cheerful expression, front 3/4 view, feet at bottom of frame,
isolated on plain magenta (#FF00FF) background
```

**Gem / token:**

```
[awalan gaya], a single glossy faceted [warna] gem, small white highlight top-left,
[T0: plain / T1: thin gold setting / T2: ornate gold frame with glow / T3: winged gold frame, strong glow],
centered, isolated on plain magenta background, mobile game icon
```

**Bingkai / tombol UI:**

```
[awalan gaya], ornate golden scrollwork [frame / button], glossy gold gradient,
cyan diamond accents, empty plain center for scaling, symmetrical,
isolated on plain magenta background, UI asset for 9-slice
```

**Latar:**

```
[awalan gaya], painterly fantasy landscape, [lokasi], soft clouds and layered depth,
lower contrast than foreground, portrait 1080x1920, no characters, no text
```

**Negative prompt tambahan:** `realistic proportions, dark grim style, pure black outline, harsh cel shading, flat vector without shading, photorealistic, 3D render, extra limbs, text, watermark, characters from other games`

---

## 4. Alur Kerja Membuat Sprite

1. **Baca brief** user dan style guide (Bagian 3).
2. **Buat daftar aset** (nama, ukuran, jumlah frame) sebelum menggambar.
3. **Buat 1 aset acuan** (master) per kategori dan minta persetujuan user.
4. **Buat sisa aset** mengikuti acuan (gaya, outline, palet identik).
5. **Bersihkan:** hapus background, trim sisa piksel, pastikan alpha bersih (tidak ada halo putih di tepi).
6. **Normalisasi kanvas:** tempatkan objek di tengah, padding seragam, ukuran kanvas sama.
7. **Ekspor** PNG dengan nama sesuai konvensi.
8. **Validasi** dengan checklist (Bagian 10).
9. **Serahkan** aset + ringkasan spesifikasi + panduan import Unity.

### Jika AI tidak bisa membuat gambar langsung

Hasilkan salah satu dari ini sebagai gantinya:

- **SVG** (vektor, lalu dikonversi ke PNG),
- **Skrip Python (Pillow)** yang menggambar sprite secara prosedural,
- **Prompt siap pakai** untuk generator gambar (Bagian 7) + instruksi pasca-proses.

---

## 5. Konvensi Penamaan & Struktur Folder

### Folder

```
Assets/
  Art/
    Sprites/
      Gems/
      Tokens/
      Heroes/
      Monsters/
      Defense/        (barikade, turret)
      VFX/
      UI/
      Backgrounds/
    Animations/
      Heroes/
      Monsters/
      VFX/
    Atlases/
```

### Nama file

Format: `kategori_nama_varian_[aksi]_[frame].png`

| Contoh | Arti |
|---|---|
| `gem_red_t0.png` | Gem merah tier 0 |
| `token_red_t2.png` | Token merah tier 2 |
| `hero_knight_idle_00.png` | Frame 0 animasi idle |
| `monster_slime_walk_03.png` | Frame 3 animasi jalan |
| `turret_left_fire_02.png` | Frame 2 turret menembak |
| `vfx_merge_burst_05.png` | Frame 5 efek merge |
| `ui_btn_start_normal.png` | Tombol Mulai (normal) |

Aturan: huruf kecil, `snake_case`, tanpa spasi, nomor frame **2 digit** (`00`, `01`, ...).

---

## 6. Animasi 2D

### 6.1 Pilih teknik animasi

| Teknik | Cocok untuk | Catatan |
|---|---|---|
| **Frame-by-frame (spritesheet)** | Karakter, monster, efek | Paling fleksibel, file lebih besar |
| **Skeletal / bone (Unity 2D Animation)** | Karakter dengan banyak gerakan | Butuh sprite dipisah per bagian |
| **Tween lewat kode (DOTween / LeanTween)** | Gem/token: swap, merge, jatuh, muncul | Tidak butuh banyak frame, sangat ringan |
| **Particle System** | Ledakan, kilau, debu | Sprite partikel kecil + tekstur gradien |

**Rekomendasi proyek puzzle:** gerakan tile (swap, jatuh, merge, spawn) memakai **tween kode**, bukan frame-by-frame. Frame-by-frame hanya untuk hero, monster, turret, dan efek.

### 6.2 Jumlah frame & FPS

| Animasi | Frame | FPS | Loop |
|---|---|---|---|
| Idle | 4–8 | 8–10 | Ya |
| Walk / Move | 6–8 | 10–12 | Ya |
| Attack | 5–8 | 12–15 | Tidak |
| Hit / Damaged | 2–4 | 12 | Tidak |
| Death | 6–10 | 10–12 | Tidak |
| Spawn / Muncul | 4–6 | 12 | Tidak |
| VFX merge / ledakan | 6–12 | 15–24 | Tidak |

### 6.3 Aturan animasi

1. **Semua frame satu animasi punya ukuran kanvas dan pivot identik.**
2. **Pose pertama dan terakhir pada animasi loop harus menyambung** (tidak ada lonjakan).
3. Objek **tidak boleh bergeser** antar frame kecuali memang bagian dari gerakan (gunakan root motion di Unity untuk perpindahan, bukan menggeser gambar).
4. Prinsip animasi: **anticipation → action → follow-through**, squash & stretch untuk kesan hidup.
5. Hindari efek yang menutupi karakter lebih dari 30%.
6. Frame penting (hit/impact) tahan 1–2 frame lebih lama.

### 6.4 Format spritesheet

- Susun frame **kiri ke kanan, atas ke bawah**, urutan sesuai nomor frame.
- Grid seragam (contoh 4x2 untuk 8 frame), sel berukuran sama.
- Ukuran total sheet idealnya ≤ 2048x2048 (maks 4096).
- Berikan metadata:

```
sheet: hero_knight_idle.png
frame_size: 256x256
columns: 4
rows: 2
frames: 8
fps: 10
loop: true
pivot: bottom_center
```

- Alternatif: kirim frame terpisah (`hero_knight_idle_00.png` dst.) agar user bisa menyusun sendiri di Unity.

### 6.5 Cara memakai di Unity

1. Sprite Mode = **Multiple** → Sprite Editor → Slice > **Grid By Cell Size** (isi ukuran frame) → Apply.
2. Pilih semua sub-sprite → seret ke Scene → Unity membuat **Animation Clip** + **Animator Controller**.
3. Atur **Sample Rate** clip sesuai FPS, centang **Loop Time** sesuai tabel.
4. Hubungkan state di Animator (Idle ↔ Walk ↔ Attack) dengan parameter/trigger.
5. Untuk event (misalnya damage saat frame hit), tambahkan **Animation Event** pada frame terkait.

---

## 7. Template Prompt untuk Generator Gambar

Gunakan struktur ini. Selalu sertakan **gaya**, **sudut pandang**, **background transparan/polos**, dan **negative prompt**.

### Struktur umum

```
[subjek], [deskripsi bentuk & warna], [gaya dari style guide],
[outline], [shading], [sumber cahaya],
centered, isolated on plain solid [warna kontras] background (untuk dihapus nanti),
front view / side view, game asset, 2D sprite, clean edges,
high contrast, readable at small size, no text, no watermark
```

**Negative prompt:** `text, watermark, signature, blurry, noisy, multiple objects, cropped, cut off, realistic photo, 3D render, background scenery, cast shadow on ground, low contrast`

### Contoh: gem T0

```
A single glossy red round gem, faceted, bold dark-red outline 4px,
two-tone cel shading, small white highlight at top-left,
centered, isolated on plain magenta (#FF00FF) background,
flat 2D mobile game icon, front view, clean edges, no text
```

### Contoh: token T2 (hasil merge)

```
A red gem set inside a golden ornate frame with small wings,
soft red glow, same style as the T0 gem (bold outline, two-tone cel shading),
centered, isolated on plain magenta background, 2D game icon,
clearly more elaborate than a basic gem, no text
```

### Contoh: monster

```
Cute but menacing green slime monster, side view facing left,
thick dark-green outline, two-tone cel shading, big angry eyes,
full body visible, feet touching bottom of the frame,
isolated on plain magenta background, 2D game sprite, no text
```

### Contoh: frame animasi (untuk konsistensi)

```
Same character as reference image, [pose: walk cycle frame 3 of 8, left foot forward],
identical outline, palette, proportions and camera angle,
isolated on plain magenta background, no text
```

> **Tips konsistensi:** pakai gambar acuan (image reference), seed tetap, dan buat semua frame dalam satu sesi. Lakukan perubahan kecil antar frame, bukan menggambar ulang dari nol.

### Hapus background

- Gunakan warna kunci yang tidak ada di sprite (magenta `#FF00FF` atau hijau `#00FF00`), lalu hapus lewat skrip/editor.
- Setelah dihapus, **periksa tepi** (halo) dan lakukan *defringe* jika perlu.

---

## 8. Masalah Umum & Solusi

| Gejala di Unity | Penyebab umum | Solusi |
|---|---|---|
| **Ikon gem terlihat kecil** | Padding kosong besar di kanvas; PPU terlalu besar; scale sel tidak sesuai | Trim lalu isi kanvas 85–90%; hitung scale dari ukuran sel grid; sesuaikan PPU |
| **Sprite tidak sejajar di sel grid** | Pivot tidak Center; ukuran kanvas berbeda antar sprite; posisi sel dihitung dari sudut, bukan tengah | Samakan kanvas & pivot Center; posisikan objek di tengah sel (`origin + (x + 0.5) * cellSize`) |
| **Sprite blur** | Mip Maps On; Filter Mode salah; scale non-integer | Mip Maps Off; Point untuk pixel art; atur Max Size = ukuran asli |
| **Tepi ada garis putih/hitam** | Alpha tidak bersih; premultiplied alpha | Defringe; Alpha Is Transparency On; ekspor ulang |
| **Animasi bergetar/melompat** | Pivot/ukuran frame beda; objek bergeser antar frame | Samakan kanvas & pivot; stabilkan posisi objek |
| **Urutan tampil salah (tumpang tindih)** | Sorting Layer / Order in Layer belum diatur | Buat Sorting Layer: Background < Grid < Tile < Unit < VFX < UI |
| **Tile tidak bisa di-swap / tidak bisa diklik** | Bukan masalah aset: biasanya **Collider2D** tidak ada/terlalu kecil, **Camera** tanpa `Physics2DRaycaster`, `EventSystem` hilang, atau collider tertutup UI | Pastikan tiap tile punya `BoxCollider2D` seukuran sel; cek raycast & input; periksa overlay UI yang menutup grid |
| **Ukuran beda antar perangkat** | Kamera orthographic tidak diatur terhadap rasio layar | Atur `orthographicSize` / Canvas Scaler (Scale With Screen Size, referensi 1080x1920) |

---

## 9. Daftar Aset Minimum (contoh proyek swap-merge)

### Tile (kanvas 256x256, pivot Center)

- [ ] `gem_[warna]_t0` x 5 warna
- [ ] `token_[warna]_t1`, `_t2`, `_t3` x 5 warna
- [ ] Bingkai sel grid + latar papan (`ui_cell_bg`, `ui_board_frame`)
- [ ] Highlight pilihan / hint (`ui_cell_select`)

### Karakter (spritesheet, pivot Bottom Center)

- [ ] Hero per tier/warna: `idle`, `attack`, `hit`, `death`
- [ ] Monster (min. 3 jenis + 1 boss): `walk`, `attack`, `hit`, `death`

### Pertahanan

- [ ] Barikade: kondisi 100% / 66% / 33% / hancur
- [ ] Turret kiri & kanan: `idle`, `fire`

### Efek (VFX)

- [ ] `vfx_merge_burst` (3-match, 4-match, 5-match: makin besar)
- [ ] `vfx_tier_up`, `vfx_hit_spark`, `vfx_explosion`, `vfx_projectile`
- [ ] `vfx_gem_pop` (hilang), `vfx_spawn_glow`

### UI

- [ ] Tombol (normal / pressed / disabled): Mulai Tempur, Ulangi, Lanjut
- [ ] Ikon: Langkah (move), Nyawa/Benteng, Wave, Pause, Pengaturan
- [ ] Panel 9-slice, bar HP, bar progres wave

### Latar

- [ ] Background per tema/area (1080x1920), parallax opsional

---

## 10. Checklist QA (cek sebelum menyerahkan)

**Visual**
- [ ] Gaya, outline, dan palet konsisten di semua aset
- [ ] Siluet terbaca pada ukuran 64 px
- [ ] Warna gem dapat dibedakan (juga untuk buta warna: bentuk berbeda)
- [ ] Naik tier terlihat jelas

**Teknis**
- [ ] PNG RGBA, background transparan, tanpa halo
- [ ] Ukuran kanvas sama dalam satu kategori, kelipatan 4
- [ ] Objek di tengah, padding seragam, mengisi 85–90% kanvas
- [ ] Pivot sesuai aturan
- [ ] Tidak ada teks/watermark di sprite
- [ ] Nama file sesuai konvensi

**Animasi**
- [ ] Semua frame berukuran sama, pivot sama
- [ ] Loop menyambung mulus
- [ ] FPS & jumlah frame sesuai tabel
- [ ] Metadata spritesheet disertakan

**Unity**
- [ ] Import settings sesuai Bagian 2.3
- [ ] Sorting Layer sudah ditentukan
- [ ] Sprite Atlas dibuat per kategori

---

## 11. Format Jawaban AI Saat Menyerahkan Aset

Selalu sertakan:

1. **Daftar file** (nama, ukuran, jumlah frame).
2. **Spesifikasi:** PPU yang disarankan, pivot, filter mode, FPS, loop.
3. **Ukuran tampil yang disarankan** di Unity (dalam unit & piksel).
4. **Langkah import** singkat (slicing, animation clip).
5. **Catatan/asumsi** (misalnya "palet diubah menjadi X agar kontras").
6. **Hal yang perlu keputusan user** (maksimal 1–2 pertanyaan).

---

## 12. Skrip Otomatis: Import Settings (Editor)

Letakkan di `Assets/Editor/SpriteImportPostprocessor.cs` agar semua sprite baru otomatis memakai pengaturan yang benar.

```csharp
using UnityEditor;
using UnityEngine;

public class SpriteImportPostprocessor : AssetPostprocessor
{
    const float PPU = 100f;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/Sprites")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = PPU;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = false;
        importer.filterMode = FilterMode.Bilinear; // Point untuk pixel art
        importer.textureCompression = TextureImporterCompression.Compressed;

        // Pivot default: Center. Karakter (Heroes/Monsters): Bottom Center
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center;

        if (assetPath.Contains("/Heroes/") || assetPath.Contains("/Monsters/"))
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;

        importer.SetTextureSettings(settings);
    }
}
```

---

## 13. Skrip Bantu: Trim & Normalisasi Kanvas (Python + Pillow)

Memperbaiki masalah "ikon terlalu kecil" dan "tidak sejajar": memangkas ruang kosong, lalu menaruh objek di tengah kanvas seragam.

```python
from PIL import Image
import os, sys

CANVAS = 256          # ukuran kanvas akhir
FILL = 0.88           # objek mengisi 88% kanvas

def normalize(src, dst):
    img = Image.open(src).convert("RGBA")
    bbox = img.getbbox()                 # area non-transparan
    if bbox is None:
        return
    img = img.crop(bbox)

    target = int(CANVAS * FILL)
    scale = target / max(img.size)
    new_size = (max(1, round(img.width * scale)), max(1, round(img.height * scale)))
    img = img.resize(new_size, Image.LANCZOS)

    canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    x = (CANVAS - img.width) // 2
    y = (CANVAS - img.height) // 2       # untuk pivot Bottom: y = CANVAS - img.height - padding
    canvas.paste(img, (x, y), img)
    canvas.save(dst, "PNG")

if __name__ == "__main__":
    in_dir, out_dir = sys.argv[1], sys.argv[2]
    os.makedirs(out_dir, exist_ok=True)
    for name in os.listdir(in_dir):
        if name.lower().endswith(".png"):
            normalize(os.path.join(in_dir, name), os.path.join(out_dir, name))
```

> Untuk **animasi**, jangan trim per frame. Hitung satu bounding box gabungan semua frame, lalu pakai crop & posisi yang sama untuk setiap frame agar tidak bergetar.

---

## 14. Ringkasan Cepat (cheat sheet)

- Transparan, satu objek, tengah kanvas, padding seragam.
- Kanvas seragam per kategori, kelipatan 4, isi 85–90%.
- PPU global, Mip Maps Off, pivot konsisten.
- Gaya, outline, palet, dan arah cahaya selalu sama.
- Gem: beda bentuk **dan** warna. Tier: beda ornamen yang jelas.
- Animasi tile → tween kode. Karakter/VFX → spritesheet.
- Semua frame satu animasi: ukuran & pivot identik, loop menyambung.
- Nama `snake_case`, frame 2 digit.
- Serahkan aset + spesifikasi + langkah import.

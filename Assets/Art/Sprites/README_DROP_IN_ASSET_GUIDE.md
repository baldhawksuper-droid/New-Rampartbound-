# PANDUAN LENGKAP DROP-IN ASSET & KATALOG SAMPLE SPRITE
## Rampartbound: Merge Defense (Stylized Citadel Fantasy)

Dokumen ini disusun khusus oleh **BaldHawk (Lead Project Manager)** bersama **Divisi Asset & Art Specialist** untuk menjawab seluruh kebingungan terkait aset visual: di mana file berada, apa makna gambar tersebut, dan bagaimana cara menimpa/menumpuk file desain baru secara aman tanpa merusak proyek Unity.

---

## 1. Lokasi Folder & Struktur Aset Visual

Seluruh aset gambar 2D game tersimpan di dalam folder:
`D:\PROJECT GAMES\Rampartbound\Assets\Art\Sprites\`

Terdapat 4 subfolder utama:
1. `Heroes/` : Sprite karakter 4 Hero utama di setiap tingkatan Tier (T1, T2, T3).
2. `Grid/` : Petak arena tempur, hover highlight, dan Token Medallion mode persiapan.
3. `Monsters/` : Sprite monster musuh (Creep, Brute, Boss).
4. `UI/` : Bingkai 9-slice, tombol, icon komoditas ekonomi, banner, dan border kelangkaan.

---

## 2. Katalog Lengkap Seluruh Sample Sprite yang Sudah Tersedia

Berikut adalah daftar inventaris lengkap file yang sudah aktif di dalam proyek:

### A. Quartet Heroes (`Assets/Art/Sprites/Heroes/`)
| Nama File | Karakter | Tingkat Tier | Makna & Visual Karakter |
| :--- | :--- | :--- | :--- |
| `Cedric_T1.png` | Cedric (The Red Knight) | Tier 1 | Frontline Striker. Jubah merah, baju zirah baja, pedang arming sword tunggal. |
| `Cedric_T2.png` | Cedric (The Red Knight) | Tier 2 | Dual Slash. Pedang ganda menyilang (*dual blades*), zirah dengan ornamen emas. |
| `Cedric_T3.png` | Cedric (The Red Knight) | Tier 3 | Blade Tempest. Zirah ksatria agung penuh aura api menyala, pedang besar bersinar. |
| `Rose_T1.png` | Rose Tanker | Tier 1 | Heavy Frontline. Kuncir rambut pink, perisai baja bundar, gada/palu besi. |
| `Rose_T2.png` | Rose Tanker | Tier 2 | Shield Slam. Perisai rose gold lebih tebal dengan duri pelindung (*Thorn Guard*). |
| `Rose_T3.png` | Rose Tanker | Tier 3 | Earthshaker. Zirah pelat mawar megah, tameng raksasa bercahaya, palu perang bercahaya. |
| `RabbitMage_T1.png` | Rabbit Mage | Tier 1 | Ranged AoE. Kelinci putih menggemaskan, jubah & topi penyihir biru, tongkat kayu berkilau. |
| `RabbitMage_T2.png` | Rabbit Mage | Tier 2 | Mystic Burst. Jubah bersulam kristal cyan, tongkat sihir berujung bola energi sihir. |
| `RabbitMage_T3.png` | Rabbit Mage | Tier 3 | Meteor Strike. Mahkota sihir kosmik, jubah biru tua bintang-bintang, staf meteor kristal. |
| `WindRanger_T1.png` | Wind Ranger | Tier 1 | Long-Range Physical. Jubah berkerudung hijau zamrud, busur panah komposit ramping. |
| `WindRanger_T2.png` | Wind Ranger | Tier 2 | Piercing Gale. Jubah berbulu elang, busur berukir angin siklon, anak panah berhulu perak. |
| `WindRanger_T3.png` | Wind Ranger | Tier 3 | Zephyr Tornado. Sayap angin bercahaya hijau lembut, busur badai angin sakti. |

---

### B. Grid & Token Medallion (`Assets/Art/Sprites/Grid/`)
| Nama File | Fungsi | Keterangan & Makna Visual |
| :--- | :--- | :--- |
| `Tile_Empty.png` | Petak Grid Standar | Petak batu benteng abu-abu bersih (dasar arena). |
| `Tile_Hover.png` | Petak Aktif / Hover | Petak dengan tepian menyala lembut saat disentuh atau dilewati unit. |
| `Token_Medallion_Cedric.png` | Token Persiapan Cedric | Medali bulat berbingkai emas dengan lambang pedang merah (mode puzzle persiapan). |
| `Token_Medallion_Rose.png` | Token Persiapan Rose | Medali bulat berbingkai rose-gold dengan lambang mawar & palu pink. |
| `Token_Medallion_Rabbit.png` | Token Persiapan Rabbit | Medali bulat berbingkai perak-kristal dengan lambang rune sihir biru. |
| `Token_Medallion_Wind.png` | Token Persiapan Wind Ranger | Medali bulat berbingkai emas-daun dengan lambang sayap angin zamrud. |

---

### C. Abyssal Monsters (`Assets/Art/Sprites/Monsters/`)
| Nama File | Jenis Monster | Makna & Perilaku dalam Pertempuran |
| :--- | :--- | :--- |
| `Monster_Creep.png` | Abyssal Minion (Kroco) | Monster gerombolan berkaki banyak, HP rendah, bergerak cepat menuju benteng. |
| `Monster_Brute.png` | Armored Orc Berserker | Monster raksasa bertameng baja tebal, tahan damage, memiliki aura percepatan. |
| `Monster_Boss.png` | Abyssal Flame Drake | Bos naga kegelapan bernapas api, pemicu gempa tanah (*Ground Smash*) & pemanggil minion. |

---

### D. User Interface (UI) & Komoditas Ekonomi (`Assets/Art/Sprites/UI/`)
| Nama File | Kategori | Penggunaan di Layar |
| :--- | :--- | :--- |
| `Frame_StoneBastion_9Slice.png` | Panel Frame | Dasar jendela modal pop-up bermotif dinding batu benteng kokoh (9-Slice). |
| `Frame_OrnateGold_9Slice.png` | Header / Frame | Bingkai ornamen emas kerajaan di sekitar kartu hero dan jendela utama. |
| `Btn_Primary_Gold.png` | Tombol Utama | Tombol aksi utama (Summon, Klaim, Upgrade, Masuk Battle). |
| `Btn_Primary_Gold_Pressed.png` | Tombol (Tekan) | Umpan balik visual saat tombol ditekan (efek mengkerut/gelap sedikit). |
| `Btn_Secondary_Stone.png` | Tombol Kedua | Tombol sekunder (Batal, Reset, Filter, Kembali). |
| `Btn_Secondary_Stone_Pressed.png`| Tombol Kedua (Tekan)| Status tertekan tombol sekunder. |
| `Btn_Close_X.png` | Tombol Tutup | Tombol silang [X] sudut kanan atas modal. |
| `Icon_Gold_Coin.png` | Komoditas | Koin emas kerajaan untuk upgrade level hero dan tempa senjata di Relic Forge. |
| `Icon_Gem_Blue.png` | Komoditas | Permata kristal biru untuk summon hero di arena tempur & gacha Shard. |
| `Icon_Shard_Purple.png` | Komoditas | Pecahan jiwa ungu untuk promosi bintang hero (1★ s/d 5★). |
| `Icon_Scrap_Iron.png` | Komoditas | Besi rongsok hasil salvage perlengkapan untuk upgrade gear di Forge. |
| `Icon_Energy_Cyan.png` | Komoditas | Stamina/Energi pertempuran (citadel stamina). |
| `Border_Rarity_Common.png` | Rarity Border | Bingkai warna abu-abu baja untuk perlengkapan Common. |
| `Border_Rarity_Rare.png` | Rarity Border | Bingkai warna biru safir untuk perlengkapan Rare. |
| `Border_Rarity_Epic.png` | Rarity Border | Bingkai warna ungu mistik untuk perlengkapan Epic. |
| `Border_Rarity_Legendary.png` | Rarity Border | Bingkai warna emas bercahaya untuk perlengkapan Legendary. |
| `Title_Screen_Backdrop.png` | Latar Belakang | Gambar pemandangan benteng megah Citadel di bawah langit senja. |
| `Title_Btn_Tap_To_Start.png` | Tombol Judul | Banner ornamen bertuliskan 'SENTUH UNTUK MEMULAI'. |
| `Hand_Cursor.png` | Tutorial Pointer | Tangan penunjuk animasi untuk panduan interaktif First-Time User (FTUE). |

---

## 3. Cara Menumpuk / Mengganti File Desain Baru (The Drop-In Workflow)

Sistem telah dibangun dengan prinsip **Preservative Asset Architecture**. Anda tidak perlu membuka Unity atau menata ulang inspector jika hanya ingin mengganti gambar.

### Langkah-langkah Praktis:
1. **Siapkan Gambar Baru Anda:**
   * Format: **PNG** (dengan latar transparan / RGBA).
   * Ukuran Resolusi yang Direkomendasikan:
     - Hero Unit / Monsters: `128x128` atau `256x256` pixel.
     - Token Medallion: `128x128` pixel (lingkaran simetris).
     - Icon UI / Border: `64x64` atau `128x128` pixel.
     - Latar Belakang Layar: `1080x1920` pixel (Portrait).
2. **Beri Nama File Sama Persis:**
   * Contoh: Jika ingin mengganti sprite Cedric Tier 1, beri nama file baru Anda `Cedric_T1.png`.
3. **Copy & Paste (Replace/Tumpuk File Lama di Windows Explorer):**
   * Masuk ke folder tujuan (misal `Assets/Art/Sprites/Heroes/`).
   * Tempelkan (*paste*) file baru Anda.
   * Pilih **"Replace the file in the destination"** (Ganti file tujuan).
4. **HUKUM UTAMA: JANGAN SENTUH FILE `.meta`!**
   * Di sebelah `Cedric_T1.png` terdapat file `Cedric_T1.png.meta`.
   * **Jangan dihapus, jangan di-rename, jangan dipindahkan.**
   * File `.meta` ini menyimpan identitas unik (GUID) yang menghubungkan gambar tersebut ke kode C#, prefab, dan scene Unity. Selama file `.meta` tetap ada, gambar baru akan langsung tampil otomatis di dalam game!
5. **Buka Unity / Simpan Proyek:**
   * Unity akan secara otomatis mengimpor gambar baru dengan pengaturan yang sudah dikalibrasi (Sprite 2D, Pixels Per Unit: 100).

---

## 4. Validasi Otomatis (1-Click Quality Check)

Jika Anda ragu apakah aset yang ditumpuk sudah terpasang dengan benar:
1. Buka Unity Editor.
2. Pada bilah menu atas, klik:  
   **Rampartbound -> Assets -> Audit & Auto-Fix Asset Settings**
3. Sistem akan otomatis memindai seluruh folder `Assets/Art/Sprites/`, memastikan setiap sprite berstatus `TextureType: Sprite (2D and UI)`, `Pixels Per Unit: 100`, dan `Alpha Is Transparency: True`.

Dengan panduan ini, tim desain grafis dan artis 2D dapat bekerja dengan leluasa dan percaya diri menyuntikkan karya seni terbaik ke dalam game!

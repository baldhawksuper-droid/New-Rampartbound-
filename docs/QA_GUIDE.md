# Rampartbound: Merge Defense — Panduan Pengujian QA (WebGL)

Dokumen ini merupakan panduan resmi pengujian fungsionalitas dan kualitas (Quality Assurance) game **Rampartbound: Merge Defense** melalui browser (WebGL).

---

## 1. Tautan Game (WebGL)

Game dapat langsung dimainkan tanpa instalasi melalui browser modern (Chrome, Edge, Firefox, Safari) di desktop maupun mobile:

- **URL Game (GitHub Pages):** [https://baldhawksuper-droid.github.io/New-Rampartbound-/](https://baldhawksuper-droid.github.io/New-Rampartbound-/)
- **Orientasi:** Portrait (1080x1920). Pada desktop browser, game akan tampil secara *letterbox* vertikal di tengah layar.
- **Rekomendasi Browser:** Google Chrome / Microsoft Edge versi terbaru dengan akselerasi perangkat keras (Hardware Acceleration) aktif.

---

## 2. Cara Melaporkan Bug

Setiap kali menemukan kendala visual, logika, atau performa, buat laporan bug baru di tab **GitHub Issues** menggunakan format standar berikut:

### Format Laporan Bug (Bug Report Template)
```markdown
### [BUG] Ringkasan Masalah Singkat & Padat

- **Versi & Nomor Build:** (Lihat di pojok bawah Title Screen, contoh: v1.2.18 · 20261107-f3b3784)
- **Perangkat & Browser:** (Contoh: PC Windows 11 - Chrome v131 / Android - Chrome Mobile)
- **Kategori:** Gameplay / Visual UI / Audio / Metagame

#### Langkah Reproduksi (Steps to Reproduce):
1. Buka game di browser dan masuk dari Title Screen ke Citadel Lobby.
2. Tekan tombol BATTLE untuk masuk ke Arena Pertempuran.
3. Lakukan swap pada token ... ke arah ...
4. Amati apa yang terjadi pada grid/hero.

#### Hasil yang Diharapkan (Expected Result):
Token tertukar dan terjadi match 3 hero, memunculkan unit tempur di barikade.

#### Hasil Aktual (Actual Result):
Token tertukar tetapi animasi membeku dan monster tidak menerima damage.

#### Bukti Tangkapan Layar / Rekaman (Screenshots / Video):
(Sematkan screenshot tampilan layar penuh atau rekaman GIF/MP4)

#### Log Browser Console (Jika Ada):
Tekan F12 -> Console -> Salin teks error merah jika muncul Exception.
```

---

## 3. Daftar Skenario Uji Awal (Sanity & Smoke Test Checklist)

Lakukan pengujian terstruktur berdasarkan checklist 8 skenario utama di bawah ini:

| No | Skenario Uji | Prosedur Pengujian | Kriteria Lolos (Pass) |
|---|---|---|---|
| **1** | **Swap Tile (Sentuh/Klik & Geser)** | Klik/sentuh token Match-3 pada grid papan arena, lalu geser secara horizontal atau vertikal ke slot tetangga. | • Token berpindah posisi secara mulus dengan animasi *tween*.<br>• Jika perpindahan tidak menghasilkan *match*, token harus kembali ke posisi semula (*revert swap*). |
| **2** | **Match-3 Standar** | Sejajarkan 3 token dengan tipe dan tier yang sama (misal: 3 Token Biru / Pippin T1). | • Ketiga token terhapus dari grid.<br>• Muncul proyektil/efek panggil hero yang mengisi barisan pertahanan.<br>• Token baru jatuh dari atas mengisi kekosongan (*gravity drop*). |
| **3** | **Match-4 & Match-5** | Cocokkan 4 token dalam satu garis lurus atau 5 token dalam formasi L / T / garis. | • Terbentuk token spesial / bonus Mana Mana Core.<br>• Partikel ledakan merge lebih besar dan memicu serangan spesial hero. |
| **4** | **Sistem Tier-Up (Merge Progression)** | Gabungkan unit atau token bertingkat rendah (T1 -> T2 -> T3 hingga batas maksimal T5). | • Unit berubah bentuk visual sesuai tier progresinya (senjata & armor lebih megah).<br>• Combat Power (CP) dan damage serangan unit meningkat signifikan sesuai master spesifikasi. |
| **5** | **Wave Monster Abyssal** | Mulai gelombang pertempuran dan amati gerbang portal musuh di bagian atas layar. | • Monster musuh keluar dari portal secara berkala.<br>• Monster bergerak menyusuri koridor jalan setapak menuju barikade benteng.<br>• Bar HP monster berkurang saat diserang proyektil hero hingga monster tumbang. |
| **6** | **Integritas Barikade Benteng** | Biarkan monster mendekat hingga menabrak barikade kayu/batu di bagian bawah arena. | • Monster tertahan di depan barikade dan melakukan animasi serang.<br>• Barikade menerima damage dan bar HP benteng/barikade berkurang.<br>• Efek visual retak/partikel muncul pada barikade. |
| **7** | **Kondisi Kemenangan (Victory Screen)** | Kalahkan seluruh monster di semua wave stage pertempuran. | • Pertempuran berhenti dan memunculkan modal perayaan *VICTORY*.<br>• Menampilkan bintang kemenangan (1-3 bintang), perolehan Gold, dan Exp.<br>• Tombol navigasi kembali ke Citadel Lobby berfungsi lancar. |
| **8** | **Kondisi Kekalahan (Defeat Screen)** | Biarkan barikade hancur dan HP benteng pemain terkuras habis hingga 0. | • Muncul layar *DEFEAT / PERTAHANAN JEBOL* dengan opsi coba lagi (*Retry*) atau kembali ke benteng (*Return to Citadel*).<br>• Tidak ada crash atau freezing pada gameplay loop. |

---

## 4. Catatan Teknis WebGL untuk Tim QA

1. **Audio WebGL Policy:** Browser modern mewajibkan interaksi pengguna (klik/sentuhan pertama) sebelum audio dapat diputar. Musik latar belakang (BGM) akan mulai terdengar setelah tombol "TAP TO START" ditekan pada Title Screen.
2. **Penyimpanan Lokal (Save Data):** Progres akun pemain pada WebGL disimpan di *IndexedDB* browser. Jika cache/data browsing dihapus, data akun akan kembali ke akun tamu baru (*Guest Level 1*).
3. **Run in Background:** Game telah dikonfigurasi agar tetap aktif (*Run in Background*) saat Anda berpindah tab browser, sehingga proses pertempuran tidak terhenti mendadak.

# UX Audit Report: Combat Pacing & Player Agency Overhaul
**Dari:** User Experience & Player Engagement Specialist (`ux_specialist`)  
**Kepada:** Mobile QA (`qa_build_engineer`)  
**Diteruskan ke:** Core Game Developer (`game_developer`)  
**Status:** CRITICAL ACTION REQUIRED  

---

## 1. Temuan Masalah Inti (Problem Statement)
> *"Level terasa terlalu cepat dan tidak ada game yang dimainkan."*

Setelah audit menyeluruh terhadap prototipe pertempuran, UX Specialist menemukan 4 faktor utama penyebab masalah ini:

1. **Ketiadaan Fase Persiapan (Zero Runway):**  
   Begitu tombol "Mulai" ditekan, monster langsung muncul seketika di layar. Pemain tidak memiliki waktu untuk melihat papan grid, merencanakan posisi, atau melakukan summon awal.
2. **Durasi Gelombang Terlalu Singkat (5–8 Detik):**  
   Wave 1 hanya memuat 5–8 monster lemah yang mati dalam beberapa detik. Akibatnya, pemain hanya sempat melakukan 1 kali klik summon dan 0 kali merge, lalu wave sudah tiba-tiba berakhir. Ini membuat game terasa seperti *benchmark* otomatis alih-alih game yang dimainkan secara aktif.
3. **Keterbatasan Mana (Mana Starvation):**  
   Pemain tidak memiliki aliran mana yang cukup selama pertempuran berlangsung. Tangan pemain menganggur karena harus menunggu monster mati untuk dapat mana sedikit.
4. **Ketiadaan Aksi Taktis Aktif (No Direct Player Agency):**  
   Pemain hanya bisa pasrah melihat tembakan otomatis tanpa adanya tombol intervensi darurat (seperti skill manual *Meteor* / *Petir* saat monster hampir menyentuh benteng).

---

## 2. Rencana Perbaikan Konkret (Actionable Remediation Specs)

Untuk mengubah game menjadi pengalaman yang intens, seru, dan adiktif seperti *SupaMerge* asli, Developer diinstruksikan menerapkan perubahan berikut:

### A. Fase Persiapan 3 Detik (Tactical Preparation Phase)
* Saat battle dimulai, monster **TIDAK** langsung keluar.
* Ada tulisan animasi: **"PERSIAPAN PERTAHANAN (3... 2... 1...)"**.
* Pemain diberikan **Starting Mana 150** sehingga bisa langsung memanggil 3–4 hero awal dan melakukan merge ke Tier 2 sebelum monster pertama tiba.

### B. Ritme Durasi Wave Standar Industri (35–50 Detik per Wave)
* Monster tidak keluar sekaligus, melainkan dalam **3 Gelombang Mini (Swarm Waves)**:
  - *Swarm 1 (Detik 0–12):* Monster pengintai cepat (Fast Scouts).
  - *Swarm 2 (Detik 15–28):* Pasukan reguler berkerumun.
  - *Swarm 3 (Detik 30–45):* Monster berzirah tebal (Armored Vanguard) atau Boss.
* Pacing ini memberi pemain waktu sekitar **40 detik penuh** untuk terus berpikir, memanggil hero baru, dan menggabungkan tier sambil memantau barisan musuh yang mendekat.

### C. Aliran Mana Otomatis (+2 Mana Tiap Detik)
* Selain dari membunuh musuh, berikan regenerasi pasif **+2 Mana per detik**.
* Hal ini memastikan bahwa tangan pemain **tidak pernah menganggur**; setiap 10–15 detik pemain selalu bisa memanggil unit baru dan mencari pasangan merge di grid.

### D. Skill Interaktif Pemain: Serangan Badai Meteor (Meteor Strike)
* Tambahkan tombol kemampuan aktif manual: **☄️ Meteor Strike** (Cooldown 20 detik, biaya 0 mana).
* Pemain bisa menekan tombol ini untuk membakar monster di area darurat ketika musuh sudah terlalu dekat ke dinding benteng.

### E. Indikator Kemajuan Gelombang (Wave Progress Bar)
* Menampilkan bar persentase di bagian atas (misal: "Wave Progress: 45%") sehingga pemain merasakan eskalasi ketegangan hingga wave tuntas.

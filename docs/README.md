# BaldHawk Telegram Bot 🦅

Asisten AI Telegram cerdas dan komunikatif yang terhubung ke [@BaldHawk_Bot](https://t.me/BaldHawk_Bot).

---

## 🌟 Fitur Utama

1. **Komunikatif Tanpa Menu:**
   * Anda bisa langsung mengobrol, bertanya, atau berdiskusi secara natural tanpa memilih menu.
2. **Dukungan Semua Jenis Dokumen & Gambar:**
   * 🖼️ **Gambar:** JPEG, PNG, WEBP, BMP, GIF (screenshot, sketsa konsep, diagram alur, mockup).
   * 📄 **Dokumen Teks & Kantor:** PDF, Microsoft Word (`.docx`), Microsoft Excel (`.xlsx`), CSV.
   * 💻 **File Kode & Konfigurasi:** `.py`, `.js`, `.ts`, `.json`, `.html`, `.css`, `.xml`, `.yaml`, `.sql`, `.txt`, `.md`, dll.
3. **Tanpa Streaming Token AI (Jawaban Lengkap Sekaligus):**
   * **Respons 1:** Konfirmasi penerimaan berkas/pesan secara instan (`⏳ Sedang membaca dan memproses dokumen Anda...`).
   * **Indikator Aktif:** Menjaga status `typing...` tetap aktif saat AI memproses.
   * **Respons 2:** Mengirimkan hasil analisis dokumen lengkap tanpa token streaming yang berkedip.
4. **Memori Percakapan Multi-turn:**
   * Bot mengingat konteks dokumen dan obrolan sebelumnya.
   * Perintah `/clear` untuk membersihkan riwayat obrolan.

---

## 📁 Struktur Folder Proyek

```
NewProjectGames/
├── config/                 # ⚙️ Pengaturan & token (.env)
│   ├── .env                # Token Telegram & pesan konfigurasi
│   ├── .env.example        # Template konfigurasi
│   └── settings.py         # Module loader konfigurasi
│
├── downloads/              # 📥 Folder Penyimpanan Unduhan Berkas
│   ├── images/             # Gambar & foto konsep yang dikirim
│   └── documents/          # Dokumen (PDF, Word, Excel, kode, teks)
│
├── src/                    # 💻 Source Code Aplikasi
│   ├── bot.py              # Handler Telegram & alur respons komunikatif
│   └── ai_service.py       # Engine AI & parser dokumen universal
│
├── scripts/                # 🚀 Script Operasional
│   └── start_bot.bat       # Launcher Windows batch
│
├── docs/                   # 📖 Dokumentasi
│   └── README.md           # Panduan lengkap
│
├── run.py                  # 🏁 Entry point utama (python run.py)
├── start.bat               # 🖱️ Shortcut 1-klik di root folder
├── requirements.txt        # 📦 Dependensi Python
└── .gitignore              # 🛡️ Aturan Git ignore
```

---

## 🚀 Cara Menjalankan Bot

* **Metode 1 (Klik Ganda):** Klik dua kali file `start.bat` di root atau `scripts/start_bot.bat`.
* **Metode 2 (Terminal):**
  ```powershell
  python run.py
  ```

---

## 🎮 Dokumentasi Pengembangan Game (Fantasy Citadel Defense)

1. [**MASTER_GAME_DESIGN_DOCUMENT.md**](MASTER_GAME_DESIGN_DOCUMENT.md) — Dokumen master desain game terpadu (Lobby Citadel, Trio Hero Cedric/Rose/Rabbit Mage, ekonomi gems, dan arsitektur Unity).
2. [**TEAM_PROJECT_WORKFLOW.md**](TEAM_PROJECT_WORKFLOW.md) — Struktur tim sub-agent (Lead Project Manager BaldHawk & 4 divisi), alur kerja sprint, dan protokol QA.
3. [**GDD_CORE_GAMEPLAY_MECHANICS.md**](GDD_CORE_GAMEPLAY_MECHANICS.md) — Mekanik pertempuran grid & merge defense.
4. [**GDD_LOBBY_CONCEPT.md**](GDD_LOBBY_CONCEPT.md) — Spesifikasi antarmuka visual Citadel Lobby.


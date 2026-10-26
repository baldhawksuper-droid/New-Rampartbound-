using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FantasyDefense.Localization
{
    /// <summary>
    /// Auto-Localizer dinamis yang memantau teks-teks UI di layar dan secara otomatis
    /// menerjemahkan frasa Indonesia <-> English secara instan saat bahasa diubah.
    /// Tidak memerlukan penyiapan manual per GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    public class CitadelDynamicLocalizer : MonoBehaviour
    {
        private static readonly Dictionary<string, string> IdToEnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Navigasi & Dock
            { "🗺️\nPeta", "🗺️\nMap" },
            { "🛡️\nHero", "🛡️\nHeroes" },
            { "🔮\nGacha", "🔮\nSummon" },
            { "🔨\nTempa", "🔨\nForge" },
            { "💎\nToko", "💎\nStore" },
            { "✨\nTalenta", "✨\nTalents" },
            { "📜\nMisi", "📜\nQuests" },
            { "🏰\nGuild", "🏰\nGuild" },
            { "🎖️\nPass", "🎖️\nPass" },
            { "🔥\nEvent", "🔥\nEvent" },
            { "👑\nArtefak", "👑\nArtifacts" },

            // Tombol Utama & Header
            { "⚔️ PERTAHANKAN BENTENG (BATTLE)", "⚔️ DEFEND CITADEL (BATTLE)" },
            { "🌌 ABYSSAL TRIAL (ENDLESS MODE)", "🌌 ABYSSAL TRIAL (ENDLESS MODE)" },
            { "Komandan Benteng", "Citadel Commander" },
            { "⚔️ MULAI PERTEMPURAN SEKARANG", "⚔️ COMMENCE DEFENSE NOW" },
            { "MULAI BERTAHAN SEKARANG", "COMMENCE DEFENSE NOW" },
            { "MULAI TEMPUR", "COMMENCE BATTLE" },
            { "KEMBALI KE CITADEL", "RETURN TO CITADEL" },
            { "COBA LAGI", "RETRY DEFENSE" },
            { "COBA LAGI (RETRY)", "RETRY DEFENSE" },
            { "PERMAINAN DIHENTIKAN", "GAME PAUSED" },
            { "LANJUTKAN (RESUME)", "RESUME" },
            { "MENYERAH & KEMBALI KE CITADEL", "SURRENDER & RETURN" },
            { "KEMENANGAN TELAK!", "VICTORY!" },
            { "PERTAHANAN GAGAL!", "DEFENSE FAILED!" },

            // Hero & Upgrade
            { "MANAJEMEN & UPGRADE PAHLAWAN", "HERO ROSTER & UPGRADE" },
            { "⚔️ PASANG TERBAIK", "⚔️ AUTO-EQUIP" },
            { "📦 LEPAS SEMUA", "📦 UNEQUIP ALL" },
            { "TINGKATKAN LEVEL", "LEVEL UP HERO" },
            { "⬆️ TINGKATKAN LEVEL (GOLD)", "⬆️ LEVEL UP (GOLD)" },
            { "⭐ PROMOSI BINTANG (SHARD)", "⭐ STAR PROMOTION" },
            { "Promosi Bintang", "Star Promotion" },
            { "Maksimal", "Max Rank" },

            // Summon / Gacha Hall
            { "BALAI PEMANGGILAN CITADEL", "CITADEL SUMMONING HALL" },
            { "PANGGIL 1x", "SUMMON 1x" },
            { "PANGGIL 10x", "SUMMON 10x" },
            { "HADIAH GACHA", "SUMMON REWARDS" },
            { "TUTUP", "CLOSE" },
            { "KLAIM SEMUA", "CLAIM ALL" },

            // Settings & Audio
            { "PENGATURAN SISTEM & AUDIO", "SYSTEM & AUDIO SETTINGS" },
            { "🌐 GANTI BAHASA (SWITCH)", "🌐 SWITCH LANGUAGE (ID/EN)" },
            { "Volume Master", "Master Volume" },
            { "Volume BGM", "BGM Volume" },
            { "Volume SFX", "SFX Volume" },
            { "Mode Hemat Baterai (30 FPS)", "Battery Saver (30 FPS)" },
            { "Getaran Sentuhan (Haptic)", "Haptic Feedback" },
            { "Getaran Haptic Ponsel", "Haptic Vibration" },
            { "🔊 UJI COBA SUARA EFEK (TEST SFX)", "🔊 TEST SOUND EFFECTS (SFX)" },

            // Store & Economy
            { "TOKO CITADEL", "CITADEL STORE" },
            { "BELI EMAS", "BUY GOLD" },
            { "BELI GEMS", "BUY GEMS" },
            { "PULIHKAN STAMINA", "REFILL ENERGY" },
            { "GRATIS", "FREE" },

            // Relic Forge
            { "BENGKEL TEMPA RELIK (FORGE)", "RELIC FORGE WORKSHOP" },
            { "TEMPA RELIK (+1)", "ENHANCE RELIC (+1)" },
            { "SALVAGE SCRAP", "DISMANTLE SCRAP" },

            // Talents
            { "POHON TALENTA CITADEL", "CITADEL TALENT TREE" },
            { "RESET TALENTA", "RESET TALENTS" },
            { "PELAJARI TALENTA", "LEARN TALENT" },

            // Quests & Pass
            { "MISI HARIAN & MINGGUAN", "DAILY & WEEKLY QUESTS" },
            { "KLAIM", "CLAIM" },
            { "SELESAI", "COMPLETED" },
            { "CITADEL SEASON PASS", "CITADEL SEASON PASS" },
            { "BUKA PASS ELIT", "UNLOCK ELITE PASS" },

            // Guild & Event
            { "MARKAS GUILD", "GUILD HEADQUARTERS" },
            { "ABSENSI HARIAN", "DAILY CHECK-IN" },
            { "TITAN RAID", "TITAN BOSS RAID" },
            { "ARENA TANTANGAN LIVE EVENT", "LIVE EVENT ARENA" },
            { "TANTANG SEKARANG", "CHALLENGE NOW" },
            { "ARTEFAK KUNO & SINERGI SET", "ANCIENT ARTIFACTS" },

            // Title Screen
            { "Pertahankan Gerbang Kerajaan dari Serbuan Abyssal", "Defend the Royal Citadel from the Abyssal Onslaught" },
            { "🏰 TAP TO START", "🏰 TAP TO START" },
            { "Ketuk Layar untuk Memulai Pertahanan", "Tap Screen to Enter Citadel" }
        };

        private static readonly Dictionary<string, string> EnToIdMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static CitadelDynamicLocalizer()
        {
            foreach (var kvp in IdToEnMap)
            {
                if (!EnToIdMap.ContainsKey(kvp.Value))
                {
                    EnToIdMap[kvp.Value] = kvp.Key;
                }
            }
        }

        private void OnEnable()
        {
            CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            ApplyLocalization(CitadelLocalizationManager.ActiveLanguage);
        }

        private void OnDisable()
        {
            CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(AppLanguage lang)
        {
            ApplyLocalization(lang);
        }

        public void ApplyLocalization(AppLanguage lang)
        {
            var textComponents = GetComponentsInChildren<Text>(true);
            bool isEnglish = (lang == AppLanguage.English);

            foreach (var txt in textComponents)
            {
                if (txt == null || string.IsNullOrWhiteSpace(txt.text)) continue;

                string current = txt.text.Trim();

                if (isEnglish)
                {
                    if (IdToEnMap.TryGetValue(current, out string translated))
                    {
                        txt.text = translated;
                    }
                }
                else
                {
                    if (EnToIdMap.TryGetValue(current, out string translated))
                    {
                        txt.text = translated;
                    }
                }
            }
        }

        /// <summary>
        /// Menerjemahkan seluruh UI aktif secara global di Scene saat ini.
        /// </summary>
        public static void RefreshSceneLocalization(AppLanguage lang)
        {
            var allTexts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool isEnglish = (lang == AppLanguage.English);

            foreach (var txt in allTexts)
            {
                if (txt == null || string.IsNullOrWhiteSpace(txt.text)) continue;
                string trimmed = txt.text.Trim();

                if (isEnglish)
                {
                    if (IdToEnMap.TryGetValue(trimmed, out string translated))
                    {
                        txt.text = translated;
                    }
                }
                else
                {
                    if (EnToIdMap.TryGetValue(trimmed, out string translated))
                    {
                        txt.text = translated;
                    }
                }
            }
        }
    }
}

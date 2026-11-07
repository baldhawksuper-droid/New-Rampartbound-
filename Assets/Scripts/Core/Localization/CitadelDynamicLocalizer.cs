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
            { "Citadel", "Citadel" },
            { "Tempa", "Forge" },
            { "Hero", "Heroes" },
            { "Bag", "Bag" },
            { "Tas", "Bag" },
            { "✨\nTalenta", "✨\nTalents" },
            { "📜\nMisi", "📜\nQuests" },
            { "🏰\nGuild", "🏰\nGuild" },
            { "🎖️\nPass", "🎖️\nPass" },
            { "🔥\nEvent", "🔥\nEvent" },
            { "👑\nArtefak", "👑\nArtifacts" },

            // Tombol Utama & Header
            { "⚔️ BERTAHAN (BATTLE)", "⚔️ BATTLE (DEFEND)" },
            { "⚔️ PERTAHANKAN BENTENG (BATTLE)", "⚔️ BATTLE (DEFEND)" },
            { "🌌 UJIAN ABYSSAL (ENDLESS)", "🌌 ABYSSAL TRIAL (ENDLESS)" },
            { "🌌 ABYSSAL TRIAL (ENDLESS MODE)", "🌌 ABYSSAL TRIAL (ENDLESS)" },
            { "Komandan Benteng", "Citadel Commander" },
            { "⚔️ MULAI PERTEMPURAN SEKARANG", "⚔️ COMMENCE DEFENSE NOW" },
            { "MULAI BERTAHAN SEKARANG", "COMMENCE DEFENSE NOW" },
            { "MULAI TEMPUR", "COMMENCE BATTLE" },
            { "KEMBALI KE CITADEL", "RETURN TO CITADEL" },
            { "COBA LAGI", "RETRY DEFENSE" },
            { "COBA LAGI (RETRY)", "RETRY DEFENSE" },
            { "PERMAINAN DIHENTIKAN", "GAME PAUSED" },
            { "LANJUTKAN", "RESUME" },
            { "LANJUTKAN (RESUME)", "RESUME" },
            { "MENYERAH", "SURRENDER" },
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

            // Sub-Panel Headers & Navigation Tabs
            { "PETA DUNIA & SELEKSI STAGE", "WORLD MAP & STAGE SELECTION" },
            { "TOKO CITADEL & IN-APP PURCHASE", "CITADEL STORE & IN-APP PURCHASE" },
            { "PANDAI BESI & RELIC FORGE", "RELIC FORGE & BLACKSMITH" },
            { "POHON TALENTA & BERKAH BENTENG", "TALENT TREE & CITADEL BLESSINGS" },
            { "MISI HARIAN & PENCAPAIAN", "DAILY QUESTS & ACHIEVEMENTS" },
            { "ALIANSI GUILD & SERBUAN TITAN", "GUILD ALLIANCE & TITAN RAID" },
            { "CITADEL SEASON PASS (30 TIERS)", "CITADEL SEASON PASS (30 TIERS)" },
            { "PENGATURAN AUDIO & SISTEM", "AUDIO & SYSTEM SETTINGS" },
            { "ENDLESS ABYSSAL TRIAL", "ENDLESS ABYSSAL TRIAL" },

            // Dock & Navigation
            { "PETA", "MAP" },
            { "HERO", "HEROES" },
            { "GACHA", "SUMMON" },
            { "TOKO", "STORE" },
            { "Peta", "Map" },
            { "Gacha", "Summon" },
            { "Toko", "Store" },

            // World Map & Stage Details
            { "Total Bintang", "Total Stars" },
            { "⚡ BIAYA ENERGI", "⚡ ENERGY COST" },
            { "TERSEDIA", "AVAILABLE" },
            { "🛡️ CP BENTENG", "🛡️ CITADEL CP" },
            { "TIM KAMU", "YOUR TEAM" },
            { "UNGGUL!", "SUPERIOR!" },
            { "WASPADA!", "WARNING!" },
            { "⚡ KESULITAN", "⚡ DIFFICULTY" },
            { "🎁 HADIAH PERTAMA", "🎁 FIRST CLEAR REWARD" },
            { "🎁 HADIAH PERTAMA: [SUDAH DIKLAIM]", "🎁 FIRST REWARD: [CLAIMED]" },
            { "REWARD GRINDING", "REPEAT VICTORY REWARD" },
            { "Aman (Mudah)", "Safe (Easy)" },
            { "Seimbang (Normal)", "Balanced (Normal)" },
            { "Menantang (Keras)", "Challenging (Hard)" },
            { "Sangat Berbahaya (Underleveled)", "Very Dangerous (Underleveled)" },
            { "Outpost Meadow (Padang Rumput Depan)", "Outpost Meadow" },
            { "Fortress Siege (Pengepungan Benteng Batu)", "Fortress Siege" },
            { "Perkemahan Mire Scrapper", "Mire Scrapper Camp" },
            { "Menara Pantau Runtuh", "Collapsed Watchtower" },
            { "Lembah Obsidian Maw", "Obsidian Maw Valley" },
            { "Persimpangan Terkutuk", "Cursed Crossroads" },
            { "Reruntuhan Kuil Kuno", "Ancient Temple Ruins" },
            { "Punggungan Bukit Karang", "Crag Ridge Crest" },
            { "Gerbang Benteng Kehampaan", "Void Citadel Gate" },
            { "Sarang Malakor Void Tyrant [BOSS]", "Malakor's Abyssal Lair [BOSS]" },
            { "Pertahankan gerbang terluar kerajaan dari serbuan gelombang patroli goblin.", "Defend the royal outskirts from goblin patrol waves." },

            // Settings & Audio
            { "60 FPS (Halus)", "60 FPS (Smooth)" },
            { "Mode Hemat Baterai", "Battery Saver Mode" },
            { "Getaran Haptic Ponsel", "Haptic Vibration" },
            { "📳 GETARAN: AKTIF", "📳 HAPTICS: ON" },
            { "📴 GETARAN: NONAKTIF", "📴 HAPTICS: OFF" },
            { "Volume Master", "Master Volume" },
            { "Volume Musik (BGM)", "BGM Music Volume" },
            { "Volume Efek (SFX)", "SFX Sound Volume" },
            { "Bungkam", "Mute" },

            // Hero Roles
            { "Frontline Melee DPS Striker (Tebasan Ganda & Badai Pedang)", "Frontline Melee DPS Striker (Dual Slash & Blade Storm)" },
            { "Ranged Magic AoE & Kontrol Massa (Meteor Siang & Api Purba)", "Ranged Magic AoE & Crowd Control (Noon Meteor & Ancient Fire)" },
            { "Heavy Frontline Tank & Crowd Control (Hantaman Palu & Stun)", "Heavy Frontline Tank & Crowd Control (Hammer Slam & Stun)" },
            { "Long-Range Physical DPS & Crowd Control (Panah Badai & Tornado Zephyr)", "Long-Range Physical DPS & Crowd Control (Storm Arrows & Zephyr Tornado)" },

            // Summon Hall Banner
            { "PEMANGGILAN KHUSUS PAHLAWAN", "HERO RATE-UP SUMMON" },
            { "Rate-Up 50%: Wind Ranger (Pemanah Angin)", "50% Rate-Up: Wind Ranger (Wind Archer)" },
            { "PANGGIL 1x\n(100 Gems)", "SUMMON 1x\n(100 Gems)" },
            { "PANGGIL 10x\n(900 Gems)", "SUMMON 10x\n(900 Gems)" },

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

                TranslateTextComponent(txt, isEnglish);
            }
        }

        /// <summary>
        /// Menerjemahkan seluruh UI aktif secara global di Scene saat ini.
        /// </summary>
        public static void RefreshSceneLocalization(AppLanguage lang)
        {
            var allTexts = FindObjectsByType<Text>(FindObjectsInactive.Include);
            bool isEnglish = (lang == AppLanguage.English);

            foreach (var txt in allTexts)
            {
                if (txt == null || string.IsNullOrWhiteSpace(txt.text)) continue;

                TranslateTextComponent(txt, isEnglish);
            }
        }

        private static void TranslateTextComponent(Text txt, bool isEnglish)
        {
            string raw = txt.text.Trim();
            bool hasBold = raw.StartsWith("<b>") && raw.EndsWith("</b>");
            string inner = hasBold ? raw.Substring(3, raw.Length - 7).Trim() : raw;

            if (isEnglish)
            {
                if (IdToEnMap.TryGetValue(raw, out string translatedDirect))
                {
                    txt.text = translatedDirect;
                }
                else if (IdToEnMap.TryGetValue(inner, out string translatedInner))
                {
                    txt.text = hasBold ? $"<b>{translatedInner}</b>" : translatedInner;
                }
            }
            else
            {
                if (EnToIdMap.TryGetValue(raw, out string translatedDirect))
                {
                    txt.text = translatedDirect;
                }
                else if (EnToIdMap.TryGetValue(inner, out string translatedInner))
                {
                    txt.text = hasBold ? $"<b>{translatedInner}</b>" : translatedInner;
                }
            }
        }
    }
}

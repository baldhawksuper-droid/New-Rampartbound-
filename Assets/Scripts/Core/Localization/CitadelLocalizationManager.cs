using System;
using System.Collections.Generic;
using UnityEngine;

namespace FantasyDefense.Localization
{
    public enum AppLanguage
    {
        Indonesian,
        English
    }

    /// <summary>
    /// Master Localization Manager untuk Rampartbound: Merge Defense.
    /// Mengelola pergantian bahasa instan (Bahasa Indonesia & English) tanpa perlu reload scene,
    /// menyimpan preferensi bahasa pemain ke PlayerPrefs, dan menyediakan kamus translasi teks UI.
    /// </summary>
    public class CitadelLocalizationManager : MonoBehaviour
    {
        public static CitadelLocalizationManager Instance { get; private set; }

        private const string PREF_KEY_LANGUAGE = "Rampartbound_AppLanguage";

        [SerializeField] private AppLanguage currentLanguage = AppLanguage.Indonesian;
        public AppLanguage CurrentLanguage => currentLanguage;
        public static AppLanguage ActiveLanguage => Instance != null ? Instance.currentLanguage : AppLanguage.Indonesian;

        public static event Action<AppLanguage> OnLanguageChanged;

        private static readonly Dictionary<string, (string id, string en)> TranslationTable = new Dictionary<string, (string id, string en)>
        {
            // Lobby & Header
            { "LOBBY_COMMANDER", ("Komandan Benteng", "Citadel Commander") },
            { "LOBBY_DEFEND_BTN", ("⚔️ PERTAHANKAN BENTENG (BATTLE)", "⚔️ DEFEND CITADEL (BATTLE)") },
            { "LOBBY_ABYSSAL_BTN", ("🌌 ABYSSAL TRIAL (ENDLESS MODE)", "🌌 ABYSSAL TRIAL (ENDLESS MODE)") },
            { "LOBBY_TEAM_CP", ("Team CP", "Team CP") },

            // Bottom Navigation Dock
            { "NAV_MAP", ("🗺️\nPeta", "🗺️\nMap") },
            { "NAV_HEROES", ("🛡️\nHero", "🛡️\nHeroes") },
            { "NAV_SUMMON", ("🔮\nGacha", "🔮\nSummon") },
            { "NAV_FORGE", ("🔨\nTempa", "🔨\nForge") },
            { "NAV_STORE", ("💎\nToko", "💎\nStore") },
            { "NAV_TALENTS", ("✨\nTalenta", "✨\nTalents") },
            { "NAV_QUESTS", ("📜\nMisi", "📜\nQuests") },
            { "NAV_GUILD", ("🏰\nGuild", "🏰\nGuild") },
            { "NAV_PASS", ("🎖️\nPass", "🎖️\nPass") },
            { "NAV_EVENT", ("🔥\nEvent", "🔥\nEvent") },
            { "NAV_ARTIFACTS", ("👑\nArtefak", "👑\nArtifacts") },

            // Stage Map
            { "MAP_CHAPTER_1", ("Chapter 1: Pertahanan Pinggiran Kerajaan", "Chapter 1: Outskirts Citadel Defense") },
            { "MAP_TOTAL_STARS", ("Total Bintang", "Total Stars") },
            { "MAP_START_BATTLE", ("⚔️ MULAI PERTEMPURAN SEKARANG", "⚔️ COMMENCE DEFENSE NOW") },
            { "MAP_RECOMMENDED_CP", ("Rekomendasi CP", "Recommended CP") },
            { "MAP_ENERGY_COST", ("Biaya Stamina", "Energy Cost") },
            { "MAP_FIRST_CLEAR", ("Hadiah First-Clear", "First-Clear Rewards") },
            { "MAP_REPEAT_CLEAR", ("Hadiah Kemenangan", "Repeat Victory Rewards") },

            // Settings
            { "SETTINGS_TITLE", ("PENGATURAN SISTEM & AUDIO", "SYSTEM & AUDIO SETTINGS") },
            { "SETTINGS_MASTER_VOL", ("Volume Master", "Master Volume") },
            { "SETTINGS_BGM_VOL", ("Volume Musik (BGM)", "Music Volume (BGM)") },
            { "SETTINGS_SFX_VOL", ("Volume Efek (SFX)", "Sound Effects (SFX)") },
            { "SETTINGS_BATTERY_SAVER", ("Mode Hemat Baterai (30 FPS)", "Battery Saver Mode (30 FPS)") },
            { "SETTINGS_HAPTICS", ("Getaran Sentuhan (Haptic)", "Haptic Vibration Feedback") },
            { "SETTINGS_LANG_SWITCH", ("Bahasa / Language: [ INDONESIA ]", "Language / Bahasa: [ ENGLISH ]") },
            { "SETTINGS_LANG_BTN", ("Ganti Bahasa (Switch)", "Switch Language (Bahasa)") },

            // Hero Upgrade
            { "HERO_TITLE", ("MANAJEMEN & UPGRADE PAHLAWAN", "HERO ROSTER & UPGRADE") },
            { "HERO_UPGRADE_BTN", ("⬆️ TINGKATKAN LEVEL (GOLD)", "⬆️ LEVEL UP HERO (GOLD)") },
            { "HERO_PROMOTE_BTN", ("⭐ PROMOSI BINTANG (SHARD)", "⭐ STAR PROMOTION (SHARD)") },

            // Battle HUD
            { "BATTLE_SPEED_1X", ("1.0x", "1.0x") },
            { "BATTLE_SPEED_2X", ("2.0x", "2.0x") },
            { "BATTLE_SPEED_3X", ("3.0x", "3.0x") },
            { "BATTLE_PAUSE", ("JEDA", "PAUSE") },
            { "BATTLE_RESUME", ("LANJUTKAN", "RESUME") },
            { "BATTLE_SURRENDER", ("MENYERAH", "SURRENDER") }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadSavedLanguage();
        }

        private void LoadSavedLanguage()
        {
            string saved = PlayerPrefs.GetString(PREF_KEY_LANGUAGE, "ID");
            currentLanguage = (saved == "EN") ? AppLanguage.English : AppLanguage.Indonesian;
        }

        public void SetLanguage(AppLanguage language)
        {
            currentLanguage = language;
            PlayerPrefs.SetString(PREF_KEY_LANGUAGE, currentLanguage == AppLanguage.English ? "EN" : "ID");
            PlayerPrefs.Save();

            Debug.Log($"[CitadelLocalizationManager] Bahasa aktif diubah ke: {currentLanguage}");
            OnLanguageChanged?.Invoke(currentLanguage);
        }

        public void ToggleLanguage()
        {
            SetLanguage(currentLanguage == AppLanguage.Indonesian ? AppLanguage.English : AppLanguage.Indonesian);
        }

        public static string Get(string key, string fallbackId = null, string fallbackEn = null)
        {
            AppLanguage lang = Instance != null ? Instance.currentLanguage : AppLanguage.Indonesian;

            if (TranslationTable.TryGetValue(key, out var val))
            {
                return lang == AppLanguage.Indonesian ? val.id : val.en;
            }

            if (lang == AppLanguage.English && !string.IsNullOrEmpty(fallbackEn))
                return fallbackEn;

            return fallbackId ?? key;
        }

        public static string Text(string indonesian, string english)
        {
            AppLanguage lang = Instance != null ? Instance.currentLanguage : AppLanguage.Indonesian;
            return lang == AppLanguage.Indonesian ? indonesian : english;
        }
    }
}

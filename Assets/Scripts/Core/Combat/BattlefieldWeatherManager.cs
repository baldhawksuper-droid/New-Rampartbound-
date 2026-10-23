using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Data;

namespace FantasyDefense.Combat
{
    public enum WeatherType
    {
        None = 0,         // Cerah / Normal
        Thunderstorm = 1, // Badai Petir (Menyambar musuh tiap 8s, AoE Stun 1s & Damage)
        AbyssalFog = 2,   // Kabut Pekat (Jarak tembak hero ranged -15%)
        BloodMoon = 3     // Bulan Darah (Monster speed +20%, reward drop gems x2)
    }

    /// <summary>
    /// Mengelola cuaca dinamis dan afiks medan pertempuran (Dynamic Weather & Battlefield Affixes).
    /// Mengubah dinamika pertempuran secara visual dan mekanikal:
    /// - Thunderstorm: Sambaran petir berkala ke arah monster (AoE Stun & Damage).
    /// - Abyssal Fog: Kabut pekat yang membatasi jarak serang hero ranged (Rabbit Mage, Wind Ranger).
    /// - Blood Moon: Gerakan monster melonjak +20% lebih cepat namun memberikan imbalan 2x Gems.
    /// </summary>
    public class BattlefieldWeatherManager : MonoBehaviour
    {
        public static BattlefieldWeatherManager Instance { get; private set; }

        [Header("Current Weather State")]
        [SerializeField] private WeatherType currentWeather = WeatherType.None;

        [Header("Thunderstorm Settings")]
        [SerializeField] private float lightningInterval = 8.0f;
        [SerializeField] private float lightningDamage = 140f;
        [SerializeField] private float lightningAoeRadius = 1.3f;
        [SerializeField] private float lightningStunDuration = 1.0f;

        [Header("Abyssal Fog Settings")]
        [SerializeField] private float rangedHeroPenaltyMultiplier = 0.85f; // -15% jarak tembak

        [Header("Blood Moon Settings")]
        [SerializeField] private float bloodMoonSpeedMultiplier = 1.20f;   // +20% kecepatan
        [SerializeField] private int bloodMoonGemMultiplier = 2;            // 2x gems drop

        private Coroutine lightningCoroutine;

        public WeatherType CurrentWeather => currentWeather;

        public static event Action<WeatherType> OnWeatherChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            ApplyWeather(currentWeather);
        }

        public void SetWeather(WeatherType newWeather)
        {
            if (currentWeather == newWeather) return;

            currentWeather = newWeather;
            ApplyWeather(currentWeather);
            OnWeatherChanged?.Invoke(currentWeather);
            Debug.Log($"[BattlefieldWeatherManager] Cuaca pertempuran berubah menjadi: {GetWeatherName(currentWeather)}");
        }

        private void ApplyWeather(WeatherType weather)
        {
            if (lightningCoroutine != null)
            {
                StopCoroutine(lightningCoroutine);
                lightningCoroutine = null;
            }

            switch (weather)
            {
                case WeatherType.Thunderstorm:
                    lightningCoroutine = StartCoroutine(ThunderstormRoutine());
                    break;
                case WeatherType.AbyssalFog:
                case WeatherType.BloodMoon:
                case WeatherType.None:
                default:
                    break;
            }
        }

        private IEnumerator ThunderstormRoutine()
        {
            var waitInterval = new WaitForSeconds(lightningInterval);
            while (currentWeather == WeatherType.Thunderstorm)
            {
                yield return waitInterval;
                TriggerRandomLightningStrike();
            }
        }

        private void TriggerRandomLightningStrike()
        {
            var monsters = MonsterController.GetActiveMonstersSnapshot();
            if (monsters.Count == 0) return;

            var eligibleMonsters = monsters.FindAll(m => m != null && !m.IsDead);
            if (eligibleMonsters.Count == 0) return;

            MonsterController target = eligibleMonsters[UnityEngine.Random.Range(0, eligibleMonsters.Count)];
            if (target == null) return;

            Vector3 strikePos = target.transform.position;

            // Efek visual petir & SFX
            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnLightningStrike(strikePos);
                SkillVfxManager.Instance.SpawnFloatingText(strikePos + Vector3.up * 0.8f, "⚡ THUNDER STRIKE ⚡", new Color(0.3f, 0.9f, 1f), 1.2f);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySkillSfx(SkillAudioType.Lightning);
            }

            // Damage AoE & Stun
            foreach (var monster in eligibleMonsters)
            {
                if (monster != null && !monster.IsDead)
                {
                    float dist = Vector3.Distance(strikePos, monster.transform.position);
                    if (dist <= lightningAoeRadius)
                    {
                        monster.TakeDamage(lightningDamage);
                        monster.ApplyStun(lightningStunDuration);
                    }
                }
            }
        }

        public float GetHeroRangeMultiplier(UnitType unitType)
        {
            if (currentWeather == WeatherType.AbyssalFog)
            {
                if (unitType == UnitType.RabbitMage || unitType == UnitType.WindRanger)
                {
                    return rangedHeroPenaltyMultiplier;
                }
            }
            return 1.0f;
        }

        public float GetMonsterSpeedMultiplier()
        {
            if (currentWeather == WeatherType.BloodMoon)
            {
                return bloodMoonSpeedMultiplier;
            }
            return 1.0f;
        }

        public int GetMonsterRewardMultiplier()
        {
            if (currentWeather == WeatherType.BloodMoon)
            {
                return bloodMoonGemMultiplier;
            }
            return 1;
        }

        public string GetWeatherName(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Thunderstorm: return "⛈️ Thunderstorm (Badai Petir)";
                case WeatherType.AbyssalFog: return "🌫️ Abyssal Fog (Kabut Pekat)";
                case WeatherType.BloodMoon: return "🌕 Blood Moon (Bulan Darah)";
                case WeatherType.None: default: return "☀️ Normal";
            }
        }

        public string GetWeatherDescription(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Thunderstorm: return "Petir menyambar lintasan monster tiap 8 detik (AoE Stun 1s & 140 DMG)!";
                case WeatherType.AbyssalFog: return "Kabut pekat membatasi jarak tembak hero ranged (Rabbit & Wind) sebesar -15%!";
                case WeatherType.BloodMoon: return "Monster bergerak +20% lebih cepat, tetapi imbalan Gems dilipatgandakan (2x)!";
                case WeatherType.None: default: return "Kondisi medan pertempuran normal tanpa afiks cuaca tambahan.";
            }
        }
    }
}

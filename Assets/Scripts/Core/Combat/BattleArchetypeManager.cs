using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Combat;
using FantasyDefense.Data;
using FantasyDefense.Economy;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Mengelola sistem peningkatan level arketipe pahlawan di dalam arena pertempuran (In-Battle Archetype Upgrades).
    /// Beroperasi secara sinergis di Mode 1 (Swap & Merge) dan Mode 2 (Real-Time Deploy).
    /// Menggunakan Royal Gold untuk menaikkan efektivitas tempur seluruh unit sejenis di arena.
    /// </summary>
    public class BattleArchetypeManager : MonoBehaviour
    {
        public static BattleArchetypeManager Instance { get; private set; }

        public const int MAX_ARCHETYPE_LEVEL = 5;

        private readonly Dictionary<UnitType, int> archetypeLevels = new Dictionary<UnitType, int>();

        public static event Action<UnitType, int, float> OnArchetypeUpgraded;
        public static event Action OnArchetypesReset;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ResetAllArchetypes();
        }

        public void ResetAllArchetypes()
        {
            archetypeLevels.Clear();
            archetypeLevels[UnitType.Cedric] = 1;
            archetypeLevels[UnitType.RabbitMage] = 1;
            archetypeLevels[UnitType.RoseTanker] = 1;
            archetypeLevels[UnitType.WindRanger] = 1;

            OnArchetypesReset?.Invoke();
        }

        public int GetLevel(UnitType type)
        {
            if (!archetypeLevels.ContainsKey(type))
            {
                archetypeLevels[type] = 1;
            }
            return archetypeLevels[type];
        }

        public bool IsMaxLevel(UnitType type) => GetLevel(type) >= MAX_ARCHETYPE_LEVEL;

        public int GetUpgradeCost(UnitType type)
        {
            if (IsMaxLevel(type)) return 0;
            int currentLvl = GetLevel(type);
            int baseCost = (type == UnitType.RoseTanker) ? 60 : 50;
            int increment = (type == UnitType.RoseTanker) ? 30 : 25;
            return baseCost + (currentLvl - 1) * increment;
        }

        public float GetDamageMultiplier(UnitType type)
        {
            int currentLvl = GetLevel(type);
            float perLevelBonus = (type == UnitType.RoseTanker) ? 0.20f : 0.15f;
            return 1.0f + (currentLvl - 1) * perLevelBonus;
        }

        public bool TryUpgradeArchetype(UnitType type)
        {
            if (IsMaxLevel(type))
            {
                return false;
            }

            int cost = GetUpgradeCost(type);
            if (EconomyManager.Instance == null || !EconomyManager.Instance.SpendBattleGold(cost))
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.ButtonClick, 0.8f);
                }
                return false;
            }

            int newLevel = GetLevel(type) + 1;
            archetypeLevels[type] = newLevel;
            float newMult = GetDamageMultiplier(type);

            // Audio & VFX Feedback
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess, 1.25f);
            }

            string heroName = type switch
            {
                UnitType.Cedric => "Cedric",
                UnitType.RabbitMage => "Rabbit Mage",
                UnitType.RoseTanker => "Rose Tanker",
                UnitType.WindRanger => "Wind Ranger",
                _ => type.ToString()
            };

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnFloatingText(
                    Vector3.zero,
                    $"★ {heroName} Lv.{newLevel} (+{Mathf.RoundToInt((newMult - 1f) * 100)}%)! ★",
                    new Color(1f, 0.85f, 0.2f),
                    1.6f
                );
            }

            OnArchetypeUpgraded?.Invoke(type, newLevel, newMult);
            return true;
        }
    }
}

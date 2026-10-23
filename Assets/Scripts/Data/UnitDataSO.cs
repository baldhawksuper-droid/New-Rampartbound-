using UnityEngine;

namespace FantasyDefense.Data
{
    public enum UnitType
    {
        Cedric,      // Ksatria Merah - Melee DPS Striker
        RoseTanker,  // Ksatria Rambut Pink - Heavy Tanker & CC
        RabbitMage,  // Penyihir Kelinci - Ranged Magic AoE
        WindRanger   // Arsip Hero (Upcoming Roster)
    }

    public enum UnitTier
    {
        Tier0 = 0,   // Batu Permata / T0 Gem (Fase Puzzle Preparation)
        Tier1 = 1,   // Pahlawan Tingkat 1 (Evolusi fusi 3 batu permata T0)
        Tier2 = 2,   // Pahlawan Tingkat 2 (Evolusi fusi 3 token T1)
        Tier3 = 3    // Pahlawan Tingkat 3 (Evolusi fusi 3 token T2)
    }

    [CreateAssetMenu(fileName = "NewUnitData", menuName = "FantasyDefense/Unit Data", order = 1)]
    public class UnitDataSO : ScriptableObject
    {
        [Header("General Identity")]
        public UnitType unitType;
        public UnitTier tier = UnitTier.Tier1;
        public string heroName;
        public string title;
        [TextArea(2, 4)]
        public string description;

        [Header("Combat Base Statistics")]
        [Tooltip("Base physical or magical attack damage")]
        public float baseDamage = 25f;

        [Tooltip("Attacks per second (e.g. 1.0 = 1 attack per second)")]
        public float attackRate = 1.0f;

        [Tooltip("Attack range in grid tiles (1 = melee, 3-4 = ranged)")]
        public float attackRange = 1.0f;

        [Tooltip("Maximum health pool")]
        public float maxHealth = 100f;

        [Tooltip("Critical strike chance (0.0 to 1.0)")]
        public float criticalChance = 0.05f;

        [Tooltip("Damage reduction percentage (0.0 to 1.0)")]
        public float defensePercent = 0.10f;

        [Header("Visual & Prefab References")]
        public Sprite unitIcon;
        public GameObject unitPrefab;
        public Color themeColor = Color.white;
        public Color unitColor => themeColor;

        [Header("Tier Sprites (Visual Evolution)")]
        [Tooltip("Sprite visual untuk Tier 0 (Batu Permata / Gem T0 di fase persiapan)")]
        public Sprite tier0GemSprite;
        [Tooltip("Sprite visual untuk Tier 1")]
        public Sprite tier1Sprite;
        [Tooltip("Sprite visual untuk Tier 2 (Dual weapon / enhanced gear)")]
        public Sprite tier2Sprite;
        [Tooltip("Sprite visual untuk Tier 3 (Aura / ultimate gear)")]
        public Sprite tier3Sprite;
        [Tooltip("Sprite medali token khusus Mode Persiapan (Puzzle Preparation) - Tier 1")]
        public Sprite preparationTokenSprite;
        [Tooltip("Sprite medali token khusus Mode Persiapan - Tier 2 (2 Bintang Emas)")]
        public Sprite preparationTokenTier2Sprite;
        [Tooltip("Sprite medali token khusus Mode Persiapan - Tier 3 (3 Bintang Emas)")]
        public Sprite preparationTokenTier3Sprite;

        private string GetShortHeroKey(UnitType type)
        {
            switch (type)
            {
                case UnitType.RoseTanker: return "Rose";
                case UnitType.RabbitMage: return "Rabbit";
                case UnitType.WindRanger: return "Wind";
                default: return type.ToString();
            }
        }

        /// <summary>
        /// Mengembalikan sprite medali untuk mode persiapan berdasarkan tingkat tier (T0, T1, T2, T3).
        /// </summary>
        public Sprite GetTokenSprite(UnitTier targetTier = UnitTier.Tier1)
        {
            if (targetTier == UnitTier.Tier0 && tier0GemSprite != null)
                return tier0GemSprite;

            string shortKey = GetShortHeroKey(unitType);

            if (targetTier == UnitTier.Tier2)
            {
                if (preparationTokenTier2Sprite != null) return preparationTokenTier2Sprite;
                var t2 = Resources.Load<Sprite>($"Sprites/Grid/Token_Medallion_{unitType}_T2") ??
                         Resources.Load<Sprite>($"Sprites/Grid/Token_Medallion_{shortKey}_T2");
                if (t2 != null) return t2;
            }
            else if (targetTier == UnitTier.Tier3)
            {
                if (preparationTokenTier3Sprite != null) return preparationTokenTier3Sprite;
                var t3 = Resources.Load<Sprite>($"Sprites/Grid/Token_Medallion_{unitType}_T3") ??
                         Resources.Load<Sprite>($"Sprites/Grid/Token_Medallion_{shortKey}_T3");
                if (t3 != null) return t3;
            }

            if (preparationTokenSprite != null) return preparationTokenSprite;
            var t1 = Resources.Load<Sprite>($"Sprites/Grid/Token_Medallion_{unitType}") ??
                     Resources.Load<Sprite>($"Sprites/Grid/Token_Medallion_{shortKey}");
            if (t1 != null) return t1;

            return tier1Sprite != null ? tier1Sprite : unitIcon;
        }

        /// <summary>
        /// Mengembalikan sprite yang tepat berdasarkan tingkat Tier unit.
        /// </summary>
        public Sprite GetSpriteForTier(UnitTier targetTier)
        {
            switch (targetTier)
            {
                case UnitTier.Tier0:
                    return tier0GemSprite != null ? tier0GemSprite : (preparationTokenSprite != null ? preparationTokenSprite : unitIcon);
                case UnitTier.Tier1:
                    return tier1Sprite != null ? tier1Sprite : unitIcon;
                case UnitTier.Tier2:
                    return tier2Sprite != null ? tier2Sprite : (tier1Sprite != null ? tier1Sprite : unitIcon);
                case UnitTier.Tier3:
                    return tier3Sprite != null ? tier3Sprite : (tier2Sprite != null ? tier2Sprite : unitIcon);
                default:
                    return unitIcon;
            }
        }

        [Header("Special Skill (Tier 2 & Tier 3)")]
        public string skillName;
        [TextArea(2, 3)]
        public string skillDescription;
        public float skillCooldown = 5f;

        /// <summary>
        /// Mengembalikan penggali stat berdasarkan tier (T0: 0x, T1: 1.0x, T2: 2.2x, T3: 4.8x)
        /// Sesuai formula resmi di MASTER_GAME_DESIGN_DOCUMENT.md
        /// </summary>
        public float GetTierMultiplier()
        {
            return GetTierMultiplier(tier);
        }

        public float GetTierMultiplier(UnitTier targetTier)
        {
            switch (targetTier)
            {
                case UnitTier.Tier0: return 0f;
                case UnitTier.Tier1: return 1.0f;
                case UnitTier.Tier2: return 2.2f;
                case UnitTier.Tier3: return 4.8f;
                default: return 1.0f;
            }
        }

        public float GetCalculatedDamage()
        {
            return GetCalculatedDamage(tier);
        }

        public float GetCalculatedDamage(UnitTier targetTier)
        {
            return baseDamage * GetTierMultiplier(targetTier);
        }

        public float GetCalculatedHealth()
        {
            return GetCalculatedHealth(tier);
        }

        public float GetCalculatedHealth(UnitTier targetTier)
        {
            return maxHealth * GetTierMultiplier(targetTier);
        }
    }
}

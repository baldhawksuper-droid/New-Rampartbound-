using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Audio;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Master Manager untuk Sistem Pemilihan Berkah / Skill Roguelike (Battle Blessing Engine).
    /// Mengelola kumpulan berkah taktis yang dapat dipilih pemain di awal pertempuran dan setiap gelombang selesai.
    /// Menerapkan pengubah stat secara real-time ke Kuartet Pahlawan, Barikade Benteng, dan Perekonomian Pertempuran.
    /// </summary>
    public class BattleBlessingManager : MonoBehaviour
    {
        public static BattleBlessingManager Instance { get; private set; }

        public static event Action<BattleBlessingData> OnBlessingApplied;

        [Header("Active Session Modifiers")]
        [SerializeField] private float heroDamageMultiplier = 1.0f;
        [SerializeField] private float heroAttackSpeedMultiplier = 1.0f;
        [SerializeField] private float rangedRangeBonus = 0f;
        [SerializeField] private float critChanceBonus = 0f;
        [SerializeField] private float gemBountyMultiplier = 1.0f;
        [SerializeField] private float thornReflectRatio = 0f;

        public float HeroDamageMultiplier => heroDamageMultiplier;
        public float HeroAttackSpeedMultiplier => heroAttackSpeedMultiplier;
        public float RangedRangeBonus => rangedRangeBonus;
        public float CritChanceBonus => critChanceBonus;
        public float GemBountyMultiplier => gemBountyMultiplier;
        public float ThornReflectRatio => thornReflectRatio;

        private readonly List<BattleBlessingData> activeBlessings = new List<BattleBlessingData>();
        public IReadOnlyList<BattleBlessingData> ActiveBlessings => activeBlessings;

        private List<BattleBlessingData> blessingCatalog = new List<BattleBlessingData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeCatalog();
        }

        private void InitializeCatalog()
        {
            blessingCatalog = new List<BattleBlessingData>
            {
                new BattleBlessingData(
                    "deadly_might",
                    "⚔️ Pukulan Mematikan",
                    "⚔️ Deadly Might",
                    "Meningkatkan Daya Serang (ATK) seluruh Kuartet Pahlawan sebesar +25%.",
                    "Increases Attack Damage of all Quartet Heroes by +25%.",
                    BlessingRarity.Rare,
                    "⚔️",
                    BlessingEffectType.HeroAttackDamage,
                    0.25f,
                    new Color(1f, 0.35f, 0.35f)
                ),
                new BattleBlessingData(
                    "tactical_haste",
                    "⚡ Kelincahan Taktis",
                    "⚡ Tactical Haste",
                    "Meningkatkan Kecepatan Serang Kuartet Pahlawan sebesar +20% dan memberi +2 Langkah Taktis.",
                    "Increases Quartet Heroes Attack Speed by +20% and grants +2 Tactical Moves.",
                    BlessingRarity.Rare,
                    "⚡",
                    BlessingEffectType.HeroAttackSpeed,
                    0.20f,
                    new Color(1f, 0.85f, 0.2f)
                ),
                new BattleBlessingData(
                    "aegis_armor",
                    "🛡️ Baja Citadel",
                    "🛡️ Aegis Citadel",
                    "Memulihkan 35% HP Barikade Benteng seketika dan menaikkan Max HP benteng sebesar +25%.",
                    "Instantly restores 35% Barricade HP and raises Max HP by +25%.",
                    BlessingRarity.Common,
                    "🛡️",
                    BlessingEffectType.BarricadeRepair,
                    0.35f,
                    new Color(0.3f, 0.7f, 1f)
                ),
                new BattleBlessingData(
                    "eagle_eye",
                    "🏹 Mata Elang",
                    "🏹 Eagle Eye",
                    "Menambah Jangkauan Serang pahlawan jarak jauh (Rabbit Mage & Wind Ranger) sebesar +0.8 petak.",
                    "Increases Attack Range of ranged heroes (Rabbit Mage & Wind Ranger) by +0.8 tiles.",
                    BlessingRarity.Common,
                    "🏹",
                    BlessingEffectType.RangedAttackRange,
                    0.8f,
                    new Color(0.3f, 0.9f, 0.4f)
                ),
                new BattleBlessingData(
                    "war_bounty",
                    "💎 Rampasan Perang",
                    "💎 War Bounty",
                    "Memberi +40 Starting Gems seketika dan meningkatkan perolehan Gems tiap monster sebesar +30%.",
                    "Grants +40 Starting Gems instantly and boosts Gem drops by +30%.",
                    BlessingRarity.Rare,
                    "💎",
                    BlessingEffectType.WarBountyGems,
                    0.30f,
                    new Color(0.4f, 0.95f, 0.85f)
                ),
                new BattleBlessingData(
                    "critical_might",
                    "🔥 Kobaran Kritis",
                    "🔥 Critical Surge",
                    "Meningkatkan Peluang Serangan Kritis (Crit Rate) seluruh pahlawan sebesar +30%.",
                    "Boosts Quartet Heroes Critical Strike Chance by +30%.",
                    BlessingRarity.Epic,
                    "🔥",
                    BlessingEffectType.CriticalMight,
                    0.30f,
                    new Color(1f, 0.4f, 0.1f)
                ),
                new BattleBlessingData(
                    "tactical_moves",
                    "♟️ Visi Maestro",
                    "♟️ Grandmaster Vision",
                    "Menambahkan +4 Langkah Taktis ekstra untuk menyusun dan menggabungkan pahlawan di arena.",
                    "Grants +4 Extra Tactical Moves to reposition and merge heroes on the board.",
                    BlessingRarity.Common,
                    "♟️",
                    BlessingEffectType.TacticalMoves,
                    4f,
                    new Color(0.85f, 0.65f, 1f)
                ),
                new BattleBlessingData(
                    "thorn_reflect",
                    "🌵 Duri Barikade",
                    "🌵 Barricade Thorns",
                    "Barikade benteng memantulkan 25% kerusakan fisik kembali kepada monster yang menyerang.",
                    "Citadel barricade reflects 25% physical damage back to attacking monsters.",
                    BlessingRarity.Epic,
                    "🌵",
                    BlessingEffectType.BarricadeThorn,
                    0.25f,
                    new Color(0.6f, 0.85f, 0.3f)
                )
            };
        }

        /// <summary>
        /// Mengatur ulang seluruh modifier berkah untuk sesi pertempuran baru.
        /// </summary>
        public void ResetBattleSession()
        {
            heroDamageMultiplier = 1.0f;
            heroAttackSpeedMultiplier = 1.0f;
            rangedRangeBonus = 0f;
            critChanceBonus = 0f;
            gemBountyMultiplier = 1.0f;
            thornReflectRatio = 0f;
            activeBlessings.Clear();
            Debug.Log("[BattleBlessingManager] Sesi berkah pertempuran di-reset.");
        }

        /// <summary>
        /// Mengambil n pilihan berkah acak yang unik dari katalog.
        /// </summary>
        public List<BattleBlessingData> GetRandomBlessingChoices(int count = 3)
        {
            if (blessingCatalog == null || blessingCatalog.Count == 0)
            {
                InitializeCatalog();
            }

            var pool = new List<BattleBlessingData>(blessingCatalog);
            // Acak (Fisher-Yates shuffle)
            for (int i = 0; i < pool.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, pool.Count);
                var temp = pool[i];
                pool[i] = pool[r];
                pool[r] = temp;
            }

            int pickCount = Mathf.Min(count, pool.Count);
            return pool.GetRange(0, pickCount);
        }

        /// <summary>
        /// Menerapkan berkah yang dipilih pemain ke dalam sesi pertempuran aktif.
        /// </summary>
        public void ApplyBlessing(BattleBlessingData blessing)
        {
            if (blessing == null) return;

            activeBlessings.Add(blessing);

            switch (blessing.effectType)
            {
                case BlessingEffectType.HeroAttackDamage:
                    heroDamageMultiplier += blessing.effectValue;
                    break;
                case BlessingEffectType.HeroAttackSpeed:
                    heroAttackSpeedMultiplier += blessing.effectValue;
                    // Beri juga +2 bonus moves
                    if (Core.GameManager.Instance != null)
                    {
                        Core.GameManager.Instance.AddMoves(2);
                    }
                    break;
                case BlessingEffectType.BarricadeRepair:
                    if (CastleBarricade.Instance != null)
                    {
                        CastleBarricade.Instance.Heal(CastleBarricade.Instance.MaxHealth * blessing.effectValue);
                    }
                    break;
                case BlessingEffectType.RangedAttackRange:
                    rangedRangeBonus += blessing.effectValue;
                    break;
                case BlessingEffectType.WarBountyGems:
                    gemBountyMultiplier += blessing.effectValue;
                    if (Economy.EconomyManager.Instance != null)
                    {
                        Economy.EconomyManager.Instance.AddGems(40);
                    }
                    break;
                case BlessingEffectType.CriticalMight:
                    critChanceBonus += blessing.effectValue;
                    break;
                case BlessingEffectType.TacticalMoves:
                    if (Core.GameManager.Instance != null)
                    {
                        Core.GameManager.Instance.AddMoves(Mathf.RoundToInt(blessing.effectValue));
                    }
                    break;
                case BlessingEffectType.BarricadeThorn:
                    thornReflectRatio += blessing.effectValue;
                    break;
            }

            // Visual Juice & Feedback
            if (SkillVfxManager.Instance != null)
            {
                string label = blessing.GetLocalizedTitle();
                SkillVfxManager.Instance.SpawnFloatingText(new Vector3(0f, 1.2f, 0f), $"✨ {label} AKTIF!", blessing.accentColor, 2.2f, true);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            OnBlessingApplied?.Invoke(blessing);
            Debug.Log($"[BattleBlessingManager] Berkah '{blessing.id}' diterapkan! Hero DMG Mult: {heroDamageMultiplier:F2}x, AtkSpd: {heroAttackSpeedMultiplier:F2}x.");
        }
    }
}

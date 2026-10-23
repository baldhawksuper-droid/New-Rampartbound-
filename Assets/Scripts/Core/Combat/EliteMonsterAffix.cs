using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Data;
using FantasyDefense.Economy;

namespace FantasyDefense.Combat
{
    public enum EliteAffixType
    {
        None = 0,
        Shielded = 1,        // Pelindung Perisai Baja: Kebal terhadap 3 serangan pertama hero
        BerserkerAura = 2,   // Aura Kecepatan: Mempercepat monster sekitar +25% dalam radius 2 unit
        AbyssalHealer = 3    // Penyembuh Gelap: Tiap 3 detik memulihkan 8% HP monster kawan terdekat
    }

    /// <summary>
    /// Mengelola varian monster mutasi elit dengan afiks pertempuran khusus:
    /// Shielded (Tameng 3 Hit), Berserker Aura (Kecepatan AoE), dan Abyssal Healer (Regenerasi HP kawan).
    /// Dilengkapi visual mahkota elit, aura warna dinamis, dan ekstra reward gems.
    /// </summary>
    [RequireComponent(typeof(MonsterController))]
    public class EliteMonsterAffix : MonoBehaviour
    {
        [Header("Affix Configuration")]
        [SerializeField] private EliteAffixType currentAffix = EliteAffixType.None;
        [SerializeField] private int maxShieldHits = 3;
        [SerializeField] private int currentShieldHits = 3;

        [Header("Aura & Ability Parameters")]
        [SerializeField] private float berserkRadius = 2.2f;
        [SerializeField] private float berserkSpeedMultiplier = 1.25f; // +25% speed
        [SerializeField] private float healRadius = 2.5f;
        [SerializeField] private float healPercent = 0.08f; // 8% HP per tick
        [SerializeField] private float healInterval = 3.0f;

        private MonsterController monsterController;
        private SpriteRenderer mainSpriteRenderer;
        private GameObject eliteVisualBadge;
        private SpriteRenderer badgeRenderer;

        private float healTimer = 0f;
        private float berserkPulseTimer = 0f;

        public EliteAffixType AffixType => currentAffix;
        public bool IsShieldActive => currentAffix == EliteAffixType.Shielded && currentShieldHits > 0;
        public int ShieldHitsRemaining => currentShieldHits;

        public static event Action<MonsterController, EliteAffixType> OnEliteMonsterSpawned;
        public static event Action<MonsterController, EliteAffixType> OnEliteMonsterDefeated;

        private void Awake()
        {
            monsterController = GetComponent<MonsterController>();
            mainSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void InitializeAffix(EliteAffixType affix)
        {
            currentAffix = affix;
            currentShieldHits = maxShieldHits;
            healTimer = 0f;
            berserkPulseTimer = 0f;

            if (currentAffix == EliteAffixType.None)
            {
                if (eliteVisualBadge != null) eliteVisualBadge.SetActive(false);
                return;
            }

            CreateOrUpdateEliteVisuals();
            OnEliteMonsterSpawned?.Invoke(monsterController, currentAffix);
            Debug.Log($"[EliteMonsterAffix] Monster {name} bermutasi menjadi Elit: {currentAffix}!");
        }

        private void Update()
        {
            if (monsterController == null || monsterController.IsDead || monsterController.IsStunned)
                return;

            if (currentAffix == EliteAffixType.BerserkerAura)
            {
                HandleBerserkerAuraPulse();
            }
            else if (currentAffix == EliteAffixType.AbyssalHealer)
            {
                HandleAbyssalHealing();
            }
        }

        #region Shielded Mechanics (Pelindung Perisai Baja)

        /// <summary>
        /// Mengecek apakah serangan pahlawan dapat diserap oleh perisai baja elit.
        /// Mengembalikan TRUE jika damage diserap sepenuhnya.
        /// </summary>
        public bool TryAbsorbHit()
        {
            if (currentAffix != EliteAffixType.Shielded || currentShieldHits <= 0)
                return false;

            currentShieldHits--;

            // Visual feedback penangkisan
            if (SkillVfxManager.Instance != null)
            {
                string text = currentShieldHits > 0 
                    ? $"🛡️ PERISAI ({currentShieldHits} Sisa)" 
                    : "💥 PERISAI PECAH!";
                Color col = currentShieldHits > 0 ? new Color(0.2f, 0.8f, 1f) : Color.white;
                SkillVfxManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 0.9f, text, col, 0.8f);
            }

            StartCoroutine(FlashShieldColor());
            return true;
        }

        private IEnumerator FlashShieldColor()
        {
            if (mainSpriteRenderer != null)
            {
                Color prev = mainSpriteRenderer.color;
                mainSpriteRenderer.color = new Color(0.3f, 0.9f, 1f, 1f);
                yield return new WaitForSeconds(0.1f);
                if (mainSpriteRenderer != null && !monsterController.IsDead)
                {
                    mainSpriteRenderer.color = GetAffixThemeColor();
                }
            }
        }

        #endregion

        #region Berserker Aura Mechanics (Aura Kecepatan Berkelompok)

        private void HandleBerserkerAuraPulse()
        {
            berserkPulseTimer += Time.deltaTime;
            if (berserkPulseTimer >= 0.8f)
            {
                berserkPulseTimer = 0f;
                Vector3 myPos = transform.position;

                var monsters = MonsterController.GetActiveMonstersSnapshot();
                for (int i = 0; i < monsters.Count; i++)
                {
                    var other = monsters[i];
                    if (other == null || other == monsterController || other.IsDead) continue;

                    float dist = Vector3.Distance(myPos, other.transform.position);
                    if (dist <= berserkRadius)
                    {
                        // Berikan dorongan kecepatan sementara (0.9s agar terus ter-refresh jika dalam jangkauan)
                        float speedBoost = -(berserkSpeedMultiplier - 1.0f);
                        other.ApplySlow(speedBoost, 0.9f);
                    }
                }
            }
        }

        #endregion

        #region Abyssal Healer Mechanics (Penyembuh Gelap)

        private void HandleAbyssalHealing()
        {
            healTimer += Time.deltaTime;
            if (healTimer >= healInterval)
            {
                healTimer = 0f;
                Vector3 myPos = transform.position;
                int healedCount = 0;

                var healMonsters = MonsterController.GetActiveMonstersSnapshot();
                for (int i = 0; i < healMonsters.Count; i++)
                {
                    var other = healMonsters[i];
                    if (other == null || other == monsterController || other.IsDead) continue;

                    float dist = Vector3.Distance(myPos, other.transform.position);
                    if (dist <= healRadius && other.CurrentHealth < other.MaxHealth)
                    {
                        float healAmount = other.MaxHealth * healPercent;
                        other.TakeDamage(-healAmount, false); // Nilai negatif menyembuhkan HP di handler TakeDamage
                        healedCount++;

                        if (SkillVfxManager.Instance != null)
                        {
                            SkillVfxManager.Instance.SpawnFloatingText(other.transform.position + Vector3.up * 0.7f, $"+{Mathf.RoundToInt(healAmount)} HP", new Color(0.2f, 1f, 0.3f), 0.8f);
                        }
                    }
                }

                if (healedCount > 0)
                {
                    Debug.Log($"[EliteMonsterAffix] Abyssal Healer memulihkan {healedCount} kawan di radius {healRadius} unit.");
                }
            }
        }

        #endregion

        #region Visual Presentation & Badges

        private void CreateOrUpdateEliteVisuals()
        {
            if (eliteVisualBadge == null)
            {
                eliteVisualBadge = new GameObject("Elite_CrownBadge");
                eliteVisualBadge.transform.SetParent(transform, false);
                eliteVisualBadge.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                eliteVisualBadge.transform.localScale = Vector3.one * 0.45f;

                badgeRenderer = eliteVisualBadge.AddComponent<SpriteRenderer>();
                badgeRenderer.sortingOrder = 18;
            }

            eliteVisualBadge.SetActive(true);
            badgeRenderer.color = GetAffixThemeColor();

            // Berikan tema warna pada sprite monster itu sendiri agar kontras
            if (mainSpriteRenderer != null)
            {
                mainSpriteRenderer.color = Color.Lerp(monsterController.Data != null ? monsterController.Data.themeColor : Color.white, GetAffixThemeColor(), 0.35f);
            }
        }

        public Color GetAffixThemeColor()
        {
            switch (currentAffix)
            {
                case EliteAffixType.Shielded:
                    return new Color(0.2f, 0.85f, 1f); // Cyan Perisai
                case EliteAffixType.BerserkerAura:
                    return new Color(1f, 0.25f, 0.2f); // Merah Darah Berserk
                case EliteAffixType.AbyssalHealer:
                    return new Color(0.3f, 1f, 0.4f); // Hijau Mistik Penyembuh
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// Dipanggil saat monster elit terbunuh, memberikan bonus gems dan menembakkan event.
        /// </summary>
        public void HandleEliteDefeated()
        {
            if (currentAffix == EliteAffixType.None) return;

            int bonusGems = 15;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddGems(bonusGems);
            }

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.0f, $"ELITE DEFEATED! +{bonusGems} GEMS", new Color(1f, 0.85f, 0.2f), 1.5f);
            }

            OnEliteMonsterDefeated?.Invoke(monsterController, currentAffix);
            Debug.Log($"[EliteMonsterAffix] Monster Elit ({currentAffix}) berhasil ditumbangkan! Bonus +{bonusGems} Gems diberikan.");
        }

        #endregion
    }
}

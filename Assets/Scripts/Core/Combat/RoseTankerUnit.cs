using UnityEngine;
using FantasyDefense.Data;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Rose Tanker (Ksatria Rambut Pink) - Heavy Frontline Tanker & Crowd Control.
    /// T1: Hammer Guard -> T2: Shield Slam (Stun 1s) -> T3: Earthshaker (AoE Stun & Thorn Reflect).
    /// </summary>
    public class RoseTankerUnit : UnitController
    {
        [Header("Rose Tanker Parameters")]
        [SerializeField] private float stunDurationT2 = 1.0f;
        [SerializeField] private float stunDurationT3 = 1.5f;
        [SerializeField] private float thornReflectPercent = 0.20f; // 20% Thorn Reflect
        [SerializeField] private float earthshakerRadius = 1.5f;

        [Header("Shield Slam Counter (Tier 2)")]
        [SerializeField] private int attackCounter = 0;
        [SerializeField] private bool hasShieldAbsorb = false;

        public float StunDuration => currentTier >= UnitTier.Tier3 ? stunDurationT3 : stunDurationT2;
        public float ThornReflectPercent => currentTier >= UnitTier.Tier3 ? thornReflectPercent : 0f;
        public float EarthshakerRadius => earthshakerRadius;
        public bool HasShieldAbsorb => hasShieldAbsorb;

        protected override void OnEnable()
        {
            base.OnEnable();
            MonsterController.OnMonsterReachedBarricade += HandleBarricadeHitForThornReflect;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            MonsterController.OnMonsterReachedBarricade -= HandleBarricadeHitForThornReflect;
        }

        public override void UpdateVisualAppearance()
        {
            base.UpdateVisualAppearance();
        }

        private float lastSkillAnnounceTime = -999f;

        protected override void ExecuteAttack(MonsterController target)
        {
            if (target == null || target.IsDead) return;

            attackCounter++;
            float damage = GetAttackDamage();

            System.Action onImpact = () =>
            {
                if (target == null || target.IsDead) return;

                float dist = Vector3.Distance(transform.position, target.transform.position);

                // Jika target berjarak (> 0.85 unit), luncurkan gelombang seismik merambat di tanah
                if (dist > 0.85f && SkillVfxManager.Instance != null)
                {
                    SkillVfxManager.Instance.SpawnTravelingSeismicShockwave(transform.position, target.transform.position, currentTier);
                }
                else if (SkillVfxManager.Instance != null)
                {
                    SkillVfxManager.Instance.SpawnUpwardRadiantShockwave(transform.position, target.transform.position, currentTier);
                }

                if (currentTier == UnitTier.Tier1)
                {
                    // Tier 1: Hammer Guard
                    target.TakeDamage(damage);
                }
                else if (currentTier == UnitTier.Tier2)
                {
                    // Tier 2: Shield Slam setiap 3 kali serangan (Stun 1s + Shield Absorb 25%)
                    if (attackCounter % 3 == 0)
                    {
                        hasShieldAbsorb = true;
                        target.TakeDamage(damage * 1.25f);
                        target.ApplyStun(stunDurationT2);
                    }
                    else
                    {
                        target.TakeDamage(damage);
                    }
                }
                else if (currentTier >= UnitTier.Tier3)
                {
                    // Tier 3, 4, 5: Earthshaker Impact (AoE Stun & Shockwave Damage)
                    if (Time.time - lastSkillAnnounceTime >= 6.0f)
                    {
                        lastSkillAnnounceTime = Time.time;
                        NotifySkillTriggered("Earthshaker");
                    }
                    ExecuteEarthshaker(target, damage);
                }
            };

            if (visualAnimator != null)
            {
                visualAnimator.PlayAttack(target.transform.position, onImpact);
            }
            else
            {
                onImpact();
                StartCoroutine(AnimateAttackPunch());
            }
        }

        private void ExecuteEarthshaker(MonsterController primaryTarget, float damage)
        {
            string skillTitle = currentTier >= UnitTier.Tier4 ? "Seismic Rose Crush" : "Earthshaker";
            if (visualAnimator != null)
            {
                visualAnimator.PlaySkillCast(skillTitle, new Color(1f, 0.35f, 0.65f));
            }

            // Trigger visual skill shockwave burst through SkillVfxManager
            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnSkillParticles(primaryTarget.transform.position, UnitType.RoseTanker, currentTier);
            }

            primaryTarget.TakeDamage(damage);
            primaryTarget.ApplyStun(stunDurationT3);

            Vector3 center = primaryTarget.transform.position;
            var monsters = MonsterController.GetActiveMonstersSnapshot();
            for (int i = 0; i < monsters.Count; i++)
            {
                var monster = monsters[i];
                if (monster != null && !monster.IsDead && monster != primaryTarget)
                {
                    if (Vector3.Distance(center, monster.transform.position) <= earthshakerRadius)
                    {
                        float splashRatio = currentTier >= UnitTier.Tier4 ? 0.85f : 0.60f;
                        monster.TakeDamage(damage * splashRatio);
                        monster.ApplyStun(stunDurationT3);
                    }
                }
            }
        }

        public override void TakeDamage(float damageAmount)
        {
            // Rose Tanker menyerap damage dengan reduksi Armor bawaan 25% (Heavy Frontline Vanguard)
            float reducedDamage = damageAmount * 0.75f;
            base.TakeDamage(reducedDamage);
        }

        private void HandleBarricadeHitForThornReflect(MonsterController monster, int barricadeDamage)
        {
            // Thorn Aegis aktif pada Tier 3+: Pantulkan 20% damage serangan monster kembali ke mereka
            if (currentTier >= UnitTier.Tier3 && monster != null && !monster.IsDead)
            {
                float reflectedDamage = barricadeDamage * thornReflectPercent * 5f; // Scaling relevan untuk monster
                monster.TakeDamage(reflectedDamage);
            }
        }
    }
}

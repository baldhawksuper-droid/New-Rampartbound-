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

        protected override void ExecuteAttack(MonsterController target)
        {
            if (target == null || target.IsDead) return;

            attackCounter++;
            float damage = GetAttackDamage();

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
                    NotifySkillTriggered("Shield Slam");
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
                NotifySkillTriggered("Earthshaker & Thorn Aegis");
                ExecuteEarthshaker(target, damage);
            }

            if (visualAnimator != null)
            {
                visualAnimator.PlayAttack(target.transform.position);
            }
            else
            {
                StartCoroutine(AnimateAttackPunch());
            }
        }

        private void ExecuteEarthshaker(MonsterController primaryTarget, float damage)
        {
            if (visualAnimator != null)
            {
                visualAnimator.PlaySkillCast("Earthshaker", new Color(1f, 0.45f, 0.75f));
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
                        monster.TakeDamage(damage * 0.6f);
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

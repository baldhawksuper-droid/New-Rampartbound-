using UnityEngine;
using FantasyDefense.Data;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Cedric (The Red Knight) - Frontline Melee DPS Striker.
    /// T1: Slash -> T2: Dual Slash (Crit bonus) -> T3: Blade Tempest & Heroic Surge.
    /// </summary>
    public class CedricUnit : UnitController
    {
        [Header("Cedric Combat Parameters")]
        [SerializeField] private float tier2CritBonus = 0.20f; // +20% Critical Chance
        [SerializeField] private float bladeTempestRadius = 1.5f;
        [SerializeField] private float bladeTempestDefShred = 0.25f; // -25% Physical DEF
        [SerializeField] private float bladeTempestDefDuration = 4.0f;
        [SerializeField] private float heroicSurgeBuffPercent = 0.15f; // +15% Attack Speed
        [SerializeField] private float heroicSurgeRadius = 1.6f;
        [SerializeField] private float heroicSurgeDuration = 5.0f;

        public float BladeTempestRadius => bladeTempestRadius;

        public override void UpdateVisualAppearance()
        {
            base.UpdateVisualAppearance();
        }

        public override float GetAttackDamage()
        {
            float dmg = base.GetAttackDamage();

            if (currentTier >= UnitTier.Tier2)
            {
                // Multiplier 180% damage untuk Dual Slash pada Tier 2
                dmg *= 1.8f;

                bool isCrit = Random.value < (GetCriticalChance() + tier2CritBonus);
                if (isCrit)
                {
                    dmg *= 1.75f; // 175% Critical Strike
                }
            }

            return dmg;
        }

        protected override void ExecuteAttack(MonsterController target)
        {
            if (target == null || target.IsDead) return;

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnUpwardCrescentSlash(transform.position, target.transform.position, currentTier);
            }

            if (currentTier == UnitTier.Tier1)
            {
                // T1: Single Slash
                float dmg = GetAttackDamage();
                target.TakeDamage(dmg, false);
                if (visualAnimator != null)
                {
                    visualAnimator.PlayAttack(target.transform.position);
                }
                else
                {
                    StartCoroutine(AnimateAttackPunch());
                }
            }
            else if (currentTier == UnitTier.Tier2)
            {
                // T2: Dual Slash (Double strike rapid sequence)
                StartCoroutine(ExecuteDualSlashRoutine(target));
            }
            else if (currentTier >= UnitTier.Tier3)
            {
                // T3, T4, T5: Blade Tempest & Heroic Surge
                ExecuteBladeTempest(target);
            }
        }

        private System.Collections.IEnumerator ExecuteDualSlashRoutine(MonsterController target)
        {
            NotifySkillTriggered("Dual Slash");

            bool isCrit = Random.value < ((unitData != null ? unitData.criticalChance : 0.05f) + tier2CritBonus);
            float totalDmg = GetAttackDamage();
            float halfDmg = totalDmg * 0.5f;

            // Hit 1
            if (target != null && !target.IsDead)
            {
                target.TakeDamage(halfDmg, isCrit);
                if (visualAnimator != null)
                {
                    visualAnimator.PlayAttack(target.transform.position);
                }
                else
                {
                    StartCoroutine(AnimateAttackPunch());
                }
            }

            yield return new WaitForSeconds(0.12f);

            // Hit 2
            if (target != null && !target.IsDead)
            {
                target.TakeDamage(halfDmg, isCrit);
                if (visualAnimator != null)
                {
                    visualAnimator.PlayAttack(target.transform.position);
                }
                else
                {
                    StartCoroutine(AnimateAttackPunch());
                }
            }
        }

        private void ExecuteBladeTempest(MonsterController primaryTarget)
        {
            NotifySkillTriggered("Blade Tempest & Heroic Surge");

            bool isCrit = Random.value < ((unitData != null ? unitData.criticalChance : 0.05f) + tier2CritBonus);
            float baseDmg = GetAttackDamage();

            // 1. Serang target utama
            if (primaryTarget != null && !primaryTarget.IsDead)
            {
                primaryTarget.TakeDamage(baseDmg, isCrit);
                primaryTarget.ApplyDefenseDebuff(bladeTempestDefShred, bladeTempestDefDuration);
            }

            // 2. Putaran Badai Pedang (AoE Whirlwind) merobek DEF seluruh monster sekitar
            Vector3 center = transform.position;
            var monsters = MonsterController.GetActiveMonstersSnapshot();
            for (int i = 0; i < monsters.Count; i++)
            {
                var monster = monsters[i];
                if (monster != null && !monster.IsDead && monster != primaryTarget)
                {
                    if (Vector3.Distance(center, monster.transform.position) <= bladeTempestRadius)
                    {
                        monster.TakeDamage(baseDmg * 0.75f, isCrit);
                        monster.ApplyDefenseDebuff(bladeTempestDefShred, bladeTempestDefDuration);
                    }
                }
            }

            // 3. Heroic Surge: Buff +15% Attack Speed untuk seluruh hero di petak terdekat
            var heroes = GetActiveUnitsSnapshot();
            for (int i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero != null && hero != this)
                {
                    if (Vector3.Distance(center, hero.transform.position) <= heroicSurgeRadius)
                    {
                        hero.ApplyAttackSpeedBuff(heroicSurgeBuffPercent, heroicSurgeDuration);
                    }
                }
            }

            if (visualAnimator != null)
            {
                visualAnimator.PlaySkillCast("Blade Tempest", new Color(1f, 0.25f, 0.25f));
                visualAnimator.PlayAttack(primaryTarget != null ? primaryTarget.transform.position : transform.position + Vector3.up);
            }
            else
            {
                StartCoroutine(AnimateAttackPunch());
            }
        }
    }
}

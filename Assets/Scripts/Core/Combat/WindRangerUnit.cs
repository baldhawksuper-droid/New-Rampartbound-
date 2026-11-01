using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Data;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Wind Ranger (Pemanah Angin) - Roster Hero ke-4: Long-Range Physical Striker & Crowd Control.
    /// Visual: Emerald hood, longbow, jubah bulu hijau zamrud.
    /// T1: Swift Arrow (Tembakan panah cepat jarak jauh, jangkauan 3.8 petak).
    /// T2: Piercing Gale (Panah badai menembus barisan monster segaris, 200% damage & +20% Crit).
    /// T3: Tornado Barrage & Zephyr Whirlwind (Pusaran angin topan AoE, menyedot monster ke tengah & Slow 40%).
    /// </summary>
    public class WindRangerUnit : UnitController
    {
        [Header("Wind Ranger Combat Parameters")]
        [SerializeField] private float attackRangeTiles = 3.8f;
        [SerializeField] private float attackSpeedBonus = 0.25f; // +25% Attack Speed bawaan
        [SerializeField] private float piercingGaleDamageMult = 2.0f;
        [SerializeField] private float tornadoRadius = 2.2f;
        [SerializeField] private float tornadoSlowPercent = 0.40f; // 40% Slow
        [SerializeField] private float tornadoDuration = 3.5f;

        [Header("Attack Counter")]
        [SerializeField] private int shotCount = 0;
        private float lastSkillAnnounceTime = -999f;

        [Header("Projectile Fallback (Optional)")]
        [SerializeField] private GameObject arrowPrefab;

        public override float GetAttackRange()
        {
            return attackRangeTiles;
        }

        public override float GetAttackRate()
        {
            return base.GetAttackRate() * (1f + attackSpeedBonus);
        }

        public override void UpdateVisualAppearance()
        {
            base.UpdateVisualAppearance();
            if (spriteRenderer != null && (unitData == null || unitData.unitIcon == null))
            {
                // Emerald theme color jika sprite placeholder
                spriteRenderer.color = new Color(0.1f, 0.85f, 0.45f, 1f);
            }
        }

        protected override void ExecuteAttack(MonsterController target)
        {
            if (target == null || target.IsDead) return;

            shotCount++;

            if (currentTier == UnitTier.Tier1)
            {
                // T1: Swift Arrow
                SpawnArrowProjectile(target, GetAttackDamage(), false, 0f, 0f);
            }
            else if (currentTier == UnitTier.Tier2)
            {
                // T2: Piercing Gale setiap 2 tembakan
                if (shotCount % 2 == 0)
                {
                    ExecutePiercingGale(target);
                }
                else
                {
                    SpawnArrowProjectile(target, GetAttackDamage(), false, 0f, 0f);
                }
            }
            else if (currentTier >= UnitTier.Tier3)
            {
                // T3, T4, T5: Tornado Barrage & Zephyr Whirlwind
                if (Time.time - lastSkillAnnounceTime >= 6.0f)
                {
                    lastSkillAnnounceTime = Time.time;
                    NotifySkillTriggered("Tornado Barrage");
                }
                ExecuteTornadoBarrage(target);
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

        private void SpawnArrowProjectile(MonsterController target, float dmg, bool isCrit, float splashRadius, float slow)
        {
            GameObject projObj;
            if (arrowPrefab != null)
            {
                projObj = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                // Dynamic clean fallback: membuat proyektil anak panah zamrud secara runtime
                projObj = new GameObject($"WindArrow_T{(int)currentTier}");
                projObj.transform.position = transform.position;

                var sr = projObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 24;

                Sprite arrowSprite = Resources.Load<Sprite>("Sprites/Combat/Projectile_Wind_Arrow");
#if UNITY_EDITOR
                if (arrowSprite == null)
                {
                    arrowSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Combat/Projectile_Wind_Arrow.png");
                }
#endif
                if (arrowSprite != null)
                {
                    sr.sprite = arrowSprite;
                    sr.color = currentTier >= UnitTier.Tier3 ? new Color(0.8f, 1f, 0.9f) : Color.white;
                }
                else
                {
                    sr.sprite = GetOrCreateProceduralArrowSprite();
                    sr.color = currentTier >= UnitTier.Tier3 ? new Color(0.3f, 1f, 0.6f) : new Color(0.1f, 0.85f, 0.45f);
                }

                projObj.transform.localScale = new Vector3(0.65f, 0.45f, 1f);
            }

            var proj = projObj.GetComponent<Projectile>();
            if (proj == null)
            {
                proj = projObj.AddComponent<Projectile>();
            }

            proj.Initialize(
                targetMonster: target,
                damageAmount: dmg,
                aoeRadius: splashRadius,
                slow: slow,
                slowTime: tornadoDuration,
                critical: isCrit,
                meteor: false,
                arrow: true
            );
        }

        private void ExecutePiercingGale(MonsterController primaryTarget)
        {
            bool isCrit = Random.value < (GetCriticalChance() + 0.20f);
            float dmg = GetAttackDamage() * piercingGaleDamageMult;

            // Luncurkan proyektil utama
            SpawnArrowProjectile(primaryTarget, dmg, isCrit, 0.5f, 0.2f);

            // Garis tembusan: cari monster di lintasan lurus antara hero dan target
            Vector3 fireDir = (primaryTarget.transform.position - transform.position).normalized;
            var monsters = MonsterController.GetActiveMonstersSnapshot();
            for (int i = 0; i < monsters.Count; i++)
            {
                var monster = monsters[i];
                if (monster != null && !monster.IsDead && monster != primaryTarget)
                {
                    Vector3 toMonster = (monster.transform.position - transform.position);
                    float projDist = Vector3.Dot(toMonster, fireDir);
                    if (projDist > 0f && projDist <= attackRangeTiles)
                    {
                        Vector3 pointOnLine = transform.position + fireDir * projDist;
                        if (Vector3.Distance(pointOnLine, monster.transform.position) <= 0.8f)
                        {
                            monster.TakeDamage(dmg * 0.7f, isCrit);
                            if (SkillVfxManager.Instance != null)
                            {
                                SkillVfxManager.Instance.SpawnArrowImpactVfx(monster.transform.position, isCrit);
                            }
                        }
                    }
                }
            }

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                    primaryTarget.transform.position,
                    new Color(0.2f, 1f, 0.5f),
                    Color.white,
                    18,
                    3.0f
                );
            }
        }

        private void ExecuteTornadoBarrage(MonsterController target)
        {
            Vector3 center = target.transform.position;
            bool isCrit = Random.value < (GetCriticalChance() + 0.25f);
            float dmg = GetAttackDamage();

            // Luncurkan panah badai utama
            SpawnArrowProjectile(target, dmg * 1.5f, isCrit, tornadoRadius, tornadoSlowPercent);

            // Ciptakan efek Vortex Topan (Tornado Vortex) yang menarik musuh ke pusat
            StartCoroutine(TornadoVortexRoutine(center, dmg));
        }

        private IEnumerator TornadoVortexRoutine(Vector3 center, float baseDamage)
        {
            float elapsed = 0f;
            float tickInterval = 0.4f;

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnFloatingText(center + Vector3.up * 0.8f, "🌪 ZEPHYR TORNADO! 🌪", new Color(0.2f, 1f, 0.6f), 1.3f);
            }

            while (elapsed < tornadoDuration)
            {
                yield return new WaitForSeconds(tickInterval);
                elapsed += tickInterval;

                // Partikel badai berputar
                if (SkillVfxManager.Instance != null)
                {
                    SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                        center + (Vector3)Random.insideUnitCircle * 0.6f,
                        new Color(0.1f, 0.9f, 0.4f),
                        new Color(0.8f, 1f, 0.9f),
                        12,
                        2.2f
                    );
                }

                // Tarik dan lukai seluruh monster di area topan
                var monsters = MonsterController.GetActiveMonstersSnapshot();
                for (int i = 0; i < monsters.Count; i++)
                {
                    var monster = monsters[i];
                    if (monster != null && !monster.IsDead)
                    {
                        float dist = Vector3.Distance(center, monster.transform.position);
                        if (dist <= tornadoRadius)
                        {
                            // Tarik sedikit menuju pusat topan (Vacuum Pull)
                            monster.transform.position = Vector3.MoveTowards(monster.transform.position, center, 0.35f * tickInterval);

                            // DoT angin topan & debuff slow
                            monster.TakeDamage(baseDamage * 0.30f);
                            monster.ApplySlow(tornadoSlowPercent, 1.2f);
                        }
                    }
                }
            }
        }

        private static Sprite cachedArrowSprite;
        private static Sprite GetOrCreateProceduralArrowSprite()
        {
            if (cachedArrowSprite != null) return cachedArrowSprite;
            int w = 32, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Arrow shape: shaft + arrowhead
                    bool isShaft = (y >= 6 && y <= 9 && x >= 4 && x <= 22);
                    bool isHead = (x >= 20 && Mathf.Abs(y - 7.5f) <= (w - 1 - x) * 0.8f);
                    bool isFletching = (x <= 6 && Mathf.Abs(y - 7.5f) <= (6 - x) * 0.7f);

                    if (isHead || isShaft || isFletching)
                    {
                        pixels[y * w + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * w + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            cachedArrowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return cachedArrowSprite;
        }
    }
}
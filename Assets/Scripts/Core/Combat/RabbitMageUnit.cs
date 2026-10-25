using UnityEngine;
using FantasyDefense.Data;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Rabbit Mage (Penyihir Kelinci) - Ranged Magic AoE & Crowd Control.
    /// T1: Apprentice Spark -> T2: Mystic Burst (1.5 tile AoE) -> T3: Meteor Strike (AoE Burn & Slow).
    /// </summary>
    public class RabbitMageUnit : UnitController
    {
        [Header("Rabbit Mage Parameters")]
        [SerializeField] private float mysticBurstSplashRadius = 1.5f;
        [SerializeField] private float meteorRadius = 2.5f;
        [SerializeField] private float meteorSlowPercent = 0.30f; // 30% Slow
        [SerializeField] private float meteorSlowDuration = 3.0f; // 3 seconds slow & burn

        public float SplashRadius => currentTier >= UnitTier.Tier3 ? meteorRadius : (currentTier == UnitTier.Tier2 ? mysticBurstSplashRadius : 0f);
        public float MeteorSlowPercent => currentTier >= UnitTier.Tier3 ? meteorSlowPercent : 0f;
        public float MeteorSlowDuration => meteorSlowDuration;

        [Header("Projectile Reference (Optional)")]
        [SerializeField] private GameObject projectilePrefab;

        public override void UpdateVisualAppearance()
        {
            base.UpdateVisualAppearance();
        }

        protected override void ExecuteAttack(MonsterController target)
        {
            if (target == null || target.IsDead) return;

            if (currentTier == UnitTier.Tier2)
            {
                NotifySkillTriggered("Mystic Burst");
            }
            else if (currentTier >= UnitTier.Tier3)
            {
                NotifySkillTriggered("Meteor Strike & Arcane Fire");
            }

            SpawnAndFireProjectile(target);
            if (visualAnimator != null)
            {
                if (currentTier >= UnitTier.Tier2)
                {
                    visualAnimator.PlaySkillCast("Arcane", currentTier >= UnitTier.Tier3 ? new Color(0.85f, 0.3f, 1f) : new Color(0.2f, 0.75f, 1f));
                }
                visualAnimator.PlayAttack(target.transform.position);
            }
            else
            {
                StartCoroutine(AnimateAttackPunch());
            }
        }

        private void SpawnAndFireProjectile(MonsterController target)
        {
            GameObject projObj;
            if (projectilePrefab != null)
            {
                projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            }
            else
            {
                // Dynamic clean fallback: buat proyektil sihir kristal secara runtime
                projObj = new GameObject($"MagicProjectile_T{(int)currentTier}");
                projObj.transform.position = transform.position;

                var sr = projObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 25;

                Sprite orbSprite = Resources.Load<Sprite>("Sprites/Combat/Projectile_Magic_Orb");
#if UNITY_EDITOR
                if (orbSprite == null)
                {
                    orbSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Combat/Projectile_Magic_Orb.png");
                }
#endif
                if (orbSprite != null)
                {
                    sr.sprite = orbSprite;
                    sr.color = currentTier >= UnitTier.Tier3 ? new Color(1f, 0.75f, 1f) : Color.white;
                }
                else
                {
                    sr.sprite = GetOrCreateProceduralOrbSprite();
                    sr.color = currentTier >= UnitTier.Tier3 ? new Color(0.9f, 0.4f, 1f) : new Color(0.2f, 0.8f, 1f);
                }

                projObj.transform.localScale = Vector3.one * (currentTier >= UnitTier.Tier3 ? 0.75f : 0.55f);
            }

            var proj = projObj.GetComponent<Projectile>();
            if (proj == null)
            {
                proj = projObj.AddComponent<Projectile>();
            }

            bool isMeteor = currentTier >= UnitTier.Tier3;

            proj.Initialize(
                targetMonster: target,
                damageAmount: GetAttackDamage(),
                aoeRadius: SplashRadius,
                slow: MeteorSlowPercent,
                slowTime: MeteorSlowDuration,
                critical: false,
                meteor: isMeteor
            );
        }

        private static Sprite cachedOrbSprite;
        private static Sprite GetOrCreateProceduralOrbSprite()
        {
            if (cachedOrbSprite != null) return cachedOrbSprite;
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float radius = size * 0.45f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01(1f - (dist / radius));
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            cachedOrbSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return cachedOrbSprite;
        }
    }
}
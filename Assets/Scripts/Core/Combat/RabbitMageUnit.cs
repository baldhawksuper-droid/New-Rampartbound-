using UnityEngine;
using FantasyDefense.Data;
using FantasyDefense.Audio;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Rabbit Mage (Penyihir Kelinci) - Ranged Magic AoE & Crowd Control.
    /// T1: Apprentice Spark -> T2: Mystic Burst (1.5 tile AoE) -> T3: Meteor Strike (AoE Burn & Slow).
    /// </summary>
    public class RabbitMageUnit : UnitController
    {
        /// <summary>
        /// Rabbit Mage (Penyihir Kelinci) - Ranged Magic AoE & Crowd Control.
        /// T1: Apprentice Spark
        /// T2: Mystic Burst (1.5 tile AoE)
        /// T3: Meteor Strike (2.5 tile AoE Burn & Slow)
        /// T4: Astral Nova (3.2 tile AoE Supernova & Gravity Slow)
        /// T5: Celestial Singularity (4.0 tile Void Rift & Total Freeze)
        /// </summary>
        [Header("Rabbit Mage Parameters")]
        [SerializeField] private float mysticBurstSplashRadius = 1.5f;
        [SerializeField] private float meteorRadius = 2.5f;
        [SerializeField] private float astralNovaRadius = 3.2f;
        [SerializeField] private float celestialSingularityRadius = 4.0f;
        [SerializeField] private float meteorSlowPercent = 0.30f; // 30% Slow
        [SerializeField] private float astralSlowPercent = 0.50f; // 50% Slow
        [SerializeField] private float celestialSlowPercent = 0.70f; // 70% Slow
        [SerializeField] private float meteorSlowDuration = 3.0f; // 3 seconds slow & burn
        [SerializeField] private float astralSlowDuration = 4.0f;
        [SerializeField] private float celestialSlowDuration = 5.0f;

        public float SplashRadius
        {
            get
            {
                if (currentTier >= UnitTier.Tier5) return celestialSingularityRadius;
                if (currentTier == UnitTier.Tier4) return astralNovaRadius;
                if (currentTier == UnitTier.Tier3) return meteorRadius;
                if (currentTier == UnitTier.Tier2) return mysticBurstSplashRadius;
                return 0f;
            }
        }

        public float MeteorSlowPercent
        {
            get
            {
                if (currentTier >= UnitTier.Tier5) return celestialSlowPercent;
                if (currentTier == UnitTier.Tier4) return astralSlowPercent;
                if (currentTier == UnitTier.Tier3) return meteorSlowPercent;
                return 0f;
            }
        }

        public float MeteorSlowDuration
        {
            get
            {
                if (currentTier >= UnitTier.Tier5) return celestialSlowDuration;
                if (currentTier == UnitTier.Tier4) return astralSlowDuration;
                if (currentTier == UnitTier.Tier3) return meteorSlowDuration;
                return 0f;
            }
        }

        [Header("Projectile Reference (Optional)")]
        [SerializeField] private GameObject projectilePrefab;
        private float lastSkillAnnounceTime = -999f;

        public override void UpdateVisualAppearance()
        {
            base.UpdateVisualAppearance();
        }

        protected override void ExecuteAttack(MonsterController target)
        {
            if (target == null || target.IsDead) return;

            if (currentTier >= UnitTier.Tier3 && (Time.time - lastSkillAnnounceTime >= 6.0f))
            {
                lastSkillAnnounceTime = Time.time;
                string sName = currentTier switch
                {
                    UnitTier.Tier3 => "Meteor Strike",
                    UnitTier.Tier4 => "Astral Nova",
                    _ => "Celestial Singularity"
                };
                NotifySkillTriggered(sName);
            }

            SpawnAndFireProjectile(target);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MagicSpark, 1.15f);
            }

            if (visualAnimator != null)
            {
                if (currentTier >= UnitTier.Tier2)
                {
                    Color auraColor = currentTier >= UnitTier.Tier5 ? new Color(0.9f, 0.95f, 1f) :
                                     (currentTier == UnitTier.Tier4 ? new Color(0.7f, 0.2f, 1f) :
                                     (currentTier == UnitTier.Tier3 ? new Color(0.85f, 0.3f, 1f) : new Color(0.2f, 0.75f, 1f)));
                    visualAnimator.PlaySkillCast("Arcane", auraColor);
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
            Vector3 originPos = transform.position + new Vector3(0.12f, 0.35f, 0f); // Dari ujung tongkat bintang Pippin

            if (projectilePrefab != null)
            {
                projObj = Instantiate(projectilePrefab, originPos, Quaternion.identity);
            }
            else
            {
                // Dynamic clean fallback: buat proyektil sihir kristal secara runtime
                projObj = new GameObject($"MagicProjectile_T{(int)currentTier}");
                projObj.transform.position = originPos;

                var sr = projObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 200; // Prioritas tinggi di atas hero (120) dan monster (25)

                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit") 
                             ?? Shader.Find("Sprites/Default");
                if (shader != null) sr.material = new Material(shader);

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
                    sr.color = currentTier >= UnitTier.Tier3 ? new Color(1f, 0.90f, 1f) : Color.white;
                }
                else
                {
                    sr.sprite = GetOrCreateProceduralOrbSprite();
                    sr.color = currentTier >= UnitTier.Tier3 ? new Color(0.95f, 0.45f, 1f) : new Color(0.25f, 0.85f, 1f);
                }

                // Skala proyektil tajam dan jelas terlihat di layar mobile
                projObj.transform.localScale = Vector3.one * (currentTier >= UnitTier.Tier3 ? 1.15f : 0.85f);
            }

            var proj = projObj.GetComponent<Projectile>();
            if (proj == null)
            {
                proj = projObj.AddComponent<Projectile>();
            }

            var pSr = projObj.GetComponent<SpriteRenderer>();
            if (pSr != null && pSr.sortingOrder < 150)
            {
                pSr.sortingOrder = 200;
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
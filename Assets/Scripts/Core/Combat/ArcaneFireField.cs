using System.Collections;
using UnityEngine;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Area kobaran api sihir (Arcane Fire Zone) yang ditinggalkan oleh hantaman Meteor Rabbit Mage Tier 3.
    /// Bertahan selama durasi tertentu (3 detik), memberikan efek bakar (Burn DoT) per detik
    /// dan efek perlambatan gerak (Slow 40%) pada seluruh monster di dalamnya.
    /// </summary>
    public class ArcaneFireField : MonoBehaviour
    {
        [Header("Arcane Fire Parameters")]
        [SerializeField] private float radius = 2.5f;
        [SerializeField] private float lifetime = 3.0f;
        [SerializeField] private float burnDps = 20f;
        [SerializeField] private float slowPercent = 0.40f; // 40% slow
        [SerializeField] private float tickInterval = 0.5f;

        private SpriteRenderer spriteRenderer;

        public void Initialize(float fieldRadius, float duration, float damagePerSecond, float slowRatio)
        {
            radius = fieldRadius;
            lifetime = duration;
            burnDps = damagePerSecond;
            slowPercent = slowRatio;

            CreateVisualField();
            StartCoroutine(BurnAndSlowRoutine());
        }

        private void CreateVisualField()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            // Visual lingkaran kobaran api sihir ungu keemasan
            spriteRenderer.color = new Color(0.85f, 0.35f, 1f, 0.35f);
            spriteRenderer.sortingOrder = 5;
            transform.localScale = Vector3.one * (radius * 2.0f);
        }

        private IEnumerator BurnAndSlowRoutine()
        {
            float elapsed = 0f;

            while (elapsed < lifetime)
            {
                ApplyFieldEffects();

                // Efek denyut visual api
                if (spriteRenderer != null)
                {
                    float pulse = 0.30f + Mathf.PingPong(elapsed * 3f, 0.2f);
                    spriteRenderer.color = new Color(0.85f, 0.35f, 1f, pulse);
                }

                yield return new WaitForSeconds(tickInterval);
                elapsed += tickInterval;
            }

            Destroy(gameObject);
        }

        private void ApplyFieldEffects()
        {
            Vector3 center = transform.position;
            var monsters = MonsterController.GetActiveMonstersSnapshot();
            for (int i = 0; i < monsters.Count; i++)
            {
                var monster = monsters[i];
                if (monster != null && !monster.IsDead)
                {
                    float dist = Vector3.Distance(center, monster.transform.position);
                    if (dist <= radius)
                    {
                        monster.TakeDamage(burnDps * tickInterval, false);
                        monster.ApplySlow(slowPercent, tickInterval + 0.3f);
                        monster.ApplyBurn(burnDps, 1.0f);
                    }
                }
            }
        }
    }
}

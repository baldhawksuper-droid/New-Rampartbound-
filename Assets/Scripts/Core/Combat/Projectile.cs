using UnityEngine;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Mengontrol proyektil serangan jarak jauh (misal peluru sihir kristal Rabbit Mage).
    /// Mendukung pelacakan target (homing), benturan single-target, dan ledakan area (AoE Splash Damage & Slow).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Properties")]
        [SerializeField] private float flightSpeed = 8.0f;
        [SerializeField] private float maxLifetime = 4.0f;

        private MonsterController target;
        private float damage = 25f;
        private float splashRadius = 0f;
        private float slowPercent = 0f;
        private float slowDuration = 0f;
        private bool isCrit = false;
        private bool isMeteor = false;
        private bool isArrow = false;
        private bool isCannon = false;
        private bool isBallista = false;
        private Vector3 lastTargetPos;

        public void Initialize(
            MonsterController targetMonster,
            float damageAmount,
            float aoeRadius = 0f,
            float slow = 0f,
            float slowTime = 0f,
            bool critical = false,
            bool meteor = false,
            bool arrow = false,
            bool cannon = false,
            bool ballista = false,
            float speedOverride = -1f)
        {
            target = targetMonster;
            damage = damageAmount;
            splashRadius = aoeRadius;
            slowPercent = slow;
            slowDuration = slowTime;
            isCrit = critical;
            isMeteor = meteor;
            isArrow = arrow;
            isCannon = cannon;
            isBallista = ballista;

            if (speedOverride > 0f)
            {
                flightSpeed = speedOverride;
            }
            else if (ballista)
            {
                // Kecepatan balista bolt benteng ditingkatkan agar melesat tajam dan lurus
                flightSpeed = 15.0f;
            }
            else if (cannon)
            {
                // Kecepatan mortir peluru meriam berbobot
                flightSpeed = 11.0f;
            }

            if (target != null)
                lastTargetPos = target.transform.position;

            Destroy(gameObject, maxLifetime);
        }

        private void Update()
        {
            if (target != null && !target.IsDead)
            {
                lastTargetPos = target.transform.position;
            }

            Vector3 direction = (lastTargetPos - transform.position).normalized;
            transform.position += direction * (flightSpeed * Time.deltaTime);

            // Rotasi proyektil menghadap arah terbang
            if (direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            // Deteksi benturan saat mendekati posisi target
            if (Vector3.Distance(transform.position, lastTargetPos) < 0.2f)
            {
                HitTarget();
            }
        }

        private void HitTarget()
        {
            // 1. Single target hit
            if (target != null && !target.IsDead)
            {
                target.TakeDamage(damage, isCrit);
                if (slowPercent > 0 && slowDuration > 0)
                {
                    target.ApplySlow(slowPercent, slowDuration);
                }
            }

            // 2. Area of Effect (AoE Splash) untuk Rabbit Mage T2 (Mystic Burst) & T3 (Meteor)
            if (splashRadius > 0.1f)
            {
                ApplySplashDamage();
            }

            // 3. Meteor Impact Zone: Menghasilkan kobaran Arcane Fire di tanah selama 3 detik
            if (isMeteor)
            {
                GameObject fireFieldObj = new GameObject("ArcaneFireField_Impact");
                fireFieldObj.transform.position = transform.position;
                var fireField = fireFieldObj.AddComponent<ArcaneFireField>();
                fireField.Initialize(splashRadius, 3.0f, damage * 0.35f, slowPercent > 0f ? slowPercent : 0.40f);
            }

            // 4. Semburan partikel impak proyektil (letupan meriam berapi, tembusan balista runik, panah zamrud, atau sihir kristal)
            if (SkillVfxManager.Instance != null)
            {
                if (isCannon)
                {
                    SkillVfxManager.Instance.SpawnCannonImpactVfx(transform.position, splashRadius);
                }
                else if (isBallista)
                {
                    SkillVfxManager.Instance.SpawnBallistaPierceImpactVfx(transform.position, isCrit);
                }
                else if (isArrow)
                {
                    SkillVfxManager.Instance.SpawnArrowImpactVfx(transform.position, isCrit);
                }
                else
                {
                    SkillVfxManager.Instance.SpawnMagicImpactVfx(transform.position, splashRadius > 0.1f, isMeteor);
                }
            }

            Destroy(gameObject);
        }

        private void ApplySplashDamage()
        {
            Vector3 center = transform.position;
            var monsters = MonsterController.GetActiveMonstersSnapshot();
            for (int i = 0; i < monsters.Count; i++)
            {
                var monster = monsters[i];
                if (monster != null && !monster.IsDead && monster != target)
                {
                    float dist = Vector3.Distance(center, monster.transform.position);
                    if (dist <= splashRadius)
                    {
                        // Falloff damage berdasarkan jarak
                        float splashDamage = damage * (1f - (dist / (splashRadius * 1.5f)));
                        monster.TakeDamage(Mathf.Max(5f, splashDamage), isCrit);

                        if (slowPercent > 0 && slowDuration > 0)
                        {
                            monster.ApplySlow(slowPercent, slowDuration);
                        }
                    }
                }
            }
        }
    }
}

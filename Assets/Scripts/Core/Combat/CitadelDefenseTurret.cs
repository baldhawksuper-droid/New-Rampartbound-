using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Core;

namespace FantasyDefense.Combat
{
    public enum CitadelTurretType
    {
        Cannon,  // Meriam Pelontar Roket Benteng (AoE Explosive)
        Archer   // Pemanah / Balista Benteng (Rapid Piercing Single Target)
    }

    /// <summary>
    /// Mengelola menara pertahanan benteng (Citadel Defense Turrets) di sayap kiri dan kanan benteng.
    /// Pemanah (Archer/Ballista) dan Meriam/Roket (Cannon) bertindak sebagai benteng pertahanan otomatis,
    /// menembaki gelombang monster yang mendekati benteng dengan proyektil dan efek visual dinamis.
    /// </summary>
    public class CitadelDefenseTurret : MonoBehaviour
    {
        [Header("Turret Configuration")]
        [SerializeField] private CitadelTurretType turretType = CitadelTurretType.Cannon;
        [SerializeField] private float attackRange = 8.5f;
        [SerializeField] private float attackInterval = 2.0f;
        [SerializeField] private float baseDamage = 50f;
        [SerializeField] private float splashRadius = 1.2f;

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer turretRenderer;
        [SerializeField] private Transform firePoint;

        private float cooldownTimer = 0f;
        private Vector3 originalLocalScale;
        private Coroutine recoilCoroutine;

        public CitadelTurretType TurretType => turretType;
        public float AttackRange => attackRange;
        public float AttackInterval => attackInterval;
        public float BaseDamage => baseDamage;

        private void Awake()
        {
            if (turretRenderer == null)
            {
                turretRenderer = GetComponent<SpriteRenderer>();
            }

            originalLocalScale = transform.localScale;
            if (originalLocalScale == Vector3.zero)
            {
                originalLocalScale = Vector3.one;
            }
        }

        public void Initialize(CitadelTurretType type, Sprite sprite = null, float customRange = -1f, float customInterval = -1f, float customDmg = -1f)
        {
            turretType = type;

            if (turretRenderer == null)
            {
                turretRenderer = GetComponent<SpriteRenderer>();
                if (turretRenderer == null)
                {
                    turretRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (sprite != null)
            {
                turretRenderer.sprite = sprite;
            }

            turretRenderer.sortingOrder = 15; // Di atas barikade

            if (type == CitadelTurretType.Cannon)
            {
                attackInterval = customInterval > 0 ? customInterval : 2.2f;
                baseDamage = customDmg > 0 ? customDmg : 60f;
                splashRadius = 1.3f;
                attackRange = customRange > 0 ? customRange : 8.0f;
            }
            else // Archer / Ballista
            {
                attackInterval = customInterval > 0 ? customInterval : 1.2f;
                baseDamage = customDmg > 0 ? customDmg : 35f;
                splashRadius = 0f;
                attackRange = customRange > 0 ? customRange : 8.5f;
            }

            cooldownTimer = Random.Range(0.2f, 0.8f); // Variasi awal agar tidak menembak barengan
        }

        private void Update()
        {
            // Menara hanya aktif menyerang saat pertarungan gelombang monster sedang berlangsung
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != BattleState.WaveActive)
            {
                return;
            }

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                MonsterController target = FindBestTarget();
                if (target != null)
                {
                    FireAtTarget(target);
                    cooldownTimer = attackInterval;
                }
            }
        }

        /// <summary>
        /// Mencari monster paling mengancam (paling dekat dengan benteng) dalam radius jangkauan tembak.
        /// </summary>
        private MonsterController FindBestTarget()
        {
            MonsterController bestTarget = null;
            float closestDistanceToBarricade = float.MaxValue;
            Vector3 myPos = transform.position;

            var monsters = MonsterController.GetActiveMonstersSnapshot();
            for (int i = 0; i < monsters.Count; i++)
            {
                var monster = monsters[i];
                if (monster == null || monster.IsDead) continue;

                float distToTurret = Vector2.Distance(myPos, monster.transform.position);
                if (distToTurret <= attackRange)
                {
                    // Prioritaskan monster dengan posisi Y paling rendah (paling mendekati benteng di bawah)
                    float monsterY = monster.transform.position.y;
                    if (monsterY < closestDistanceToBarricade)
                    {
                        closestDistanceToBarricade = monsterY;
                        bestTarget = monster;
                    }
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Melancarkan tembakan proyektil meriam atau panah ke target monster.
        /// </summary>
        private void FireAtTarget(MonsterController target)
        {
            if (target == null || target.IsDead) return;

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 0.4f;

            // 1. Buat GameObject Proyektil Pertahanan Benteng
            GameObject projObj = new GameObject($"CitadelProj_{turretType}");
            projObj.transform.position = spawnPos;

            var sr = projObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 24;

            var proj = projObj.AddComponent<Projectile>();

            if (turretType == CitadelTurretType.Cannon)
            {
                // Roket / Peluru Meriam Ledakan Area
                sr.color = new Color(1f, 0.55f, 0.2f);
                projObj.transform.localScale = Vector3.one * 0.5f;
                proj.Initialize(target, baseDamage, aoeRadius: splashRadius, slow: 0.15f, slowTime: 1.5f, critical: false, meteor: false, arrow: false);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.CannonFire);
                }

                if (SkillVfxManager.Instance != null)
                {
                    SkillVfxManager.Instance.SpawnProceduralParticleBurst(spawnPos, new Color(1f, 0.7f, 0.2f), Color.white, 8, 1.5f);
                }
            }
            else
            {
                // Panah Tajam Balista Pemanah
                sr.color = new Color(0.9f, 0.95f, 1f);
                projObj.transform.localScale = new Vector3(0.35f, 0.65f, 1f);
                proj.Initialize(target, baseDamage, aoeRadius: 0f, slow: 0f, slowTime: 0f, critical: false, meteor: false, arrow: true);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.ArrowShot);
                }

                if (SkillVfxManager.Instance != null)
                {
                    SkillVfxManager.Instance.SpawnProceduralParticleBurst(spawnPos, new Color(0.7f, 0.9f, 1f), Color.white, 6, 1.2f);
                }
            }

            // Animasi recoil / kickback visual saat menembak
            if (recoilCoroutine != null)
            {
                StopCoroutine(recoilCoroutine);
            }
            recoilCoroutine = StartCoroutine(AnimateRecoilKick());
        }

        private IEnumerator AnimateRecoilKick()
        {
            Vector3 squashedScale = new Vector3(originalLocalScale.x * 1.15f, originalLocalScale.y * 0.82f, originalLocalScale.z);
            float elapsed = 0f;
            float kickDuration = 0.08f;

            while (elapsed < kickDuration)
            {
                transform.localScale = Vector3.Lerp(originalLocalScale, squashedScale, elapsed / kickDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            float recoverDuration = 0.14f;
            while (elapsed < recoverDuration)
            {
                transform.localScale = Vector3.Lerp(squashedScale, originalLocalScale, elapsed / recoverDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = originalLocalScale;
            recoilCoroutine = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = turretType == CitadelTurretType.Cannon ? Color.red : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}

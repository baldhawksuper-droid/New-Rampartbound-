using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Data;
using FantasyDefense.GridSystem;
using FantasyDefense.Core;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Specialized Controller untuk Boss Monster (Stage 1-5 Mini-Boss & Stage 1-10 Abyssal Drake Final Boss).
    /// Mengekstensi MonsterController dengan AI perilaku khusus:
    /// 1. Bilah HP Boss Raksasa & multi-phase tracking
    /// 2. Skill Ground Smash: menghentak tanah, mengguncang arena, dan men-stun pahlawan terdepan
    /// 3. Abyssal Roar & Enrage: saat sisa HP <= 35%, kecepatan meningkat +40%, aura merah menyala, dan memanggil minion pelindung
    /// 4. Dragon Firestorm / Flame Breath khusus Final Boss Drake
    /// </summary>
    public class BossMonsterController : MonsterController
    {
        [Header("Boss Identity")]
        [SerializeField] private string bossTitle = "Panglima Orc Kegelapan";
        [SerializeField] private bool isFinalBoss = false;

        [Header("Boss AI Timers & Thresholds")]
        [SerializeField] private float groundSmashCooldown = 7.0f;
        [SerializeField] private float enrageThreshold = 0.35f; // Aktif saat HP <= 35%
        [SerializeField] private float flameBreathCooldown = 5.5f;

        [Header("Runtime State")]
        [SerializeField] private bool isEnraged = false;
        private float groundSmashTimer = 0f;
        private float flameBreathTimer = 0f;

        public string BossTitle => bossTitle;
        public bool IsFinalBoss => isFinalBoss;
        public bool IsEnraged => isEnraged;

        // Boss-specific events untuk UI Boss Bar dan integrasi audio/VFX
        public static event Action<BossMonsterController> OnBossSpawned;
        public static event Action<BossMonsterController> OnBossDefeated;
        public static event Action<BossMonsterController, float, float> OnBossHealthChanged; // (boss, current, max)
        public static event Action<BossMonsterController> OnBossEnraged;
        public static event Action<BossMonsterController, string> OnBossSkillUsed;

        protected override void Awake()
        {
            base.Awake();
            groundSmashTimer = groundSmashCooldown;
            flameBreathTimer = flameBreathCooldown;
        }

        public override void Initialize(MonsterDataSO data, Vector3[] pathWaypoints, float lateralOffset = 0f)
        {
            base.Initialize(data, pathWaypoints, lateralOffset);

            // Deteksi apakah ini Final Boss berdasarkan nama atau data
            if (data != null)
            {
                if (data.monsterName.Contains("Drake") || data.monsterName.Contains("Naga") || data.maxHealth >= 800f)
                {
                    isFinalBoss = true;
                    bossTitle = "Raja Kegelapan Api Purba";
                }
                else
                {
                    isFinalBoss = false;
                    bossTitle = "Panglima Orc Penakluk";
                }
            }

            // Standar skala Boss terkalibrasi proporsional:
            // Boss berwibawa (~1.15m s/d ~1.30m) tanpa menutupi seluruh layar
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                float spriteW = spriteRenderer.sprite.bounds.size.x;
                if (spriteW > 0.01f)
                {
                    // Skala Bos Warlord & Drake Purba diperbesar proporsional agar mencerminkan otoritas Bos Medan Tempur
                    float targetWidth = isFinalBoss ? 1.55f : 1.35f;
                    float scale = targetWidth / spriteW;
                    baseScale = Vector3.one * scale;
                    transform.localScale = baseScale;

                    var col = GetComponent<CircleCollider2D>();
                    if (col != null)
                    {
                        col.radius = isFinalBoss ? 0.85f : 0.70f;
                    }
                }
            }

            OnBossSpawned?.Invoke(this);
            OnBossHealthChanged?.Invoke(this, currentHealth, MaxHealth);
        }

        public override void Initialize(MonsterDataSO data, Vector3[] pathWaypoints)
        {
            Initialize(data, pathWaypoints, 0f);
        }

        protected override void Update()
        {
            base.Update();
            if (isDead || isStunned) return;

            HandleBossAI();
        }

        private void HandleBossAI()
        {
            // 1. Skill Ground Smash
            groundSmashTimer -= Time.deltaTime;
            if (groundSmashTimer <= 0f)
            {
                groundSmashTimer = isEnraged ? (groundSmashCooldown * 0.75f) : groundSmashCooldown;
                ExecuteGroundSmash();
            }

            // 2. Skill Flame Breath khusus Final Boss saat Enraged
            if (isFinalBoss && isEnraged)
            {
                flameBreathTimer -= Time.deltaTime;
                if (flameBreathTimer <= 0f)
                {
                    flameBreathTimer = flameBreathCooldown;
                    ExecuteDragonFlameBreath();
                }
            }
        }

        public override void TakeDamage(float damage, bool isCrit = false)
        {
            base.TakeDamage(damage, isCrit);

            if (!isDead)
            {
                OnBossHealthChanged?.Invoke(this, currentHealth, MaxHealth);

                // Cek aktivasi fase Enrage saat sisa HP turun di bawah threshold
                if (!isEnraged && HealthPercent <= enrageThreshold)
                {
                    TriggerEnragePhase();
                }
            }
        }

        private void TriggerEnragePhase()
        {
            isEnraged = true;
            Debug.Log($"[BossMonsterController] {monsterData?.monsterName ?? "Boss"} memasuki FASE ENRAGE!");

            // Buff stat kecepatan gerak +40%
            currentMoveSpeed *= 1.40f;

            // Visual feedback: Boss membara merah menyala
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.25f, 0.25f, 1f);
            }

            // VFX aura & audio roar
            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                    transform.position,
                    new Color(1f, 0.1f, 0f),
                    new Color(1f, 0.7f, 0f),
                    35,
                    4.0f
                );
            }

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.Earthshaker);
            }

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Warning);
            }

            OnBossEnraged?.Invoke(this);
            OnBossSkillUsed?.Invoke(this, "ABYSSAL ROAR (ENRAGE)!");

            // Panggil minion pengawal (Minion Adds)
            SpawnMinionAdds();
        }

        private void ExecuteGroundSmash()
        {
            Debug.Log($"[BossMonsterController] Boss melancarkan Ground Smash!");

            OnBossSkillUsed?.Invoke(this, "GROUND SMASH!");

            // Alihkan visual boss ke mode menyerang saat melepaskan Ground Smash
            if (attackSprite != null && spriteRenderer != null)
            {
                StartCoroutine(BossSkillAttackPoseRoutine());
            }

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Heavy);
            }

            // Efek guncangan kamera khusus Boss Ground Smash dengan mikro-rotasi (Point 4 - Boss Impact Screen-Shake Twist)
            if (MobileCameraAdapter.Instance != null)
            {
                MobileCameraAdapter.Instance.TriggerBossScreenShake(0.40f, 0.32f, 0.70f);
            }

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnSkillParticles(transform.position, UnitType.RoseTanker, UnitTier.Tier3);
                SkillVfxManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.2f, "★ GROUND SMASH! ★", Color.red, 1.3f);
            }

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.Earthshaker);
            }

            // Stun hero terdekat di barisan arena selama 1.2 detik
            float smashRadius = 2.4f;
            foreach (var hero in UnitController.ActiveUnits)
            {
                if (hero != null && Vector3.Distance(transform.position, hero.transform.position) <= smashRadius)
                {
                    // Berikan stagger/visual effect ke hero
                    if (hero.VisualAnimator != null)
                    {
                        hero.VisualAnimator.PlayAttack(transform.position);
                    }
                }
            }

            // Jika boss sudah dekat dengan benteng, berikan hantaman gempa langsung ke barricade
            if (CastleBarricade.Instance != null && Vector3.Distance(transform.position, CastleBarricade.Instance.transform.position) <= 3.5f)
            {
                CastleBarricade.Instance.TakeDamage(15);
            }
        }

        private System.Collections.IEnumerator BossSkillAttackPoseRoutine()
        {
            Sprite prev = spriteRenderer != null ? spriteRenderer.sprite : null;
            if (attackSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackSprite;
            }
            yield return new WaitForSeconds(0.45f);
            if (prev != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = prev;
            }
        }

        private void ExecuteDragonFlameBreath()
        {
            Debug.Log($"[BossMonsterController] Abyssal Drake menyemburkan Firestorm!");

            OnBossSkillUsed?.Invoke(this, "DRAGON FIRESTORM!");

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                    transform.position,
                    new Color(1f, 0.4f, 0f),
                    new Color(1f, 0.9f, 0.1f),
                    40,
                    4.5f
                );
                SkillVfxManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.2f, "🔥 ABYSSAL FIRESTORM! 🔥", new Color(1f, 0.5f, 0f), 1.4f);
            }

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.MeteorImpact);
            }

            // Damage semburan api ke benteng
            if (CastleBarricade.Instance != null)
            {
                CastleBarricade.Instance.TakeDamage(20);
            }
        }

        private void SpawnMinionAdds()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            // Buat 2 minion orc pengawal di belakang boss
            int minionCount = isFinalBoss ? 3 : 2;
            for (int i = 0; i < minionCount; i++)
            {
                Vector3 spawnOffset = (Vector3)UnityEngine.Random.insideUnitCircle * 0.5f;
                GameObject minionObj = new GameObject($"BossMinion_{i + 1}");
                minionObj.transform.position = transform.position + spawnOffset;

                var sr = minionObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 14;
                sr.color = new Color(0.85f, 0.3f, 0.3f, 1f);

                var col = minionObj.AddComponent<CircleCollider2D>();
                col.radius = 0.4f;

                var minionCtrl = minionObj.AddComponent<MonsterController>();
                var minionData = ScriptableObject.CreateInstance<MonsterDataSO>();
                minionData.monsterType = MonsterType.Creep;
                minionData.monsterName = "Abyssal Guard";
                minionData.maxHealth = MaxHealth * 0.15f;
                minionData.moveSpeed = currentMoveSpeed * 1.1f;
                minionData.castleDamage = 5;
                minionData.gemsReward = 4;
                minionData.themeColor = new Color(0.85f, 0.3f, 0.3f, 1f);

                // Buat waypoints lanjutan dari posisi boss
                List<Vector3> remainingPath = new List<Vector3>();
                for (int w = currentWaypointIndex; w < waypoints.Length; w++)
                {
                    remainingPath.Add(waypoints[w]);
                }
                if (remainingPath.Count == 0) remainingPath.Add(transform.position);

                minionCtrl.Initialize(minionData, remainingPath.ToArray());
            }
        }

        protected override void Die()
        {
            if (isDead) return;

            Debug.Log($"[BossMonsterController] Boss {monsterData?.monsterName ?? "Boss"} berhasil dikalahkan!");
            OnBossDefeated?.Invoke(this);

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.5f, "★ BOSS DEFEATED! ★", Color.yellow, 1.6f);
                SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                    transform.position,
                    Color.yellow,
                    Color.white,
                    50,
                    5.0f
                );
            }

            base.Die();
        }
    }
}

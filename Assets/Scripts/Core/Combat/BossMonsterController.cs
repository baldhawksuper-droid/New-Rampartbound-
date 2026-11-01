using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Data;
using FantasyDefense.GridSystem;
using FantasyDefense.Core;

namespace FantasyDefense.Combat
{
    public enum BossArchetype
    {
        TitanWarlord,   // Stage 10 Volcanic Final Boss (Malakor, The Void Tyrant / Titan Warlord)
        ForestGolem,    // Stage 4 Forest Mini-Boss (Ancient Ironwood Golem)
        FrostWyrm,      // Stage 8 Mountain Mini-Boss (Glacial Frost Wyrm)
        ObsidianMaw     // Stage 5 Checkpoint Mini-Boss (Abyssal Obsidian Maw)
    }

    /// <summary>
    /// Specialized Controller untuk Boss Monster (Stage 1-4 Forest Golem, Stage 1-8 Frost Wyrm, Stage 1-10 Titan Warlord).
    /// Mengekstensi MonsterController dengan AI perilaku dan variasi serangan khusus:
    /// 1. Forest Golem: Entangling Bramble (stagger/root) & Barkskin Fortification (-40% damage resistance pada 50% HP).
    /// 2. Frost Wyrm: Glacial Blizzard Breath (chill/slow ke hero) & Permafrost Shards ground slam.
    /// 3. Titan Warlord: Ground Smash dengan Charred Fissure saat enrage & Dragon Firestorm.
    /// </summary>
    public class BossMonsterController : MonsterController
    {
        [Header("Boss Identity")]
        [SerializeField] private BossArchetype bossArchetype = BossArchetype.TitanWarlord;
        [SerializeField] private string bossTitle = "Panglima Orc Kegelapan";
        [SerializeField] private bool isFinalBoss = false;

        [Header("Mini-Boss Specific State")]
        private bool isBarkskinActive = false;
        private bool hasTriggeredBarkskin = false;
        private GameObject barkskinShieldObj;
        private Coroutine barkskinCoroutine;

        [Header("Boss AI Timers & Thresholds")]
        [SerializeField] private float groundSmashCooldown = 7.0f;
        [SerializeField] private float enrageThreshold = 0.35f; // Aktif saat HP <= 35%
        [SerializeField] private float flameBreathCooldown = 5.5f;

        [Header("Runtime State")]
        [SerializeField] private bool isEnraged = false;
        private float groundSmashTimer = 0f;
        private float flameBreathTimer = 0f;

        [Header("Enrage Magma Aura Visuals")]
        private GameObject magmaAuraObj;
        private SpriteRenderer magmaAuraSr;
        private Coroutine magmaPulseCoroutine;
        private Coroutine magmaEmberCoroutine;

        public BossArchetype Archetype => bossArchetype;
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

            // Deteksi arketipe boss berdasarkan nama atau data
            if (data != null)
            {
                if (data.monsterName.Contains("Golem") || data.monsterName.Contains("Ironwood") || data.monsterName.Contains("Forest"))
                {
                    bossArchetype = BossArchetype.ForestGolem;
                    isFinalBoss = false;
                    bossTitle = "Pelindung Hutan Purba (Ironwood Golem)";
                }
                else if (data.monsterName.Contains("Wyrm") || data.monsterName.Contains("Frost") || data.monsterName.Contains("Glacial"))
                {
                    bossArchetype = BossArchetype.FrostWyrm;
                    isFinalBoss = false;
                    bossTitle = "Naga Es Abadi Puncak Salju (Frost Wyrm)";
                }
                else if (data.monsterName.Contains("Obsidian") || data.monsterName.Contains("Maw"))
                {
                    bossArchetype = BossArchetype.ObsidianMaw;
                    isFinalBoss = false;
                    bossTitle = "Monster Magma Lembah Vulkanik (Obsidian Maw)";
                }
                else
                {
                    bossArchetype = BossArchetype.TitanWarlord;
                    isFinalBoss = true;
                    bossTitle = "Penguasa Kehampaan (Titan Warlord Malakor)";
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
            switch (bossArchetype)
            {
                case BossArchetype.ForestGolem:
                    HandleForestGolemAI();
                    break;
                case BossArchetype.FrostWyrm:
                    HandleFrostWyrmAI();
                    break;
                case BossArchetype.ObsidianMaw:
                case BossArchetype.TitanWarlord:
                default:
                    HandleTitanWarlordAI();
                    break;
            }
        }

        private void HandleForestGolemAI()
        {
            groundSmashTimer -= Time.deltaTime;
            if (groundSmashTimer <= 0f)
            {
                groundSmashTimer = isEnraged ? (groundSmashCooldown * 0.70f) : groundSmashCooldown;
                ExecuteForestGolemEntanglingBramble();
            }
        }

        private void HandleFrostWyrmAI()
        {
            // 1. Skill Permafrost Shards
            groundSmashTimer -= Time.deltaTime;
            if (groundSmashTimer <= 0f)
            {
                groundSmashTimer = isEnraged ? (groundSmashCooldown * 0.70f) : groundSmashCooldown;
                ExecuteFrostWyrmPermafrostShards();
            }

            // 2. Skill Glacial Blizzard Breath
            flameBreathTimer -= Time.deltaTime;
            if (flameBreathTimer <= 0f)
            {
                flameBreathTimer = isEnraged ? (flameBreathCooldown * 0.75f) : flameBreathCooldown;
                ExecuteFrostWyrmGlacialBlizzard();
            }
        }

        private void HandleTitanWarlordAI()
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
            float finalDamage = damage;
            if (isBarkskinActive)
            {
                finalDamage *= 0.60f; // 40% damage resistance saat Barkskin Fortification aktif
            }

            base.TakeDamage(finalDamage, isCrit);

            if (!isDead)
            {
                OnBossHealthChanged?.Invoke(this, currentHealth, MaxHealth);

                // Forest Golem Barkskin Fortification aktif saat HP <= 50%
                if (bossArchetype == BossArchetype.ForestGolem && !hasTriggeredBarkskin && HealthPercent <= 0.50f)
                {
                    TriggerBarkskinShield();
                }

                // Cek aktivasi fase Enrage saat sisa HP turun di bawah threshold
                if (!isEnraged && HealthPercent <= enrageThreshold)
                {
                    TriggerEnragePhase();
                }
            }
        }

        public override Color GetEffectiveThemeColor()
        {
            Color baseCol = base.GetEffectiveThemeColor();
            if (isEnraged)
            {
                return Color.Lerp(baseCol, new Color(1f, 0.15f, 0.15f, 1f), 0.65f);
            }
            return baseCol;
        }

        private void TriggerEnragePhase()
        {
            isEnraged = true;
            Debug.Log($"[BossMonsterController] {monsterData?.monsterName ?? "Boss"} memasuki FASE ENRAGE!");

            // Buff stat kecepatan gerak +40%
            currentMoveSpeed *= 1.40f;

            // Visual feedback: Boss membara merah menyala (berbasis kombinasi warna tema)
            if (spriteRenderer != null)
            {
                spriteRenderer.color = GetEffectiveThemeColor();
            }

            // Inisialisasi Cincin Magma Membara (Magma Enrage Aura) di bawah kaki Boss
            SpawnMagmaEnrageAura();

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

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null && FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Heavy);
            }

            // Efek guncangan kamera khusus Boss Ground Smash dengan mikro-rotasi (Point 4 - Boss Impact Screen-Shake Twist)
            if (MobileCameraAdapter.Instance != null)
            {
                if (isEnraged)
                {
                    MobileCameraAdapter.Instance.TriggerBossScreenShake(0.55f, 0.45f, 1.10f);
                }
                else
                {
                    MobileCameraAdapter.Instance.TriggerBossScreenShake(0.40f, 0.32f, 0.70f);
                }
            }

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnSkillParticles(transform.position, UnitType.RoseTanker, UnitTier.Tier3);
                SkillVfxManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.2f, isEnraged ? "★ MAGMA GROUND SMASH! ★" : "★ GROUND SMASH! ★", Color.red, 1.35f);
                SkillVfxManager.Instance.SpawnBossGroundSmashDebris(transform.position);

                if (isEnraged)
                {
                    SkillVfxManager.Instance.SpawnBossEnrageGroundFissure(transform.position);
                }
            }

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.Earthshaker);
                if (isEnraged)
                {
                    Audio.AudioManager.Instance.PlaySound(Audio.SoundType.CastleHitBoss, 1.10f);
                }
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

        #region Forest Golem Mini-Boss Skills

        private void ExecuteForestGolemEntanglingBramble()
        {
            Debug.Log($"[BossMonsterController] Forest Golem melancarkan Entangling Bramble!");
            OnBossSkillUsed?.Invoke(this, "ENTANGLING BRAMBLE!");

            if (attackSprite != null && spriteRenderer != null)
            {
                StartCoroutine(BossSkillAttackPoseRoutine());
            }

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null && FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Medium);
            }

            if (MobileCameraAdapter.Instance != null)
            {
                MobileCameraAdapter.Instance.TriggerBossScreenShake(0.35f, 0.25f, 0.60f);
            }

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                    transform.position,
                    new Color(0.18f, 0.65f, 0.22f), // Forest vine emerald
                    new Color(0.60f, 0.90f, 0.40f), // Leaf green
                    28,
                    3.2f
                );
                SkillVfxManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.2f, "🌿 ENTANGLING BRAMBLE! 🌿", new Color(0.25f, 0.85f, 0.35f), 1.35f);
            }

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.AltarWhooshClose, 0.85f);
            }

            // Disrupsi hero terdekat dengan root tangle
            float rootRadius = 2.8f;
            foreach (var hero in UnitController.ActiveUnits)
            {
                if (hero != null && Vector3.Distance(transform.position, hero.transform.position) <= rootRadius)
                {
                    hero.ApplyCrowdControl(1.6f, "🌿 ENTANGLED!");
                }
            }

            if (CastleBarricade.Instance != null && Vector3.Distance(transform.position, CastleBarricade.Instance.transform.position) <= 3.5f)
            {
                CastleBarricade.Instance.TakeDamage(12);
            }
        }

        private void TriggerBarkskinShield()
        {
            hasTriggeredBarkskin = true;
            Debug.Log($"[BossMonsterController] Forest Golem mengaktifkan Barkskin Fortification!");
            OnBossSkillUsed?.Invoke(this, "BARKSKIN FORTIFICATION!");

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null && FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Warning);
            }

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                    transform.position,
                    new Color(0.85f, 0.65f, 0.20f), // Amber bark
                    new Color(0.30f, 0.85f, 0.30f), // Jade glow
                    35,
                    3.5f
                );
                SkillVfxManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.4f, "🛡️ BARKSKIN FORTIFICATION! (-40% DMG)", new Color(0.55f, 0.95f, 0.50f), 1.45f);
            }

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.AltarWhooshOpen, 0.80f);
            }

            if (barkskinCoroutine != null) StopCoroutine(barkskinCoroutine);
            barkskinCoroutine = StartCoroutine(BarkskinShieldRoutine());
        }

        private IEnumerator BarkskinShieldRoutine()
        {
            isBarkskinActive = true;

            if (barkskinShieldObj == null)
            {
                barkskinShieldObj = new GameObject("BarkskinShieldAura");
                barkskinShieldObj.transform.SetParent(transform, false);
                barkskinShieldObj.transform.localPosition = new Vector3(0f, 0f, -0.05f);
                barkskinShieldObj.transform.localScale = Vector3.one * 1.35f;

                var sr = barkskinShieldObj.AddComponent<SpriteRenderer>();
                sr.sprite = Resources.Load<Sprite>("Sprites/VFX/VFX_Aura_Magma_Ring");
                sr.sortingLayerName = "Gem";
                if (spriteRenderer != null) sr.sortingOrder = spriteRenderer.sortingOrder + 1;
                sr.color = new Color(0.35f, 0.85f, 0.40f, 0.80f);
            }

            float duration = 5.0f;
            float elapsed = 0f;
            while (elapsed < duration && !isDead)
            {
                elapsed += Time.deltaTime;
                if (barkskinShieldObj != null)
                {
                    float pulse = 1.30f + Mathf.PingPong(elapsed * 2.5f, 0.15f);
                    barkskinShieldObj.transform.localScale = new Vector3(pulse, pulse, 1f);
                }
                yield return null;
            }

            isBarkskinActive = false;
            if (barkskinShieldObj != null)
            {
                Destroy(barkskinShieldObj);
                barkskinShieldObj = null;
            }
        }

        #endregion

        #region Frost Wyrm Mini-Boss Skills

        private void ExecuteFrostWyrmGlacialBlizzard()
        {
            Debug.Log($"[BossMonsterController] Frost Wyrm menyemburkan Glacial Blizzard!");
            OnBossSkillUsed?.Invoke(this, "GLACIAL BLIZZARD!");

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                    transform.position,
                    new Color(0.40f, 0.85f, 1.0f), // Glacial cyan
                    new Color(0.90f, 0.98f, 1.0f), // Frost white
                    36,
                    4.2f
                );
                SkillVfxManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.2f, "❄️ GLACIAL BLIZZARD! ❄️", new Color(0.45f, 0.90f, 1.0f), 1.4f);
            }

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.BladeTempest, 1.25f);
            }

            // Perlambat ritme serang hero dengan hawa dingin beku
            float blizzardRadius = 3.8f;
            foreach (var hero in UnitController.ActiveUnits)
            {
                if (hero != null && Vector3.Distance(transform.position, hero.transform.position) <= blizzardRadius)
                {
                    hero.ApplyCrowdControl(1.4f, "❄️ CHILLED!");
                }
            }

            if (CastleBarricade.Instance != null && Vector3.Distance(transform.position, CastleBarricade.Instance.transform.position) <= 4.0f)
            {
                CastleBarricade.Instance.TakeDamage(16);
            }
        }

        private void ExecuteFrostWyrmPermafrostShards()
        {
            Debug.Log($"[BossMonsterController] Frost Wyrm menghempaskan Permafrost Shards!");
            OnBossSkillUsed?.Invoke(this, "PERMAFROST SHARDS!");

            if (attackSprite != null && spriteRenderer != null)
            {
                StartCoroutine(BossSkillAttackPoseRoutine());
            }

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null && FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Medium);
            }

            if (MobileCameraAdapter.Instance != null)
            {
                MobileCameraAdapter.Instance.TriggerBossScreenShake(0.38f, 0.30f, 0.75f);
            }

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                    transform.position,
                    new Color(0.10f, 0.80f, 0.95f), // Cyan ice
                    new Color(0.85f, 0.95f, 1.0f),  // Crystal shard
                    30,
                    3.8f
                );
                SkillVfxManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.2f, "🧊 PERMAFROST SHARDS! 🧊", new Color(0.60f, 0.95f, 1.0f), 1.35f);
            }

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.Earthshaker, 1.35f);
            }

            // Stun hero terdekat
            float smashRadius = 2.6f;
            foreach (var hero in UnitController.ActiveUnits)
            {
                if (hero != null && Vector3.Distance(transform.position, hero.transform.position) <= smashRadius)
                {
                    hero.ApplyCrowdControl(1.2f, "🧊 FROZEN!");
                }
            }

            if (CastleBarricade.Instance != null && Vector3.Distance(transform.position, CastleBarricade.Instance.transform.position) <= 3.5f)
            {
                CastleBarricade.Instance.TakeDamage(14);
            }
        }

        #endregion

        #region Magma Enrage Aura Systems

        private void SpawnMagmaEnrageAura()
        {
            if (magmaAuraObj == null)
            {
                magmaAuraObj = new GameObject("MagmaEnrageAura");
                magmaAuraObj.transform.SetParent(transform, false);
                magmaAuraObj.transform.localPosition = new Vector3(0f, -0.55f, 0.05f);
                magmaAuraObj.transform.localScale = new Vector3(1.4f, 0.9f, 1f);

                magmaAuraSr = magmaAuraObj.AddComponent<SpriteRenderer>();
                magmaAuraSr.sprite = Resources.Load<Sprite>("Sprites/VFX/VFX_Aura_Magma_Ring");
                magmaAuraSr.sortingLayerName = "Gem";
                if (spriteRenderer != null)
                {
                    magmaAuraSr.sortingOrder = spriteRenderer.sortingOrder - 1;
                }

                Color initialColor = bossArchetype switch
                {
                    BossArchetype.ForestGolem => new Color(0.25f, 0.85f, 0.35f, 0.85f),
                    BossArchetype.FrostWyrm => new Color(0.35f, 0.85f, 1.0f, 0.85f),
                    _ => new Color(1f, 0.45f, 0.1f, 0.85f)
                };
                magmaAuraSr.color = initialColor;
            }

            if (magmaPulseCoroutine != null) StopCoroutine(magmaPulseCoroutine);
            magmaPulseCoroutine = StartCoroutine(MagmaAuraPulseRoutine());

            if (magmaEmberCoroutine != null) StopCoroutine(magmaEmberCoroutine);
            magmaEmberCoroutine = StartCoroutine(MagmaEmberEruptionRoutine());
        }

        private System.Collections.IEnumerator MagmaAuraPulseRoutine()
        {
            float timer = 0f;
            Color lowColor = bossArchetype switch
            {
                BossArchetype.ForestGolem => new Color(0.15f, 0.65f, 0.20f, 0.70f),
                BossArchetype.FrostWyrm => new Color(0.20f, 0.65f, 0.90f, 0.70f),
                _ => new Color(1f, 0.35f, 0.05f, 0.75f)
            };
            Color highColor = bossArchetype switch
            {
                BossArchetype.ForestGolem => new Color(0.60f, 0.95f, 0.40f, 0.95f),
                BossArchetype.FrostWyrm => new Color(0.70f, 0.95f, 1.0f, 0.95f),
                _ => new Color(1f, 0.85f, 0.20f, 0.95f)
            };

            while (isEnraged && !isDead)
            {
                timer += Time.deltaTime * 5f;
                float pulse = 1.35f + Mathf.Sin(timer) * 0.15f;
                if (magmaAuraObj != null)
                {
                    magmaAuraObj.transform.localScale = new Vector3(pulse, pulse * 0.65f, 1f);
                }
                if (magmaAuraSr != null)
                {
                    magmaAuraSr.color = Color.Lerp(
                        lowColor,
                        highColor,
                        (Mathf.Sin(timer) + 1f) * 0.5f
                    );
                }
                yield return null;
            }
        }

        private System.Collections.IEnumerator MagmaEmberEruptionRoutine()
        {
            var wait = new WaitForSeconds(0.35f);
            Color sparkMain = bossArchetype switch
            {
                BossArchetype.ForestGolem => new Color(0.25f, 0.80f, 0.25f),
                BossArchetype.FrostWyrm => new Color(0.20f, 0.85f, 1.0f),
                _ => new Color(1f, 0.3f, 0.05f)
            };
            Color sparkSec = bossArchetype switch
            {
                BossArchetype.ForestGolem => new Color(0.85f, 0.95f, 0.40f),
                BossArchetype.FrostWyrm => new Color(0.90f, 0.98f, 1.0f),
                _ => new Color(1f, 0.85f, 0.2f)
            };

            while (isEnraged && !isDead)
            {
                yield return wait;
                if (SkillVfxManager.Instance != null)
                {
                    Vector3 sparkPos = transform.position + new Vector3(UnityEngine.Random.Range(-0.35f, 0.35f), -0.4f, 0);
                    SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                        sparkPos,
                        sparkMain,
                        sparkSec,
                        5,
                        2.2f
                    );
                }
            }
        }

        private void CleanupMagmaAura()
        {
            if (magmaPulseCoroutine != null) StopCoroutine(magmaPulseCoroutine);
            if (magmaEmberCoroutine != null) StopCoroutine(magmaEmberCoroutine);
            if (magmaAuraObj != null)
            {
                Destroy(magmaAuraObj);
                magmaAuraObj = null;
            }

            if (barkskinCoroutine != null) StopCoroutine(barkskinCoroutine);
            if (barkskinShieldObj != null)
            {
                Destroy(barkskinShieldObj);
                barkskinShieldObj = null;
            }
        }

        #endregion

        protected override void Die()
        {
            if (isDead) return;

            CleanupMagmaAura();

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

        protected override void OnDestroy()
        {
            CleanupMagmaAura();
            base.OnDestroy();
        }
    }
}

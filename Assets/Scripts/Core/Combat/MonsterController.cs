using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Economy;
using FantasyDefense.Metagame;
using FantasyDefense.Wave;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Mengontrol perilaku fisik, AI gerak susur lintasan waypoint, sistem HP bar,
    /// serta respon efek status (Stun, Slow, Burn) pada monster serbuan.
    /// Memelihara daftar statis ActiveMonsters untuk efisiensi targeting para pahlawan.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class MonsterController : MonoBehaviour
    {
        // Daftar global monster yang sedang aktif di arena (untuk targeting hero)
        public static readonly List<MonsterController> ActiveMonsters = new List<MonsterController>();

        /// <summary>
        /// Mengembalikan salinan aman daftar monster aktif untuk iterasi anti-koleksi modifikasi (InvalidOperationException).
        /// </summary>
        public static List<MonsterController> GetActiveMonstersSnapshot()
        {
            return new List<MonsterController>(ActiveMonsters);
        }

        [Header("Monster Data Reference")]
        [SerializeField] protected MonsterDataSO monsterData;

        [Header("Runtime Stats")]
        [SerializeField] protected float currentHealth;
        [SerializeField] protected float currentMoveSpeed;

        [Header("Components")]
        [SerializeField] protected SpriteRenderer spriteRenderer;

        [Header("Status Effect States")]
        [SerializeField] protected bool isStunned = false;
        [SerializeField] protected bool isSlowed = false;
        [SerializeField] protected bool isBurned = false;
        [SerializeField] protected float defenseDebuff = 0f;

        [Header("Elite Mutation Affix (Sprint 13)")]
        [SerializeField] protected EliteMonsterAffix eliteAffix;

        [Header("Procedural Walk Animation Juice & Frame Cycling")]
        [SerializeField] protected Sprite[] walkFrames;
        protected float walkCycleTimer = 0f;
        protected int currentWalkFrame = 0;
        protected Vector3 baseScale = Vector3.one;

        [Header("Hero Combat Interaction")]
        [SerializeField] protected float heroEngagementRange = 1.35f;
        [SerializeField] protected float heroAttackInterval = 1.0f;
        protected float heroAttackTimer = 0f;
        protected UnitController currentEngagedHero = null;

        [Header("Barricade Combat Interaction")]
        [SerializeField] protected bool isAttackingBarricade = false;
        [SerializeField] protected bool isAttackingHero = false;
        public bool IsAttackingHero => isAttackingHero;
        [SerializeField] protected float barricadeAttackInterval = 1.2f;
        protected float barricadeAttackTimer = 0f;

        [Header("Siege Mini HP Bar (Point 3)")]
        [SerializeField] protected GameObject miniHpBarRoot;
        [SerializeField] protected SpriteRenderer miniHpBarFillRenderer;
        [SerializeField] protected SpriteRenderer miniHpBarBgRenderer;
        protected static Sprite cachedMiniHpBarSprite;

        protected Vector3[] waypoints;
        protected int currentWaypointIndex = 0;
        protected bool isDead = false;

        public MonsterDataSO Data => monsterData;
        public EliteMonsterAffix EliteAffix => eliteAffix;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => monsterData != null ? monsterData.maxHealth : 100f;
        public float HealthPercent => currentHealth / Mathf.Max(1f, MaxHealth);
        public bool IsDead => isDead;
        public bool IsAlive => !isDead && currentHealth > 0;
        public bool IsStunned => isStunned;
        public bool IsBurned => isBurned;
        public float DefenseDebuff => defenseDebuff;
        public bool IsAttackingBarricade => isAttackingBarricade;

        public static event Action<MonsterController> OnMonsterSpawned;
        public static event Action<MonsterController> OnMonsterKilled;
        public static event Action<MonsterController> OnMonsterDied
        {
            add => OnMonsterKilled += value;
            remove => OnMonsterKilled -= value;
        }
        public static event Action<MonsterController, int> OnMonsterReachedBarricade; // (monster, damage)
        public static event Action<MonsterController, float, bool> OnMonsterDamaged; // (monster, finalDamage, isCrit)

        protected virtual void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (eliteAffix == null)
                eliteAffix = GetComponent<EliteMonsterAffix>();
        }

        protected virtual void OnEnable()
        {
            if (!ActiveMonsters.Contains(this))
                ActiveMonsters.Add(this);
        }

        protected virtual void OnDisable()
        {
            ActiveMonsters.Remove(this);
        }

        [Header("Formation Spread & Depth Sorting")]
        [SerializeField] protected float laneOffset = 0f;

        public virtual void Initialize(MonsterDataSO data, Vector3[] pathWaypoints, float lateralOffset = 0f)
        {
            monsterData = data;
            waypoints = pathWaypoints;
            laneOffset = lateralOffset;
            currentWaypointIndex = 0;
            isDead = false;

            if (data != null)
            {
                currentHealth = data.maxHealth;
                float speedMult = BattlefieldWeatherManager.Instance != null ? BattlefieldWeatherManager.Instance.GetMonsterSpeedMultiplier() : 1.0f;

                // Kurva Kecepatan Level 1-1 Wave 1 (Sprint 43):
                // Pacing awal ramah (80% kecepatan jalan Creep) agar pemain baru dapat mengagumi aksi tempur pertama pahlawan T1
                var currentStage = StageManager.Instance != null ? StageManager.Instance.GetSelectedStage() : null;
                bool isStage1 = currentStage == null || currentStage.stageId == 101 || currentStage.stageNumber == 1;
                bool isWave1 = WaveSpawner.Instance == null || WaveSpawner.Instance.CurrentWaveIndex == 0;
                float pacingSpeedMult = (isStage1 && isWave1 && data.monsterType == MonsterType.Creep) ? 0.80f : 1.0f;

                currentMoveSpeed = data.moveSpeed * speedMult * pacingSpeedMult;

                if (spriteRenderer == null)
                    spriteRenderer = GetComponentInChildren<SpriteRenderer>();

                if (spriteRenderer != null)
                {
                    if (data.monsterSprite != null)
                    {
                        spriteRenderer.sprite = data.monsterSprite;
                    }
                    else
                    {
                        spriteRenderer.sprite = Resources.Load<Sprite>($"Sprites/Monsters/Monster_{data.monsterType}");
                    }

                    if (spriteRenderer.sprite == null)
                    {
                        spriteRenderer.sprite = GetOrCreateProceduralMonsterSprite(data.monsterType);
                    }

                    spriteRenderer.color = data.themeColor != default ? data.themeColor : Color.white;

                    // Konfigurasi siklus 2 frame berjalan
                    if (data.monsterWalkSprite != null && data.monsterSprite != null)
                    {
                        walkFrames = new Sprite[] { data.monsterSprite, data.monsterWalkSprite };
                    }
                    else
                    {
                        string walkName = $"Monster_{data.monsterType}_Walk";
                        Sprite walkSpr = Resources.Load<Sprite>($"Sprites/Monsters/{walkName}");
                        if (walkSpr != null && spriteRenderer.sprite != null)
                        {
                            walkFrames = new Sprite[] { spriteRenderer.sprite, walkSpr };
                        }
                    }
                }

                float scale = data.visualScale > 0.05f ? data.visualScale : 0.22f;

                // Standar Proporsional Chibi Anime Fantasy:
                // Ukuran monster di World Space harus pas di dalam lajur jalan (Lane Width ~0.75m)
                // Creep: ~0.44m (imut & gesit dalam gerombolan), Brute: ~0.62m (kokoh berlapis zirah), Boss: ~1.10m (megah berwibawa)
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    float spriteW = spriteRenderer.sprite.bounds.size.x;
                    if (spriteW > 0.01f)
                    {
                        float targetWidth = data.monsterType switch
                        {
                            MonsterType.Creep => 0.44f,
                            MonsterType.Brute => 0.62f,
                            MonsterType.Boss => 1.10f,
                            _ => 0.48f
                        };
                        scale = targetWidth / spriteW;
                    }
                }

                baseScale = Vector3.one * scale;
                transform.localScale = baseScale;

                // Load frame animasi berjalan multi-frame jika tersedia
                if (walkFrames == null || walkFrames.Length == 0)
                {
                    var loadedFrames = Resources.LoadAll<Sprite>($"Sprites/Monsters/Monster_{data.monsterType}_Walk");
                    if (loadedFrames != null && loadedFrames.Length > 0)
                    {
                        walkFrames = loadedFrames;
                    }
                }
            }

            if (waypoints != null && waypoints.Length > 0)
            {
                transform.position = waypoints[0] + new Vector3(laneOffset, 0f, 0f);
            }

            UpdateSortingOrder();

            OnMonsterSpawned?.Invoke(this);
        }

        public virtual void Initialize(MonsterDataSO data, Vector3[] pathWaypoints)
        {
            Initialize(data, pathWaypoints, 0f);
        }

        protected virtual void Update()
        {
            if (isDead || isStunned) return;

            // Prioritas Tempur Barikade: Monster yang sudah menempel di gerbang fokus mendobrak gerbang
            if (isAttackingBarricade)
            {
                AttackBarricade();
                return;
            }

            // Prioritas Tempur: Cek apakah monster sudah mengunci pertarungan dengan pahlawan penghadang (Sticky Engagement)
            if (currentEngagedHero != null && currentEngagedHero.IsAlive && currentEngagedHero.gameObject.activeInHierarchy)
            {
                // Hard Body-Blocking Barrier: Monster tidak boleh melangkah menembus melewati garis tubuh pahlawan
                float minBlockingY = currentEngagedHero.transform.position.y + 0.35f;
                if (transform.position.y < minBlockingY)
                {
                    transform.position = new Vector3(transform.position.x, minBlockingY, transform.position.z);
                }

                isAttackingHero = true;
                EngageHero(currentEngagedHero);
                return;
            }
            else
            {
                currentEngagedHero = null;
            }

            // Deteksi pahlawan (hero defender) yang menghadang di lajur depan
            UnitController blockingHero = FindBlockingHero();
            if (blockingHero != null && blockingHero.IsAlive)
            {
                currentEngagedHero = blockingHero;
                isAttackingHero = true;
                EngageHero(blockingHero);
                return;
            }
            else
            {
                isAttackingHero = false;
            }

            MoveAlongWaypoints();
        }

        /// <summary>
        /// Mencari pahlawan (UnitController) terdekat yang berada di depan monster untuk dihadang dan diserang terlebih dahulu.
        /// Menggunakan deteksi body-blocking berbasis lajur (lane) horizontal & vertikal agar monster tidak menyelinap lewat sisi tepi.
        /// </summary>
        protected virtual UnitController FindBlockingHero()
        {
            var heroes = UnitController.GetActiveUnitsSnapshot();
            UnitController bestHero = null;
            float closestDist = heroEngagementRange;
            Vector3 myPos = transform.position;

            for (int i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero == null || !hero.IsAlive || hero.CurrentTier == UnitTier.Tier0 || !hero.gameObject.activeInHierarchy) continue;

                float dx = Mathf.Abs(hero.transform.position.x - myPos.x);
                float dy = myPos.y - hero.transform.position.y; // bernilai positif jika monster berada di atas hero (menuju hero)
                float dist = Vector3.Distance(myPos, hero.transform.position);

                // Body-Blocking Hard Gate:
                // 1. Hero berada di radius lingkaran benturan melee (dist <= closestDist)
                // 2. ATAU monster berada di lajur yang sama atau petak bersebelahan (|dx| <= 1.25f) dan sedang melaju turun tepat di depan hero (dy antara -0.3f dan 1.6f)
                bool isInLaneFront = (dx <= 1.25f && dy >= -0.3f && dy <= 1.6f);

                if (dist <= closestDist || isInLaneFront)
                {
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestHero = hero;
                    }
                    else if (bestHero == null)
                    {
                        bestHero = hero;
                    }
                }
            }

            return bestHero;
        }

        /// <summary>
        /// Menghentikan gerak monster untuk menyerang pahlawan yang menghadang.
        /// </summary>
        protected virtual void EngageHero(UnitController hero)
        {
            // Menghadap ke arah hero
            if (spriteRenderer != null)
            {
                float dx = hero.transform.position.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.05f)
                {
                    spriteRenderer.flipX = dx > 0;
                }
            }

            heroAttackTimer -= Time.deltaTime;
            if (heroAttackTimer <= 0f)
            {
                heroAttackTimer = heroAttackInterval;

                // Animasi serangan menerjang ke hero (Attack Lunge)
                StartCoroutine(AttackHeroLungeRoutine(hero));

                // Berikan damage serangan monster ke hero
                int monsterAtk = monsterData != null ? monsterData.castleDamage : 10;
                hero.TakeDamage(monsterAtk);

                // Mainkan SFX benturan serangan
                if (FantasyDefense.Audio.AudioManager.Instance != null)
                {
                    FantasyDefense.Audio.AudioManager.Instance.PlaySound(FantasyDefense.Audio.SoundType.CastleHit);
                }
            }

            UpdateSortingOrder();
        }

        private System.Collections.IEnumerator AttackHeroLungeRoutine(UnitController targetHero)
        {
            if (targetHero == null) yield break;
            Vector3 startPos = transform.position;
            // Lunge dibatasi agar tidak meluncur melewati posisi pahlawan
            float targetY = Mathf.Max(startPos.y - 0.25f, targetHero.transform.position.y + 0.35f);
            Vector3 lungeTarget = new Vector3(
                Mathf.Lerp(startPos.x, targetHero.transform.position.x, 0.45f),
                targetY,
                startPos.z
            );

            float t = 0f;
            while (t < 0.10f)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, lungeTarget, t / 0.10f);
                yield return null;
            }

            t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(lungeTarget, startPos, t / 0.15f);
                yield return null;
            }
            transform.position = startPos;
        }

        protected virtual void MoveAlongWaypoints()
        {
            if (isAttackingHero || isAttackingBarricade) return;
            if (waypoints == null || currentWaypointIndex >= waypoints.Length) return;

            Vector3 targetPoint = waypoints[currentWaypointIndex] + new Vector3(laneOffset, 0f, 0f);
            Vector3 moveDirection = (targetPoint - transform.position).normalized;

            // Flip visual sprite berdasarkan arah horizontal
            if (spriteRenderer != null && Mathf.Abs(moveDirection.x) > 0.05f)
            {
                spriteRenderer.flipX = moveDirection.x > 0;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPoint,
                currentMoveSpeed * Time.deltaTime
            );

            // Animasi langkah berjalan procedural (Bobbing, Tilting & Squash-Stretch)
            walkCycleTimer += Time.deltaTime * (currentMoveSpeed * 3.2f);
            float stepBob = Mathf.Abs(Mathf.Sin(walkCycleTimer * Mathf.PI)) * 0.06f;
            float stepTilt = Mathf.Sin(walkCycleTimer * Mathf.PI) * 3.0f * (spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f);
            float squash = Mathf.Sin(walkCycleTimer * Mathf.PI * 2f) * 0.04f;

            if (spriteRenderer != null)
            {
                // Jika spriteRenderer berada pada child GameObject "Visual"
                if (spriteRenderer.transform != transform)
                {
                    spriteRenderer.transform.localPosition = new Vector3(0f, stepBob, 0f);
                    spriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, stepTilt);
                    spriteRenderer.transform.localScale = new Vector3(1f + squash, 1f - squash, 1f);
                }
                else
                {
                    // Fallback aman jika spriteRenderer di root objek: JANGAN sentuh localPosition!
                    transform.localScale = new Vector3(baseScale.x * (1f + squash), baseScale.y * (1f - squash), baseScale.z);
                    transform.localRotation = Quaternion.Euler(0f, 0f, stepTilt);
                }

                // Multi-frame animation cycle jika frame sprite tersedia
                if (walkFrames != null && walkFrames.Length > 1)
                {
                    int frameIndex = Mathf.FloorToInt(walkCycleTimer) % walkFrames.Length;
                    if (frameIndex != currentWalkFrame && walkFrames[frameIndex] != null)
                    {
                        currentWalkFrame = frameIndex;
                        spriteRenderer.sprite = walkFrames[frameIndex];
                    }
                }
            }

            UpdateSortingOrder();

            // Cek apakah sudah tiba di waypoint target
            if (Vector3.Distance(transform.position, targetPoint) < 0.15f)
            {
                currentWaypointIndex++;

                // Jika telah mencapai titik akhir lintasan (Benteng Pertahanan)
                if (currentWaypointIndex >= waypoints.Length)
                {
                    ReachBarricade();
                }
            }
        }

        public void UpdateSortingOrder()
        {
            if (spriteRenderer != null)
            {
                // Dynamic depth sorting: monster di bawah layar tampil di depan monster di atasnya.
                // Menggunakan Sorting Layer "Gem" dengan base 25 menjamin monster selalu berada di atas background, board, dan grid cell.
                spriteRenderer.sortingLayerName = "Gem";
                spriteRenderer.sortingOrder = 25 - Mathf.RoundToInt(transform.position.y * 2);
            }
        }

        protected virtual void ReachBarricade()
        {
            if (isDead || isAttackingBarricade) return;

            // Sesuai Instruksi Komandan (Poin 1 & 6):
            // Monster yang mencapai gerbang TETAP HIDUP dan terus mendobrak benteng hingga gugur dalam pertempuran!
            // Stagger timer serangan awal (0.35s - 1.0s) agar pukulan rombongan monster terdistribusi secara ritmis (DPS)
            isAttackingBarricade = true;
            barricadeAttackTimer = UnityEngine.Random.Range(0.35f, 1.0f);

            // Tampilkan Mini HP Bar di atas kepala monster pengepung (Point 3)
            EnsureMiniHpBar();
            UpdateMiniHpBar();

            int damage = GetCalculatedBarricadeDamage();
            OnMonsterReachedBarricade?.Invoke(this, damage);
        }

        protected virtual int GetCalculatedBarricadeDamage()
        {
            if (monsterData == null) return 5;
            int baseDmg = monsterData.castleDamage;

            // Kalibrasi Pacing DPS Benteng (Point 6):
            // Creep: 2-4 damage per pukulan (gesit tapi ringan)
            // Brute: 6-9 damage per pukulan (berat berlapis baja)
            // Boss: Sesuai data boss (15-25 damage hantaman destruktif)
            return monsterData.monsterType switch
            {
                MonsterType.Creep => Mathf.Clamp(baseDmg, 2, 4),
                MonsterType.Brute => Mathf.Clamp(baseDmg, 6, 9),
                _ => baseDmg
            };
        }

        protected virtual void AttackBarricade()
        {
            if (CastleBarricade.Instance == null || CastleBarricade.Instance.CurrentHealth <= 0) return;

            barricadeAttackTimer -= Time.deltaTime;
            if (barricadeAttackTimer <= 0f)
            {
                barricadeAttackTimer = barricadeAttackInterval;

                int damage = GetCalculatedBarricadeDamage();
                CastleBarricade.Instance.TakeDamage(damage);

                // Mainkan animasi menerjang dan hentakan debu ke gerbang benteng (Point 5)
                StartCoroutine(AttackBarricadeLungeRoutine());

                // Mainkan SFX benturan ke benteng bervariasi sesuai jenis monster & haptic ponsel (Point 4)
                if (FantasyDefense.Audio.AudioManager.Instance != null)
                {
                    FantasyDefense.Audio.AudioManager.Instance.PlayCastleHit(monsterData != null ? monsterData.monsterType : MonsterType.Creep);
                }

                // Guncangan mikro-rotasi dramatis jika Boss menyerang barikade (Point 4 - Boss Impact Screen-Shake Twist)
                if (monsterData != null && monsterData.monsterType == MonsterType.Boss)
                {
                    if (FantasyDefense.Core.MobileCameraAdapter.Instance != null)
                    {
                        FantasyDefense.Core.MobileCameraAdapter.Instance.TriggerBossScreenShake(0.38f, 0.30f, 0.65f);
                    }
                }
            }

            UpdateSortingOrder();
        }

        private System.Collections.IEnumerator AttackBarricadeLungeRoutine()
        {
            Vector3 startPos = transform.position;
            Vector3 lungeTarget = startPos + Vector3.down * 0.25f;

            float t = 0f;
            while (t < 0.10f)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, lungeTarget, t / 0.10f);
                yield return null;
            }

            // Hentakan Debu & Pukulan saat menghantam gerbang benteng (Divisi Art & Visual - Point 5)
            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                    transform.position + Vector3.down * 0.22f,
                    new Color(0.85f, 0.78f, 0.65f), // Dust tan
                    new Color(0.55f, 0.50f, 0.42f), // Ash stone
                    8,
                    2.0f
                );
            }

            t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(lungeTarget, startPos, t / 0.15f);
                yield return null;
            }
            transform.position = startPos;
        }

        public virtual void TakeDamage(float damage, bool isCrit = false)
        {
            if (isDead) return;

            // Jika damage bernilai negatif, diproses sebagai penyembuhan HP (misal Abyssal Healer)
            if (damage < 0f)
            {
                currentHealth = Mathf.Min(MaxHealth, currentHealth - damage);
                return;
            }

            // Periksa apakah serangan diserap oleh perisai mutasi Shielded (Sprint 13)
            if (eliteAffix != null && eliteAffix.TryAbsorbHit())
            {
                return;
            }

            // Efek penembusan armor/debuff pertahanan (misal Blade Tempest Cedric: +25% extra damage)
            float finalDamage = defenseDebuff > 0f ? damage * (1f + defenseDebuff) : damage;

            currentHealth -= finalDamage;
            StartCoroutine(FlashDamageColor(isCrit ? Color.yellow : Color.red));

            UpdateMiniHpBar();

            OnMonsterDamaged?.Invoke(this, finalDamage, isCrit);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            if (isDead) return;
            isDead = true;
            isAttackingBarricade = false;
            isAttackingHero = false;

            if (miniHpBarRoot != null)
            {
                Destroy(miniHpBarRoot);
            }

            // Segera hapus dari daftar monster aktif agar sinkronisasi jumlah musuh akurat seketika
            ActiveMonsters.Remove(this);

            // Berikan bonus reward jika ini monster varian elit (Sprint 13)
            if (eliteAffix != null)
            {
                eliteAffix.HandleEliteDefeated();
            }

            // Berikan reward gems dasar
            int reward = monsterData != null ? monsterData.gemsReward : 5;
            if (BattlefieldWeatherManager.Instance != null)
            {
                reward *= BattlefieldWeatherManager.Instance.GetMonsterRewardMultiplier();
            }

            if (EconomyManager.Instance != null && reward > 0)
            {
                EconomyManager.Instance.AddGems(reward);
            }

            OnMonsterKilled?.Invoke(this);
            Destroy(gameObject);
        }

        public void SetEliteAffix(EliteAffixType affix)
        {
            if (affix == EliteAffixType.None) return;

            if (eliteAffix == null)
            {
                eliteAffix = gameObject.AddComponent<EliteMonsterAffix>();
            }

            eliteAffix.InitializeAffix(affix);
        }

        public void ApplyStun(float duration)
        {
            if (isDead) return;
            StartCoroutine(StunRoutine(duration));
        }

        private IEnumerator StunRoutine(float duration)
        {
            isStunned = true;
            Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            if (spriteRenderer != null) spriteRenderer.color = new Color(0.7f, 0.7f, 1f, 1f); // Stun tint

            yield return new WaitForSeconds(duration);

            isStunned = false;
            if (spriteRenderer != null && !isDead) spriteRenderer.color = originalColor;
        }

        public void ApplySlow(float slowPercent, float duration)
        {
            if (isDead || isSlowed) return;
            StartCoroutine(SlowRoutine(slowPercent, duration));
        }

        private IEnumerator SlowRoutine(float slowPercent, float duration)
        {
            isSlowed = true;
            float baseSpeed = monsterData != null ? monsterData.moveSpeed : 1.5f;
            currentMoveSpeed = baseSpeed * (1f - Mathf.Clamp01(slowPercent));

            yield return new WaitForSeconds(duration);

            currentMoveSpeed = baseSpeed;
            isSlowed = false;
        }

        public void ApplyDefenseDebuff(float percent, float duration)
        {
            if (isDead) return;
            StartCoroutine(DefenseDebuffRoutine(percent, duration));
        }

        private IEnumerator DefenseDebuffRoutine(float percent, float duration)
        {
            defenseDebuff = Mathf.Max(defenseDebuff, percent);
            yield return new WaitForSeconds(duration);
            defenseDebuff = 0f;
        }

        public void ApplyBurn(float damagePerSecond, float duration)
        {
            if (isDead) return;
            StartCoroutine(BurnRoutine(damagePerSecond, duration));
        }

        private IEnumerator BurnRoutine(float damagePerSecond, float duration)
        {
            isBurned = true;
            float elapsed = 0f;
            float tickInterval = 0.5f;

            Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.5f, 0.2f, 1f); // Orange burn tint

            while (elapsed < duration && !isDead)
            {
                yield return new WaitForSeconds(tickInterval);
                elapsed += tickInterval;

                if (!isDead)
                {
                    TakeDamage(damagePerSecond * tickInterval, false);
                }
            }

            isBurned = false;
            if (spriteRenderer != null && !isDead) spriteRenderer.color = originalColor;
        }

        private IEnumerator FlashDamageColor(Color flashColor)
        {
            if (spriteRenderer == null) yield break;

            Color original = monsterData != null ? monsterData.themeColor : Color.white;
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(0.08f);
            if (spriteRenderer != null && !isDead) spriteRenderer.color = original;
        }

        private static readonly System.Collections.Generic.Dictionary<MonsterType, Sprite> cachedProceduralMonsterSprites = new System.Collections.Generic.Dictionary<MonsterType, Sprite>();

        /// <summary>
        /// Self-healing fallback: Menghasilkan sprite monster bertekstur warna kontras tinggi jika file sprite asli tidak terbaca.
        /// Menjamin monster TIDAK PERNAH tidak terlihat (never invisible).
        /// </summary>
        public static Sprite GetOrCreateProceduralMonsterSprite(MonsterType type)
        {
            if (cachedProceduralMonsterSprites.TryGetValue(type, out var cached) && cached != null)
            {
                return cached;
            }

            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color baseColor = type switch
            {
                MonsterType.Boss => new Color(0.95f, 0.15f, 0.2f, 1f),
                MonsterType.Brute => new Color(0.9f, 0.5f, 0.1f, 1f),
                _ => new Color(0.35f, 0.85f, 0.25f, 1f)
            };
            Color edgeColor = new Color(0.12f, 0.12f, 0.18f, 1f);

            float center = size / 2f;
            float radius = (size / 2f) - 4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (dist > radius - 6f)
                    {
                        tex.SetPixel(x, y, edgeColor);
                    }
                    else
                    {
                        float grad = 1f - (dist / radius) * 0.35f;
                        tex.SetPixel(x, y, baseColor * grad);
                    }
                }
            }
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            cachedProceduralMonsterSprites[type] = sprite;
            return sprite;
        }

        #region Siege Mini HP Bar System (Point 3)

        protected static Sprite GetOrCreateMiniHpBarSprite()
        {
            if (cachedMiniHpBarSprite != null) return cachedMiniHpBarSprite;
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Color[] colors = new Color[] { Color.white, Color.white, Color.white, Color.white };
            tex.SetPixels(colors);
            tex.Apply();
            cachedMiniHpBarSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
            return cachedMiniHpBarSprite;
        }

        protected virtual void EnsureMiniHpBar()
        {
            if (miniHpBarRoot != null)
            {
                miniHpBarRoot.SetActive(true);
                return;
            }

            miniHpBarRoot = new GameObject("MiniHpBar_Siege");
            miniHpBarRoot.transform.SetParent(transform, false);

            // Mengimbangi skala induk agar dimensi bar di layar tetap konsisten dan tajam di segala resolusi
            float invScaleX = 1f / Mathf.Max(0.01f, baseScale.x);
            float invScaleY = 1f / Mathf.Max(0.01f, baseScale.y);
            miniHpBarRoot.transform.localScale = new Vector3(invScaleX, invScaleY, 1f);

            // Posisi vertikal tepat melayang di atas puncak sprite monster
            float headOffset = (spriteRenderer != null && spriteRenderer.sprite != null)
                ? (spriteRenderer.sprite.bounds.extents.y * baseScale.y + 0.12f)
                : 0.40f;
            miniHpBarRoot.transform.localPosition = new Vector3(0f, headOffset * invScaleY, 0f);

            // Background Bingkai Mini Bar (Hitam arang kontras tinggi dengan alpha halus)
            GameObject bgObj = new GameObject("HpBar_Bg");
            bgObj.transform.SetParent(miniHpBarRoot.transform, false);
            miniHpBarBgRenderer = bgObj.AddComponent<SpriteRenderer>();
            miniHpBarBgRenderer.sprite = GetOrCreateMiniHpBarSprite();
            miniHpBarBgRenderer.color = new Color(0.10f, 0.10f, 0.12f, 0.90f);
            miniHpBarBgRenderer.sortingLayerName = "UI";
            miniHpBarBgRenderer.sortingOrder = 350;
            bgObj.transform.localScale = new Vector3(0.52f, 0.08f, 1f);

            // Foreground Fill Bar
            GameObject fillObj = new GameObject("HpBar_Fill");
            fillObj.transform.SetParent(miniHpBarRoot.transform, false);
            miniHpBarFillRenderer = fillObj.AddComponent<SpriteRenderer>();
            miniHpBarFillRenderer.sprite = GetOrCreateMiniHpBarSprite();
            miniHpBarFillRenderer.color = new Color(0.28f, 0.92f, 0.42f, 1f); // Emerald Green
            miniHpBarFillRenderer.sortingLayerName = "UI";
            miniHpBarFillRenderer.sortingOrder = 351;
            fillObj.transform.localScale = new Vector3(0.48f, 0.055f, 1f);
        }

        protected virtual void UpdateMiniHpBar()
        {
            if (!isAttackingBarricade || miniHpBarRoot == null || miniHpBarFillRenderer == null) return;

            float pct = Mathf.Clamp01(HealthPercent);
            float fullWidth = 0.48f;
            float currentWidth = fullWidth * pct;

            miniHpBarFillRenderer.transform.localScale = new Vector3(currentWidth, 0.055f, 1f);
            // Geser anchor ke kiri agar bar memendek ke arah kiri secara alami
            miniHpBarFillRenderer.transform.localPosition = new Vector3(-((fullWidth - currentWidth) * 0.5f), 0f, 0f);

            // Pewarnaan dinamis adaptif (Hijau Sehat -> Amber Kuning -> Merah Kritis)
            if (pct > 0.50f)
            {
                miniHpBarFillRenderer.color = new Color(0.28f, 0.92f, 0.42f, 1f);
            }
            else if (pct > 0.25f)
            {
                miniHpBarFillRenderer.color = new Color(0.98f, 0.78f, 0.15f, 1f);
            }
            else
            {
                miniHpBarFillRenderer.color = new Color(0.95f, 0.22f, 0.22f, 1f);
            }
        }

        #endregion
    }
}

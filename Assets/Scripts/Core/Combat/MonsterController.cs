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
        [SerializeField] protected float heroEngagementRange = 0.95f;
        [SerializeField] protected float heroAttackInterval = 1.0f;
        protected float heroAttackTimer = 0f;

        [Header("Barricade Combat Interaction")]
        [SerializeField] protected bool isAttackingBarricade = false;
        [SerializeField] protected float barricadeAttackInterval = 1.2f;
        protected float barricadeAttackTimer = 0f;

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

            // Prioritas Tempur: Monster mendeteksi pahlawan (hero defender) yang menghadang di depan
            UnitController blockingHero = FindBlockingHero();
            if (blockingHero != null && blockingHero.IsAlive)
            {
                EngageHero(blockingHero);
                return;
            }

            MoveAlongWaypoints();
        }

        /// <summary>
        /// Mencari pahlawan (UnitController) terdekat yang berada di depan monster untuk dihadang dan diserang terlebih dahulu.
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
                if (hero == null || !hero.IsAlive || hero.CurrentTier == UnitTier.Tier0) continue;

                float dist = Vector3.Distance(myPos, hero.transform.position);
                // Hero yang berada di depan/bawah monster dalam jangkauan melee (monster bergerak turun)
                if (dist <= closestDist && hero.transform.position.y <= myPos.y + 0.35f)
                {
                    closestDist = dist;
                    bestHero = hero;
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
            Vector3 lungeTarget = Vector3.Lerp(startPos, targetHero.transform.position, 0.35f);

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

            // Sesuai Instruksi Komandan (Poin 4):
            // Monster yang mencapai gerbang TIDAK LANGSUNG MENGHILANG!
            // Monster berhenti di depan gerbang dan terus menyerang benteng berkala hingga dieliminasi oleh hero/turret!
            isAttackingBarricade = true;
            barricadeAttackTimer = 0.2f;

            int damage = monsterData != null ? monsterData.castleDamage : 10;
            OnMonsterReachedBarricade?.Invoke(this, damage);
        }

        protected virtual void AttackBarricade()
        {
            if (CastleBarricade.Instance == null || CastleBarricade.Instance.CurrentHealth <= 0) return;

            barricadeAttackTimer -= Time.deltaTime;
            if (barricadeAttackTimer <= 0f)
            {
                barricadeAttackTimer = barricadeAttackInterval;

                int damage = monsterData != null ? monsterData.castleDamage : 10;
                CastleBarricade.Instance.TakeDamage(damage);

                // Mainkan animasi menerjang ke arah gerbang benteng
                StartCoroutine(AttackBarricadeLungeRoutine());

                // Mainkan SFX benturan ke benteng
                if (FantasyDefense.Audio.AudioManager.Instance != null)
                {
                    FantasyDefense.Audio.AudioManager.Instance.PlaySound(FantasyDefense.Audio.SoundType.CastleHit);
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
    }
}

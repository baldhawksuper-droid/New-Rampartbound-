using System;
using System.Collections;
using UnityEngine;
using FantasyDefense.Core;
using FantasyDefense.Metagame;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Mengelola daya tahan Benteng Kerajaan (Citadel Barricade) di garis akhir pertahanan.
    /// Jika monster berhasil menembus pertahanan dan mengurangi HP benteng hingga 0,
    /// permainan berakhir dengan status Kekalahan (Defeat).
    /// </summary>
    public class CastleBarricade : MonoBehaviour
    {
        public static CastleBarricade Instance { get; private set; }

        [Header("Citadel Health")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hitColor = new Color(1f, 0.3f, 0.3f, 1f);

        [Header("Cracked Barricade Multi-Tier Visuals (Point 2)")]
        [SerializeField] private SpriteRenderer crackOverlayRenderer;
        private static Sprite crackStage1Sprite; // HP <= 75%
        private static Sprite crackStage2Sprite; // HP <= 50%
        private static Sprite crackStage3Sprite; // HP <= 25%

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public float HealthPercent => (float)currentHealth / Mathf.Max(1, maxHealth);

        public static event Action<int, int> OnCastleHealthChanged; // (current, max)
        public static event Action OnCastleBreached;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sortingLayerName = "Board";
                spriteRenderer.sortingOrder = 5;
            }

            if (spriteRenderer != null && spriteRenderer.sprite == null)
            {
                var fence = Resources.Load<Sprite>("Sprites/UI/Barricade_Fence");
                if (fence != null)
                {
                    spriteRenderer.sprite = fence;
                    transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                }
            }

            currentHealth = maxHealth;
            EnsureCrackOverlay();
            UpdateDamageVisuals();
        }

        private void Start()
        {
            OnCastleHealthChanged?.Invoke(currentHealth, maxHealth);
            StartCoroutine(AegisRegenerationLoop());
        }

        private IEnumerator AegisRegenerationLoop()
        {
            var waitInterval = new WaitForSeconds(8.0f);
            while (true)
            {
                yield return waitInterval;
                if (currentHealth > 0 && currentHealth < maxHealth)
                {
                    float regenPercent = 0f;
                    if (CitadelTalentManager.Instance != null)
                    {
                        regenPercent = CitadelTalentManager.Instance.GetBarricadeRegenPercent();
                    }

                    if (regenPercent > 0f)
                    {
                        int healAmount = Mathf.Max(1, Mathf.RoundToInt(maxHealth * regenPercent));
                        Repair(healAmount);
                        if (SkillVfxManager.Instance != null)
                        {
                            SkillVfxManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 0.7f, $"+{healAmount} HP (Aegis)", new Color(0.2f, 1f, 0.4f), 1.0f);
                        }
                    }
                }
            }
        }

        public void Initialize(int startingHealth)
        {
            maxHealth = startingHealth;
            currentHealth = startingHealth;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            EnsureCrackOverlay();
            UpdateDamageVisuals();
            OnCastleHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        [Header("Damage Pacing & DPS Protection (Point 6)")]
        private float lastDamageTime = -1f;
        private int recentDamageAccumulator = 0;
        private const float DAMAGE_WINDOW = 0.18f;
        private const int MAX_DAMAGE_PER_BURST = 16;

        /// <summary>
        /// Menerima damage saat monster berhasil mencapai garis batas benteng.
        /// Dilengkapi proteksi pacing DPS agar barikade tidak lenyap seketika dalam 1 detik saat rombongan tiba bersamaan.
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (currentHealth <= 0) return;

            // Pacing DPS Guardrail: Batasi lonjakan damage serentak dalam rentang 0.18 detik
            if (Time.time - lastDamageTime < DAMAGE_WINDOW)
            {
                if (recentDamageAccumulator >= MAX_DAMAGE_PER_BURST)
                {
                    // Throttle damage berlebih agar pemain memiliki jeda reaksi
                    damage = Mathf.Min(damage, 2);
                }
                else
                {
                    damage = Mathf.Min(damage, MAX_DAMAGE_PER_BURST - recentDamageAccumulator);
                }
                recentDamageAccumulator += damage;
            }
            else
            {
                lastDamageTime = Time.time;
                recentDamageAccumulator = damage;
            }

            if (damage <= 0) return;

            // Integrasi Aegis Barrier: Guardian's Vow 4-Piece (Kebal 4 detik) (Sprint 13)
            if (ArtifactManager.Instance != null && ArtifactManager.Instance.IsAegisBarrierActive)
            {
                Debug.Log("[CastleBarricade] Serangan monster ditangkis sepenuhnya oleh Aegis Barrier!");
                if (SkillVfxManager.Instance != null)
                {
                    SkillVfxManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 0.8f, "AEGIS BARRIER (KEBAL)", new Color(1f, 0.9f, 0.2f), 1.0f);
                }
                return;
            }

            // Integrasi Guardian's Vow 2-Piece: Pengurangan damage 15% (Sprint 13)
            if (ArtifactManager.Instance != null)
            {
                float reductionMultiplier = ArtifactManager.Instance.GetBarricadeDamageReductionMultiplier();
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * reductionMultiplier));
            }

            // Integrasi Berkah Taktis: Barricade Thorns
            if (BattleBlessingManager.Instance != null && BattleBlessingManager.Instance.ThornReflectRatio > 0f)
            {
                int reflectDmg = Mathf.RoundToInt(damage * BattleBlessingManager.Instance.ThornReflectRatio);
                var monsters = MonsterController.GetActiveMonstersSnapshot();
                if (reflectDmg > 0 && monsters.Count > 0)
                {
                    var firstMonster = monsters[0];
                    if (firstMonster != null && firstMonster.IsAlive)
                    {
                        firstMonster.TakeDamage(reflectDmg);
                    }
                }
            }

            currentHealth = Mathf.Max(0, currentHealth - damage);
            Debug.Log($"[CastleBarricade] Benteng terkena damage {damage}! Sisa HP: {currentHealth}/{maxHealth}");

            // Efek Visual Floating Combat Number & Percikan Benturan pada Barikade
            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnFloatingCombatNumber(transform.position + Vector3.up * 0.8f, $"-{damage}", new Color(1f, 0.25f, 0.25f), 1.25f, true);
                SkillVfxManager.Instance.SpawnProceduralParticleBurst(transform.position + Vector3.up * 0.3f, new Color(1f, 0.45f, 0.2f), new Color(0.9f, 0.15f, 0.15f), 12, 3.5f);

                // Efek Visual Retakan Gerbang & Cipratan Debu Batu Halus (Point 2 - Siege FX)
                Vector3 dustPos = transform.position + new Vector3(UnityEngine.Random.Range(-0.85f, 0.85f), 0.25f, 0f);
                SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                    dustPos,
                    new Color(0.85f, 0.80f, 0.70f), // Debu batu kapur halus
                    new Color(0.48f, 0.42f, 0.35f), // Serpihan batu & arang
                    14,
                    2.8f
                );
            }

            // Cek aktivasi darurat Aegis Barrier jika HP turun di bawah 30%
            if (HealthPercent <= 0.30f && ArtifactManager.Instance != null)
            {
                ArtifactManager.Instance.TryTriggerAegisBarrier();
            }

            OnCastleHealthChanged?.Invoke(currentHealth, maxHealth);

            // Memicu Screen Shake dan getaran haptic ponsel saat benteng terkena hantaman
            if (MobileCameraAdapter.Instance != null)
            {
                MobileCameraAdapter.Instance.TriggerScreenShake(0.22f, 0.20f);
            }

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Heavy);
            }

            StartCoroutine(FlashHitEffect());

            // Pemicu Reaksi Emotif Pahlawan Terdekat (Point 3 - Hero Defender Battle Emotes)
            TriggerNearestHeroDefenderEmote();

            if (currentHealth <= 0)
            {
                Debug.LogWarning("[CastleBarricade] Benteng Runtuh! Pertahanan jebol!");

                // Efek Shatter Debris Burst saat barikade runtuh
                if (SkillVfxManager.Instance != null)
                {
                    SkillVfxManager.Instance.SpawnBarricadeCollapseShatter(transform.position);
                }
                if (spriteRenderer != null) spriteRenderer.enabled = false;
                if (crackOverlayRenderer != null) crackOverlayRenderer.enabled = false;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.CastleHitBoss, 0.85f);
                }

                if (MobileCameraAdapter.Instance != null)
                {
                    MobileCameraAdapter.Instance.TriggerScreenShake(0.40f, 0.30f);
                }

                OnCastleBreached?.Invoke();

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetState(BattleState.Defeat);
                }
            }
        }

        public void Heal(float amount) => Repair(Mathf.RoundToInt(amount));

        public void Repair(int amount)
        {
            if (currentHealth <= 0) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnCastleHealthChanged?.Invoke(currentHealth, maxHealth);

            // Efek Angka Pemulihan Hijau Melayang
            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnFloatingCombatNumber(transform.position + Vector3.up * 0.9f, $"+{amount}", new Color(0.28f, 0.95f, 0.45f), 1.25f, false);
            }

            // Kilatan Pendaran Hijau Keemasan Menyapu Pagar Benteng (Point 2 - Barricade Repair Gleam)
            PlayRepairGleamEffect();

            UpdateDamageVisuals();
        }

        private Color GetDamageStateColor()
        {
            if (HealthPercent > 0.60f)
                return normalColor;
            if (HealthPercent > 0.30f)
                return new Color(0.92f, 0.88f, 0.82f, 1f); // Retak ringan/weathered
            return new Color(0.85f, 0.65f, 0.65f, 1f); // Kritis membara retak parah
        }

        private IEnumerator FlashHitEffect()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hitColor;
            }

            // Punch shake halus pada barikade saat hantaman
            Vector3 origPos = transform.position;
            float duration = 0.16f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float offset = Mathf.Sin(t * Mathf.PI * 4f) * (1f - t) * 0.08f;
                transform.position = origPos + new Vector3(offset, 0f, 0f);
                yield return null;
            }
            transform.position = origPos;

            UpdateDamageVisuals();
        }

        #region Cracked Barricade Multi-Tier Visual System (Point 2)

        private void EnsureCrackOverlay()
        {
            if (crackOverlayRenderer != null) return;

            Transform existing = transform.Find("CrackOverlay");
            if (existing != null)
            {
                crackOverlayRenderer = existing.GetComponent<SpriteRenderer>();
            }

            if (crackOverlayRenderer == null)
            {
                GameObject overlayObj = new GameObject("CrackOverlay");
                overlayObj.transform.SetParent(transform, false);
                overlayObj.transform.localPosition = Vector3.zero;
                overlayObj.transform.localScale = Vector3.one;

                crackOverlayRenderer = overlayObj.AddComponent<SpriteRenderer>();
                crackOverlayRenderer.sortingLayerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Board";
                crackOverlayRenderer.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 5) + 1;
            }
        }

        private Coroutine criticalCrackPulseCoroutine;

        public void UpdateDamageVisuals()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = GetDamageStateColor();
            }

            if (crackOverlayRenderer == null)
            {
                EnsureCrackOverlay();
            }

            if (crackOverlayRenderer == null) return;

            float pct = HealthPercent;
            if (pct <= 0.25f)
            {
                crackOverlayRenderer.gameObject.SetActive(true);
                crackOverlayRenderer.sprite = GetOrCreateCrackSprite(3);
                crackOverlayRenderer.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
                if (criticalCrackPulseCoroutine == null && gameObject.activeInHierarchy)
                {
                    criticalCrackPulseCoroutine = StartCoroutine(PulsingCriticalCrackRoutine());
                }
            }
            else
            {
                if (criticalCrackPulseCoroutine != null)
                {
                    StopCoroutine(criticalCrackPulseCoroutine);
                    criticalCrackPulseCoroutine = null;
                }

                if (pct <= 0.50f)
                {
                    crackOverlayRenderer.gameObject.SetActive(true);
                    crackOverlayRenderer.sprite = GetOrCreateCrackSprite(2);
                    crackOverlayRenderer.color = new Color(0.95f, 0.80f, 0.55f, 0.90f); // Retakan struktural menengah
                    crackOverlayRenderer.transform.localScale = Vector3.one;
                }
                else if (pct <= 0.75f)
                {
                    crackOverlayRenderer.gameObject.SetActive(true);
                    crackOverlayRenderer.sprite = GetOrCreateCrackSprite(1);
                    crackOverlayRenderer.color = new Color(0.85f, 0.85f, 0.90f, 0.75f); // Retakan halus awal
                    crackOverlayRenderer.transform.localScale = Vector3.one;
                }
                else
                {
                    crackOverlayRenderer.gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator PulsingCriticalCrackRoutine()
        {
            Color emberLow = new Color(1f, 0.22f, 0.10f, 0.90f);
            Color emberHigh = new Color(1f, 0.78f, 0.25f, 1.0f);
            while (HealthPercent <= 0.25f && crackOverlayRenderer != null)
            {
                float t = (Mathf.Sin(Time.time * 5.0f) + 1f) * 0.5f;
                crackOverlayRenderer.color = Color.Lerp(emberLow, emberHigh, t);
                crackOverlayRenderer.transform.localScale = Vector3.one * Mathf.Lerp(1.03f, 1.08f, t);
                yield return null;
            }
            criticalCrackPulseCoroutine = null;
        }

        private static Sprite GetOrCreateCrackSprite(int stage)
        {
            if (stage == 1 && crackStage1Sprite != null) return crackStage1Sprite;
            if (stage == 2 && crackStage2Sprite != null) return crackStage2Sprite;
            if (stage == 3 && crackStage3Sprite != null) return crackStage3Sprite;

            int w = 128;
            int h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Color crackDark = new Color(0.12f, 0.10f, 0.08f, 0.90f);
            Color crackRed = new Color(0.88f, 0.22f, 0.12f, 0.85f);

            // Seed deterministic agar bentuk retakan stabil dan rapi
            var rng = new System.Random(stage * 1337);

            int crackCount = stage switch
            {
                1 => 3,
                2 => 6,
                _ => 10
            };

            for (int c = 0; c < crackCount; c++)
            {
                int startX = rng.Next(8, w - 8);
                int startY = rng.Next(0, 10);
                int endX = Mathf.Clamp(startX + rng.Next(-25, 25), 4, w - 4);
                int endY = rng.Next(h - 15, h);

                int steps = Mathf.Max(Mathf.Abs(endX - startX), Mathf.Abs(endY - startY));
                float curX = startX;
                float curY = startY;
                float dx = (endX - startX) / (float)steps;
                float dy = (endY - startY) / (float)steps;

                for (int s = 0; s <= steps; s++)
                {
                    int px = Mathf.Clamp(Mathf.RoundToInt(curX + (float)(rng.NextDouble() * 2.0 - 1.0)), 0, w - 1);
                    int py = Mathf.Clamp(Mathf.RoundToInt(curY), 0, h - 1);

                    pixels[py * w + px] = stage == 3 && rng.Next(0, 3) == 0 ? crackRed : crackDark;

                    // Buat retakan lebih tebal pada stage 2 dan 3
                    if (stage >= 2 && px + 1 < w)
                    {
                        pixels[py * w + (px + 1)] = crackDark;
                    }
                    if (stage == 3 && py + 1 < h)
                    {
                        pixels[(py + 1) * w + px] = crackDark;
                    }

                    curX += dx;
                    curY += dy;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
            if (stage == 1) crackStage1Sprite = sprite;
            else if (stage == 2) crackStage2Sprite = sprite;
            else crackStage3Sprite = sprite;

            return sprite;
        }

        #region Barricade Repair Gleam & Hero Emote Systems (Points 2 & 3)

        private float nextHeroEmoteTime = 0f;

        private void TriggerNearestHeroDefenderEmote()
        {
            if (Time.time < nextHeroEmoteTime) return;
            nextHeroEmoteTime = Time.time + 6.5f;

            // Cari pahlawan aktif terdekat dari gerbang benteng
            UnitController[] allUnits = UnityEngine.Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            if (allUnits == null || allUnits.Length == 0) return;

            UnitController closestHero = null;
            float closestDist = float.MaxValue;
            Vector3 gatePos = transform.position;

            for (int i = 0; i < allUnits.Length; i++)
            {
                var unit = allUnits[i];
                if (unit == null || !unit.gameObject.activeInHierarchy) continue;

                float dist = Vector3.Distance(gatePos, unit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestHero = unit;
                }
            }

            if (closestHero != null)
            {
                closestHero.TriggerDefenderEmote();
            }
        }

        private void PlayRepairGleamEffect()
        {
            StartCoroutine(RepairGleamRoutine());

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.BarricadeRepair, 1.05f);
            }
        }

        private IEnumerator RepairGleamRoutine()
        {
            GameObject gleamObj = new GameObject("BarricadeRepairGleam");
            gleamObj.transform.SetParent(transform, false);
            gleamObj.transform.localPosition = new Vector3(-1.8f, 0.25f, 0f);

            SpriteRenderer gleamRenderer = gleamObj.AddComponent<SpriteRenderer>();
            gleamRenderer.sprite = GetOrCreateGleamSprite();
            gleamRenderer.color = new Color(0.35f, 1f, 0.60f, 0.85f);
            gleamRenderer.sortingLayerName = "UI";
            gleamRenderer.sortingOrder = 360;
            gleamObj.transform.localScale = new Vector3(0.6f, 0.45f, 1f);

            float duration = 0.55f;
            float elapsed = 0f;
            float startX = -1.8f;
            float endX = 1.8f;

            int sparkleStep = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                float curX = Mathf.Lerp(startX, endX, t);
                gleamObj.transform.localPosition = new Vector3(curX, 0.25f, 0f);

                Color gleamColor = Color.Lerp(new Color(0.25f, 1f, 0.55f, 0.9f), new Color(1f, 0.92f, 0.35f, 0.9f), t);
                gleamRenderer.color = gleamColor;

                if (t >= 0.25f * (sparkleStep + 1) && sparkleStep < 3)
                {
                    sparkleStep++;
                    if (SkillVfxManager.Instance != null)
                    {
                        Vector3 burstPos = transform.position + new Vector3(curX, 0.3f, 0f);
                        SkillVfxManager.Instance.SpawnProceduralParticleBurst(burstPos, new Color(0.35f, 1f, 0.55f), new Color(1f, 0.92f, 0.35f), 8, 2.2f);
                    }
                }

                yield return null;
            }

            Destroy(gleamObj);
        }

        private static Sprite cachedGleamSprite;
        private static Sprite GetOrCreateGleamSprite()
        {
            if (cachedGleamSprite != null) return cachedGleamSprite;

            int w = 64;
            int h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (y - (h - 1) * 0.5f) / ((h - 1) * 0.5f);
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - (w - 1) * 0.5f) / ((w - 1) * 0.5f);
                    float d = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny));
                    float alpha = d * d;
                    pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            cachedGleamSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            return cachedGleamSprite;
        }

        #endregion

        #endregion
    }
}

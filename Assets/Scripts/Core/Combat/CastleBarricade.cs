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
            OnCastleHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Menerima damage saat monster berhasil mencapai garis batas benteng.
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (currentHealth <= 0) return;

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

            if (currentHealth <= 0)
            {
                Debug.LogWarning("[CastleBarricade] Benteng Runtuh! Pertahanan jebol!");
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

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnProceduralParticleBurst(transform.position + Vector3.up * 0.3f, new Color(0.2f, 1f, 0.4f), new Color(0.1f, 0.8f, 0.3f), 8, 2.5f);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = GetDamageStateColor();
            }
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
                yield return new WaitForSeconds(0.12f);
                spriteRenderer.color = GetDamageStateColor();
            }
        }
    }
}

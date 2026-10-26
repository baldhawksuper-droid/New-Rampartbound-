using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.GridSystem;
using FantasyDefense.Audio;
using FantasyDefense.Mobile;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Base Controller untuk seluruh hero di arena (Cedric, Rose Tanker, Rabbit Mage).
    /// Mendukung interaksi Mobile Touch & Tablet (tap, drag-and-drop merge, finger offset)
    /// serta input mouse untuk pengujian di Unity Editor.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class UnitController : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        // Global tracking unit aktif di grid arena
        public static readonly System.Collections.Generic.List<UnitController> ActiveUnits = new System.Collections.Generic.List<UnitController>();
        // Global tracking batu permata T0 yang tertidur saat fase pertempuran agar kembali di wave berikutnya (Sprint 44)
        public static readonly System.Collections.Generic.List<UnitController> DormantT0Gems = new System.Collections.Generic.List<UnitController>();

        /// <summary>
        /// Mengembalikan salinan aman daftar hero aktif untuk iterasi anti-koleksi modifikasi (InvalidOperationException).
        /// </summary>
        public static System.Collections.Generic.List<UnitController> GetActiveUnitsSnapshot()
        {
            return new System.Collections.Generic.List<UnitController>(ActiveUnits);
        }

        /// <summary>
        /// Memulihkan seluruh pahlawan di papan saat pergantian antar-gelombang (Sprint 48).
        /// </summary>
        public static void ReviveAllActiveHeroes()
        {
            var heroes = GetActiveUnitsSnapshot();
            for (int i = 0; i < heroes.Count; i++)
            {
                if (heroes[i] != null)
                {
                    heroes[i].ReviveHero();
                }
            }
        }

        [Header("Combat Health & Defense")]
        [SerializeField] protected float currentHealth = 100f;
        [SerializeField] protected float maxHealth = 100f;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => currentHealth > 0f;
        public float HealthPercent => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        [Header("Unit Identity & Data")]
        [SerializeField] protected UnitDataSO unitData;
        [SerializeField] protected UnitTier currentTier = UnitTier.Tier1;

        [Header("Grid Attachment")]
        [SerializeField] protected TileCell currentCell;

        [Header("Visual Components")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected int normalSortingOrder = 30;
        [SerializeField] protected int dragSortingOrder = 70;

        [Header("2.5D Contact Ground Shadow (Sprint 44 Poin 5)")]
        [SerializeField] protected GameObject groundShadowObj;
        [SerializeField] protected SpriteRenderer groundShadowSr;

        [Header("3D Hero Plinth Pedestal")]
        [SerializeField] protected GameObject groundPlinthObj;
        [SerializeField] protected SpriteRenderer groundPlinthSr;

        [Header("Mobile Ergonomics")]
        [Tooltip("Offset posisi token saat disentuh agar tidak tertutup ibu jari pemain")]
        [SerializeField] protected Vector3 touchDragOffset = new Vector3(0f, 0.45f, 0f);

        [Header("Drag State")]
        protected bool isDragging = false;
        protected Vector3 initialPosition;
        protected Camera mainCamera;
        protected Coroutine snapRoutine;

        [Header("Preparation Token Mode")]
        [Tooltip("Menandai apakah unit sedang dalam wujud medali token di fase persiapan")]
        [SerializeField] protected bool isPreparationToken = false;
        public bool IsPreparationToken => isPreparationToken;

        [Header("Tactical Range Visualizer")]
        [SerializeField] protected GameObject rangeCircleObj;
        [SerializeField] protected LineRenderer rangeLineRenderer;
        protected bool isRangeCircleVisible = false;
        public bool IsRangeCircleVisible => isRangeCircleVisible;

        [Header("Buff System")]
        protected float attackSpeedBuff = 0f;

        public UnitDataSO UnitData => unitData;
        public UnitDataSO HeroData => unitData;
        public UnitTier CurrentTier => currentTier;
        public UnitType UnitType => unitData != null ? unitData.unitType : UnitType.Cedric;
        public TileCell CurrentCell => currentCell;
        public bool IsDragging => isDragging;

        /// <summary>
        /// Menampilkan atau menyembunyikan lingkaran jangkauan serang (Attack Range Circle).
        /// Memberikan kejelasan visual (Visual Clarity) mengenai radius serang hero di medan tempur.
        /// </summary>
        public void ToggleRangeCircle(bool show)
        {
            isRangeCircleVisible = show;

            if (show)
            {
                if (rangeCircleObj == null)
                {
                    SetupRangeCircleVisualizer();
                }
                if (rangeCircleObj != null)
                {
                    rangeCircleObj.SetActive(true);
                }
            }
            else
            {
                if (rangeCircleObj != null)
                {
                    rangeCircleObj.SetActive(false);
                }
            }
        }

        private void SetupRangeCircleVisualizer()
        {
            Transform existing = transform.Find("AttackRangeCircle");
            if (existing != null)
            {
                rangeCircleObj = existing.gameObject;
                rangeLineRenderer = rangeCircleObj.GetComponent<LineRenderer>();
            }
            else
            {
                rangeCircleObj = new GameObject("AttackRangeCircle");
                rangeCircleObj.transform.SetParent(transform, false);
                rangeCircleObj.transform.localPosition = Vector3.zero;

                rangeLineRenderer = rangeCircleObj.AddComponent<LineRenderer>();
                rangeLineRenderer.useWorldSpace = false;
                rangeLineRenderer.loop = true;
                rangeLineRenderer.startWidth = 0.045f;
                rangeLineRenderer.endWidth = 0.045f;

                Material lineMat = new Material(Shader.Find("Sprites/Default"));
                rangeLineRenderer.material = lineMat;
                Color glowColor = new Color(0.25f, 0.9f, 1.0f, 0.65f);
                rangeLineRenderer.startColor = glowColor;
                rangeLineRenderer.endColor = glowColor;
                rangeLineRenderer.sortingOrder = 28;

                int segments = 40;
                rangeLineRenderer.positionCount = segments;
                float cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1.18f;
                float rangeRadius = (unitData != null ? unitData.attackRange : 1.0f) * cellSize;

                Vector3[] points = new Vector3[segments];
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    points[i] = new Vector3(Mathf.Cos(angle) * rangeRadius, Mathf.Sin(angle) * rangeRadius, 0f);
                }
                rangeLineRenderer.SetPositions(points);
            }
        }

        // Events untuk integrasi sistem audio, VFX, dan UI
        public static event Action<UnitController, UnitController, UnitTier> OnUnitMerged; // (source, target, newTier)
        public static event Action<UnitController, TileCell> OnUnitMoved;
        public static event Action<UnitController> OnUnitDragStarted;
        public static event Action<UnitController> OnUnitDragEnded;
        public static event Action<UnitController, string> OnUnitSkillTriggered; // (unit, skillName)

        protected virtual void OnEnable()
        {
            if (!ActiveUnits.Contains(this))
                ActiveUnits.Add(this);

            if (transform.localScale.sqrMagnitude < 0.001f)
            {
                UpdateVisualAppearance();
            }
        }

        protected virtual void OnDisable()
        {
            if (CurrentlySelectedUnit == this)
                DeselectCurrentUnit();

            ActiveUnits.Remove(this);
        }

        protected virtual void OnDestroy()
        {
            if (ActiveUnits.Contains(this))
                ActiveUnits.Remove(this);
            if (DormantT0Gems.Contains(this))
                DormantT0Gems.Remove(this);
        }

        [Header("Animation & Visual Juice")]
        protected HeroVisualAnimator visualAnimator;

        public HeroVisualAnimator VisualAnimator => visualAnimator;

        protected virtual void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            visualAnimator = GetComponent<ModularHeroVisualAnimator>();
            if (visualAnimator == null)
                visualAnimator = GetComponent<HeroVisualAnimator>();
            if (visualAnimator == null)
                visualAnimator = gameObject.AddComponent<ModularHeroVisualAnimator>();

            mainCamera = Camera.main;
        }

        /// <summary>
        /// Inisialisasi unit saat dipanggil (Summon) ke arena grid.
        /// </summary>
        public virtual void Initialize(UnitDataSO data, UnitTier tier, TileCell cell)
        {
            unitData = data;
            currentTier = tier;
            currentCell = cell;

            maxHealth = data != null ? data.GetCalculatedHealth(tier) : 100f;
            currentHealth = maxHealth;

            if (spriteRenderer != null && data != null && data.unitIcon != null)
            {
                spriteRenderer.sprite = data.unitIcon;
                spriteRenderer.sortingOrder = normalSortingOrder;
            }

            if (cell != null)
            {
                transform.position = cell.transform.position;
                cell.SetOccupant(this);
            }

            UpdateVisualAppearance();
        }

        public void SetCurrentCell(TileCell cell)
        {
            currentCell = cell;
        }

        private TileCell currentHoveredCell;

        public void SetAsPreparationToken(bool isToken)
        {
            isPreparationToken = isToken;
            UpdateVisualAppearance();

            if (isToken && gameObject.activeInHierarchy)
            {
                StartCoroutine(AnimateReturnToTokenRoutine());
            }
        }

        private IEnumerator AnimateReturnToTokenRoutine()
        {
            if (spriteRenderer == null) yield break;

            float cellW = GridManager.Instance != null ? GridManager.Instance.CellSize : 1.18f;
            float spriteW = (spriteRenderer.sprite != null && spriteRenderer.sprite.bounds.size.x > 0.001f)
                ? spriteRenderer.sprite.bounds.size.x
                : (spriteRenderer.sprite != null ? (spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit) : 1f);
            float targetScale = (cellW * fillRatio) / Mathf.Max(0.01f, spriteW);
            targetScale = Mathf.Clamp(targetScale, 0.65f, 1.25f);
            Vector3 baseScale = Vector3.one * targetScale;

            Color origColor = spriteRenderer.color;
            float duration = 0.20f;
            float elapsed = 0f;

            // Efek pop & golden flash penanda pahlawan kembali ke mode token yang siap digeser
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float bounce = Mathf.Sin(t * Mathf.PI) * 0.20f;
                transform.localScale = baseScale * (1f + bounce);
                spriteRenderer.color = Color.Lerp(origColor, new Color(1f, 0.95f, 0.5f, 1f), Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            transform.localScale = baseScale;
            spriteRenderer.color = origColor;
            if (visualAnimator != null)
            {
                visualAnimator.SetBaseScale(baseScale);
            }
        }

        /// <summary>
        /// Mentransformasikan token persiapan menjadi Hero Unit tempur penuh saat pertempuran dimulai.
        /// Sesuai aturan desain resmi: Permata mentah (Tier 0) TIDAK menjadi apa-apa dan lenyap menjadi lahan kosong!
        /// </summary>
        public void TransformTokenToCombatHero()
        {
            if (currentTier == UnitTier.Tier0)
            {
                // ATURAN RESMI: T0 adalah batu permata, TIDAK MENJADI HERO!
                // Larut halus dengan animasi visual meninggalkan petak kosong
                if (gameObject.activeInHierarchy)
                {
                    StartCoroutine(AnimateGemDissolveRoutine());
                }
                else
                {
                    if (!DormantT0Gems.Contains(this)) DormantT0Gems.Add(this);
                }
                return;
            }

            if (!isPreparationToken) return;
            isPreparationToken = false;
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(AnimateTokenTransformationRoutine());
            }
        }

        private IEnumerator AnimateGemDissolveRoutine()
        {
            if (spriteRenderer == null)
            {
                if (!DormantT0Gems.Contains(this)) DormantT0Gems.Add(this);
                gameObject.SetActive(false);
                yield break;
            }

            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;
            Color startColor = spriteRenderer.color;

            float duration = 0.55f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, t));
                yield return null;
            }

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                    startPos,
                    new Color(1f, 1f, 1f, 0.7f),
                    new Color(0.6f, 0.85f, 1f, 0.25f),
                    8,
                    1.2f
                );
            }

            // SIMPAN PERMATA T0 (Sprint 44):
            // JANGAN DESTROY! Masukkan ke DormantT0Gems dan nonaktifkan GameObject agar tersimpan utuh untuk wave berikutnya.
            if (!DormantT0Gems.Contains(this))
            {
                DormantT0Gems.Add(this);
            }
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Mengembalikan seluruh batu permata T0 yang tertidur saat fase perang kembali ke arena pertempuran
        /// dengan animasi pop-in elastis yang segar (Point 1).
        /// </summary>
        public static void RestoreDormantT0Gems()
        {
            var gemsToRestore = DormantT0Gems.ToArray();
            DormantT0Gems.Clear();

            foreach (var gem in gemsToRestore)
            {
                if (gem != null && gem.gameObject != null)
                {
                    // Pastikan petak tempatnya bernaung masih valid dan tidak ditempati oleh Hero lain
                    if (gem.CurrentCell != null)
                    {
                        if (gem.CurrentCell.CurrentUnit == null || gem.CurrentCell.CurrentUnit == gem)
                        {
                            gem.CurrentCell.SetOccupant(gem);
                            gem.gameObject.SetActive(true);
                            gem.UpdateVisualAppearance();
                            if (gem.gameObject.activeInHierarchy)
                            {
                                gem.StartCoroutine(gem.AnimateGemRestoreRoutine());
                            }
                        }
                        else
                        {
                            // Jika petak sudah ditempati oleh Hero, hancurkan gem ini
                            Destroy(gem.gameObject);
                        }
                    }
                    else
                    {
                        Destroy(gem.gameObject);
                    }
                }
            }
        }

        private IEnumerator AnimateGemRestoreRoutine()
        {
            if (spriteRenderer == null) yield break;

            float cellW = GridManager.Instance != null ? GridManager.Instance.CellSize : 1.18f;
            float spriteW = (spriteRenderer.sprite != null && spriteRenderer.sprite.bounds.size.x > 0.001f)
                ? spriteRenderer.sprite.bounds.size.x
                : (spriteRenderer.sprite != null ? (spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit) : 1f);
            float targetScale = (cellW * fillRatio) / Mathf.Max(0.01f, spriteW);
            // Batas minimal skala (clamping) agar permata T0 tidak pernah mengecil secara abnormal saat next wave
            targetScale = Mathf.Clamp(targetScale, 0.65f, 1.25f);
            Vector3 targetScaleVec = Vector3.one * targetScale;

            transform.localScale = Vector3.zero;
            // Kembalikan warna murni Color.white agar artwork permata T0 asli tampil tajam, cerah dan tidak terdistorsi tint
            Color targetColor = Color.white;
            spriteRenderer.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);

            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Elastic overshoot curve
                float overshoot = Mathf.Sin(t * Mathf.PI * 0.75f) * 1.15f;
                if (t >= 0.75f)
                {
                    overshoot = Mathf.Lerp(1.15f, 1.0f, (t - 0.75f) / 0.25f);
                }
                transform.localScale = targetScaleVec * overshoot;
                spriteRenderer.color = new Color(targetColor.r, targetColor.g, targetColor.b, Mathf.Clamp01(t * 1.5f));
                yield return null;
            }

            transform.localScale = targetScaleVec;
            spriteRenderer.color = targetColor;
        }

        private IEnumerator AnimateTokenTransformationRoutine()
        {
            if (spriteRenderer == null)
            {
                UpdateVisualAppearance();
                yield break;
            }

            // Kunci posisi hero tepat di tengah petak asal (Garansi Posisi Tetap - Point 4)
            Vector3 anchorPos = currentCell != null ? currentCell.transform.position : transform.position;
            anchorPos.z = -0.1f;
            transform.position = anchorPos;

            Vector3 baseScale = transform.localScale;
            Color originalColor = spriteRenderer.color;

            // Fase 1: Mengambang naik sedikit dengan pendaran aura persiapan (0.35 detik)
            float elapsed = 0f;
            float duration1 = 0.35f;
            Color auraColor = currentTier switch
            {
                UnitTier.Tier5 => new Color(1f, 0.95f, 0.6f, 1f),  // Celestial Gold
                UnitTier.Tier4 => new Color(0.85f, 0.35f, 1f, 1f), // Mythic Astral Purple
                UnitTier.Tier3 => new Color(1f, 0.9f, 0.3f, 1f),
                _ => new Color(0.5f, 0.95f, 1f, 1f)
            };

            while (elapsed < duration1)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration1;
                transform.position = anchorPos + new Vector3(0f, Mathf.Sin(t * Mathf.PI * 0.5f) * 0.22f, 0f);
                spriteRenderer.color = Color.Lerp(originalColor, auraColor, t);
                transform.localScale = baseScale * (1f + 0.15f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            // Fase 2: Ganti wujud ke Hero Unit tempur sesungguhnya
            UpdateVisualAppearance();
            Color targetHeroColor = spriteRenderer.color;

            // Fase 3: Mendarat kokoh kembali ke petak dengan hentakan tanah heroik (0.40 detik)
            elapsed = 0f;
            float duration2 = 0.40f;
            while (elapsed < duration2)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration2;
                transform.position = Vector3.Lerp(anchorPos + new Vector3(0f, 0.22f, 0f), anchorPos, t * t);

                // Squash & stretch heroik saat mendarat di petak
                float squash = t > 0.7f ? Mathf.Sin((t - 0.7f) / 0.3f * Mathf.PI) * 0.20f : 0f;
                transform.localScale = new Vector3(baseScale.x * (1f + squash), baseScale.y * (1f - squash), baseScale.z);
                spriteRenderer.color = Color.Lerp(auraColor, targetHeroColor, t);
                yield return null;
            }

            // Kunci posisi akhir tepat di petak
            transform.position = anchorPos;
            transform.localScale = baseScale;
            spriteRenderer.color = targetHeroColor;

            // Efek partikel pendaran transformasi
            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                    anchorPos,
                    new Color(1f, 0.92f, 0.35f), // Golden sparkle
                    Color.white,
                    16,
                    2.2f
                );
            }

            if (visualAnimator != null)
            {
                visualAnimator.PlayMergeCelebration();
            }
        }

        [Header("Mobile Presentation & Uniform Scaling")]
        [Tooltip("Rasio ukuran gem terhadap ukuran sel (default: 0.75 atau 75%)")]
        [SerializeField] protected float fillRatio = 0.75f;
        public float FillRatio => fillRatio;

        /// <summary>
        /// Menyesuaikan dynamic depth sorting berdasarkan posisi Y agar unit tidak tumpang tindih secara visual.
        /// Menggunakan Sorting Layer standar: Gem (sortingOrder 15+).
        /// </summary>
        public void UpdateDepthSorting()
        {
            if (spriteRenderer != null && !isDragging)
            {
                spriteRenderer.sortingLayerName = "Gem";
                int sortOrder = Mathf.Max(15, normalSortingOrder - Mathf.RoundToInt(transform.position.y * 3));
                spriteRenderer.sortingOrder = sortOrder;

                if (groundPlinthSr != null)
                {
                    groundPlinthSr.sortingLayerName = "Gem";
                    groundPlinthSr.sortingOrder = sortOrder - 1;
                }

                if (groundShadowSr != null)
                {
                    groundShadowSr.sortingLayerName = "Gem";
                    groundShadowSr.sortingOrder = sortOrder - 2;
                }

                if (visualAnimator is ModularHeroVisualAnimator modularAnim)
                {
                    modularAnim.UpdateModularSortingOrder(sortOrder);
                }
            }
        }

        protected void UpdateGroundShadowVisibility(bool show)
        {
            if (currentTier == UnitTier.Tier0 || isPreparationToken)
            {
                if (groundPlinthObj != null) groundPlinthObj.SetActive(false);
                if (groundShadowObj != null) groundShadowObj.SetActive(false);
                return;
            }

            if (show)
            {
                int sortOrder = Mathf.Max(15, normalSortingOrder - Mathf.RoundToInt(transform.position.y * 3));
                int tierNum = Mathf.Clamp((int)currentTier, 1, 5);

                // 1. 3D Tactile Hero Plinth Pedestal (Tatakan Hero Semi-3D)
                if (groundPlinthObj == null)
                {
                    groundPlinthObj = new GameObject("HeroPlinth_3D");
                    groundPlinthObj.transform.SetParent(transform, false);
                    groundPlinthObj.transform.localPosition = new Vector3(0f, -0.22f, 0.05f);
                    groundPlinthSr = groundPlinthObj.AddComponent<SpriteRenderer>();
                    groundPlinthSr.sortingLayerName = "Gem";
                }
                else
                {
                    groundPlinthObj.transform.localPosition = new Vector3(0f, -0.22f, 0.05f);
                }

                Sprite plinthSprite = Resources.Load<Sprite>($"Sprites/Combat/Plinth_Hero_Base_T{tierNum}")
                                   ?? Resources.Load<Sprite>($"Sprites/Grid/Plinth_Hero_Base_T{tierNum}")
                                   ?? Resources.Load<Sprite>("Sprites/Combat/Plinth_Hero_Base_T1")
                                   ?? Resources.Load<Sprite>("Sprites/Grid/Plinth_Hero_Base_T1");

                if (groundPlinthSr != null)
                {
                    groundPlinthSr.sprite = plinthSprite;
                    groundPlinthSr.sortingOrder = sortOrder - 1;

                    // Warna tatakan menyesuaikan level/tier hero jika menggunakan fallback T1
                    if (plinthSprite != null && plinthSprite.name.EndsWith("_T1") && tierNum > 1)
                    {
                        switch (currentTier)
                        {
                            case UnitTier.Tier2:
                                groundPlinthSr.color = new Color(0.70f, 0.90f, 1.0f); // Polished Silver & Ice-Rune Blue
                                break;
                            case UnitTier.Tier3:
                                groundPlinthSr.color = new Color(1.0f, 0.85f, 0.35f); // Radiant Gold Dragon
                                break;
                            case UnitTier.Tier4:
                                groundPlinthSr.color = new Color(0.85f, 0.60f, 1.0f); // Astral Violet-Gold
                                break;
                            case UnitTier.Tier5:
                                groundPlinthSr.color = new Color(1.0f, 0.95f, 0.70f); // Celestial Platinum-Gold
                                break;
                            default:
                                groundPlinthSr.color = Color.white;
                                break;
                        }
                    }
                    else
                    {
                        groundPlinthSr.color = Color.white;
                    }

                    groundPlinthObj.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
                    groundPlinthObj.SetActive(true);
                }

                // 2. Soft contact shadow di bawah plinth
                if (groundShadowObj == null)
                {
                    groundShadowObj = new GameObject("GroundShadow_2.5D");
                    groundShadowObj.transform.SetParent(transform, false);
                    groundShadowObj.transform.localPosition = new Vector3(0f, -0.28f, 0.08f);
                    groundShadowSr = groundShadowObj.AddComponent<SpriteRenderer>();
                    groundShadowSr.sprite = Resources.Load<Sprite>("Sprites/UI/Shadow_Ground_Oval");
                    groundShadowSr.sortingLayerName = "Gem";
                    groundShadowSr.color = new Color(0f, 0f, 0f, 0.42f);
                    groundShadowObj.transform.localScale = new Vector3(0.85f, 0.35f, 1f);
                }
                else
                {
                    groundShadowObj.transform.localPosition = new Vector3(0f, -0.28f, 0.08f);
                }

                if (groundShadowSr != null)
                {
                    groundShadowSr.sortingOrder = sortOrder - 2;
                }
                groundShadowObj.SetActive(true);
            }
            else
            {
                if (groundPlinthObj != null) groundPlinthObj.SetActive(false);
                if (groundShadowObj != null) groundShadowObj.SetActive(false);
            }
        }

        [Header("Combat Presentation Scaling")]
        [Tooltip("Rasio ukuran unit hero saat mode tempur (berdiri di atas tatakan plinth)")]
        [SerializeField] protected float combatHeroScaleMultiplier = 0.72f;

        public virtual void UpdateVisualAppearance()
        {
            if (unitData == null || spriteRenderer == null) return;

            float cellW = GridManager.Instance != null ? GridManager.Instance.CellSize : 1.18f;

            Sprite targetSprite = null;
            if (isPreparationToken)
            {
                // Tampilkan sprite medali ringkas atau batu permata T0 di fase persiapan
                targetSprite = unitData.GetTokenSprite(currentTier);
            }
            else
            {
                // Prioritas sprite tempur menghadap ke atas / North jika tersedia
                if (unitData.unitType == UnitType.Cedric && currentTier == UnitTier.Tier1)
                {
                    targetSprite = Resources.Load<Sprite>("Sprites/Heroes/Cedric_T1_Combat_North")
                                ?? unitData.GetSpriteForTier(currentTier);
                }
                else
                {
                    targetSprite = unitData.GetSpriteForTier(currentTier);
                }
            }

            if (targetSprite != null)
            {
                spriteRenderer.sprite = targetSprite;
            }
            else if (spriteRenderer.sprite == null)
            {
                // Procedural fallback sprite agar unit/permata TIDAK PERNAH INVISIBLE di layar
                spriteRenderer.sprite = GetOrCreateProceduralFallbackSprite();
                spriteRenderer.color = unitData.themeColor;
            }

            // Rumus baku seragam:
            // Mode token/permata: mengisi cellW * fillRatio (0.75)
            // Mode hero tempur: di-scale proporsional agar kaki pas menapak di tatakan plinth dan tidak menabrak petak lain
            float spriteW = (spriteRenderer.sprite != null && spriteRenderer.sprite.bounds.size.x > 0.001f)
                ? spriteRenderer.sprite.bounds.size.x
                : (spriteRenderer.sprite != null ? (spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit) : 1f);

            float currentFill = (isPreparationToken || currentTier == UnitTier.Tier0) ? fillRatio : (fillRatio * combatHeroScaleMultiplier);
            float targetScale = (cellW * currentFill) / Mathf.Max(0.01f, spriteW);
            if (isPreparationToken || currentTier == UnitTier.Tier0)
            {
                targetScale = Mathf.Clamp(targetScale, 0.65f, 1.25f);
            }
            transform.localScale = Vector3.one * targetScale;

            // Sorting Layer standar arsitektur: Gem (selalu di atas Cell dan Board)
            spriteRenderer.sortingLayerName = "Gem";
            spriteRenderer.sortingOrder = isDragging ? dragSortingOrder : Mathf.Max(15, normalSortingOrder - Mathf.RoundToInt(transform.position.y * 3));

            if (visualAnimator != null)
            {
                visualAnimator.SetBaseScale(transform.localScale);

                // Modular 2.5D Rigging (Kepala Chibi, Dada/Jubah, Lengan Pedang, Lengan Perisai, dan Slash VFX)
                if (visualAnimator is ModularHeroVisualAnimator modularAnim)
                {
                    if (!isPreparationToken && currentTier > UnitTier.Tier0 && unitData != null && unitData.unitType == UnitType.Cedric)
                    {
                        int tNum = Mathf.Clamp((int)currentTier, 1, 5);
                        // Prioritas: Sprite Modular Combat North (menghadap utara / musuh yang datang dari atas layar)
                        Sprite torso = Resources.Load<Sprite>($"Sprites/Heroes/Modular/Cedric_T{tNum}_Torso_North")
                                    ?? Resources.Load<Sprite>("Sprites/Heroes/Modular/Cedric_T1_Torso_North")
                                    ?? Resources.Load<Sprite>($"Sprites/Heroes/Modular/Cedric_T{tNum}_Torso")
                                    ?? Resources.Load<Sprite>("Sprites/Heroes/Modular/Cedric_T1_Torso");
                        Sprite head = Resources.Load<Sprite>($"Sprites/Heroes/Modular/Cedric_T{tNum}_Head_North")
                                   ?? Resources.Load<Sprite>("Sprites/Heroes/Modular/Cedric_T1_Head_North")
                                   ?? Resources.Load<Sprite>($"Sprites/Heroes/Modular/Cedric_T{tNum}_Head")
                                   ?? Resources.Load<Sprite>("Sprites/Heroes/Modular/Cedric_T1_Head");
                        Sprite sword = Resources.Load<Sprite>($"Sprites/Heroes/Modular/Cedric_T{tNum}_Arm_Sword_North")
                                    ?? Resources.Load<Sprite>("Sprites/Heroes/Modular/Cedric_T1_Arm_Sword_North")
                                    ?? Resources.Load<Sprite>($"Sprites/Heroes/Modular/Cedric_T{tNum}_Arm_Sword")
                                    ?? Resources.Load<Sprite>("Sprites/Heroes/Modular/Cedric_T1_Arm_Sword");
                        Sprite shield = Resources.Load<Sprite>($"Sprites/Heroes/Modular/Cedric_T{tNum}_Arm_Shield_North")
                                     ?? Resources.Load<Sprite>("Sprites/Heroes/Modular/Cedric_T1_Arm_Shield_North")
                                     ?? Resources.Load<Sprite>($"Sprites/Heroes/Modular/Cedric_T{tNum}_Arm_Shield")
                                     ?? Resources.Load<Sprite>("Sprites/Heroes/Modular/Cedric_T1_Arm_Shield");
                        Sprite slash = Resources.Load<Sprite>("Sprites/VFX/VFX_Slash_Arc");

                        if (torso != null && head != null)
                        {
                            modularAnim.ConfigureModularSprites(torso, head, sword, shield, slash);
                            modularAnim.SetModularRigActive(true);
                            // Sembunyikan single-sprite bawaan agar tidak dobel/tumpang tindih
                            spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
                        }
                    }
                    else
                    {
                        // Sembunyikan rig modular tubuh saat mode token persiapan atau dragging
                        modularAnim.SetModularRigActive(false);

                        // Pulihkan visual normal (koin token medallion utuh) saat mode persiapan
                        if (spriteRenderer.color.a < 0.05f)
                        {
                            spriteRenderer.color = Color.white;
                        }
                    }
                }
            }

            UpdateDepthSorting();
            UpdateGroundShadowVisibility(!isPreparationToken && currentTier > UnitTier.Tier0);
        }

        private static Sprite proceduralGemFallbackSprite;

        protected Sprite GetOrCreateProceduralFallbackSprite()
        {
            if (proceduralGemFallbackSprite != null) return proceduralGemFallbackSprite;

            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = (size / 2f) - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float manhattanDist = Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y);
                    if (manhattanDist <= radius)
                    {
                        float borderDist = radius - manhattanDist;
                        colors[y * size + x] = borderDist <= 3f ? new Color(1f, 0.85f, 0.3f, 1f) : Color.white;
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            proceduralGemFallbackSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return proceduralGemFallbackSprite;
        }

        #region Mobile & Pointer Drag-and-Drop Interaction

        public static UnitController CurrentlySelectedUnit { get; private set; }

        public static void DeselectCurrentUnit()
        {
            if (CurrentlySelectedUnit != null)
            {
                var unit = CurrentlySelectedUnit;
                CurrentlySelectedUnit = null;
                unit.ToggleRangeCircle(false);
                if (unit.currentCell != null)
                {
                    unit.currentCell.SetHighlightState(TileHighlightState.None);
                }
                unit.UpdateVisualAppearance();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Respon awal sentuhan mobile
        }

        /// <summary>
        /// Dukungan Aksesibilitas Mobile: Tap-to-Select & Tap-to-Move/Merge/Swap.
        /// Memungkinkan pemain menata petak hanya dengan 2 ketukan jari tanpa harus menyeret (Zero Drag Friction).
        /// Pada mode pertempuran aktif, ketukan menampilkan radius jangkauan tembak hero (Attack Range Circle).
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging) return;

            // Jika grid sedang memproses pergeseran/fusi otomatis (Cascade Refill), tahan interaksi sentuhan
            if (GridMatch3Solver.Instance != null && GridMatch3Solver.Instance.IsResolving)
            {
                return;
            }

            // Jika dalam mode pertempuran aktif, ketukan menampilkan atau menyembunyikan lingkaran jangkauan serang (Visual Clarity)
            if (GameManager.Instance != null &&
                (GameManager.Instance.CurrentState == BattleState.WaveActive || GameManager.Instance.CurrentState == BattleState.CombatActive))
            {
                bool newState = !isRangeCircleVisible;
                foreach (var unit in ActiveUnits)
                {
                    if (unit != null && unit != this) unit.ToggleRangeCircle(false);
                }
                ToggleRangeCircle(newState);
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.ButtonClick, 1.25f);
                }
                return;
            }

            if (CurrentlySelectedUnit == null)
            {
                // Ketukan 1: Pilih unit/gem ini
                CurrentlySelectedUnit = this;
                transform.localScale = transform.localScale * 1.18f;
                ToggleRangeCircle(true);
                if (currentCell != null)
                {
                    currentCell.SetHighlightState(TileHighlightState.Hover);
                }
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.ButtonClick);
                }
            }
            else if (CurrentlySelectedUnit == this)
            {
                // Ketukan pada unit yang sama -> Batalkan pilihan
                DeselectCurrentUnit();
            }
            else
            {
                // Ketukan 2: Jalankan aksi pindah / fusi / swap dari unit terpilih ke unit ini
                UnitController sourceUnit = CurrentlySelectedUnit;
                DeselectCurrentUnit();

                if (sourceUnit != null && currentCell != null)
                {
                    sourceUnit.HandleDropOnCell(currentCell);
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Jika grid sedang memproses pergeseran/fusi otomatis (Cascade Refill), tahan aksi seret
            if (GridMatch3Solver.Instance != null && GridMatch3Solver.Instance.IsResolving)
            {
                return;
            }

            // Di mode puzzle saat gelombang tempur sedang aktif, token/hero fokus bertarung
            if (GameManager.Instance != null && GameManager.Instance.CurrentBattleMode == BattleMode.PuzzlePreparation && (GameManager.Instance.CurrentState == BattleState.WaveActive || GameManager.Instance.CurrentState == BattleState.CombatActive))
            {
                return;
            }

            isDragging = true;
            initialPosition = currentCell != null ? currentCell.transform.position : transform.position;
            transform.position = new Vector3(transform.position.x, transform.position.y, -0.5f);

            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = dragSortingOrder;

            // Visual feedback: Token terangkat sedikit membesar saat disentuh/diseret
            transform.localScale = transform.localScale * 1.15f;

            OnUnitDragStarted?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            if (mainCamera == null)
                mainCamera = Camera.main;

            Vector3 screenPos = eventData.position;
            screenPos.z = -mainCamera.transform.position.z;
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
            // Tambahkan offset vertikal khusus mobile agar jari tidak menutupi hero saat digeser
            Vector3 dragPos = worldPos + touchDragOffset;
            dragPos.z = -0.5f;
            transform.position = dragPos;

            // Deteksi petak terdekat secara matematis (World-Space Slot Snapping)
            TileCell hitCell = null;
            if (GridManager.Instance != null)
            {
                hitCell = GridManager.Instance.GetClosestCell(transform.position, GridManager.Instance.CellSize * 0.95f);
                if (hitCell == null)
                {
                    hitCell = GridManager.Instance.GetClosestCell(worldPos, GridManager.Instance.CellSize * 0.95f);
                }
            }

            if (hitCell != currentHoveredCell)
            {
                if (currentHoveredCell != null)
                {
                    currentHoveredCell.SetHighlightState(TileHighlightState.None);
                }

                currentHoveredCell = hitCell;

                if (currentHoveredCell != null && currentHoveredCell != currentCell)
                {
                    if (!currentHoveredCell.IsOccupied)
                    {
                        currentHoveredCell.SetHighlightState(TileHighlightState.ValidMove);
                    }
                    else if (CanMergeWith(currentHoveredCell.CurrentUnit))
                    {
                        currentHoveredCell.SetHighlightState(TileHighlightState.ValidMerge);
                    }
                    else
                    {
                        currentHoveredCell.SetHighlightState(TileHighlightState.Hover); // Kuning: Indikasi Tukar Posisi (Swap)
                    }
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;

            if (currentHoveredCell != null)
            {
                currentHoveredCell.SetHighlightState(TileHighlightState.None);
                currentHoveredCell = null;
            }

            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = normalSortingOrder;

            UpdateVisualAppearance(); // Kembalikan skala normal tier

            OnUnitDragEnded?.Invoke(this);

            if (mainCamera == null)
                mainCamera = Camera.main;

            Vector3 dropScreenPos = eventData.position;
            dropScreenPos.z = -mainCamera.transform.position.z;
            Vector3 dropWorldPos = mainCamera.ScreenToWorldPoint(dropScreenPos);

            // Deteksi petak target berdasarkan World-Space Slot Snapping standar 2D Merge Defense
            TileCell targetCell = null;
            if (GridManager.Instance != null)
            {
                // Prioritas 1: Posisi token visual saat dilepas
                targetCell = GridManager.Instance.GetClosestCell(transform.position, GridManager.Instance.CellSize * 0.95f);
                // Prioritas 2: Posisi jari sentuhan saat dilepas
                if (targetCell == null)
                {
                    targetCell = GridManager.Instance.GetClosestCell(dropWorldPos, GridManager.Instance.CellSize * 0.95f);
                }
            }

            if (targetCell != null && targetCell != currentCell)
            {
                HandleDropOnCell(targetCell);
                return;
            }

            // Jika dilepas di luar grid atau di petak asal, kembalikan secara halus
            SnapBack();
        }

        public virtual void HandleDropOnCell(TileCell targetCell)
        {
            if (targetCell == null || targetCell == currentCell)
            {
                SnapBack();
                return;
            }

            // FASE PERSIAPAN TAKTIS: MODE 1 (Turn-Based Puzzle Preparation / Inter-Wave Tactical Preparation)
            bool isPuzzlePreparationMode = GameManager.Instance != null &&
                GameManager.Instance.CurrentBattleMode == BattleMode.PuzzlePreparation &&
                GameManager.Instance.CurrentState == BattleState.Preparation;

            if (isPuzzlePreparationMode)
            {
                int dist = Mathf.Abs(currentCell.X - targetCell.X) + Mathf.Abs(currentCell.Y - targetCell.Y);

                if (targetCell.IsOccupied && targetCell.CurrentUnit != null)
                {
                    UnitController otherUnit = targetCell.CurrentUnit;

                    // Sesuai Aturan Resmi Mode 1 (Puzzle Preparation / SupaMerge):
                    // Menghapus mekanik tumpuk 2-unit; setiap geseran antar kotak bersebelahan murni tukar tempat (Swap).
                    // Fusi unit HANYA terjadi secara otomatis melalui Match-3 atau lebih (termasuk 4, 5, L, +) oleh GridMatch3Solver.
                    if (dist == 1)
                    {
                        ExecuteSwapWith(otherUnit);
                        GameManager.Instance.TryConsumeMove();
                        return;
                    }
                    else
                    {
                        // Menukar lebih dari 1 langkah tidak diizinkan di mode puzzle
                        SnapBack();
                        return;
                    }
                }
                else
                {
                    // Kasus Pindah ke Petak Kosong:
                    if (dist == 1)
                    {
                        TileCell oldCell = currentCell;
                        oldCell.ClearOccupant();
                        targetCell.SetOccupant(this);
                        SetCurrentCell(targetCell);
                        SmoothSnapTo(targetCell.transform.position, 0.12f);

                        OnUnitMoved?.Invoke(this, targetCell);
                        GameManager.Instance.TryConsumeMove();

                        if (GridMatch3Solver.Instance != null)
                        {
                            GridMatch3Solver.Instance.CheckAndResolveMatches(targetCell);
                        }
                        return;
                    }
                    else
                    {
                        SnapBack();
                        return;
                    }
                }
            }

            // MODE 2: REAL-TIME DEPLOY & MERGE (Lucky Defense / Rush Royale Style)
            if (!targetCell.IsOccupied)
            {
                currentCell.ClearOccupant();
                targetCell.SetOccupant(this);
                SetCurrentCell(targetCell);
                SmoothSnapTo(targetCell.transform.position, 0.12f);
                OnUnitMoved?.Invoke(this, targetCell);
                return;
            }

            UnitController targetUnit = targetCell.CurrentUnit;
            if (targetUnit != null && CanMergeWith(targetUnit))
            {
                ExecuteMergeWith(targetUnit, targetCell);
            }
            else
            {
                // Di mode real-time jika tidak merge, tukar posisi (Swap)
                ExecuteSwapWith(targetUnit);
            }
        }

        /// <summary>
        /// Menukar posisi dua unit secara halus di grid (Two-Way Cell Swap) dan memicu pemindaian Match-3 jika terbentuk deretan.
        /// </summary>
        public void ExecuteSwapWith(UnitController otherUnit)
        {
            if (otherUnit == null || currentCell == null || otherUnit.CurrentCell == null)
            {
                SnapBack();
                return;
            }

            TileCell cellA = currentCell;
            TileCell cellB = otherUnit.CurrentCell;

            cellA.ClearOccupant();
            cellB.ClearOccupant();

            cellA.SetOccupant(otherUnit);
            otherUnit.SetCurrentCell(cellA);
            otherUnit.SmoothSnapTo(cellA.transform.position, 0.14f);

            otherOldCellSwap(cellB);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            // PENTING: Kunci petak tujuan jari pemain (cellB) sebagai pusat fusi interaksi pemain!
            // Jangan memicu event unit sekunder (otherUnit) yang berpotensi menimpa lastInteractionCell.
            OnUnitMoved?.Invoke(this, cellB);

            // Cek apakah swap ini memicu Match-3 (kunci petak cellB dan this sebagai target swap mutlak)
            if (GridMatch3Solver.Instance != null)
            {
                GridMatch3Solver.Instance.SetPlayerInteraction(this, cellB);
                GridMatch3Solver.Instance.CheckAndResolveMatches(cellB);
            }
        }

        private void otherOldCellSwap(TileCell cellB)
        {
            cellB.SetOccupant(this);
            SetCurrentCell(cellB);
            SmoothSnapTo(cellB.transform.position, 0.14f);
        }

        public virtual bool CanMergeWith(UnitController otherUnit)
        {
            if (otherUnit == null) return false;
            if (otherUnit == this) return false;

            // Merge mensyaratkan: Tipe hero sama, Tier sama, dan belum mencapai Tier 5 maksimal
            return (unitData != null && otherUnit.UnitData != null) &&
                   (unitData.unitType == otherUnit.UnitType) &&
                   (currentTier == otherUnit.CurrentTier) &&
                   (currentTier < UnitTier.Tier5);
        }

        /// <summary>
        /// Mengeksekusi fusi langsung secara programatik (misalnya oleh Citadel Auto-Merge Assistant).
        /// </summary>
        public virtual bool MergeInto(UnitController targetUnit)
        {
            if (targetUnit == null || !CanMergeWith(targetUnit)) return false;
            if (targetUnit.CurrentCell == null || currentCell == null) return false;

            ExecuteMergeWith(targetUnit, targetUnit.CurrentCell);
            return true;
        }

        protected virtual void ExecuteMergeWith(UnitController targetUnit, TileCell targetCell)
        {
            UnitTier nextTier = (UnitTier)((int)currentTier + 1);

            // Bersihkan petak asal unit ini
            currentCell.ClearOccupant();

            // Naikkan tier unit penerima di petak tujuan
            targetUnit.UpgradeTier(nextTier);

            // Siarkan event keberhasilan merge
            OnUnitMerged?.Invoke(this, targetUnit, nextTier);

            // Respon Haptic Vibration pada perangkat ponsel
            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null)
            {
                var hapticType = nextTier >= UnitTier.Tier4 ? FantasyDefense.Mobile.HapticFeedbackType.Heavy :
                                 (nextTier == UnitTier.Tier3 ? FantasyDefense.Mobile.HapticFeedbackType.Medium : FantasyDefense.Mobile.HapticFeedbackType.Light);
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(hapticType);
            }

            // Hancurkan unit yang digeser (melebur ke unit tujuan)
            Destroy(gameObject);

            // Picu pergeseran isi dari bawah (Cascade from Below) HANYA pada Mode 1 (Puzzle Preparation) saat fase persiapan
            if (GridMatch3Solver.Instance != null && GameManager.Instance != null &&
                GameManager.Instance.CurrentBattleMode == BattleMode.PuzzlePreparation &&
                GameManager.Instance.CurrentState == BattleState.Preparation)
            {
                GridMatch3Solver.Instance.TriggerCascadeRefill();
            }
        }

        public virtual void UpgradeTier(UnitTier nextTier)
        {
            currentTier = nextTier;
            UpdateVisualAppearance();

            if (visualAnimator != null)
            {
                visualAnimator.PlayMergeCelebration(nextTier);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            if (DevicePerformanceManager.Instance != null)
            {
                var haptic = nextTier >= UnitTier.Tier4 ? HapticFeedbackType.Heavy :
                             (nextTier == UnitTier.Tier3 ? HapticFeedbackType.Medium : HapticFeedbackType.Light);
                DevicePerformanceManager.Instance.TriggerHaptic(haptic);
            }
        }

        /// <summary>
        /// Mengembalikan unit secara magnetis dan halus ke posisi petaknya.
        /// </summary>
        public void SnapBack()
        {
            Vector3 target = currentCell != null ? currentCell.transform.position : initialPosition;
            SmoothSnapTo(target, 0.12f);
        }

        /// <summary>
        /// Animasi lerp meluncur halus (magnetic snap) ke posisi target.
        /// </summary>
        public void SmoothSnapTo(Vector3 targetPos, float duration = 0.12f)
        {
            if (this == null) return;
            if (!gameObject.activeInHierarchy)
            {
                targetPos.z = -0.1f;
                transform.position = targetPos;
                return;
            }

            if (snapRoutine != null)
            {
                StopCoroutine(snapRoutine);
            }
            snapRoutine = StartCoroutine(SmoothSnapRoutine(targetPos, duration));
        }

        private IEnumerator SmoothSnapRoutine(Vector3 targetPos, float duration)
        {
            targetPos.z = -0.1f;
            Vector3 startPos = transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * (2f - t); // Ease-Out Quad curve
                Vector3 current = Vector3.Lerp(startPos, targetPos, t);
                current.z = -0.1f;
                transform.position = current;
                yield return null;
            }
            transform.position = targetPos;
            UpdateDepthSorting();
            snapRoutine = null;
        }

        #endregion

        #region Combat Base Properties & Attack Loop

        [Header("Combat Runtime")]
        protected float attackCooldownTimer = 0f;
        protected MonsterController currentCombatTarget;

        protected virtual void Update()
        {
            // Saat sedang digeser, dalam wujud token, atau dalam fase persiapan, tahan loop tempur
            if (isDragging || isPreparationToken) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == BattleState.Preparation) return;

            HandleCombatLoop();
        }

        protected virtual void LateUpdate()
        {
            // Guardrail Anti-Melayang (Point 2):
            // Saat unit sedang diam (tidak ditarik jari dan tidak dalam animasi lerp),
            // posisinya mutlak terkunci di titik pusat petaknya.
            if (!isDragging && snapRoutine == null && currentCell != null)
            {
                if (visualAnimator == null || visualAnimator.CurrentState == FantasyDefense.Combat.AnimationState.Idle)
                {
                    if (Vector3.Distance(transform.position, currentCell.transform.position) > 0.02f)
                    {
                        transform.position = new Vector3(currentCell.transform.position.x, currentCell.transform.position.y, -0.1f);
                    }
                }
            }
        }

        protected virtual void HandleCombatLoop()
        {
            attackCooldownTimer -= Time.deltaTime;

            if (attackCooldownTimer <= 0f)
            {
                MonsterController target = FindBestTargetInRange();
                if (target != null)
                {
                    currentCombatTarget = target;

                    // Hadapkan sprite ke arah monster yang diserang
                    // Guardrail Anti-Mirror Text: Jangan pernah flipX jika sprite adalah medali token berteks (Point 2)
                    if (spriteRenderer != null)
                    {
                        bool isTokenSprite = isPreparationToken || (spriteRenderer.sprite != null && (spriteRenderer.sprite.name.StartsWith("Token_") || spriteRenderer.sprite.name.StartsWith("Gem_")));
                        if (!isTokenSprite)
                        {
                            spriteRenderer.flipX = target.transform.position.x < transform.position.x;
                        }
                        else
                        {
                            spriteRenderer.flipX = false;
                        }
                    }

                    ExecuteAttack(target);
                    attackCooldownTimer = 1f / Mathf.Max(0.1f, GetAttackRate());
                }
            }
        }

        public virtual MonsterController FindBestTargetInRange()
        {
            var monsters = MonsterController.GetActiveMonstersSnapshot();
            if (monsters.Count == 0) return null;

            MonsterController bestTarget = null;
            float maxRange = GetAttackRange();
            float closestDist = maxRange;
            Vector3 myPos = transform.position;

            // Prioritas Taktis Pertahanan (Point 1 - Focus-Fire Defender Priority):
            // Cari monster yang sedang aktif mendobrak gerbang benteng di garis batas
            MonsterController barricadeThreat = null;
            float closestThreatDist = maxRange;

            for (int i = 0; i < monsters.Count; i++)
            {
                var monster = monsters[i];
                if (monster == null || monster.IsDead) continue;

                float dist = Vector3.Distance(myPos, monster.transform.position);
                if (dist <= maxRange)
                {
                    // Prioritas Tertinggi: Monster yang sedang mendobrak gerbang benteng
                    if (monster.IsAttackingBarricade)
                    {
                        if (dist <= closestThreatDist)
                        {
                            closestThreatDist = dist;
                            barricadeThreat = monster;
                        }
                    }

                    if (dist <= closestDist)
                    {
                        closestDist = dist;
                        bestTarget = monster;
                    }
                }
            }

            // Utamakan musuh yang mengancam gerbang jika ada dalam jangkauan
            if (barricadeThreat != null)
            {
                return barricadeThreat;
            }

            return bestTarget;
        }

        public virtual void TakeDamage(float damageAmount)
        {
            if (!IsAlive) return;

            currentHealth -= damageAmount;

            if (visualAnimator != null)
            {
                visualAnimator.PlayHitRecoil();
            }

            if (FantasyDefense.Combat.SkillVfxManager.Instance != null)
            {
                FantasyDefense.Combat.SkillVfxManager.Instance.SpawnFloatingText(
                    transform.position + Vector3.up * 0.35f,
                    $"-{Mathf.RoundToInt(damageAmount)}",
                    new Color(1f, 0.3f, 0.3f)
                );
            }

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                OnHeroDefeated();
            }
        }

        protected virtual void OnHeroDefeated()
        {
            if (visualAnimator != null)
            {
                visualAnimator.PlayDefeatAnimation();
            }

            // Jika HP 0, hero gugur dan dihilangkan dari papan pada wave tempur tersebut
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(DefeatedDisappearRoutine());
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator DefeatedDisappearRoutine()
        {
            // Efek dissolve/fade out saat pahlawan gugur di tengah pertempuran
            float duration = 0.45f;
            float elapsed = 0f;
            Vector3 initialScale = transform.localScale;
            Color initColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(initColor.r, initColor.g, initColor.b, Mathf.Lerp(initColor.a, 0f, t));
                }
                yield return null;
            }

            // Nonaktifkan visual dan objek di papan wave ini agar monster dapat lewat jika tidak ada hero lain yang menghadang
            gameObject.SetActive(false);
        }

        public virtual void ReviveHero()
        {
            currentHealth = maxHealth;
            gameObject.SetActive(true);
            UpdateVisualAppearance();
            if (visualAnimator != null)
            {
                visualAnimator.RestoreNormalVisual();
            }
        }

        protected virtual void ExecuteAttack(MonsterController target)
        {
            if (target == null || target.IsDead) return;

            float damage = GetAttackDamage();
            bool isCrit = UnityEngine.Random.value < GetCriticalChance();

            if (isCrit)
            {
                float critMultiplier = 1.75f;
                // Integrasi Berserker's Fury 4-Piece: +50% Crit DMG saat benteng HP < 50% (Sprint 13)
                if (Metagame.ArtifactManager.Instance != null && CastleBarricade.Instance != null && CastleBarricade.Instance.HealthPercent < 0.5f)
                {
                    critMultiplier += Metagame.ArtifactManager.Instance.GetHeroCritDamageBonus();
                }
                damage *= critMultiplier;
            }

            target.TakeDamage(damage, isCrit);

            if (visualAnimator != null)
            {
                visualAnimator.PlayAttack(target.transform.position);
            }
            else if (gameObject.activeInHierarchy)
            {
                StartCoroutine(AnimateAttackPunch());
            }
        }

        protected System.Collections.IEnumerator AnimateAttackPunch()
        {
            Vector3 originalScale = transform.localScale;
            transform.localScale = originalScale * 1.15f;
            yield return new WaitForSeconds(0.08f);
            transform.localScale = originalScale;
        }

        public virtual float GetAttackDamage()
        {
            float baseAtk = unitData != null ? unitData.baseDamage : 10f;

            // Integrasi Level & Equipment dari Citadel Lobby Metagame
            if (Metagame.CitadelLobbyManager.Instance != null)
            {
                var profile = Metagame.CitadelLobbyManager.Instance.GetHeroProfile(UnitType);
                if (profile != null)
                {
                    baseAtk = profile.GetTotalAttack();
                }
            }

            float tierMultiplier = unitData != null ? unitData.GetTierMultiplier(currentTier) : 1.0f;
            float totalDamage = baseAtk * tierMultiplier;
            if (BattleBlessingManager.Instance != null)
            {
                totalDamage *= BattleBlessingManager.Instance.HeroDamageMultiplier;
            }
            return totalDamage;
        }

        public virtual float GetCriticalChance()
        {
            float baseCrit = unitData != null ? unitData.criticalChance : 0.05f;

            if (Metagame.CitadelLobbyManager.Instance != null)
            {
                var profile = Metagame.CitadelLobbyManager.Instance.GetHeroProfile(UnitType);
                if (profile != null)
                {
                    baseCrit = profile.GetTotalCritChance();
                }
            }

            // Integrasi Talenta Citadel: Lethal Precision (Sprint 12)
            if (Metagame.CitadelTalentManager.Instance != null)
            {
                baseCrit += Metagame.CitadelTalentManager.Instance.GetHeroCritChanceBonus();
            }

            // Integrasi Set Artefak: Berserker's Fury 2-Piece (+8% Crit Chance) (Sprint 13)
            if (Metagame.ArtifactManager.Instance != null)
            {
                baseCrit += Metagame.ArtifactManager.Instance.GetHeroCritChanceBonus();
            }

            if (BattleBlessingManager.Instance != null)
            {
                baseCrit += BattleBlessingManager.Instance.CritChanceBonus;
            }
            return baseCrit;
        }

        public virtual float GetAttackRange()
        {
            float baseRange = unitData != null ? unitData.attackRange : 1.0f;

            // Jangkauan serang pahlawan ke lintasan serbuan jalan batu tengah:
            // Unit Melee (Cedric, Rose) menjangkau hingga 2.8 unit dunia ke arah jalan
            // Unit Ranged (Rabbit, Wind) menjangkau hingga 6.5 unit dunia ke seluruh lintasan tempur
            float worldRange = baseRange <= 1.5f ? 2.8f : (baseRange * 1.6f);

            // Integrasi Berkah Taktis: Eagle Eye untuk Hero Ranged
            if (BattleBlessingManager.Instance != null && baseRange > 1.5f)
            {
                worldRange += BattleBlessingManager.Instance.RangedRangeBonus;
            }

            // Integrasi Afiks Cuaca: Abyssal Fog memotong range hero jarak jauh (Sprint 12)
            if (BattlefieldWeatherManager.Instance != null)
            {
                worldRange *= BattlefieldWeatherManager.Instance.GetHeroRangeMultiplier(UnitType);
            }

            return worldRange;
        }

        public virtual float GetAttackRate()
        {
            float baseRate = unitData != null ? unitData.attackRate : 1.0f;

            // Integrasi Talenta Citadel: Elemental Synergy (Sprint 12)
            float talentSpeed = 0f;
            if (Metagame.CitadelTalentManager.Instance != null)
            {
                talentSpeed = Metagame.CitadelTalentManager.Instance.GetHeroAttackSpeedBonus();
            }

            // Integrasi Set Artefak: Sorcerer's Surge 2-Piece (+10% Attack Speed) (Sprint 13)
            float artifactSpeed = 0f;
            if (Metagame.ArtifactManager.Instance != null)
            {
                artifactSpeed = Metagame.ArtifactManager.Instance.GetHeroAttackSpeedBonus();
            }

            return baseRate * (1f + attackSpeedBuff + talentSpeed + artifactSpeed);
        }

        public void ApplyAttackSpeedBuff(float percent, float duration)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(AttackSpeedBuffRoutine(percent, duration));
            }
        }

        private System.Collections.IEnumerator AttackSpeedBuffRoutine(float percent, float duration)
        {
            attackSpeedBuff += percent;
            yield return new WaitForSeconds(duration);
            attackSpeedBuff = Mathf.Max(0f, attackSpeedBuff - percent);
        }

        protected void NotifySkillTriggered(string skillName)
        {
            OnUnitSkillTriggered?.Invoke(this, skillName);
        }

        #endregion

        #region Hero Defender Battle Emotes (Point 3)

        /// <summary>
        /// Memicu balon emote reaksi hero ketika benteng diserang musuh (Point 3 - Hero Defender Battle Emotes).
        /// </summary>
        public void TriggerDefenderEmote()
        {
            if (unitData == null || !gameObject.activeInHierarchy) return;

            string emoteText = UnitType switch
            {
                UnitType.Cedric => FantasyDefense.Localization.CitadelLocalizationManager.Text("🛡️ TAHAN GERBANG!", "🛡️ HOLD THE GATE!"),
                UnitType.RoseTanker => FantasyDefense.Localization.CitadelLocalizationManager.Text("🌸 LINDUNGI BENTENG!", "🌸 DEFEND THE CITADEL!"),
                UnitType.RabbitMage => FantasyDefense.Localization.CitadelLocalizationManager.Text("✨ MUNDUR KALIAN!", "✨ BEGONE FOOLS!"),
                UnitType.WindRanger => FantasyDefense.Localization.CitadelLocalizationManager.Text("🏹 FOKUS PANAH KE GERBANG!", "🏹 TARGET THE SIEGE!"),
                _ => FantasyDefense.Localization.CitadelLocalizationManager.Text("⚔️ BERTAHAN!", "⚔️ DEFEND!")
            };

            Color theme = unitData != null ? unitData.themeColor : new Color(1f, 0.85f, 0.3f);

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnDefenderBattleEmote(transform.position + Vector3.up * 0.95f, emoteText, theme);
            }
        }

        #endregion
    }
}

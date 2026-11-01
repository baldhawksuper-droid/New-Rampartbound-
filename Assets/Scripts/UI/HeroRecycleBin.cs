using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FantasyDefense.Combat;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Economy;
using FantasyDefense.GridSystem;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Zona pembuangan hero (Trash / Recycle Altar) pada HUD pertempuran (Sprint 14 Polish).
    /// Mengizinkan pemain untuk mendrag unit pahlawan yang tidak diinginkan ke zona ini
    /// guna mengosongkan petak arena dan memperoleh pengembalian sebagian Gems/Mana (refund 50%).
    /// </summary>
    public class HeroRecycleBin : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static HeroRecycleBin Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image binIconImage;
        [SerializeField] private Image binFrameImage;
        [SerializeField] private Text binLabelText;
        [SerializeField] private Text refundPreviewText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Tutorial Hint Tooltip")]
        private GameObject firstTimeTooltipObj;
        private const string PREF_SEEN_RECYCLE_TUTORIAL = "Rampartbound_SeenRecycleTutorial";

        [Header("State")]
        private bool isHovered = false;
        private bool isDragActive = false;
        private Coroutine pulseCoroutine;
        private Coroutine runeGlowCoroutine;

        [Header("Undo State")]
        private GameObject undoButtonObj;
        private Text undoButtonText;
        private Image undoFillBarImage;
        private Coroutine undoCountdownCoroutine;
        private UnitDataSO lastRecycledHeroData;
        private UnitTier lastRecycledTier;
        private TileCell lastRecycledCell;
        private int lastRecycledRefundAmount;

        public static event Action<UnitController, int> OnHeroRecycled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            EnsureVisualElementsBuilt();
        }

        private void OnEnable()
        {
            UnitController.OnUnitDragStarted += HandleUnitDragStarted;
            UnitController.OnUnitDragEnded += HandleUnitDragEnded;
            SetDropZoneActive(false);
        }

        private void OnDisable()
        {
            UnitController.OnUnitDragStarted -= HandleUnitDragStarted;
            UnitController.OnUnitDragEnded -= HandleUnitDragEnded;
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            if (runeGlowCoroutine != null) StopCoroutine(runeGlowCoroutine);
            if (undoCountdownCoroutine != null) StopCoroutine(undoCountdownCoroutine);
            ClearUndoWindow();

            if (firstTimeTooltipObj != null)
            {
                firstTimeTooltipObj.SetActive(false);
            }
        }

        private void EnsureVisualElementsBuilt()
        {
            var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            var frame9Slice = Resources.Load<Sprite>("Sprites/UI/Frame_StoneBastion_9Slice")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Frame_StoneBastion_9Slice.png")
#endif
                          ;
            var altarIconSprite = Resources.Load<Sprite>("Sprites/UI/UI_Altar_Recycle_Hero")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/UI_Altar_Recycle_Hero.png")
#endif
                          ;

            if (binFrameImage == null)
            {
                binFrameImage = gameObject.GetComponent<Image>();
                if (binFrameImage == null) binFrameImage = gameObject.AddComponent<Image>();
                binFrameImage.sprite = frame9Slice;
                binFrameImage.type = Image.Type.Sliced;
                binFrameImage.color = new Color(0.18f, 0.08f, 0.10f, 0.88f); // Dark crimson slate
            }

            // Ikon Grafis Altar Peleburan / Tungku Runic Recycle
            if (binIconImage == null)
            {
                var iconObj = new GameObject("AltarIcon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(transform, false);
                var rt = iconObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.62f);
                rt.anchorMax = new Vector2(0.5f, 0.62f);
                rt.sizeDelta = new Vector2(58f, 58f);

                binIconImage = iconObj.GetComponent<Image>();
                binIconImage.sprite = altarIconSprite;
                binIconImage.preserveAspect = true;
                binIconImage.raycastTarget = false;
            }

            if (binLabelText == null)
            {
                var labelObj = new GameObject("RecycleLabel", typeof(RectTransform), typeof(Text));
                labelObj.transform.SetParent(transform, false);
                var rt = labelObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.26f);
                rt.anchorMax = new Vector2(1f, 0.52f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                binLabelText = labelObj.GetComponent<Text>();
                binLabelText.font = lilitaFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                binLabelText.fontSize = 15;
                binLabelText.alignment = TextAnchor.MiddleCenter;
                binLabelText.color = new Color(1.0f, 0.45f, 0.45f, 1f);
                binLabelText.text = "RECYCLE";
                binLabelText.raycastTarget = false;
            }

            if (refundPreviewText == null)
            {
                var refundObj = new GameObject("RefundPreview", typeof(RectTransform), typeof(Text));
                refundObj.transform.SetParent(transform, false);
                var rt = refundObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.02f);
                rt.anchorMax = new Vector2(1f, 0.28f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                refundPreviewText = refundObj.GetComponent<Text>();
                refundPreviewText.font = lilitaFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                refundPreviewText.fontSize = 13;
                refundPreviewText.alignment = TextAnchor.MiddleCenter;
                refundPreviewText.color = new Color(1.0f, 0.85f, 0.35f, 0.95f);
                refundPreviewText.text = "+50% GEMS";
                refundPreviewText.raycastTarget = false;
            }
        }

        private void HandleUnitDragStarted(UnitController unit)
        {
            // Tempat sampah aktif di Mode 2 (Defense) atau saat pemain menyeret hero tempur
            bool isPuzzlePreparation = GameManager.Instance != null &&
                GameManager.Instance.CurrentBattleMode == BattleMode.PuzzlePreparation &&
                GameManager.Instance.CurrentState == BattleState.Preparation;

            if (isPuzzlePreparation)
            {
                // Pada Mode 1 puzzle, token papan tidak dibuang melainkan di-swap match-3
                return;
            }

            if (unit != null)
            {
                isDragActive = true;
                int refundVal = CalculateRefund(unit);
                if (refundPreviewText != null)
                {
                    refundPreviewText.text = $"+{refundVal} 💎";
                }
                SetDropZoneActive(true);
                CheckAndShowFirstTimeRecycleHint();
            }
        }

        private void HandleUnitDragEnded(UnitController unit)
        {
            isDragActive = false;
            isHovered = false;
            SetDropZoneActive(false);
        }

        private void SetDropZoneActive(bool active)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = active ? 0.95f : 0f;
                canvasGroup.interactable = active;
                canvasGroup.blocksRaycasts = active;
            }
            else
            {
                gameObject.SetActive(active);
            }

            // Audio respon buka / tutup altar
            if (active)
            {
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySound(Audio.SoundType.AltarWhooshOpen, 1.1f);
                }

                if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
                pulseCoroutine = StartCoroutine(PulseRoutine());
            }
            else
            {
                if (isDragActive && Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySound(Audio.SoundType.AltarWhooshClose, 0.95f);
                }

                if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
                transform.localScale = Vector3.one;
            }
        }

        private IEnumerator PulseRoutine()
        {
            while (isDragActive)
            {
                float targetScale = isHovered ? 1.15f : 1.04f;
                float s = 1.0f + Mathf.Sin(Time.unscaledTime * (isHovered ? 8f : 4f)) * (targetScale - 1.0f);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isDragActive) return;
            isHovered = true;

            if (binFrameImage != null)
            {
                binFrameImage.color = new Color(0.55f, 0.15f, 0.18f, 0.98f); // Glowing active crimson
            }

            // Pendaran skala mikro ikon altar saat drag hero melintasi area (1.05x scale pop)
            if (binIconImage != null)
            {
                binIconImage.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
            }

            if (runeGlowCoroutine != null) StopCoroutine(runeGlowCoroutine);
            runeGlowCoroutine = StartCoroutine(RuneGlowPulseRoutine());

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Light);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isDragActive) return;
            isHovered = false;

            if (runeGlowCoroutine != null) StopCoroutine(runeGlowCoroutine);

            if (binFrameImage != null)
            {
                binFrameImage.color = new Color(0.18f, 0.08f, 0.10f, 0.88f);
            }

            if (binIconImage != null)
            {
                binIconImage.color = Color.white;
                binIconImage.transform.localScale = Vector3.one; // Kembalikan skala ikon ke normal
            }
        }

        private IEnumerator RuneGlowPulseRoutine()
        {
            float timer = 0f;
            while (isHovered && isDragActive)
            {
                timer += Time.unscaledDeltaTime * 6f;
                float glow = (Mathf.Sin(timer) + 1f) * 0.5f;

                if (binIconImage != null)
                {
                    // Pendaran warna rune emas-oranye membara di sekeliling kawah altar
                    binIconImage.color = Color.Lerp(Color.white, new Color(1.0f, 0.75f, 0.35f, 1f), glow);
                }

                // Emisi partikel percikan bara esensi halus saat melayang di atas altar
                if (SkillVfxManager.Instance != null && UnityEngine.Random.value < 0.25f)
                {
                    Vector3 jitter = new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(-0.25f, 0.25f), 0f);
                    SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                        transform.position + jitter,
                        new Color(1f, 0.75f, 0.2f),
                        new Color(1f, 0.3f, 0.1f),
                        4,
                        1.5f,
                        555
                    );
                }

                yield return null;
            }

            if (binIconImage != null) binIconImage.color = Color.white;
        }

        /// <summary>
        /// Menghitung jumlah refund gems dari pembuangan unit berdasarkan tier pahlawan.
        /// </summary>
        public int CalculateRefund(UnitController unit)
        {
            if (unit == null) return 10;
            int baseSummonCost = 20;
            if (EconomyManager.Instance != null)
            {
                baseSummonCost = Mathf.Max(20, EconomyManager.Instance.GetCurrentSummonCost() / 2);
            }

            int multiplier = unit.CurrentTier switch
            {
                UnitTier.Tier1 => 1,
                UnitTier.Tier2 => 2,
                UnitTier.Tier3 => 4,
                UnitTier.Tier4 => 8,
                UnitTier.Tier5 => 16,
                _ => 1
            };

            return (baseSummonCost * multiplier) / 2;
        }

        /// <summary>
        /// Mengecek apakah koordinat layar sentuhan (screen position) berada di dalam area batas Recycle Bin.
        /// </summary>
        public bool ContainsScreenPoint(Vector2 screenPoint, Camera cam = null)
        {
            if (rectTransform == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, cam);
        }

        /// <summary>
        /// Mengeksekusi daur ulang / penjualan unit pahlawan:
        /// 1. Memberikan refund gems ke pemain via EconomyManager.
        /// 2. Memunculkan teks melayang '+XX GEMS' dan partikel recycle.
        /// 3. Mengosongkan petak TileCell asal.
        /// 4. Menghancurkan GameObject unit dari arena.
        /// </summary>
        public bool TryRecycleHero(UnitController unit)
        {
            if (unit == null) return false;

            int refundGems = CalculateRefund(unit);

            // 1. Tambahkan refund Gems
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddGems(refundGems);
            }

            // 2. Efek visual dan audio daur ulang
            Vector3 unitPos = unit.transform.position;
            Vector3 altarTargetPos = transform.position;
            Sprite heroSprite = unit.GetComponent<SpriteRenderer>() != null ? unit.GetComponent<SpriteRenderer>().sprite : null;

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.HeroRecycleChime);
            }

            // Feedback getaran perangkat
            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Medium);
            }

            // 3. Simpan state untuk Undo Window 3 detik
            TileCell originCell = unit.CurrentCell;
            UnitDataSO originData = unit.HeroData;
            UnitTier originTier = unit.CurrentTier;

            // Kosongkan cell petak
            if (unit.CurrentCell != null)
            {
                unit.CurrentCell.ClearOccupant();
            }

            OnHeroRecycled?.Invoke(unit, refundGems);
            Debug.Log($"[HeroRecycleBin] Unit {unit.HeroType} T{(int)unit.CurrentTier} berhasil didaur ulang. Refund: {refundGems} Gems.");

            // Aktifkan jendela waktu Urungkan (Undo Window 3 Detik)
            ActivateUndoWindow(originData, originTier, originCell, refundGems);

            // 4. Efek animasi hisap (Suction Vortex) ke kawah altar sebelum GameObject dihancurkan
            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnFloatingText(
                    unitPos + Vector3.up * 0.7f,
                    $"♻️ +{refundGems} 💎 REFUND!",
                    new Color(0.35f, 1.0f, 0.55f),
                    1.35f,
                    true
                );

                GameObject heroObjToDestroy = unit.gameObject;
                // Sembunyikan unit asli seketika agar aktor hisap vortex mengambil alih secara mulus
                unit.gameObject.SetActive(false);

                SkillVfxManager.Instance.SpawnRecycleSuctionVortex(unitPos, altarTargetPos, heroSprite, () =>
                {
                    if (heroObjToDestroy != null)
                    {
                        Destroy(heroObjToDestroy);
                    }
                });
            }
            else
            {
                Destroy(unit.gameObject);
            }

            return true;
        }

        #region Undo Recycle Feature (Window 3 Detik)

        private void ActivateUndoWindow(UnitDataSO data, UnitTier tier, TileCell cell, int refundAmount)
        {
            if (data == null || cell == null) return;

            lastRecycledHeroData = data;
            lastRecycledTier = tier;
            lastRecycledCell = cell;
            lastRecycledRefundAmount = refundAmount;

            EnsureUndoButtonCreated();

            if (undoCountdownCoroutine != null) StopCoroutine(undoCountdownCoroutine);
            undoCountdownCoroutine = StartCoroutine(UndoCountdownRoutine(3.0f));
        }

        private void EnsureUndoButtonCreated()
        {
            if (undoButtonObj != null)
            {
                UpdateUndoButtonPosition();
                return;
            }

            var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            var frame9Slice = Resources.Load<Sprite>("Sprites/UI/Frame_StoneBastion_9Slice")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Frame_StoneBastion_9Slice.png")
#endif
                          ;

            undoButtonObj = new GameObject("RecycleUndoButton", typeof(RectTransform), typeof(Image), typeof(Button));
            undoButtonObj.transform.SetParent(transform.parent, false);

            var rt = undoButtonObj.GetComponent<RectTransform>();
            rt.anchorMin = rectTransform.anchorMin;
            rt.anchorMax = rectTransform.anchorMax;
            rt.pivot = rectTransform.pivot;
            rt.sizeDelta = new Vector2(104f, 44f);
            UpdateUndoButtonPosition();

            var img = undoButtonObj.GetComponent<Image>();
            img.sprite = frame9Slice;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.25f, 0.20f, 0.22f, 0.95f); // Dark stone backing

            var btn = undoButtonObj.GetComponent<Button>();
            btn.onClick.AddListener(OnUndoButtonClicked);

            // Bar Pengukur Waktu Undo (Fill Bar) menyusut di latar belakang tombol
            var fillBarObj = new GameObject("UndoFillBar", typeof(RectTransform), typeof(Image));
            fillBarObj.transform.SetParent(undoButtonObj.transform, false);
            var fillRt = fillBarObj.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0.04f, 0.08f);
            fillRt.anchorMax = new Vector2(0.96f, 0.22f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            undoFillBarImage = fillBarObj.GetComponent<Image>();
            undoFillBarImage.color = new Color(0.35f, 0.95f, 0.55f, 0.85f); // Glowing emerald timer bar
            undoFillBarImage.raycastTarget = false;

            var textObj = new GameObject("UndoText", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(undoButtonObj.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0.18f);
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            undoButtonText = textObj.GetComponent<Text>();
            undoButtonText.font = lilitaFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            undoButtonText.fontSize = 13;
            undoButtonText.alignment = TextAnchor.MiddleCenter;
            undoButtonText.color = new Color(1.0f, 0.85f, 0.35f, 1f);
            undoButtonText.text = "↩️ UNDO (3s)";
            undoButtonText.raycastTarget = false;
        }

        /// <summary>
        /// Menyesuaikan posisi tombol undo secara responsif berdasarkan rasio aspek layar
        /// agar tidak terpotong oleh notch, bezel, atau batas layar tablet (4:3) / ultrawide (20:9).
        /// </summary>
        private void UpdateUndoButtonPosition()
        {
            if (undoButtonObj == null || rectTransform == null) return;
            var rt = undoButtonObj.GetComponent<RectTransform>();

            float screenAspect = (float)Screen.width / Mathf.Max(1f, Screen.height);
            // Default offset ke kiri altar
            float targetOffsetX = -115f;
            float targetOffsetY = 0f;

            // Jika rasio layar sempit/tablet (<= 9:16 atau 4:3), geser ke bawah sedikit agar tetap di dalam safe area
            if (screenAspect < 0.52f) // Rasio sangat tinggi/ramping (20:9 atau 21:9)
            {
                targetOffsetX = -110f;
            }
            else if (screenAspect > 0.65f) // Tablet lebar (4:3 / 3:2)
            {
                targetOffsetX = -120f;
            }

            rt.anchoredPosition = rectTransform.anchoredPosition + new Vector2(targetOffsetX, targetOffsetY);
        }

        private IEnumerator UndoCountdownRoutine(float duration)
        {
            if (undoButtonObj != null) undoButtonObj.SetActive(true);

            float remaining = duration;
            while (remaining > 0f)
            {
                if (undoButtonText != null)
                {
                    undoButtonText.text = $"↩️ UNDO ({remaining:F1}s)";
                }

                if (undoFillBarImage != null)
                {
                    float pct = Mathf.Clamp01(remaining / duration);
                    var rt = undoFillBarImage.rectTransform;
                    rt.anchorMax = new Vector2(0.04f + 0.92f * pct, 0.22f);
                    // Warna timer bar bertransisi dari hijau zamrud ke kuning lalu merah saat waktu menipis
                    undoFillBarImage.color = pct > 0.45f
                        ? Color.Lerp(new Color(1f, 0.85f, 0.2f, 0.85f), new Color(0.35f, 0.95f, 0.55f, 0.85f), (pct - 0.45f) / 0.55f)
                        : Color.Lerp(new Color(1f, 0.25f, 0.25f, 0.85f), new Color(1f, 0.85f, 0.2f, 0.85f), pct / 0.45f);
                }

                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            ClearUndoWindow();
        }

        private void OnUndoButtonClicked()
        {
            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null && FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Selection);
            }

            if (lastRecycledHeroData == null || lastRecycledCell == null)
            {
                ClearUndoWindow();
                return;
            }

            // Periksa apakah petak asal sudah terisi oleh unit lain
            if (lastRecycledCell.IsOccupied)
            {
                if (SkillVfxManager.Instance != null)
                {
                    SkillVfxManager.Instance.SpawnFloatingText(
                        lastRecycledCell.transform.position + Vector3.up * 0.7f,
                        "⚠️ PETAK SUDAH TERISI!",
                        Color.yellow,
                        1.2f,
                        true
                    );
                }
                ClearUndoWindow();
                return;
            }

            // Potong kembali Gems yang sebelumnya direfund
            if (EconomyManager.Instance != null)
            {
                if (!EconomyManager.Instance.SpendGems(lastRecycledRefundAmount))
                {
                    if (SkillVfxManager.Instance != null)
                    {
                        SkillVfxManager.Instance.SpawnFloatingText(
                            transform.position + Vector3.up * 0.7f,
                            "⚠️ GEMS TIDAK CUKUP UNTUK UNDO!",
                            Color.red,
                            1.2f,
                            true
                        );
                    }
                    ClearUndoWindow();
                    return;
                }
            }

            // Spawn kembali unit pahlawan ke petak asal
            if (HeroSummoner.Instance != null)
            {
                HeroSummoner.Instance.SpawnUnitAtCell(lastRecycledHeroData, lastRecycledCell, lastRecycledTier);
            }

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnFloatingText(
                    lastRecycledCell.transform.position + Vector3.up * 0.7f,
                    $"↩️ UNIT DIKEMBALIKAN!",
                    new Color(0.4f, 1.0f, 0.6f),
                    1.35f,
                    true
                );

                SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                    lastRecycledCell.transform.position,
                    new Color(0.4f, 1.0f, 0.6f),
                    Color.white,
                    16,
                    2.5f
                );
            }

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.UndoRestoreChime, 1.25f);
            }

            ClearUndoWindow();
        }

        private void ClearUndoWindow()
        {
            if (undoCountdownCoroutine != null)
            {
                StopCoroutine(undoCountdownCoroutine);
                undoCountdownCoroutine = null;
            }

            if (undoButtonObj != null)
            {
                undoButtonObj.SetActive(false);
            }

            lastRecycledHeroData = null;
            lastRecycledCell = null;
            lastRecycledRefundAmount = 0;
        }

        #endregion

        #region First-Time Recycle Tooltip

        private void CheckAndShowFirstTimeRecycleHint()
        {
            if (PlayerPrefs.GetInt(PREF_SEEN_RECYCLE_TUTORIAL, 0) == 1) return;
            PlayerPrefs.SetInt(PREF_SEEN_RECYCLE_TUTORIAL, 1);
            PlayerPrefs.Save();

            if (firstTimeTooltipObj == null)
            {
                var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
                var frame9Slice = Resources.Load<Sprite>("Sprites/UI/Frame_StoneBastion_9Slice")
#if UNITY_EDITOR
                              ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Frame_StoneBastion_9Slice.png")
#endif
                              ;

                firstTimeTooltipObj = new GameObject("RecycleTooltipBubble", typeof(RectTransform), typeof(Image));
                firstTimeTooltipObj.transform.SetParent(transform.parent, false);

                var rt = firstTimeTooltipObj.GetComponent<RectTransform>();
                rt.anchorMin = rectTransform.anchorMin;
                rt.anchorMax = rectTransform.anchorMax;
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = rectTransform.anchoredPosition + new Vector2(0f, 50f);
                rt.sizeDelta = new Vector2(170f, 65f);

                var img = firstTimeTooltipObj.GetComponent<Image>();
                img.sprite = frame9Slice;
                img.type = Image.Type.Sliced;
                img.color = new Color(0.12f, 0.15f, 0.22f, 0.98f); // Deep fortress obsidian

                var textObj = new GameObject("HintText", typeof(RectTransform), typeof(Text));
                textObj.transform.SetParent(firstTimeTooltipObj.transform, false);
                var textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(8f, 6f);
                textRt.offsetMax = new Vector2(-8f, -6f);

                var txt = textObj.GetComponent<Text>();
                txt.font = lilitaFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.fontSize = 11;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = new Color(1.0f, 0.88f, 0.45f, 1f);
                txt.text = "💡 LEPAS DI SINI UNTUK LEBUR HERO & REFUND 50% GEMS!";
                txt.raycastTarget = false;

                img.raycastTarget = false;
            }

            firstTimeTooltipObj.SetActive(true);
            StartCoroutine(DismissTooltipRoutine(firstTimeTooltipObj, 4.5f));
        }

        private IEnumerator DismissTooltipRoutine(GameObject tooltip, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (tooltip != null) tooltip.SetActive(false);
        }

        #endregion
    }
}
